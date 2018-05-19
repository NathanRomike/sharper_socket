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
        
        public static void Main(string[] args)
        {
            var server = new TcpListener(IPAddress.Parse(ListeningIpAddress), Port);
            server.Start();
            Console.WriteLine($"Server has started on {ListeningIpAddress}:{Port} {Environment.NewLine} Waiting for a connection...");

            var client = server.AcceptTcpClient();
            Console.WriteLine("Client connected!");

            var stream = client.GetStream();

            while (true)
            {
                while (!stream.DataAvailable)
                {
                }
                var bytes = new byte[client.Available];
                stream.Read(bytes, 0, bytes.Length);
                var data = Encoding.UTF8.GetString(bytes);
                const string wsGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

                if (!new System.Text.RegularExpressions.Regex("^GET").IsMatch(data)) continue;
                Console.WriteLine($"Received request: {Environment.NewLine} {data}");

                const string endOfLine = "\r\n";

                var webSocketAccept = Convert.ToBase64String(System.Security.Cryptography.SHA1.Create()
                    .ComputeHash(Encoding.UTF8.GetBytes(
                        new System.Text.RegularExpressions.Regex("Sec-WebSocket-Key: (.*)").Match(data)
                            .Groups[1]
                            .Value.Trim() + wsGuid)));

                var response = Encoding.UTF8.GetBytes("HTTP/1.1 101 Switching Protocols" + endOfLine +
                                                      "Connection: Upgrade" + endOfLine + 
                                                      "Upgrade: websocket" + endOfLine +
                                                      "Sec-WebSocket-Accept: " + webSocketAccept + endOfLine +
                                                      endOfLine);
                
                stream.Write(response, 0, response.Length);
            }
        }
    }
}