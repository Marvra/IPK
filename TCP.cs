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
        private static event EventHandler StdinNotAvailable;

        public static void CtrlDPressed(){
            bool notPressed = true;
            while (notPressed)
                {
                    ConsoleKeyInfo keyInfo = Console.ReadKey(true);

                    // Check if Ctrl+D was pressed
                    if (keyInfo.Modifiers == ConsoleModifiers.Control && keyInfo.Key == ConsoleKey.D)
                    {
                        // Raise the Ctrl+D event
                        StdinNotAvailable?.Invoke(null, null);
                        notPressed = false;
                        break;
                    }
                }
         }



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
                        Console.Error.WriteLine($"Success: " + givenMess.Substring(givenMess.IndexOf("IS") + 3));
                        SemaphoreResult = "OK";
                        responseSemaphore.Release();
                    }
                    else
                    {
                        Console.Error.WriteLine($"Failure: " + givenMess.Substring(givenMess.IndexOf("IS") + 3));
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

        private static async Task<states> StartState(string userMessage, string[] splitUserMessage, Send data, ClientParsing MsgParsing){

            if (splitUserMessage[0] == "/auth")
            {
                MsgParsing.AuthValidity(userMessage, splitUserMessage);
                data.Authorization(MsgParsing.Username,MsgParsing.DisplayName,MsgParsing.Secret);
                await responseSemaphore.WaitAsync();
                responseSemaphore = new SemaphoreSlim(0);
                
                if (SemaphoreResult == "OK"){
                    return states.open_state;
                } else {
                    return states.auth_state;
                }
            }
            else
            {
                Console.Error.WriteLine("ERR: You have to authenticate first using \"/auth {username} {secret} {displayname}\"\n"); 
                return states.auth_state;
            }
        }

        private static async Task<states> AuthState(string userMessage, string[] splitUserMessage, Send data, ClientParsing MsgParsing)
        {
            if (splitUserMessage[0] == "/auth")
            {
                MsgParsing.AuthValidity(userMessage, splitUserMessage);
                data.Authorization(MsgParsing.Username,MsgParsing.DisplayName,MsgParsing.Secret);
                await responseSemaphore.WaitAsync();
                responseSemaphore = new SemaphoreSlim(0);

                if (SemaphoreResult == "OK"){
                    return states.open_state;
                } else {
                    return states.auth_state;
                }
            }
            else if (userMessage == "/exit")
            {
                return states.end_state;
            }
            else
            {
                Console.WriteLine("Please authenticate or leave using /exit");
            }
            return states.auth_state;
        }
        private static async Task<states> OpenState(string userMessage, string[] splitUserMessage, Send data, ClientParsing MsgParsing)
        {
            if(userMessage == "/exit"){
                return states.end_state;
            }
            else if (splitUserMessage[0] == "/join")
            { 
                MsgParsing.JoinValidity(userMessage, splitUserMessage);
                data.Join(MsgParsing.ChannelID,MsgParsing.DisplayName);

                await responseSemaphore.WaitAsync();
                responseSemaphore = new SemaphoreSlim(0);
            }
            else if (splitUserMessage[0] == "/rename")
            {
                MsgParsing.RenameValidity(userMessage,splitUserMessage);
            }
            else {
                MsgParsing.MsgValidity(userMessage);
                data.Message(MsgParsing.MessageContent, MsgParsing.DisplayName);

                Console.WriteLine($"Message sent: {MsgParsing.MessageContent}");
            }
            return states.open_state;
        }
        

        private static async Task Sending(Send data)
        {
            ClientParsing MsgParsing = new ClientParsing( );
            string userMessage;
            string[] splitUserMessage;
            var state = states.start_state;
            
            Task.Run(() => CtrlDPressed());
            
            ServerErrorOccurred += (sender, e ) => {

                if(e == "BYE" || e == "ERR"){
                    // responseSemaphore.Release();
                    // data.Bye();
                    Console.WriteLine("BYE SENDED FROM SERVER");
                    Environment.Exit(0);
                }
            };

            StdinNotAvailable += (sender, e) =>
            {
                Console.WriteLine("Ctrl+D pressed. Exiting...");
                responseSemaphore.Release();
                // Perform any cleanup or tasks
                // Exit the program
                data.Bye();
                Environment.Exit(0);
            };
            
            
            Console.CancelKeyPress += (sender, e) => {

                responseSemaphore.Release();
                if (e.SpecialKey == ConsoleSpecialKey.ControlC)
                    {
                        Console.WriteLine("Ctrl+C pressed. Exiting...");
                        data.Bye();
                    }
            };

            Console.WriteLine("SENDING");

            while (Connection)
            {
                Console.WriteLine($"STATE : {state}");

                userMessage = Console.ReadLine();

                if (userMessage == null)
                {
                    StdinNotAvailable?.Invoke(null, EventArgs.Empty);
                }

                splitUserMessage = userMessage.Split(" ");

                switch (state)
                {
                    case states.start_state:

                        state = await StartState(userMessage, splitUserMessage, data, MsgParsing);

                    break;

                    case states.auth_state:

                        state = await AuthState(userMessage, splitUserMessage, data, MsgParsing);

                    break;

                    case states.open_state:

                        state = await OpenState(userMessage, splitUserMessage, data, MsgParsing);
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