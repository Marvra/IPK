using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;

namespace ipk_protocol
{
    class ServerParsing 
    {
        /* 
            Method which gets specific part of message {MessageContent} and checks grammar of whole message 
            Same syntax after the MSG and ERR part is passed
        */
       public string MsgErrGrammar(string[] message, string GivenMessage)
       {
            if (message.Length < 5)
            {
                throw new Exception("Invalid message format from server");
            }

            Regex MsgRegex = new Regex(@"^[\x20-\x7E]+\r\n$");
            Regex DisplaynameRegex = new Regex( @"^[\x21-\x7E]+$");

            // Convert ToUpper beacause of case insensitivity
            if(message[1].ToUpper() != "FROM" || message[3].ToUpper() != "IS")
            {
                throw new Exception("Invalid message format from server");
            }

            // Get substring after "IS"
            string MessageCheck = GivenMessage.Substring(GivenMessage.IndexOf("IS") + 3);

            // Regex and length check
            if(!DisplaynameRegex.IsMatch(message[2]) || message[2].Length > 20 || !MsgRegex.IsMatch(MessageCheck) || MessageCheck.Length > 1400)
            {
                throw new Exception("Invalid message format from server");
            }

            return MessageCheck;
       }

        /* 
            Method which gets specific part of message {MessageContent} and checks grammar of whole message 
        */
       public string ReplyGrammar(string[] message, string GivenMessage)
       {
            if (message.Length < 4)
            {
                throw new Exception("Invalid message format from server");
            }

            Regex MsgRegex = new Regex(@"^[\x20-\x7E]+\r\n$");

            // Convert ToUpper beacause of case insensitivity
            if(message[1].ToUpper() != "OK" && message[1].ToUpper() != "NOK"  || message[2].ToUpper() != "IS")
            {
                throw new Exception("Invalid message format from server");
            }

            // Get substring after "IS"
            string MessageCheck = GivenMessage.Substring(GivenMessage.IndexOf("IS") + 3);

            // Regex and length check
            if(!MsgRegex.IsMatch(MessageCheck) || MessageCheck.Length > 1400)
            {
                throw new Exception("Invalid message format from server");
            }

            return MessageCheck;
       }
    }
}