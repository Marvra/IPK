using System.Net;
using System.Net.Sockets;

namespace ipk_protocol
{
    class ArgParser
    {
            public string? transportProtocol;
            public UInt16 serverPort = 4567;
            public UInt16 ConfirmationTimeout = 250;
            public byte Retransmissions = 3;
            public string help = "Usage: client [serverAdress] [serverPort]";
            public IPAddress? serverAdress;
            
            public void getArguments(string[] args)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    switch (args[i])
                    {
                        case "-h":
                            Console.WriteLine(help);
                            Environment.Exit(0);
                            break;
                        case "-t":
                            i++;

                            // Only tcp or udp transport protocol is allowed
                            if(args[i] != "tcp" && args[i] != "udp")
                            {
                                Console.Error.WriteLine("Invalid argument: transport protocol must be tcp or udp");
                                Environment.Exit(1);
                            }
                            transportProtocol = args[i];
                            break;
                        case "-s":
                            i++;

                            // Check if the provided server adress is valid saving output
                            IPAddress[] input = DnsResolveCheck(args[i]);

                            if (input.Length == 0)
                            {
                                Console.Error.WriteLine("Invalid argument: No server adress was provided");
                                Environment.Exit(1);
                            }

                            // Check for IpV4 adress and save it
                            foreach (var ValidAdress in input)
                            {
                                if (ValidAdress.AddressFamily == AddressFamily.InterNetwork)
                                {
                                    serverAdress = ValidAdress;
                                    break;
                                }
                            }
                            break;
                        case "-p":
                            i++;
                            serverPort = UInt16.Parse(args[i]);
                            break;
                        case "-d":
                            i++;
                            ConfirmationTimeout = UInt16.Parse(args[i]);
                            break;
                        case "-r":
                            i++;
                            Retransmissions = byte.Parse(args[i]);
                            break;
                        default:
                            Console.Error.WriteLine($"Invalid argument: {args[i]}");
                            Environment.Exit(1);
                            break;
                    }
                }
            }

            // Method for checking if the provided server adress is valid
            private IPAddress[] DnsResolveCheck(string arg)
            {
                try
                {
                    return Dns.GetHostAddresses(arg);
                }
                catch (Exception)
                {
                    Console.Error.WriteLine("Invalid argument: Invalid server adress provided");
                    Environment.Exit(1);
                    return new IPAddress[0];
                }
            }
    }
}