using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace WebSocketServer
{
    internal static class Program
    {
        private const string ListeningIpAddress = "127.0.0.1";
        private const int Port = 80;

        private static TcpListener _tcpListener = null;
        private static TcpClient _tcpClient = null;
        private static NetworkStream _stream = null;
        
        public static void Main(string[] args)
        {
            ListenForConnections();  
        }

        private static void ListenForConnections()
        {
            while (true)
            {
                _tcpListener = new TcpListener(IPAddress.Parse(ListeningIpAddress), Port);
                _tcpListener.Start();
                Console.WriteLine($"Server has started on {ListeningIpAddress}:{Port} {Environment.NewLine} Waiting for a connection...");

                _tcpClient = _tcpListener.AcceptTcpClient();
                Console.WriteLine("Client connected!");
                ConnectSocket(_tcpClient);
            }
        }

        private static void ConnectSocket(TcpClient client)
        {
            _stream = client.GetStream();
            
            while (true)
            {
                while (!_stream.DataAvailable)
                {
                }
                var bytes = new byte[client.Available];
                _stream.Read(bytes, 0, bytes.Length);
                var data = Encoding.UTF8.GetString(bytes);
                
                if (new System.Text.RegularExpressions.Regex("^GET").IsMatch(data))
                {
                    PerformHandshake(data);
                    continue;
                }
                
                Console.WriteLine($"client message data received: {bytes}");
            }
        }

        private static void PerformHandshake(string clientReqest)
        {
            const string wsGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

            Console.WriteLine($"Received GET request: {Environment.NewLine} {clientReqest}");

            const string endOfLine = "\r\n";

            var webSocketAccept = Convert.ToBase64String(System.Security.Cryptography.SHA1.Create()
                .ComputeHash(Encoding.UTF8.GetBytes(
                    new System.Text.RegularExpressions.Regex("Sec-WebSocket-Key: (.*)").Match(clientReqest)
                        .Groups[1]
                        .Value.Trim() + wsGuid)));

            var response = Encoding.UTF8.GetBytes("HTTP/1.1 101 Switching Protocols" + endOfLine +
                                                  "Connection: Upgrade" + endOfLine + 
                                                  "Upgrade: websocket" + endOfLine +
                                                  "Sec-WebSocket-Accept: " + webSocketAccept + endOfLine +
                                                  endOfLine);
                
            _stream.Write(response, 0, response.Length);
        }
    }
}