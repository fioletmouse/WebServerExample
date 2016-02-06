using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace WebServerExample
{
    class Server
    {
        TcpListener Listener;

        public Server(int Port)
        {
            Listener = new TcpListener(IPAddress.Any, Port);
            Listener.Start();

            while (true)
            {
                TcpClient Client = Listener.AcceptTcpClient();
                Thread thread = new Thread(new ParameterizedThreadStart(ClientThread));
                thread.Start(Client);
            }

        }
        ~Server()
        {
            if (Listener != null) 
            {
                Listener.Stop();
            }
        }

        static void ClientThread(Object StateInfo)
        {
            new Client((TcpClient)StateInfo);
        }
        static void Main(string[] args)
        {
            new Server(80);
        }
    }
}
