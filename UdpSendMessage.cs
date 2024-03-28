using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ipk_protocol
{
    class UdpSend
    {
        // public string message;
        // public Socket clientSocket;
        // private void SendMessage(string message)
        // {
        //     // Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //     // Console.WriteLine("su tu ");
        //     byte[] bytes = Encoding.ASCII.GetBytes(message);
        //     clientSocket.Send(bytes);
        //     // Console.WriteLine("posrane tu ");
        // }

        public byte[] Authorization(string login, string displayName, string key, UInt16 MessageID)
        {
            byte[] msg = new byte[6 + login.Length + displayName.Length + key.Length];
            msg[0] = (byte)UdpMsgType.AUTH;
            BitConverter.GetBytes(MessageID).CopyTo(msg, 1);
            Encoding.ASCII.GetBytes(login).CopyTo(msg, 3);
            msg[3 + login.Length] = 0;
            Encoding.ASCII.GetBytes(displayName).CopyTo(msg, 4 + login.Length);
            msg[4 + login.Length + displayName.Length] = 0;
            Encoding.ASCII.GetBytes(key).CopyTo(msg, 5 + login.Length + displayName.Length);
            msg[5 + login.Length + displayName.Length + key.Length] = 0;

            return msg;
        }

        

        public byte[] Join(string ChannelID, string displayName, UInt16 MessageID)
        {
            byte[] msg = new byte[5 + ChannelID.Length + displayName.Length];
            msg[0] = (byte)UdpMsgType.JOIN;
            BitConverter.GetBytes(MessageID).CopyTo(msg, 1);
            Encoding.ASCII.GetBytes(ChannelID).CopyTo(msg, 3);
            Encoding.ASCII.GetBytes(displayName).CopyTo(msg, 4 + ChannelID.Length);
            msg[4 + ChannelID.Length + displayName.Length] = 0;
            return msg;
        }
        public byte[] Msg(string displayName, string MessageContents, UInt16 MessageID)
        {
            byte[] msg = new byte[5 + displayName.Length + MessageContents.Length];
            msg[0] = (byte)UdpMsgType.MSG;
            BitConverter.GetBytes(MessageID).CopyTo(msg, 1);
            Encoding.ASCII.GetBytes(displayName).CopyTo(msg, 3);
            msg[3 + displayName.Length] = 0;
            Encoding.ASCII.GetBytes(MessageContents).CopyTo(msg, 4 + displayName.Length);
            msg[4 + displayName.Length + MessageContents.Length] = 0;

            return msg;
        }

        public byte[] Confirm(UInt16 RefMessageId)
        {
            byte[] msg = new byte[3];
            msg[0] = (byte)UdpMsgType.CONFIRM;
            BitConverter.GetBytes(RefMessageId).CopyTo(msg, 1);

            return msg;
        }
        public byte[] Bye(UInt16 RefMessageID)
        {
            byte[] msg = new byte[3];
            msg[0] = (byte)UdpMsgType.BYE;
            BitConverter.GetBytes(RefMessageID).CopyTo(msg, 1);
            return msg;
        }

        // constructor pre clientSocket maybe ?
    }
}