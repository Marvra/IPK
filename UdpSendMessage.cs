using System.Net.Sockets;
using System.Text;

namespace ipk_protocol
{
    class UdpSend
    {
        public Socket clientSocket;

        // Constructor
        public UdpSend(Socket socket)
        {
            clientSocket = socket;
        }

        // Method for fillining the byte array with the user provided arguemnts AUTH
        public byte[] Authorization(string login, string displayName, string key, UInt16 MessageID)
        {
            // Array byte of whole message 
            byte[] msg = new byte[6 + login.Length + displayName.Length + key.Length];

            // First byte is the message type
            msg[0] = (byte)UdpMsgType.AUTH;

            // Next two bytes are the message ID
            msg[1] = (byte)((MessageID >> 8) & 0xFF);
            msg[2] = (byte)(MessageID & 0xFF);

            // Copy the login, display name and key to the message each terminated by 0
            Encoding.ASCII.GetBytes(login).CopyTo(msg, 3);
            msg[3 + login.Length] = 0;
            Encoding.ASCII.GetBytes(displayName).CopyTo(msg, 4 + login.Length);
            msg[4 + login.Length + displayName.Length] = 0;
            Encoding.ASCII.GetBytes(key).CopyTo(msg, 5 + login.Length + displayName.Length);
            msg[5 + login.Length + displayName.Length + key.Length] = 0;

            return msg;
        }

        
        // Method for fillining the byte array with the user provided arguemnts JOIN
        public byte[] Join(string ChannelID, string displayName, UInt16 MessageID)
        {
            // Array byte of whole message
            byte[] msg = new byte[5 + ChannelID.Length + displayName.Length];

            // First byte is the message type
            msg[0] = (byte)UdpMsgType.JOIN;
            msg[1] = (byte)((MessageID >> 8) & 0xFF);
            msg[2] = (byte)(MessageID & 0xFF);
            Encoding.ASCII.GetBytes(ChannelID).CopyTo(msg, 3);
            Encoding.ASCII.GetBytes(displayName).CopyTo(msg, 4 + ChannelID.Length);
            msg[4 + ChannelID.Length + displayName.Length] = 0;
            return msg;
        }

        // Method for fillining the byte array with the user provided arguemnts MSG
        public byte[] Msg(string displayName, string MessageContents, UInt16 MessageID)
        {
            // Array byte of whole message
            byte[] msg = new byte[5 + displayName.Length + MessageContents.Length];

            // First byte is the message type
            msg[0] = (byte)UdpMsgType.MSG;
            msg[1] = (byte)((MessageID >> 8) & 0xFF);
            msg[2] = (byte)(MessageID & 0xFF);
            Encoding.ASCII.GetBytes(displayName).CopyTo(msg, 3);
            msg[3 + displayName.Length] = 0;
            Encoding.ASCII.GetBytes(MessageContents).CopyTo(msg, 4 + displayName.Length);
            msg[4 + displayName.Length + MessageContents.Length] = 0;

            return msg;
        }

        // Method for fillining the byte array with the user provided arguemnts ERR
        public byte[] Err(string displayName, string MessageContents, UInt16 MessageID)
        {
            // Array byte of whole message
            byte[] msg = new byte[5 + displayName.Length + MessageContents.Length];

            // First byte is the message type
            msg[0] = (byte)UdpMsgType.ERR;
            msg[1] = (byte)((MessageID >> 8) & 0xFF);
            msg[2] = (byte)(MessageID & 0xFF);
            Encoding.ASCII.GetBytes(displayName).CopyTo(msg, 3);
            msg[3 + displayName.Length] = 0;
            Encoding.ASCII.GetBytes(MessageContents).CopyTo(msg, 4 + displayName.Length);
            msg[4 + displayName.Length + MessageContents.Length] = 0;

            return msg;
        }

        // Method for fillining the byte array with the user provided arguemnts CONFIRM
        public byte[] Confirm(UInt16 RefMessageId)
        {
            // Array byte of whole message
            byte[] msg = new byte[3];
            
            // First byte is the message type
            msg[0] = (byte)UdpMsgType.CONFIRM;

            // Next two bytes are the message ID
            msg[1] = (byte)((RefMessageId) & 0xFF);
            msg[2] = (byte)((RefMessageId >> 8) & 0xFF);

            return msg;
        }

        // Method for fillining the byte array with the user provided arguemnts BYE
        public byte[] Bye(UInt16 MessageID)
        {
            // Array byte of whole message
            byte[] msg = new byte[3];

            // First byte is the message type
            msg[0] = (byte)UdpMsgType.BYE;

            // Next two bytes are the message ID
            msg[1] = (byte)((MessageID >> 8) & 0xFF);
            msg[2] = (byte)(MessageID & 0xFF);
            
            return msg;
        }
    }
}