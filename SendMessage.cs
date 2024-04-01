using System.Net.Sockets;
using System.Text;

namespace ipk_protocol
{
    class Send
    {
        // public string message;
        public Socket clientSocket;

        // Constructor
        public Send(Socket socket)
        {
            clientSocket = socket;
        }
        // Method for sending messages to the server
        private void SendMessage(string message)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(message);
            clientSocket.Send(bytes);
        }

        // Method for filling the message with correct syntax and sending it to the server
        // Used for authentification
        public void Authorization(string? login, string? displayName, string? key)
        {
            string authMessage = "AUTH " + login + " AS " + displayName + " USING " + key + "\r\n";
            SendMessage(authMessage);
        }

        // Method for filling the message with correct syntax and sending it to the server
        // Used for joining channel
        public void Join(string? channelName, string? displayName)
        {
            string joinMessage = "JOIN " + channelName + " AS " + displayName + "\r\n";
            SendMessage(joinMessage);
        }

        // Method for filling the message with correct syntax and sending it to the server
        // Used for user writen messages 
        public void Message(string? messageContent, string? displayName)
        {
            string sendMessage = "MSG FROM " + displayName + " IS " + messageContent + "\r\n";
            SendMessage(sendMessage);
        }

        // Method for filling the message with correct syntax and sending it to the server
        // Used for error messages
        public void Error(string messageContent, string? displayName)
        {
            string sendMessage = "ERR FROM " + displayName + " IS " + messageContent + "\r\n";
            SendMessage(sendMessage);
        }

        // Method for filling the message with correct syntax and sending it to the server
        // Used for bye before closing the connection
        public void Bye()
        {
            string sendMessage = "BYE\r\n";
            SendMessage(sendMessage);
        }
    }
}