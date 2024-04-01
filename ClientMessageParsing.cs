using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
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
        private static Regex BaseRegex = new Regex( @"^[A-Za-z0-9\-]+$");
        private static Regex ASCIIRegex = new Regex(@"^[\x21-\x7E]+$");

        /* 
            Method for setting the attributes of the class
            Checks grammar of arguments given by user
            Used for /auth command
        */
        public void AuthValidity(string message)
        {
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

        /* 
            Method for setting the attributes of the class
            Checks grammar of arguments given by user
            Used for /join command
        */
        public void JoinValidity(string message)
        {
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

        /* 
            Method for setting the attributes of the class
            Checks grammar of arguments given by user
            Used for messages sent by user
        */
        public void MsgValidity(string message)
        {
            Regex MsgRegex = new Regex( @"^[\x20-\x7E]+$");
            
            if(message.Length > 1400){
                throw new Exception($"Expected lenght of arguments [MessageContent == 20], got : Username = {message.Length}");
            }
            if(!MsgRegex.IsMatch(message)){
                throw new Exception("Expected regex of arguments [MessageContent == x20-x7E].");
            }

            MessageContent = message;
        }

        /* 
            Method for setting the attributes of the class
            Checks grammar of arguments given by user
            Used for /rename command
        */
        public void RenameValidity(string message)
        {
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