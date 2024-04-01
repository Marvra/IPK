using System.Net;
using System.Net.Sockets;

namespace ipk_protocol
{
    public enum UdpMsgType
    {
        CONFIRM = 0x00,
        REPLY = 0x01,
        AUTH = 0x02,
        JOIN = 0x03,
        MSG = 0x04,
        ERR = 0xfe,
        BYE = 0xff,
    }
    class UDP
    {
        public bool Connection = true;
        private static EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
        private static UInt16 ConfirmationTimeout;
        private static int RetryCount;
        private static int ConfirmGot;
        private static SemaphoreSlim ReplySemaphore = new SemaphoreSlim(0);
        private static int SemaphoreResult;
        public static event EventHandler<string> ServerErrorOccurred;
        private static event EventHandler? StdinNotAvailable;
        private HashSet<int> ProcessedMessageIds = new HashSet<int>();

        /*
        *   Method for checking if given id from server wasnt already used
        */
        public bool ProcessID(int messageId)
        {
            if (!ProcessedMessageIds.Contains(messageId))
            {
                ProcessedMessageIds.Add(messageId);

                return true;
            }
            return false;
        }

        /*
        * MainProcess for running Sending, Recieving 
        */
        public void MainProces(ArgParser arguments)
        {

            // Create a socket for sending the message
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            try
            {
                ConfirmationTimeout = arguments.ConfirmationTimeout;
                RetryCount = arguments.Retransmissions;

                // Start Sendind, Recieving asynchronously
                Task sendingTask = Task.Run(() => Sending(s, arguments));
                Task receivingTask = Task.Run(() => Recieving(s));

                // Wait for both tasks to end
                Task.WaitAll(sendingTask, receivingTask);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERR : " + ex.Message);
            }
            return;
        }

        //--------------------------------------------------------------------//
        //-----------------------------SENDING--------------------------------//
        //--------------------------------------------------------------------//

        /*
        * Method responsible for handling user input, based on a current state
        */
        public async Task Sending(Socket s, ArgParser arguments)
        {
            // Initialize objects
            UdpSend data = new UdpSend(s);
            ClientParsing MsgParsing = new ClientParsing();
            string? userMessage;

            var state = States.start_state;
            UInt16 MessageID = 0;
            IPAddress ip = IPAddress.Parse(arguments.serverAdress.ToString()); 
            remoteEndPoint = new IPEndPoint(ip, arguments.serverPort);

            // Event handler for handling CTRL+C
            Console.CancelKeyPress += async (sender, e) =>
            {
                byte[] UdpDatagram;
                ReplySemaphore.Release(); // Release semaphore
                if (e.SpecialKey == ConsoleSpecialKey.ControlC && state != States.start_state)
                {
                    try
                    {
                        UdpDatagram = data.Bye(MessageID); 
                        data.clientSocket.SendTo(UdpDatagram, remoteEndPoint);
                        await ConfirmCheck(UdpDatagram, data);
                        Connection = false;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("ERR : " + ex.Message); 
                    }
                }
                s.Shutdown(SocketShutdown.Both);
                s.Close();
                Environment.Exit(0);
            };

            // Event handler for handling server errors
            ServerErrorOccurred += async (sender, e) =>
            {
                byte[] UdpDatagram;
                if (e == "BYE" || e == "ERR")
                {
                    UdpDatagram = data.Bye(MessageID);
                    data.clientSocket.SendTo(UdpDatagram, remoteEndPoint);
                    await ConfirmCheck(UdpDatagram, data);
                    Connection = false; 
                }
                else if (e == "SERVERERR")
                {
                    UdpDatagram = data.Err(MsgParsing.DisplayName, "Server invalid message", MessageID);
                    data.clientSocket.SendTo(UdpDatagram, remoteEndPoint);
                    await ConfirmCheck(UdpDatagram, data);
                    
                    UdpDatagram = data.Bye(MessageID);
                    data.clientSocket.SendTo(UdpDatagram, remoteEndPoint);
                    await ConfirmCheck(UdpDatagram, data);
                    Connection = false; 
                }
                s.Shutdown(SocketShutdown.Both);
                s.Close();
                Environment.Exit(0);
            };

            // Event handler for handling stdin not available
            StdinNotAvailable += async (sender, e) =>
            {
                byte[] UdpDatagram;
                ReplySemaphore.Release();
                if(state != States.start_state)
                {
                    UdpDatagram = data.Bye(MessageID); 
                    data.clientSocket.SendTo(UdpDatagram, remoteEndPoint);
                    await ConfirmCheck(UdpDatagram, data);
                    Connection = false; 
                }
                s.Shutdown(SocketShutdown.Both);
                s.Close();
                Environment.Exit(0);
            };

            // Main loop for handling user input and state transitions
            while (Connection)
            {
                userMessage = Console.ReadLine();

                if (userMessage == null)
                {
                    StdinNotAvailable.Invoke(null, EventArgs.Empty); // Invoke event if stdin is not available
                }
                else

                // State machine for handling different states of the client
                switch (state)
                {
                    case States.start_state:
                        state = await StartState(userMessage, MsgParsing, data, MessageID); 
                        break;

                    case States.auth_state:
                        state = await AuthState(userMessage, MsgParsing, data, MessageID); 
                        break;

                    case States.open_state:
                        if (userMessage.StartsWith("/rename"))
                        {
                            MessageID--; // Decrement MessageID if user wants to rename (rename doesnt send anything to server)
                        }
                        state = await OpenState(userMessage, MsgParsing, data, MessageID); 
                        break;

                    case States.end_state:
                        data.Bye(MessageID); 
                        Connection = false;
                        break;
                }
                MessageID++; 
            }
        }

        private static async Task ConfirmCheck(byte[] datagram, UdpSend data)
        {
            int retry = 0; // Initialize retry counter
            while (retry < RetryCount) // Continue checking for confirmation until reaching the maximum retry count
            {
                await Task.Delay(ConfirmationTimeout); // Wait for the confirmation timeout duration
                if (ConfirmGot == BitConverter.ToUInt16(datagram, 1)) // Check if the confirmation is received
                {
                    break; // Confirmation is recieved breaking loop
                }
                else
                {
                    data.clientSocket.SendTo(datagram, remoteEndPoint); // Resend the datagram if confirmation is not received
                    retry++;
                }
            }
            if (retry == RetryCount)
            {
                Console.Error.WriteLine("ERR: No confirmation received");
                Environment.Exit(0);
            }
        }

        //--------------------------------------------------------------------//
        //--------------------------SENDING-STATES----------------------------//
        //--------------------------------------------------------------------//

        private async Task<States> StartState(string userMessage, ClientParsing MsgParsing, UdpSend data, UInt16 MessageID)
        {

            // Check if user wants to authenticate
            if (userMessage.StartsWith("/auth"))
            {
                return await AuthAsync(data, MsgParsing, userMessage, MessageID); // Authenticate user
            }
            else
            {
                // Print error message if user doesn't attempt authentication
                Console.Error.WriteLine("ERR: You have to authenticate first using \"/auth {username} {secret} {displayname}\""); 
                return States.auth_state;
            }
        }

        private async Task<States> OpenState(string userMessage, ClientParsing MsgParsing, UdpSend data, UInt16 MessageID)
        {
            // Check if user wants to exit
            if (userMessage == "/exit")
            {
                return States.end_state;
            }
            // Check if user wants to join a channel
            else if (userMessage.StartsWith("/join"))
            { 
                return await JoinChannelAsync(data, MsgParsing, userMessage, MessageID);
            }
            // Check if user wants to rename
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
            // Check if user entered an invalid command
            else if (userMessage.StartsWith("/"))
            {
                Console.Error.WriteLine("ERR: Invalid command");
                return States.open_state;
            }
            // Process the message to send
            else 
            {
                return await MessageAsync(data, MsgParsing, userMessage, MessageID);
            }

            return States.open_state;
        }

        private async Task<States> AuthState(string userMessage, ClientParsing MsgParsing, UdpSend data, UInt16 MessageID)
        {

            // Check if user wants to authenticate
            if (userMessage.StartsWith("/auth"))
            {
                return await AuthAsync(data, MsgParsing, userMessage, MessageID);
            }
            // Check if user wants to exit
            else if (userMessage == "/exit")
            {
                return States.end_state; 
            }
            else
            {
                Console.Error.WriteLine("ERR: Please authenticate or leave using /exit");
            }
            return States.auth_state; 
        }

        public async Task<States> JoinChannelAsync(UdpSend data, ClientParsing MsgParsing, string userMessage, UInt16 MessageID)
        {
            try
            {
                MsgParsing.JoinValidity(userMessage);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"ERR : {e.Message}");
                return States.open_state;
            }

            // Create UDP datagram for joining channel and send it
            byte[] UdpDatagram  = data.Join(MsgParsing.ChannelID, MsgParsing.DisplayName, MessageID);
            data.clientSocket.SendTo(UdpDatagram, remoteEndPoint);

            // Check for confirmation of join and await response
            await ConfirmCheck(UdpDatagram, data);

            // Wait for reply semaphore and reset it
            await ReplySemaphore.WaitAsync();
            ReplySemaphore = new SemaphoreSlim(0);

            return States.open_state;
        }

        public async Task<States> AuthAsync(UdpSend data, ClientParsing MsgParsing, string userMessage, UInt16 MessageID)
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
            // Create UDP datagram for authentication and send it
            byte[] UdpDatagram  = data.Authorization(MsgParsing.Username, MsgParsing.DisplayName, MsgParsing.Secret, MessageID);
            data.clientSocket.SendTo(UdpDatagram, remoteEndPoint);
            // Check for confirmation of authentication and await response
            await ConfirmCheck(UdpDatagram, data);
            // Wait for reply semaphore and reset it
            await ReplySemaphore.WaitAsync();
            ReplySemaphore = new SemaphoreSlim(0);
            // Check semaphore result to determine next state
            if (SemaphoreResult == 1)
            {
                return States.open_state; 
            }
            else
            {
                return States.auth_state; 
            }
        }

        public async Task<States> MessageAsync(UdpSend data, ClientParsing MsgParsing, string userMessage, UInt16 MessageID)
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
            // Create UDP datagram for message and send it
            byte[] UdpDatagram  = data.Msg(MsgParsing.DisplayName, MsgParsing.MessageContent, MessageID);
            data.clientSocket.SendTo(UdpDatagram, remoteEndPoint);
            // Check for confirmation of message delivery and await response
            await ConfirmCheck(UdpDatagram, data);
            return States.open_state;
        }

        //--------------------------------------------------------------------//
        //----------------------------RECIEVING-------------------------------//
        //--------------------------------------------------------------------//

        /*
        *   Method for asynchronously recieving messages from server
        *   processing them and sending confirms
        */
        public async void Recieving(Socket s)
        {
            byte[] receiveBuffer = new byte[1024];
            UdpRecieve MsgReceived = new UdpRecieve();
            UdpSend data = new UdpSend(s);

            // Set up remote endpoint to listen on any IP address and any port
            remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            // Bind socket to remoteEndpoint
            s.Bind(remoteEndPoint);

            while (Connection)
            {
                // Receive data into the receive buffer and get the number of bytes received
                int bytesReceived = s.ReceiveFrom(receiveBuffer, ref remoteEndPoint);

                // Process the received message and fill the MsgReceived object
                await FillMsgRecieved(MsgReceived, receiveBuffer, bytesReceived, s, data);

                // If the received message is not a confirmation message send cconfirmation back
                if (MsgReceived.MsgType != UdpMsgType.CONFIRM)
                {
                    s.SendTo(data.Confirm(MsgReceived.MessageID), remoteEndPoint);
                }
            }
        }
        
        /*
        *   Method which checks the message from the server and processes it
        *   Fills UdpRevoeve obejct with data and checks for grammar in messages sended by server
        */
        public async Task FillMsgRecieved(UdpRecieve msg, byte[] buffer, int bytes, Socket s, UdpSend data)
        {
            // Extract the message type from the first byte of the buffer
            msg.MsgType = (UdpMsgType)buffer[0];
            
            // Switch based on the message type
            switch (msg.MsgType)
            {
                case UdpMsgType.REPLY:
                    try
                    {
                        // Check for grammar 
                        msg.ReplyGrammar(buffer, bytes, s, data);
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine($"ERR : {e.Message}");
                        ServerErrorOccurred.Invoke(null, "SERVERERR");
                    }

                    // Check if the ID wasnt already used
                    if(ProcessID(msg.MessageID))
                    {
                        if (msg.result == 1)
                        {
                            Console.Error.WriteLine($"Success: {msg.MessageContents}");
                            // Semaphore success
                            SemaphoreResult = 1;
                        }
                        else
                        {
                            Console.Error.WriteLine($"Failure: {msg.MessageContents}");
                            // Semaphore failure
                            SemaphoreResult = 0;
                        }
                    }
                
                    ReplySemaphore.Release();

                break;
                case UdpMsgType.MSG:
                    try
                    {
                        // Check for grammar 
                        msg.MessageGrammar(buffer);
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine($"ERR : {e.Message}");
                        // Invoke server error event
                        ServerErrorOccurred.Invoke(null, "SERVERERR");
                    }

                    // Check if the ID wasnt already used
                    if(ProcessID(msg.MessageID))
                    {
                        Console.WriteLine($"{msg.DisplayName}: {msg.MessageContents}");
                    }
    
                break;
                case UdpMsgType.CONFIRM:
                    try
                    {
                        // Check for grammar 
                        msg.ConfirmGrammar(buffer); // Process confirm message
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine($"ERR : {e.Message}");
                        // Invoke server error event
                        ServerErrorOccurred.Invoke(null, "SERVERERR");
                    }
                    // Getting confirmation number for further checks
                    ConfirmGot = BitConverter.ToUInt16(buffer, 1);
                break;
                case UdpMsgType.BYE:
                    // Sending confirmation to give bye message
                    s.SendTo(data.Confirm(msg.MessageID), remoteEndPoint);
                    // Invoke bye event
                    ServerErrorOccurred.Invoke(null, "BYE");
                    // Setting connection to false
                    Connection = false;
                break;
                case UdpMsgType.ERR:
                    try
                    {
                        // Check for grammar 
                        msg.ErrorGrammar(buffer, s, data);
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine($"ERR: {e.Message}");
                        // Invoke server error event
                        ServerErrorOccurred.Invoke(null, "SERVERERR");
                    }

                    // Check if the ID wasnt already used
                    if(ProcessID(msg.MessageID))
                    {
                        Console.Error.WriteLine($"ERR FROM {msg.DisplayName}: {msg.MessageContents}");
                    }
                    // Send confirm to err message
                    s.SendTo(data.Confirm(msg.MessageID), remoteEndPoint);
                    // Invoke error event
                    ServerErrorOccurred.Invoke(null, "ERR");
                break;
                default:
                    try
                    {
                        // Trying to get Id from invalid message and sending confirm with given Id
                        await s.SendToAsync(data.Confirm(BitConverter.ToUInt16(buffer, 1)), remoteEndPoint);

                        Console.Error.WriteLine($"ERR: Unknown message type recieved from server");
                    }
                    catch(Exception)
                    {
                        Console.Error.WriteLine($"ERR: No message ID from given invalid message");
                    }
                    // Invoke server error event
                    ServerErrorOccurred.Invoke(null, "SERVERERR");
                break;
            }
        }
    }
}