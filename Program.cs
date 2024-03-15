// See https://aka.ms/new-console-template for more information
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class Client
{
    public static void Main(string[] args)
    {
        // -t 	User provided 	tcp or udp 	Transport protocol used for connection
        // -s 	User provided 	IP address or hostname 	Server IP or hostname
        // -p 	4567 	uint16 	Server port
        // -d 	250 	uint16 	UDP confirmation timeout
        // -r 	3 	uint8 	Maximum number of UDP retransmissions
        // -h 			Prints program help output and exits
        string transportProtocol;
        // // string serverAdress;
        UInt16 serverPort = 6969;
        UInt16 UdpConfirmationTimeout = 250; // UInt16 == uint16
        byte maxUdpRetransmissions = 3; // byte == uint8
        string help = "Usage: client [serverAdress] [serverPort]";
        IPAddress serverAdress = IPAddress.Parse("127.0.0.1");
        var buffer = new byte[1_024];


        for (int i = 0; i < args.Length; i++)
        {
            Console.WriteLine("arguments used :" + args[i] + "  " + args[i + 1]);
            switch (args[i])
            {
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
                case "-h":
                    Console.WriteLine(help);
                    return;
                default:
                    Console.WriteLine("Invalid argument: " + args[i]);
                    Console.WriteLine(help);
                    return;
            }
        }

        Console.WriteLine("SENDING TO : " + serverAdress);
        Console.WriteLine("AT PORT : " + serverPort);

        Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        // try 
        // { 
        //     clientSocket.Connect(serverAdress, serverPort);
        // }
        // catch (Exception e)
        // {
        //     Console.WriteLine("Error: " + e.Message);
        //     return;
        // }
        clientSocket.Connect(serverAdress, serverPort);

        string message = "AUTH xvrabl06 AS epoh USING b696b89a-6959-4394-9918-28e7dbbcb804\r\n";
        byte[] bytes = Encoding.ASCII.GetBytes(message);
        clientSocket.Send(bytes);
        var bytesLength = clientSocket.Receive(buffer, SocketFlags.None);
        var givenMess = Encoding.UTF8.GetString(buffer, 0, bytesLength); // 0 znamena index 
        Console.WriteLine("RECIEVED MESS :  " + givenMess);
        clientSocket.Shutdown(SocketShutdown.Both);
        clientSocket.Close();
        return;
    }
}
