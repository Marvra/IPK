using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace ipk_protocol
{
    class TCP
    {
         public static bool Connection = true;
        private static SemaphoreSlim responseSemaphore = new SemaphoreSlim(0);
        private static string SemaphoreResult;
        public static event EventHandler<string> ServerErrorOccurred;



        private static async Task Response(string[] message, string givenMess)
        {
            switch (message[0])
            {
                case "BYE":
                    ServerErrorOccurred.Invoke(null, "BYE");
                    break;
                case "REPLY":
                    if (message[1] == "OK")
                    {
                        Console.Error.WriteLine($"Success: {message[3]} {message[4]}");
                        SemaphoreResult = "OK";
                        responseSemaphore.Release();
                    }
                    else
                    {
                        Console.Error.WriteLine($"Failure: {message[3]} {message[4]}");
                        SemaphoreResult = "NOK";
                        responseSemaphore.Release();
                    }
                    break;
                case "ERR":
                    ServerErrorOccurred.Invoke(null, "ERR");
                    break;
                default :
                    Console.WriteLine($"{message[2]}: " + givenMess.Substring(givenMess.IndexOf("IS") + 3));
                    break;
            }
        }

        private static async Task Recieving(Send data)
        {
            var buffer = new byte[1_024];
            // bool connection = true;
            Console.WriteLine("RECIEVING");
            while (Connection)
            {
                try
                {
                    var bytesLength = data.clientSocket.Receive(buffer, SocketFlags.None);
                    string givenMess = Encoding.UTF8.GetString(buffer, 0, bytesLength); // 0 znamena index 
                    string[] messageArguments = givenMess.Split(' ');

                    Task.Run(() => Response(messageArguments, givenMess));
                }
                catch (Exception e)
                {
                    Console.WriteLine("CONNECTION STOPPED");
                    return;
                }
            }
        }

        private static async Task Sending(Send data)
        {
            // bool connection = true;
            string userMessage;
            string[] splitUserMessage;
            ClientParsing MsgParsing = new ClientParsing();
            states state = states.start_state;

            ServerErrorOccurred += (sender, e ) => {
                if(e == "BYE" || e == "ERR"){
                    // responseSemaphore.Release();
                    // data.Bye();
                    Console.WriteLine("BYE SENDED FROM SERVER");
                    Environment.Exit(0);
                }
            };
            Console.CancelKeyPress += (sender, e) => {
            responseSemaphore.Release();
            // Check if Ctrl+C was pressed
            if (e.SpecialKey == ConsoleSpecialKey.ControlC)
                {
                    Console.WriteLine("Ctrl+C pressed. Exiting...");
                    // Perform any cleanup or exit logic here
                    data.Bye();
                }
            };
            // // Send data = new Send();
            Console.WriteLine("SENDING");

            while (Connection)
            {
                Console.WriteLine($"STATE : {state}");
                switch (state)
                {
                    case states.start_state:
                        userMessage = Console.ReadLine();
                        splitUserMessage = userMessage.Split(" ");

                        if (splitUserMessage[0] == "/auth")
                        {
                            MsgParsing.AuthValidity(userMessage, splitUserMessage);
                            // data.Authorization("xvrabl06","epoh","a3bb2d2f-c085-4fcd-aa1c-edbdac32c575");
                            data.Authorization(MsgParsing.Username,MsgParsing.DisplayName,MsgParsing.Secret);

                            // Console.WriteLine("Waiting for Reply resolution");
                            await responseSemaphore.WaitAsync();
                            // Console.WriteLine("Reply resolved");
                            if (SemaphoreResult == "OK"){
                                state = states.open_state;
                            } else {
                                state = states.auth_state;
                            }

                            responseSemaphore = new SemaphoreSlim(0);
                            }
                        else
                        {
                            Console.WriteLine("You have to authenticate first");
                        }
                        break;
                    case states.auth_state:
                        userMessage = Console.ReadLine();
                        splitUserMessage = userMessage.Split(" ");
                        if (splitUserMessage[0] == "/auth")
                        {
                            MsgParsing.AuthValidity(userMessage, splitUserMessage);
                            data.Authorization(MsgParsing.Username,MsgParsing.DisplayName,MsgParsing.Secret);

                            // Console.WriteLine("Waiting for Reply resolution");
                            await responseSemaphore.WaitAsync();
                            // Console.WriteLine("Reply resolved");
                            if (SemaphoreResult == "OK"){
                                state = states.open_state;
                            } else {
                                state = states.auth_state;
                            }

                            responseSemaphore = new SemaphoreSlim(0);
                        }
                        else if (userMessage == "/exit")
                        {
                            state = states.end_state;
                        }
                        else if (splitUserMessage[0] == "/rename")
                        {
                            MsgParsing.RenameValidity(userMessage,splitUserMessage);
                        }
                        else
                        {
                            Console.WriteLine("Please authenticate or leave using /exit");
                        }
                        break;
                    case states.open_state:
                        userMessage = Console.ReadLine();
                        splitUserMessage = userMessage.Split(" ");
                        if(userMessage == "/exit"){
                            state = states.end_state;
                        }
                        else if (splitUserMessage[0] == "/join")
                        { 
                            MsgParsing.JoinValidity(userMessage, splitUserMessage);
                            data.Join($"discord.{MsgParsing.ChannelID}",MsgParsing.DisplayName);

                            // Console.WriteLine("Waiting for Reply resolution");
                            await responseSemaphore.WaitAsync();
                            // Console.WriteLine("Reply resolved");
                            // if (SemaphoreResult == "OK"){
                            //     state = states.open_state;
                            // } else {
                            //     state = states.auth_state;
                            // }

                            responseSemaphore = new SemaphoreSlim(0);
                        }
                        else if (splitUserMessage[0] == "/rename")
                        {
                            MsgParsing.RenameValidity(userMessage,splitUserMessage);
                        }
                        else {
                            MsgParsing.MsgValidity(userMessage);
                            Console.WriteLine(" asas" + MsgParsing.MessageContent);
                            data.Message(MsgParsing.MessageContent, MsgParsing.DisplayName);
                        }
                        break;
                    case states.error_state:
                        break;
                    case states.end_state:
                        data.Bye();
                        Connection = false;
                        break;
                }
            }
            Console.WriteLine("END OF SENDING");
        }

        public void MainProces(ArgParser arguments){
            Send data = new Send(); 
            data.clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try 
            { 
                data.clientSocket.Connect(arguments.serverAdress, arguments.serverPort);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Error: {e.Message}");
                return;
            }

            Task sendingTask = Task.Run(() => Sending(data));
            Task receivingTask = Task.Run(() => Recieving(data));

            Task.WaitAll(sendingTask, receivingTask);
            return;
        }
    }
}