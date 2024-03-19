using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ipk_protocol
{
    class ArgParser
    {
            // spravit cez commandline parser  url: https://devblogs.microsoft.com/ifdef-windows/command-line-parser-on-net5/
            // -t 	User provided 	tcp or udp 	Transport protocol used for connection
            // -s 	User provided 	IP address or hostname 	Server IP or hostname
            // -p 	4567 	uint16 	Server port
            // -d 	250 	uint16 	UDP confirmation timeout
            // -r 	3 	uint8 	Maximum number of UDP retransmissions
            // -h 			Prints program help output and exits
            public string? transportProtocol;
            // string serverAdress;
            public UInt16 serverPort = 6969;
            public UInt16 UdpConfirmationTimeout = 250; // UInt16 == uint16
            public byte maxUdpRetransmissions = 3; // byte == uint8
            public string help = "Usage: client [serverAdress] [serverPort]";
            public IPAddress serverAdress = IPAddress.Parse("127.0.0.1");
            
            public void getArguments(string[] args)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    // Console.WriteLine("arguments used :" + args[i] + "  " + args[i + 1]);
                    switch (args[i])
                    {
                        case "-h":
                            Console.WriteLine(help);
                            Environment.Exit(0);
                            break;
                        case "-t":
                            i++; // to skip argument afte -t + saving right argument to transportProtocol
                            transportProtocol = args[i];
                            break;
                        case "-s":
                            i++; // to skip argument afte -t + saving right argument to transportProtocol
                            serverAdress = IPAddress.Parse(args[i]);
                            break;
                        case "-p":
                            i++; // to skip argument afte -t + saving right argument to transportProtocol
                            serverPort = UInt16.Parse(args[i]);
                            break;
                        case "-d":
                            i++; // to skip argument afte -t + saving right argument to transportProtocol
                            UdpConfirmationTimeout = UInt16.Parse(args[i]);
                            break;
                        case "-r":
                            i++; // to skip argument afte -t + saving right argument to transportProtocol
                            maxUdpRetransmissions = byte.Parse(args[i]);
                            break;
                        default:
                            Console.WriteLine("Invalid argument: " + args[i]);
                            Environment.Exit(0);
                            break;
                    }
                }
            }
    }
}