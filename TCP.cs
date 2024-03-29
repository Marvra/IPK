using System;
using System.Linq.Expressions;
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
        private static string? SemaphoreResult;
        public static event EventHandler<string>? ServerErrorOccurred;
        private static event EventHandler? StdinNotAvailable;

        // public static void CtrlDPressed(){
        //     bool notPressed = true;
        //     while (notPressed)
        //         {
        //             ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);

        //             // Check if Ctrl+D was pressed
        //             if (keyInfo.Modifiers == ConsoleModifiers.Control && keyInfo.Key == ConsoleKey.D)
        //             {
        //                 // Raise the Ctrl+D event
        //                 StdinNotAvailable?.Invoke(null, null);
        //                 notPressed = false;
        //                 break;
        //             }
        //         }
        //  }



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
                    Console.Error.WriteLine($"ERR FROM {message[2]}: " + givenMess.Substring(givenMess.IndexOf("IS") + 3));
                    ServerErrorOccurred.Invoke(null, "ERR");
                    break;
                case "MSG" :
                    Console.WriteLine($"{message[2]}: " + givenMess.Substring(givenMess.IndexOf("IS") + 3));
                    break;
                default:
                    Console.Error.WriteLine($"ERR: " + givenMess); // invalid message err from server 
                    ServerErrorOccurred.Invoke(null, "ERR");
                    break;
            }
        }

        private static async Task Recieving(Send data)
        {
            var buffer = new byte[1_024];
            // bool connection = true;
            // Console.WriteLine("RECIEVING");
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

        private static async Task<States> StartState(string userMessage, Send data, ClientParsing MsgParsing){

            if (userMessage.StartsWith("/auth"))
            {
                try
                {
                    MsgParsing.AuthValidity(userMessage);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"ERR: {e.Message}");
                    return States.auth_state;
                }

                data.Authorization(MsgParsing.Username,MsgParsing.DisplayName,MsgParsing.Secret);
                await responseSemaphore.WaitAsync();
                responseSemaphore = new SemaphoreSlim(0);
                
                if (SemaphoreResult == "OK"){
                    return States.open_state;
                } else {
                    return States.auth_state;
                }
            }
            else
            {
                Console.Error.WriteLine("ERR: You have to authenticate first using \"/auth {username} {secret} {displayname}\"\n"); 
                return States.auth_state;
            }
        }

        private static async Task<States> AuthState(string userMessage, Send data, ClientParsing MsgParsing)
        {
            if (userMessage.StartsWith("/auth"))
            {

                try
                {
                    MsgParsing.AuthValidity(userMessage);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"ERR: {e.Message}");
                    return States.auth_state;
                }

                data.Authorization(MsgParsing.Username,MsgParsing.DisplayName,MsgParsing.Secret);
                await responseSemaphore.WaitAsync();
                responseSemaphore = new SemaphoreSlim(0);

                if (SemaphoreResult == "OK"){
                    return States.open_state;
                } else {
                    return States.auth_state;
                }
            }
            else if (userMessage.StartsWith("/exit"))
            {
                return States.end_state;
            }
            else
            {
                Console.WriteLine("ERR: Please authenticate or leave using /exit");
            }
            return States.auth_state;
        }
        private static async Task<States> OpenState(string userMessage, Send data, ClientParsing MsgParsing)
        {
            if(userMessage == "/exit"){
                return States.end_state;
            }
            else if (userMessage.StartsWith("/join"))
            {
                try
                {
                    MsgParsing.JoinValidity(userMessage);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"ERR: {e.Message}");
                    return States.open_state;
                }

                data.Join(MsgParsing.ChannelID, MsgParsing.DisplayName);

                await responseSemaphore.WaitAsync();
                responseSemaphore = new SemaphoreSlim(0);

                return States.open_state;
            }
            else if (userMessage.StartsWith("/rename"))
            {
                try
                {
                    MsgParsing.RenameValidity(userMessage);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"ERR: {e.Message}");
                    return States.open_state;
                }
            }
            else if (userMessage.StartsWith("/"))
            {
                Console.Error.WriteLine("ERR: Invalid command");
            }
            else 
            {
                try
                {
                    MsgParsing.MsgValidity(userMessage);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"ERR: {e.Message}");
                    return States.open_state;
                }

                data.Message(MsgParsing.MessageContent, MsgParsing.DisplayName);

                Console.WriteLine($"Message sent: {MsgParsing.MessageContent}");

            }
            return States.open_state;
        }
        

        private static async Task Sending(Send data)
        {
            ClientParsing MsgParsing = new ClientParsing( );
            string userMessage;
            var state = States.start_state;
            
            // Task.Run(() => CtrlDPressed());
            
            ServerErrorOccurred += (sender, e ) => {

                if(e == "BYE" || e == "ERR"){
                    // responseSemaphore.Release();
                    data.Bye();
                    Environment.Exit(0);
                }
            };

            StdinNotAvailable += (sender, e) =>
            {
                // Console.WriteLine("Ctrl+D pressed. Exiting...");
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

            // Console.WriteLine("SENDING");

            while (Connection)
            {
                // Console.WriteLine($"STATE : {state}");

                userMessage = Console.ReadLine();

                if (userMessage == null)
                {
                    StdinNotAvailable?.Invoke(null, EventArgs.Empty);
                }

                switch (state)
                {
                    case States.start_state:

                        state = await StartState(userMessage, data, MsgParsing);

                    break;

                    case States.auth_state:

                        state = await AuthState(userMessage, data, MsgParsing);

                    break;

                    case States.open_state:

                        state = await OpenState(userMessage, data, MsgParsing);
                    break;

                    case States.error_state:

                    break;

                    case States.end_state:

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