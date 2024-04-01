using System.Net.Sockets;
using System.Text;

namespace ipk_protocol
{
    class TCP
    {
        public static bool Connection = true;
        private static States GlobalState;
        private static SemaphoreSlim ReplySemaphore = new SemaphoreSlim(0);
        private static string? SemaphoreResult;
        public static event EventHandler<string> ServerErrorOccurred;
        private static event EventHandler? StdinNotAvailable;

        /*
        * Main method for starting the connection, sending and recieving messages
        */
        public void MainProces(ArgParser arguments)
        {
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Send data = new Send(s); 

            // Try connect to server
            try 
            { 
                data.clientSocket.Connect(arguments.serverAdress, arguments.serverPort);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"ERR: {e.Message}");
                return;
            }

            // Start Sendind, Recieving asynchronously
            Task sendingTask = Task.Run(() => Sending(data));
            Task receivingTask = Task.Run(() => Recieving(data));

            // Wait for both tasks to end
            Task.WaitAll(sendingTask, receivingTask);
            return;
        }
        
        /*
        *   Method for determing the state of the client
        *   Handling events 
        */
        private static async Task Sending(Send data)
        {
            ClientParsing MsgParsing = new ClientParsing( );
            string userMessage;
            GlobalState = States.start_state;
            
            // Event when server sends Err, Bye or invalid message
            ServerErrorOccurred += (sender, e ) => {

                if(e == "BYE" || e == "ERR"){
                    data.Bye();
                } 
                else if(e == "SERVERERR"){
                    Connection = false;
                    data.Error("Server invalid message", MsgParsing.DisplayName);
                    data.Bye();
                }
                
                data.clientSocket.Shutdown(SocketShutdown.Both);
                data.clientSocket.Close();
                Environment.Exit(0);
            };

            // Event when stdin is not available
            StdinNotAvailable += (sender, e) =>
            {
                ReplySemaphore.Release();
                if(GlobalState != States.start_state)
                {
                    data.Bye();
                    Connection = false;
                }
                data.clientSocket.Shutdown(SocketShutdown.Both);
                data.clientSocket.Close();
                Environment.Exit(0);
            };
            
            // Event when ctrl + c is pressed
            Console.CancelKeyPress += (sender, e) => {

                ReplySemaphore.Release();
                if (e.SpecialKey == ConsoleSpecialKey.ControlC && GlobalState != States.start_state)
                {
                    data.Bye();
                    Connection = false;
                }
                data.clientSocket.Shutdown(SocketShutdown.Both);
                data.clientSocket.Close();
                Environment.Exit(0);
            };

            while (Connection)
            {
                userMessage = Console.ReadLine();

                // Stdin is not available invoking event
                if (userMessage == null)
                {
                    StdinNotAvailable?.Invoke(null, EventArgs.Empty);
                }
                else

                switch (GlobalState) // State handling and awaiting for setting global state
                {
                    case States.start_state:

                        GlobalState = await StartState(userMessage, data, MsgParsing);

                    break;

                    case States.auth_state:

                        GlobalState = await AuthState(userMessage, data, MsgParsing);

                    break;

                    case States.open_state:

                        GlobalState = await OpenState(userMessage, data, MsgParsing);
                    break;

                    case States.end_state:

                        data.Bye();
                        Connection = false;

                    break;
                }
            }
            Console.WriteLine("END OF SENDING");
        }

        /*
        *   Method for Start state 
        *   Handling users inputs based on the state
        */
        private static async Task<States> StartState(string userMessage, Send data, ClientParsing MsgParsing)
        {

            if (userMessage.StartsWith("/auth"))
            {
                // Awaiting for the result of the authentication
                return await Authenticate(userMessage, data, MsgParsing);
            }
            else // Invalid command/ message
            {
                Console.Error.WriteLine("ERR: You have to authenticate first using \"/auth {username} {secret} {displayname}\"\n"); 
                return States.auth_state;
            }
        }

        /*
        *   Method for Auth state 
        *   Handling users inputs based on the state
        */
        private static async Task<States> AuthState(string userMessage, Send data, ClientParsing MsgParsing)
        {
            if (userMessage.StartsWith("/auth"))
            {
                // Awaiting for the result of the authentication
                return await Authenticate(userMessage, data, MsgParsing);
            }
            else // Invalid command/ message
            {
                Console.WriteLine("ERR: You have to authenticate first using \"/auth {username} {secret} {displayname}\"\n");
                return States.auth_state;
            }
        }

        /*
        *   Method for Open state 
        *   Handling users inputs based on the state
        */
        private static async Task<States> OpenState(string userMessage, Send data, ClientParsing MsgParsing)
        {
            if (userMessage.StartsWith("/join"))
            {
                // Awaiting for the result of joining the channel
                return await Join(userMessage, data, MsgParsing);
            }
            else if (userMessage.StartsWith("/rename"))
            {
                try
                {
                    // Grammar check for the rename command
                    MsgParsing.RenameValidity(userMessage);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"ERR: {e.Message}");
                }
                return States.open_state;
            }
            else if (userMessage.StartsWith("/")) // Invalid command
            {
                Console.Error.WriteLine("ERR: Invalid command");
                return States.open_state;
            }
            else // Sending message
            {
                return Message(userMessage, data, MsgParsing);
            }
        }

        /*
        *   Method which resolves Authentification
        *   Checks the grammar of users input and sends the message
        *   Returns the state based on the reply result 
        */
        private static async Task<States> Authenticate(string userMessage, Send data, ClientParsing MsgParsing)
        {
            try
            {
                // Grammar check for the authentication command
                MsgParsing.AuthValidity(userMessage);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"ERR: {e.Message}");
            }

            // Sending the authentication message

            data.Authorization(MsgParsing.Username, MsgParsing.DisplayName, MsgParsing.Secret);
            await ReplySemaphore.WaitAsync();  // Waiting for the reply response from server

            // Reply was OK going to the open state
            if (SemaphoreResult == "OK")
            {
                return States.open_state;
            }

            return States.auth_state;
        }

        /*
        *   Method which resolves Join
        *   Checks the grammar of users input and sends the message
        */
        private static async Task<States> Join(string userMessage, Send data, ClientParsing MsgParsing)
        {
            try
            {
                // Grammar check for the join command
                MsgParsing.JoinValidity(userMessage);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"ERR: {e.Message}");
            }

            // Sending the join message
            data.Join(MsgParsing.ChannelID, MsgParsing.DisplayName);
            await ReplySemaphore.WaitAsync(); // Waiting for the reply response from server

            return States.open_state;
        }

        /*
        *   Method which resolves sending Messages
        *   Checks the grammar of users input and sends the message
        */
        private static States Message(string userMessage, Send data, ClientParsing MsgParsing)
        {
            try
            {
                // Grammar check for the authentication command
                MsgParsing.MsgValidity(userMessage);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"ERR: {e.Message}");
            }

            // Sending message
            data.Message(MsgParsing.MessageContent, MsgParsing.DisplayName);

            return States.open_state;
        }

        /*
        *   Method for recieving messages from the server
        */
        private static async Task Recieving(Send data)
        {
            // Buffer for recieving the message
            var buffer = new byte[1_024];
            while (Connection)
            {
                try
                {
                    // Get lenght of bytes recieved and decode them
                    var bytesLength = data.clientSocket.Receive(buffer, SocketFlags.None);
                    string givenMess = Encoding.UTF8.GetString(buffer, 0, bytesLength);
                    
                    // Splitting the message into arguments
                    string[] messageArguments = givenMess.Split(' ');

                    // Awaiting for function to process the message
                    await Task.Run(() => Response(messageArguments, givenMess));
                }
                catch (Exception)
                {
                    return;
                }
            }
        }

        /*
        *   Method which checks the message from the server and processes it
        */
        private static void Response(string[] message, string givenMess)
        {
            // Instance of the class for parsing the server messages
            ServerParsing recieve = new ServerParsing();
            string MessageContent = "";

            // ToUpper because of case insensitivity
            switch (message[0].ToUpper())
            {
                case "BYE":
                    if(message.Length != 1)
                    {
                        Console.Error.WriteLine("ERR: Server invalid message");
                        ServerErrorOccurred.Invoke(null, "SERVERERR");
                    }
                    ServerErrorOccurred.Invoke(null, "BYE");
                    break;
                case "REPLY":
                    try
                    {
                        MessageContent = recieve.ReplyGrammar(message, givenMess);
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine($"ERR: {e.Message}");
                        ServerErrorOccurred.Invoke(null, "SERVERERR");
                    }
                    
                    if (message[1] == "OK")
                    {
                        Console.Error.WriteLine($"Success: {MessageContent}");
                        SemaphoreResult = "OK";
                        ReplySemaphore.Release();
                    }
                    else
                    {
                        Console.Error.WriteLine($"Failure: {MessageContent}");
                        SemaphoreResult = "NOK";
                        ReplySemaphore.Release();
                    }
                    break;
                case "ERR":
                    try
                    {
                        MessageContent = recieve.MsgErrGrammar(message, givenMess);
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine($"ERR: {e.Message}");
                        ServerErrorOccurred.Invoke(null, "SERVERERR");
                    }

                    Console.Error.WriteLine($"ERR FROM {message[2]}: {MessageContent}");
                    ServerErrorOccurred.Invoke(null, "ERR");
                    break;
                case "MSG" :

                    if (GlobalState == States.auth_state)
                    {
                        Console.Error.WriteLine("ERR: Server invalid message");
                        ServerErrorOccurred.Invoke(null, "SERVERERR");
                    }
                    
                    try
                    {
                        MessageContent = recieve.MsgErrGrammar(message, givenMess);
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine($"ERR: {e.Message}");
                        ServerErrorOccurred.Invoke(null, "SERVERERR");
                    }
                    Console.WriteLine($"{message[2]}: {MessageContent}");
                    break;
                default:
                    Console.Error.WriteLine($"ERR: Server invalid message");
                    ServerErrorOccurred.Invoke(null, "SERVERERR");
                    break;
            }
        }
    }
}