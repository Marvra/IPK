using System;
using System.Collections.Immutable;
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
    class UDP : UdpSend
    {
        public bool Connected = true;
        EndPoint remoteEndPoint;
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
                        Console.Error.WriteLine($"Success: { msg.MessageContents}");
                    }

                break;
                case UdpMsgType.MSG:
                    msg.MessageID = BitConverter.ToUInt16(buffer,1); // Message ID
                    int DisplayNameEnd = Array.IndexOf(buffer, (byte)0, 3); // Find end of DIssplayName (terminated by zero byte)
                    msg.DisplayName = Encoding.ASCII.GetString(buffer, 3, DisplayNameEnd - 3);
                    int MessageContentsEnd = Array.IndexOf(buffer, (byte)0, DisplayNameEnd+1); // Find end of MessageContent (terminated by zero byte)
                    msg.MessageContents = Encoding.ASCII.GetString(buffer, 3, MessageContentsEnd - (DisplayNameEnd-3));
                    Console.WriteLine($"{msg.DisplayName}: {msg.MessageContents} ");

                break;
                case UdpMsgType.CONFIRM:
                    Console.WriteLine("CONFIRM GOT \n");
                break;
            }

        }
        public async void Recieving(Socket s,ArgParser arguments)
        {
            Console.WriteLine("RECIEVING");
            byte[] receiveBuffer = new byte[1024];
            UdpRecieve MsgRecieved = new UdpRecieve();
            IPAddress ip = IPAddress.Parse("147.229.8.244");
            remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            s.Bind(remoteEndPoint);

            while(Connected)
            {
                Console.WriteLine("Waiting for a message");
                int bytesReceived = s.ReceiveFrom(receiveBuffer, ref remoteEndPoint);
                Console.WriteLine($"Received {bytesReceived} bytes from {remoteEndPoint}");

                FillMsgRecieved(MsgRecieved,receiveBuffer, bytesReceived);

                if ( MsgRecieved.MsgType != UdpMsgType.CONFIRM ){
                    Console.WriteLine($"Received message type: {MsgRecieved.MsgType}\n ID: {MsgRecieved.MessageID}\n Result: {MsgRecieved.result}\n Reference message ID: {MsgRecieved.RefMsgId}\n Message: {MsgRecieved.MessageContents}");
                    s.SendTo(Confirm(MsgRecieved.MessageID),remoteEndPoint);
                    Console.WriteLine($"CONFIRM SNED TO THIS ID :{MsgRecieved.MessageID}");
                }
            }

        }

        public void Sending(Socket s,ArgParser arguments)
        {
            Console.WriteLine("SENDING");
            UInt16 MessageID = 0;
            IPAddress ip = IPAddress.Parse("147.229.8.244");
            IPEndPoint ep = new IPEndPoint(	ip, arguments.serverPort);
            while(Connected)
            {
                string input = Console.ReadLine();
                if(input.StartsWith("/auth")){
                    string username = "xvrabl06";
                    string displayName = "PlsUdpWIn";
                    string secret = "a3bb2d2f-c085-4fcd-aa1c-edbdac32c575";
                    s.SendTo(Authorization(username,displayName, secret, MessageID), ep);
                    Console.WriteLine("SENDING AUTH");
                }
                else
                {
                    s.SendTo(Msg("PlsUdpWIn", input, MessageID), remoteEndPoint);
                }
                MessageID ++;
            }

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