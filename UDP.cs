using System;
using System.Collections.Immutable;
using System.Data.Common;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Runtime.Versioning;
using System.Text;

namespace ipk_protocol
{
        public enum UdpMsgType
        {
            CONFIRM = 0x00,
            REPLY = 0x01,
            AUTH = 0x02,
            JOIN = 0x03,
            MSG = 0x04,
            ERR = 0xfe,
            BYE = 0xff,
        }
    class UDP
    {
        public bool Connection = true;
        EndPoint remoteEndPoint;

        private static UInt16 ConfirmationTimeout;
        private static int RetryCoutnt;

        private static int ConfirmGot;

        private static SemaphoreSlim responseSemaphore = new SemaphoreSlim(0);
        private static int SemaphoreResult;
        public static event EventHandler<string> ServerErrorOccurred;
        private static event EventHandler? StdinNotAvailable;
        public void FillMsgRecieved(UdpRecieve msg, byte[] buffer, int bytes)
        {
            msg.MsgType = (UdpMsgType)buffer[0];
            switch (msg.MsgType)
            {
                case UdpMsgType.REPLY:
                    msg.MessageID = BitConverter.ToUInt16(buffer,1); // Message ID
                    msg.result = buffer[3]; // Result
                    msg.RefMsgId = BitConverter.ToUInt16(buffer,4); // Ref Message ID
                    msg.MessageContents = Encoding.ASCII.GetString(buffer, 6, bytes-7);

                    if (msg.result == 1)
                    {
                        Console.Error.WriteLine($"Success: {msg.MessageContents}");
                        SemaphoreResult = 1;
                    }
                    else if (msg.result == 0 )
                    {
                        Console.Error.WriteLine($"Failure: {msg.MessageContents}");
                        SemaphoreResult = 0;
                    }
                    responseSemaphore.Release();

                break;
                case UdpMsgType.MSG:
                    msg.MessageID = BitConverter.ToUInt16(buffer, 1);// Message ID
                    int DisplayNameEnd = Array.IndexOf(buffer, (byte)0, 3); // Find end of DIssplayName (terminated by zero byte)
                    msg.DisplayName = Encoding.ASCII.GetString(buffer, 3, DisplayNameEnd - 3);
                    int MessageContentsEnd = Array.IndexOf(buffer, (byte)0, DisplayNameEnd+1); // Find end of MessageContent (terminated by zero byte)
                    msg.MessageContents = Encoding.ASCII.GetString(buffer, DisplayNameEnd+1 , MessageContentsEnd + DisplayNameEnd - 3);
                    Console.WriteLine($"{msg.DisplayName}: {msg.MessageContents}");

                break;
                case UdpMsgType.CONFIRM:
                    ConfirmGot = BitConverter.ToUInt16(buffer, 1);
                    Console.WriteLine($"CONFIRM GOT  : {ConfirmGot}\n");
                break;
                case UdpMsgType.BYE:
                    ServerErrorOccurred.Invoke(null, "BYE");
                    Console.WriteLine("BYE GOT \n");
                    Connection = false;
                break;
                case UdpMsgType.ERR:
                    ServerErrorOccurred.Invoke(null, "ERR");
                break;


            }

        }

        private static async Task ConfirmCheck(byte[] datagram, UdpSend data, EndPoint ep)
        {
            int retry = 0;
            while (retry < RetryCoutnt)
            {
                await Task.Delay(ConfirmationTimeout);
                Console.WriteLine("RESENDING  ______________________________________");
                if (ConfirmGot == BitConverter.ToUInt16(datagram, 1))
                {
                    break;
                }
                else
                {
                    data.clientSocket.SendTo(datagram, ep);
                    retry++;
                }
            }
            if (retry == RetryCoutnt)
            {
                Console.Error.WriteLine("ERR: No confirmation received");
                Environment.Exit(1);
            }
        }

        public async void Recieving(Socket s,ArgParser arguments)
        {
            Console.WriteLine("RECIEVING");
            byte[] receiveBuffer = new byte[1024];
            UdpRecieve MsgRecieved = new UdpRecieve();
            UdpSend data = new UdpSend();
            IPAddress ip = IPAddress.Parse("147.229.8.244");
            remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            s.Bind(remoteEndPoint);

            while(Connection)
            {
                Console.WriteLine("Waiting for a message");
                int bytesReceived = s.ReceiveFrom(receiveBuffer, ref remoteEndPoint);
                Console.WriteLine($"Received {bytesReceived} bytes from {remoteEndPoint}");

                FillMsgRecieved(MsgRecieved,receiveBuffer, bytesReceived);

                if ( MsgRecieved.MsgType != UdpMsgType.CONFIRM ){
                   // Console.WriteLine($"Received message type: {MsgRecieved.MsgType}\n ID: {MsgRecieved.MessageID}\n Result: {MsgRecieved.result}\n Reference message ID: {MsgRecieved.RefMsgId}\n Message: {MsgRecieved.MessageContents}");
                    s.SendTo(data.Confirm(MsgRecieved.MessageID),remoteEndPoint);
                    Console.WriteLine($"CONFIRM SNED TO THIS ID :{MsgRecieved.MessageID}");
                }
            }

        }


        private static async Task<States> StartState(string userMessage, ClientParsing MsgParsing, UdpSend data, UInt16 MessageID,  IPEndPoint ep){
            byte[] UdpDatagram;

            if (userMessage.StartsWith("/auth"))
            {
                try
                {
                    MsgParsing.AuthValidity(userMessage);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"ERR: {e.Message}");
                    return States.auth_state;
                }
                UdpDatagram = data.Authorization(MsgParsing.Username, MsgParsing.DisplayName, MsgParsing.Secret, MessageID);
                data.clientSocket.SendTo(UdpDatagram, ep);

                await ConfirmCheck(UdpDatagram, data, ep);
                
                await responseSemaphore.WaitAsync();
                responseSemaphore = new SemaphoreSlim(0);

                if (SemaphoreResult == 1)
                {
                    return States.open_state;
                }
                else
                {
                    return States.auth_state;
                }
            }
            else
            {
                Console.Error.WriteLine("ERR: You have to authenticate first using \"/auth {username} {secret} {displayname}\""); 
                return States.auth_state;
            }
        }

        private static async Task<States> AuthState(string userMessage, ClientParsing MsgParsing, UdpSend data, UInt16 MessageID, IPEndPoint ep)
        {
            byte[] UdpDatagram;

            if (userMessage.StartsWith("/auth"))
            {
                try
                {
                    MsgParsing.AuthValidity(userMessage);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"ERR: {e.Message}");
                    return States.auth_state;
                }

                UdpDatagram = data.Authorization(MsgParsing.Username, MsgParsing.DisplayName, MsgParsing.Secret, MessageID);
                data.clientSocket.SendTo(UdpDatagram, ep);

                await ConfirmCheck(UdpDatagram, data, ep);

                await responseSemaphore.WaitAsync();
                responseSemaphore = new SemaphoreSlim(0);

                if (SemaphoreResult == 1){
                    return States.open_state;
                } else {
                    return States.auth_state;
                }
            }
            else if (userMessage == "/exit")
            {
                return States.end_state;
            }
            else
            {
                Console.Error.WriteLine("ERR: Please authenticate or leave using /exit");
            }
            return States.auth_state;
        }
        private async Task<States> OpenState(string userMessage, ClientParsing MsgParsing, UdpSend data, UInt16 MessageID)
        {
            byte[] UdpDatagram;

            if(userMessage == "/exit"){
                return States.end_state;
            }
            else if (userMessage.StartsWith("/join"))
            { 
                try
                {
                    MsgParsing.JoinValidity(userMessage);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"ERR: {e.Message}");
                    return States.open_state;
                }

                UdpDatagram = data.Join(MsgParsing.ChannelID,MsgParsing.DisplayName, MessageID);
                data.clientSocket.SendTo(UdpDatagram, remoteEndPoint);

                await ConfirmCheck(UdpDatagram, data, remoteEndPoint);

                await responseSemaphore.WaitAsync();
                responseSemaphore = new SemaphoreSlim(0);

                return States.open_state;
            }
            else if (userMessage.StartsWith("/rename"))
            {
                try
                {
                    MsgParsing.RenameValidity(userMessage);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"ERR: {e.Message}");
                    return States.open_state;
                }
            }
            else if (userMessage.StartsWith("/"))
            {
                Console.Error.WriteLine("ERR: Invalid command");
                return States.open_state;
            }
            else 
            {
                try
                {
                    MsgParsing.MsgValidity(userMessage);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"ERR: {e.Message}");
                    return States.open_state;
                }

                UdpDatagram = data.Msg(MsgParsing.DisplayName, MsgParsing.MessageContent, MessageID);
                data.clientSocket.SendTo(UdpDatagram, remoteEndPoint);

                await ConfirmCheck(UdpDatagram, data, remoteEndPoint);

                Console.WriteLine($"Message sent: {MsgParsing.MessageContent}");
            }
            return States.open_state;
        }

        public async Task Sending(Socket s,ArgParser arguments)
        {
            Console.WriteLine("SENDING");
            UdpSend data = new UdpSend();
            data.clientSocket = s;

            ClientParsing MsgParsing = new ClientParsing( );
            string userMessage;
            var state = States.start_state;

            UInt16 MessageID = 0;

            IPAddress ip = IPAddress.Parse(arguments.serverAdress[0].ToString());

            IPEndPoint ep = new IPEndPoint(	ip, arguments.serverPort);

            Console.CancelKeyPress += (sender, e) => {

                responseSemaphore.Release();
                if (e.SpecialKey == ConsoleSpecialKey.ControlC)
                    {
                        Console.WriteLine("Ctrl+C pressed. Exiting...");
                        data.clientSocket.SendTo(data.Bye(MessageID), ep); //////////  POZOR NA ENDPOINTS TREBA NEJAKO PORIESIT
                        Environment.Exit(0);
                    }
            };

            ServerErrorOccurred += (sender, e ) => {

                if(e == "BYE" || e == "ERR"){
                    // responseSemaphore.Release();
                    data.clientSocket.SendTo(data.Bye(MessageID), remoteEndPoint);
                    Environment.Exit(0);
                }
            };

            StdinNotAvailable += (sender, e) =>
            {
                // Console.WriteLine("Ctrl+D pressed. Exiting...");
                responseSemaphore.Release();
                // Perform any cleanup or tasks
                // Exit the program
                data.clientSocket.SendTo(data.Bye(MessageID), remoteEndPoint);
                Environment.Exit(0);
            };

           while (Connection)
            {
                Console.WriteLine($"STATE : {state}");

                userMessage = Console.ReadLine();

                if (userMessage == null)
                {
                    StdinNotAvailable?.Invoke(null, EventArgs.Empty);
                }

                switch (state)
                {
                    case States.start_state:

                        state = await StartState(userMessage, MsgParsing, data, MessageID, ep);

                    break;

                    case States.auth_state:

                        state = await AuthState(userMessage, MsgParsing, data, MessageID, ep);

                    break;

                    case States.open_state:
                        if (userMessage.StartsWith("/rename"))
                        {
                            MessageID--;
                        }
                        state = await OpenState(userMessage, MsgParsing, data, MessageID);
                    break;

                    case States.error_state:

                    break;

                    case States.end_state:

                        data.Bye(MessageID);
                        Connection = false;

                    break;
                }
                MessageID++;
            }
            Console.WriteLine("END OF SENDING");

        }
        public void MainProces(ArgParser arguments)
        {

            // Create a socket for sending the message
            using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                try
                {
                    ConfirmationTimeout = arguments.ConfirmationTimeout;
                    RetryCoutnt = arguments.Retransmissions;

                    Console.WriteLine("TRYING");
                    Task sendingTask = Task.Run(() => Sending(s, arguments));
                    Task receivingTask = Task.Run(() => Recieving(s, arguments));

                    Task.WaitAll(sendingTask, receivingTask);
                    
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred: " + ex.Message);
                }
            }
        }
    }
}