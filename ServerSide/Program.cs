using System.Net.Sockets;
using System.Net;
using System.Text;

namespace ServerSide
{
    public class Program
    {
        // lock for thread synchronization
        static readonly object _lock = new object();
        static readonly Dictionary<int, TcpClient> list_clients = new Dictionary<int, TcpClient>();

        static void Main(string[] args)
        {
            Console.WriteLine("[Info]_> Starting server...");

            IPAddress ipAddr = IPAddress.Parse("127.0.0.1"); // default
            int port = 8899; // default
            // Getting ip and port from user start arguments
            try
            {
                if (args.Length == 3)
                {
                    ipAddr = IPAddress.Parse(args[1]);
                    port = int.Parse(args[2]);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error]_> (startup arguments) {ex.Message}");
                return;
            }
             
            try
            {
                // Creating tcp socket
                TcpListener ServerSocket = new TcpListener(ipAddr, port);
                ServerSocket.Start();

                Console.WriteLine($"[Info]_> Host: {ipAddr}   Port: {port}");
                Console.WriteLine("[Info]_> Server started!\n");

                // Listening for new clients
                int clientNumber = 1;
                while (true)
                {
                    TcpClient client = ServerSocket.AcceptTcpClient();
                    // Adding new connected clients to clients list
                    lock (_lock) list_clients.Add(clientNumber, client);
                    Console.WriteLine($"[Info]_> Client connected.\n");

                    // Creating new thread for working with client
                    Thread td = new Thread(handle_clients);
                    td.Start(clientNumber);
                    clientNumber++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error]_> (running) {ex.Message}");
            }
        }

        public static void handle_clients(object o)
        {
            int id = (int)o;
            TcpClient client;

            lock (_lock) client = list_clients[id];

            // Getting message from client
            while (true)
            {

                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[2048];
                int byte_count = stream.Read(buffer, 0, buffer.Length);
                string data = Encoding.ASCII.GetString(buffer, 0, byte_count);
                string[] splitedData = data.Split("|");

                if (byte_count == 0 || data == "exit()" || data == "q()")
                {
                    Console.WriteLine("[Client]_> Disconnected.\n");
                    break;
                }
                
                broadcast(data);
                Console.WriteLine($"[Client_{splitedData[1]}]_> {splitedData[2]}\n");
            }
            // dispose threads
            lock (_lock) list_clients.Remove(id);
            client.Client.Shutdown(SocketShutdown.Both);
            client.Close();
        }

        public static void broadcast(string data)
        {
            // sending message to all clients in network
            byte[] buffer = Encoding.ASCII.GetBytes(data + Environment.NewLine);

            lock (_lock)
            {
                foreach (TcpClient c in list_clients.Values)
                {
                    NetworkStream stream = c.GetStream();

                    stream.Write(buffer, 0, buffer.Length);
                }
            }
        }
    }
}