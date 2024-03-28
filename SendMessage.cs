using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ipk_protocol
{
    class Send
    {
        // public string message;
        public Socket clientSocket;
        private void SendMessage(string message)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(message);
            clientSocket.Send(bytes);
        }

        public void Authorization(string login, string displayName, string key)
        {
            string authMessage = "AUTH " + login + " AS " + displayName + " USING " + key + "\r\n";
            SendMessage(authMessage);
        }

        public void Join(string channelName, string displayName)
        {
            string joinMessage = "JOIN " + channelName + " AS " + displayName + "\r\n";
            SendMessage(joinMessage);
        }

        public void Message(string messageContent, string displayName)
        {
            string sendMessage = "MSG FROM " + displayName + " IS " + messageContent + "\r\n";
            SendMessage(sendMessage);
        }

        public async void Bye()
        {
            string sendMessage = "BYE\r\n";
            SendMessage(sendMessage);
            clientSocket.Shutdown(SocketShutdown.Both);
            clientSocket.Close();
        }
    }
}