using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ipk_protocol
{
    class ArgParser
    {
            public string? transportProtocol;
            public UInt16 serverPort = 6969;
            public UInt16 ConfirmationTimeout = 250;
            public byte Retransmissions = 3;
            public string help = "Usage: client [serverAdress] [serverPort]";
            public IPAddress[]? serverAdress;
            
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
                            serverAdress = Dns.GetHostAddresses(args[i]);
                            break;
                        case "-p":
                            i++; // to skip argument afte -t + saving right argument to transportProtocol
                            serverPort = UInt16.Parse(args[i]);
                            break;
                        case "-d":
                            i++; // to skip argument afte -t + saving right argument to transportProtocol
                            ConfirmationTimeout = UInt16.Parse(args[i]);
                            break;
                        case "-r":
                            i++; // to skip argument afte -t + saving right argument to transportProtocol
                            Retransmissions = byte.Parse(args[i]);
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