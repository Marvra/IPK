// See https://aka.ms/new-console-template for more information
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
// using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
//secret : a3bb2d2f-c085-4fcd-aa1c-edbdac32c575

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
        static public void Main(string[] args)
        {

            ArgParser Arguments = new ArgParser();
            Arguments.getArguments(args);
            if (Arguments.transportProtocol == "udp")
            {
                Console.WriteLine("UDP");
                UDP ClientUdp = new UDP();
                ClientUdp.MainProces(Arguments);

            }
            else if (Arguments.transportProtocol == "tcp")
            {
                Console.WriteLine("TCP");
                TCP ClientTcp = new TCP();
                ClientTcp.MainProces(Arguments);

            } else {
                Console.Error.WriteLine("Invalid transport protocol");
                return;
            }
        }
    }
}