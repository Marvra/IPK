using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ipk_protocol
{
    class UdpSend
    {
        public Socket clientSocket;

        public byte[] Authorization(string login, string displayName, string key, UInt16 MessageID)
        {
            byte[] msg = new byte[6 + login.Length + displayName.Length + key.Length];
            msg[0] = (byte)UdpMsgType.AUTH;
            msg[1] = (byte)((MessageID >> 8) & 0xFF); // little endian problem skipped  most significant byte
            msg[2] = (byte)(MessageID & 0xFF); // least significant byte
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
            msg[1] = (byte)((MessageID >> 8) & 0xFF); // little endian problem skipped  most significant byte
            msg[2] = (byte)(MessageID & 0xFF); // least significant byte
            Encoding.ASCII.GetBytes(ChannelID).CopyTo(msg, 3);
            Encoding.ASCII.GetBytes(displayName).CopyTo(msg, 4 + ChannelID.Length);
            msg[4 + ChannelID.Length + displayName.Length] = 0;
            return msg;
        }
        public byte[] Msg(string displayName, string MessageContents, UInt16 MessageID)
        {
            byte[] msg = new byte[5 + displayName.Length + MessageContents.Length];
            msg[0] = (byte)UdpMsgType.MSG;
            msg[1] = (byte)((MessageID >> 8) & 0xFF); // little endian problem skipped  most significant byte
            msg[2] = (byte)(MessageID & 0xFF); // least significant byte
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
        public byte[] Bye(UInt16 MessageID)
        {
            byte[] msg = new byte[3];
            msg[0] = (byte)UdpMsgType.BYE;
            msg[1] = (byte)((MessageID >> 8) & 0xFF); // little endian problem skipped  most significant byte
            msg[2] = (byte)(MessageID & 0xFF); // least significant byte
            
            return msg;
        }

        // constructor pre clientSocket maybe ?
    }
}