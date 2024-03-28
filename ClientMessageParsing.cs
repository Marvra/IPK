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
    class ClientParsing
    {   
        enum ErrUserInput
        {
            ErrRegex,
            ErrLenghtPart,
            ErrLenghtMsg,

        }
        public  string? ChannelID; 
        public  string? DisplayName; 
        public  string? Username;
        public  string? Secret;
        public  string? MessageContent;
        private static string BasePattern = @"^[A-Za-z0-9\-]+$";
        private static string PrintableASCIIPattern = @"^[\x21-\x7E]+$";
        private static Regex BaseRegex = new Regex(BasePattern);
        private static Regex ASCIIRegex = new Regex(PrintableASCIIPattern);


        public bool AuthValidity(string message, string[] messageSplit){
            if(messageSplit.Length != 4){
                Console.WriteLine("argument to big");
                return false;
            }
            if(!BaseRegex.IsMatch(messageSplit[1]+messageSplit[3]) || !ASCIIRegex.IsMatch(messageSplit[2])){
                Console.WriteLine("regex not matched");
                return false;
            }
            if(messageSplit[1].Length > 20 || messageSplit[2].Length > 128 || messageSplit[3].Length > 20){
                Console.WriteLine("lenght to big");
                return false;
            }

            Username = messageSplit[1];
            Secret = messageSplit[2];
            DisplayName = messageSplit[3];

            return true;
        }

        public bool JoinValidity(string message, string[] messageSplit){
            if(messageSplit.Length != 2){
                return false;
            }
            if(!BaseRegex.IsMatch(messageSplit[1])){
                Console.Error.WriteLine("invalid regex");
                return false;
            }
            if(messageSplit[1].Length > 20){
                return false;
            }

            ChannelID = messageSplit[1];

            return true;
        }

        public bool MsgValidity(string message){
            string MsgPattern = @"^[\x20-\x7E]+$";
            Regex MsgRegex = new Regex(MsgPattern);
            
            if(message.Length > 1400){
                return false;
            }
            if(!MsgRegex.IsMatch(message)){
                return false;
            }

            MessageContent = message;

            return false;
        }

         public bool RenameValidity(string message, string[] messageSplit){
            if(messageSplit.Length != 2){
                return false;
            }
            if(!ASCIIRegex.IsMatch(messageSplit[1])){
                return false;
            }
            if(messageSplit[1].Length > 20){
                return false;
            }

            DisplayName = messageSplit[1];

            return true;
        }
    }
}