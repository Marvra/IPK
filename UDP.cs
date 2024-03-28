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
            ERR = 0xFE,
            BYE = 0xFF,
        }
    class UDP
    {
        public bool Connection = true;
        EndPoint remoteEndPoint;

        // private static SemaphoreSlim responseSemaphore = new SemaphoreSlim(0);
        // private static string SemaphoreResult;
        public static event EventHandler<string> ServerErrorOccurred;
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
                        Console.Error.WriteLine($"Success: { msg.MessageContents}");
                    }
                    else if (msg.result == 0 )
                    {
                        Console.Error.WriteLine($"Failure: { msg.MessageContents}");
                    }

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
                    Console.WriteLine("CONFIRM GOT \n");
                break;
                case UdpMsgType.BYE:
                    Console.WriteLine("BYE GOT \n");
                    Connection = false;
                break;
                case UdpMsgType.ERR:
                    
                break;


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


        private static async Task<states> StartState(string userMessage, string[] splitUserMessage, ClientParsing MsgParsing, UdpSend data, UInt16 MessageID,  IPEndPoint ep){

            if (splitUserMessage[0] == "/auth")
            {
                MsgParsing.AuthValidity(userMessage, splitUserMessage);
                data.clientSocket.SendTo(data.Authorization(MsgParsing.Username, MsgParsing.DisplayName, MsgParsing.Secret, MessageID), ep);
                
                // await responseSemaphore.WaitAsync();
                // responseSemaphore = new SemaphoreSlim(0);

                // treba spravit cekani

                // if (SemaphoreResult == "OK")
                // {
                //     return states.open_state;
                // }
                // else
                // {
                //     return states.auth_state;
                // }
            }
            else
            {
                Console.WriteLine("You have to authenticate first using \"/auth {username} {secret} {displayname}\""); 
                return states.auth_state;
            }
            return states.start_state;
        }

        private static async Task<states> AuthState(string userMessage, string[] splitUserMessage, ClientParsing MsgParsing, UdpSend data, UInt16 MessageID, IPEndPoint ep)
        {
            if (splitUserMessage[0] == "/auth")
            {
                MsgParsing.AuthValidity(userMessage, splitUserMessage);
                data.Authorization(MsgParsing.Username,MsgParsing.DisplayName,MsgParsing.Secret, MessageID);
                // await responseSemaphore.WaitAsync();
                // responseSemaphore = new SemaphoreSlim(0);

                // if (SemaphoreResult == "OK"){
                //     return states.open_state;
                // } else {
                //     return states.auth_state;
                // }
            }
            else if (userMessage == "/exit")
            {
                return states.end_state;
            }
            else
            {
                Console.WriteLine("Please authenticate or leave using /exit");
            }
            return states.auth_state;
        }
        private async Task<states> OpenState(string userMessage, string[] splitUserMessage, ClientParsing MsgParsing, UdpSend data,  UInt16 MessageID)
        {
            if(userMessage == "/exit"){
                return states.end_state;
            }
            else if (splitUserMessage[0] == "/join")
            { 
                MsgParsing.JoinValidity(userMessage, splitUserMessage);
                data.clientSocket.SendTo(data.Join($"discord.{MsgParsing.ChannelID}",MsgParsing.DisplayName, MessageID), remoteEndPoint);

                // await responseSemaphore.WaitAsync();
                // responseSemaphore = new SemaphoreSlim(0);
            }
            else if (splitUserMessage[0] == "/rename")
            {
                MsgParsing.RenameValidity(userMessage,splitUserMessage);
            }
            else {
                MsgParsing.MsgValidity(userMessage);
                data.clientSocket.SendTo(data.Msg(MsgParsing.DisplayName, MsgParsing.MessageContent, MessageID), remoteEndPoint);

                Console.WriteLine($"Message sent: {MsgParsing.MessageContent}");
            }
            return states.open_state;
        }

        public async Task Sending(Socket s,ArgParser arguments)
        {
            Console.WriteLine("SENDING");
            UdpSend data = new UdpSend();
            data.clientSocket = s;

            ClientParsing MsgParsing = new ClientParsing( );
            string userMessage;
            string[] splitUserMessage;
            var state = states.start_state;

            UInt16 MessageID = 0;

            IPAddress ip = IPAddress.Parse(arguments.serverAdress[0].ToString());

            IPEndPoint ep = new IPEndPoint(	ip, arguments.serverPort);

           while (Connection)
            {
                Console.WriteLine($"STATE : {state}");

                userMessage = Console.ReadLine();
                splitUserMessage = userMessage.Split(" ");

                switch (state)
                {
                    case states.start_state:

                        state = await StartState(userMessage, splitUserMessage, MsgParsing, data, MessageID, ep);

                    break;

                    case states.auth_state:

                        state = await AuthState(userMessage, splitUserMessage, MsgParsing, data, MessageID, ep);

                    break;

                    case states.open_state:

                        state = await OpenState(userMessage, splitUserMessage, MsgParsing, data, MessageID);
                    break;

                    case states.error_state:

                    break;

                    case states.end_state:

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