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
        public  string? ChannelID; 
        public  string? DisplayName; 
        public  string? Username;
        public  string? Secret;
        public  string? MessageContent;
        private static string BasePattern = @"^[A-Za-z0-9\-]+$";
        private static string PrintableASCIIPattern = @"^[\x21-\x7E]+$";
        private static Regex BaseRegex = new Regex(BasePattern);
        private static Regex ASCIIRegex = new Regex(PrintableASCIIPattern);


        public void AuthValidity(string message){
            string[] messageSplit = message.Split(" ");

            if(messageSplit.Length != 4){
                throw new Exception($"Expected number of arguments 4, got: {messageSplit.Length}");
            }
            if(!BaseRegex.IsMatch(messageSplit[1]+messageSplit[2]) || !ASCIIRegex.IsMatch(messageSplit[3])){
                throw new Exception("Expected regex of arguments [Username == A-Za-z0-9\\-] [Secret == A-Za-z0-9\\-] [DisplayName == x21-x7E].");
            }
            if(messageSplit[1].Length > 20 || messageSplit[2].Length > 128 || messageSplit[3].Length > 20){
               throw new Exception($"Expected lenght of arguments [Username == 20] [Secret == 128] [DisplayName == 20], got : Username = {messageSplit[1].Length} Secret = {messageSplit[2].Length} DisplayName = {messageSplit[3].Length}.");
            }

            Username = messageSplit[1];
            Secret = messageSplit[2];
            DisplayName = messageSplit[3];
        }

        public void JoinValidity(string message){
            string[] messageSplit = message.Split(" ");

            if(messageSplit.Length != 2){
                throw new Exception($"Expected number of arguments 2, got: {messageSplit.Length}");
            }
            if(!BaseRegex.IsMatch(messageSplit[1])){
                throw new Exception("Expected regex of arguments [ChannelName == A-Za-z0-9\\-].");
            }
            if(messageSplit[1].Length > 20){
                throw new Exception($"Expected lenght of arguments [ChannelName == 20], got : Username = {messageSplit[1].Length}");
            }

            ChannelID = messageSplit[1];
        }

        public void MsgValidity(string message){
            string MsgPattern = @"^[\x20-\x7E]+$";
            Regex MsgRegex = new Regex(MsgPattern);
            
            if(message.Length > 1400){
                throw new Exception($"Expected lenght of arguments [MessageContent == 20], got : Username = {message.Length}");
            }
            if(!MsgRegex.IsMatch(message)){
                throw new Exception("Expected regex of arguments [MessageContent == x20-x7E].");
            }

            MessageContent = message;
        }

         public void RenameValidity(string message){
            string[] messageSplit = message.Split(" ");

            if(messageSplit.Length != 2){
                throw new Exception($"Expected number of arguments 2, got: {messageSplit.Length}");
            }
            if(!ASCIIRegex.IsMatch(messageSplit[1])){
                throw new Exception("Expected regex of arguments [MessageContent == x21-x7E].");
            }
            if(messageSplit[1].Length > 20){
                throw new Exception($"Expected lenght of arguments [DisplayName == 20], got : Username = {messageSplit[1].Length}");
            }

            DisplayName = messageSplit[1];
        }
    }
}