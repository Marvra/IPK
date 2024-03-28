using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ipk_protocol
{
    class UdpRecieve
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

        public UdpMsgType MsgType;
        public UInt16 MessageID;
        public byte result;
        public UInt16 RefMsgId;
        public string? MessageContents;
        public string? DisplayName;

        // public byte[] Reply(int BytesRecieved, byte[] BytesBuffer)
        // {
        //     string[] msg = new string[bytesRecieved];
        //     msg[0] = Encoding.ASCII.GetString(BytesBuffer, 0, 1);
        //     msg[1] = Encoding.ASCII.GetString(BytesBuffer, 1, 2);
        //     return msg;
        // }

        

        // public byte[] Err(int BytesRecieved)
        // {
        //     byte[] msg = new byte[5 + ChannelID.Length + displayName.Length];
        //     msg[0] = (byte)UdpMsgType.JOIN;
        //     BitConverter.GetBytes(MessageID).CopyTo(msg, 1);
        //     Encoding.ASCII.GetBytes(ChannelID).CopyTo(msg, 3);
        //     Encoding.ASCII.GetBytes(displayName).CopyTo(msg, 4 + ChannelID.Length);
        //     msg[4 + ChannelID.Length + displayName.Length] = 0;
        //     return msg;
        // }
        // public byte[] Msg(int BytesRecieved)
        // {
        //     byte[] msg = new byte[5 + displayName.Length + MessageContents.Length];
        //     msg[0] = (byte)UdpMsgType.MSG;
        //     BitConverter.GetBytes(MessageID).CopyTo(msg, 1);
        //     Encoding.ASCII.GetBytes(displayName).CopyTo(msg, 3);
        //     msg[3 + displayName.Length] = 0;
        //     Encoding.ASCII.GetBytes(MessageContents).CopyTo(msg, 4 + displayName.Length);
        //     msg[4 + displayName.Length + MessageContents.Length] = 0;

        //     return msg;
        // }
        // public byte[] Bye(int BytesRecieved)
        // {
        //     byte[] msg = new byte[3];
        //     msg[0] = (byte)UdpMsgType.BYE;
        //     BitConverter.GetBytes(MessageID).CopyTo(msg, 1);
        //     return msg;
        // }

        // public byte[] Confirm(int BytesRecieved)
        // {
        //     byte[] msg = new byte[3];
        //     msg[0] = (byte)UdpMsgType.BYE;
        //     BitConverter.GetBytes(MessageID).CopyTo(msg, 1);
        //     return msg;
        // }

        // constructor pre clientSocket maybe ?
    }
}