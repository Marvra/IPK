//secret : a3bb2d2f-c085-4fcd-aa1c-edbdac32c575

namespace ipk_protocol 
{   
    public enum States 
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
            // Get arguments from command line
            ArgParser Arguments = new ArgParser();
            Arguments.getArguments(args);

            // Start the client based on users input
            if (Arguments.transportProtocol == "udp")
            {
                UDP ClientUdp = new UDP();
                ClientUdp.MainProces(Arguments);

            }
            else if (Arguments.transportProtocol == "tcp")
            {
                TCP ClientTcp = new TCP();
                ClientTcp.MainProces(Arguments);

            }
            else
            {
                Console.WriteLine("Invalid transport protocol");
            }
            return;
        }
    }
}