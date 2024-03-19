// See https://aka.ms/new-console-template for more information
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace ipk_protocol 
{   
    public enum states 
    {
        start_state,
        auth_state,
        open_state,
        error_state,
        end_state

    }
    public class Client
    {
        public static bool Connection = true;
        private static SemaphoreSlim responseSemaphore = new SemaphoreSlim(0);
        private static string SemaphoreResult;

        private static async Task Response(Send data, string[] message, string givenMess)
        {
            switch (message[0])
            {
                case "BYE":
                    break;
                case "REPLY":
                    if (message[1] == "OK")
                    {
                        Console.Error.WriteLine($"Success: {message[3]} {message[4]}");
                        // Release the semaphore when valid response received
                        // responseSemaphore.Release();
                        SemaphoreResult = "OK";
                        responseSemaphore.Release();
                    }
                    else
                    {
                        Console.Error.WriteLine($"Failure: {message[3]} {message[4]}");
                        // Release the semaphore when valid response received
                        // responseSemaphore.Release();
                        SemaphoreResult = "NOK";
                        responseSemaphore.Release();
                    }
                    break;
                case "ERR":
                    break;
                default :
                    Console.WriteLine(givenMess);
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

                    Task.Run(() => Response(data, messageArguments, givenMess));
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
            states state = states.start_state;
            // // Send data = new Send();
            Console.WriteLine("SENDING");

            while (Connection)
            {
                Console.WriteLine($"STATE : {state}");
                switch (state)
                {
                    case states.start_state:
                        userMessage = Console.ReadLine();
                        if (userMessage == "/auth")
                        {
                            data.Authorization("xvrabl06", "epoh", "b696b89a-6959-4394-9918-28e7dbbcb804");

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
                        if (userMessage == "/auth")
                        {
                            data.Authorization("xvrabl06", "epoh", "b696b89a-6959-4394-9918-28e7dbbcb804");

                            Console.WriteLine("Waiting for Reply resolution");
                            await responseSemaphore.WaitAsync();
                            Console.WriteLine("Reply resolved");
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
                        else
                        {
                            Console.WriteLine("nieco ine ty kokto");
                        }
                        break;
                    case states.open_state:
                        userMessage = Console.ReadLine();
                        if(userMessage == "/exit"){
                            state = states.end_state;
                        } else {
                            data.Message(userMessage, "epoh");
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

        static public void Main(string[] args)
        {

            ArgParser arguments = new ArgParser();
            // Send data = new Send();
            // Receive things = new Receive();

            arguments.getArguments(args);


            Console.WriteLine($"SENDING TO : {arguments.serverAdress}");
            Console.WriteLine($"AT PORT : {arguments.serverPort}");

            Send data = new Send(); 
            data.clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try 
            { 
                data.clientSocket.Connect(arguments.serverAdress, arguments.serverPort);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return;
            }

            Task sendingTask = Task.Run(() => Sending(data));
            Task receivingTask = Task.Run(() => Recieving(data));

            Task.WaitAll(sendingTask, receivingTask);
            return;
        }
    }
}