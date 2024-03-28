using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace ipk_protocol
{
    class ServerParsing
    {   
        private static string BasePattern = @"^[A-Za-z0-9\-]+$";
        private static string PrintableASCIIPattern = @"^[\x21-\x7E]+$";
        private static Regex BaseRegex = new Regex(BasePattern);
        private static Regex ASCIIRegex = new Regex(PrintableASCIIPattern);



        public bool MsgValidity(string message){
            string MsgPattern = @"^[\x20-\x7E]+$";
            Regex MsgRegex = new Regex(MsgPattern);
            
            if(message.Length > 1400){
                throw new Exception($"Expected lenght of arguments [MessageContent == 20], got : Username = {message.Length}");
            }
            if(!MsgRegex.IsMatch(message)){
                throw new Exception("Expected regex of arguments [MessageContent == x20-x7E].");
            }

            return false;
        }
    }
}