using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace ipk_protocol
{
    class UdpRecieve 
    {
        public UdpMsgType MsgType;
        public UInt16 MessageID;
        public byte result;
        public UInt16 RefMsgId;
        public string? MessageContents;
        public string? DisplayName;
        private Regex MessageRegex = new Regex(@"^[\x20-\x7E]+$");

        /* 
            Method for parsing the buffer into the reply format
            Checks grammar of incoming message from server
            Sets attributes of the class
        */
        public void ReplyGrammar(byte[] buffer, int bytes, Socket s, UdpSend data)
        {
            if (bytes < 7)
            {
                throw new Exception($"Expected lenght of buffer >7, got : {buffer.Length}");
            }

            // Get MessageID
            MessageID = BitConverter.ToUInt16(buffer, 1);

            // Get Result
            result = buffer[3];
            if (result != 0 && result != 1)
            {
                throw new Exception($"Expected result value is 0 or 1, got : {result}");
            }

            // Get Ref Message ID
            RefMsgId = BitConverter.ToUInt16(buffer, 4);

            // Get and validate MessageContent
            MessageContents = Encoding.ASCII.GetString(buffer, 6, bytes - 7);
            if (MessageContents.Length > 1400)
            {
                throw new Exception("Invalid message format: MessageContent exceeds maximum length (1400 characters)");
            }
            if (!MessageRegex.IsMatch(MessageContents))
            {
                throw new Exception("Invalid message format: MessageContent contains invalid characters");
            }
        }

        /* 
            Method for parsing the buffer into the message format
            Checks grammar of incoming message from server
            Sets attributes of the class
        */
        public void MessageGrammar(byte[] buffer)
        {
            if (buffer.Length < 7)
            {
                throw new Exception($"Expected lenght of buffer >7, got : {buffer.Length}");
            }

            // Get MessageID
            MessageID = BitConverter.ToUInt16(buffer, 1);

            // Find end of DisplayName (terminated by zero byte)
            int DisplayNameEnd = Array.IndexOf(buffer, (byte)0, 3);
            // Check if DisplayName end index is within bounds
            if (DisplayNameEnd < 0 || DisplayNameEnd >= buffer.Length - 1)
            {
                throw new Exception($"Expected terminator after DisplayName 0.");
            }

            // Get and validate DisplayName
            DisplayName = Encoding.ASCII.GetString(buffer, 3, DisplayNameEnd - 3);
            if (DisplayName.Length > 20)
            {
                throw new Exception($"Expected lenght of arguments [DisplayName == 20], got : DisplayName = {DisplayName.Length}.");
            }
            Regex DisplayNameRegex = new Regex(@"^[\x21-\x7E]+$");
            if (!DisplayNameRegex.IsMatch(DisplayName))
            {
                throw new Exception("Invalid message format: DisplayName contains invalid characters");
            }

            // Find end of MessageContent (terminated by zero byte)
            int MessageContentsEnd = Array.IndexOf(buffer, (byte)0, DisplayNameEnd + 1);
            // Check if DisplayName end index is within bounds
            if (MessageContentsEnd < 0 || MessageContentsEnd >= buffer.Length - 1)
            {
                throw new Exception($"Expected terminator after MessageContent 0.");
            }

            // Get and validate MessageContent
            MessageContents = Encoding.ASCII.GetString(buffer, DisplayNameEnd + 1, MessageContentsEnd - DisplayNameEnd - 1);
            if (MessageContents.Length > 1400)
            {
                throw new Exception("Invalid message format: MessageContent exceeds maximum length (1400 characters)");
            }
            if (!MessageRegex.IsMatch(MessageContents))
            {
                throw new Exception("Invalid message format: MessageContent contains invalid characters");
            }
        }

        /* 
            Method for parsing the buffer into the confirm format
            Sets attributes of the class
        */
        public void ConfirmGrammar(byte[] buffer)
        {
            if (buffer.Length < 3)
            {
                throw new Exception("Invalid message format: Insufficient length");
            }

            MessageID = BitConverter.ToUInt16(buffer, 1);
        }

        /* 
            Method for parsing the buffer into the error format
            Checks grammar of incoming message from server
            Sets attributes of the class
        */
        public void ErrorGrammar(byte[] buffer, Socket s, UdpSend data)
        {
            if (buffer.Length < 7)
            {
                throw new Exception("Invalid message format: Insufficient length");
            }

            // Get MessageID
            MessageID = BitConverter.ToUInt16(buffer, 1);

            // Find end of DisplayName (terminated by zero byte)
            int DisplayNameEnd = Array.IndexOf(buffer, (byte)0, 3);
            // Check if DisplayName end index is within bounds
            if (DisplayNameEnd < 0 || DisplayNameEnd >= buffer.Length - 1)
            {
                throw new Exception("Invalid message format: DisplayName not terminated properly");
            }

            // Get and validate DisplayName
            DisplayName = Encoding.ASCII.GetString(buffer, 3, DisplayNameEnd - 3);

            if (DisplayName.Length > 20)
            {
                throw new Exception("Invalid message format: DisplayName exceeds maximum length (20 characters)");
            }
            Regex DisplayNameRegex = new Regex(@"^[\x21-\x7E]+$");
            if (!DisplayNameRegex.IsMatch(DisplayName))
            {
                throw new Exception("Invalid message format: DisplayName contains invalid characters");
            }

            // Find end of MessageContent (terminated by zero byte)
            int MessageContentsEnd = Array.IndexOf(buffer, (byte)0, DisplayNameEnd + 1);
            // Check if MessageContent end index is within bounds
            if (MessageContentsEnd < 0 || MessageContentsEnd >= buffer.Length - 1)
            {
                throw new Exception($"Expected terminator after MessageContent 0.");
            }

            // Get and validate MessageContent
            MessageContents = Encoding.ASCII.GetString(buffer, DisplayNameEnd + 1, MessageContentsEnd - DisplayNameEnd - 1);

            if (MessageContents.Length > 1400)
            {
                throw new Exception("Invalid message format: MessageContent exceeds maximum length (1400 characters)");
            }
            if (!MessageRegex.IsMatch(MessageContents))
            {
                throw new Exception("Invalid message format: MessageContent contains invalid characters");
            }
        }
    }
}