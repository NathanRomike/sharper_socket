using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;

namespace WebSocketServer
{
    internal static class Program
    {
        private const string ListeningIpAddress = "127.0.0.1";
        private const int Port = 80;

        private static TcpListener _tcpListener;
        private static TcpClient _tcpClient;
        private static NetworkStream _stream;
        
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
                while (!_stream.DataAvailable) ;
                var bytes = new byte[client.Available];
                _stream.Read(bytes, 0, bytes.Length);
                var data = Encoding.UTF8.GetString(bytes);
                
                if (new System.Text.RegularExpressions.Regex("^GET").IsMatch(data))
                {
                    PerformHandshake(data);
                    continue;
                }
                
                var decodedMessage = DecodeClientMessage(bytes);
                
                decodedMessage.ForEach(byteArray =>
                {
                    foreach (var thisByte in byteArray)
                    {
                        Console.Write(Convert.ToChar(thisByte));
                    }
                });
                CloseConnection();
                break;
            }
        }

        private static void PerformHandshake(string clientReqest)
        {
            const string wsGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

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

        private static List<byte[]> DecodeClientMessage(IReadOnlyList<byte> messageBytes)
        {
            var decodedReturn = new List<byte[]>();
            var offset = 0;
            while (offset + 6 < messageBytes.Count)
            {
                var length = messageBytes[offset + 1] - 0x80;

                if (length <= 125)
                {
                    var key = new[]
                    {
                        messageBytes[offset + 2], 
                        messageBytes[offset + 3], 
                        messageBytes[offset + 4], 
                        messageBytes[offset + 5]
                    };

                    var decoded = new byte[length];
                    for (var i = 0; i < length; i++)
                    {
                        var realPosition = offset + 6 + i;
                        decoded[i] = (byte) (messageBytes[realPosition] ^ key[i % 4]);
                    }

                    offset += 6 + length;
                    decodedReturn.Add(decoded);
                }
                else
                {
                    var a = messageBytes[offset + 2];
                    var b = messageBytes[offset + 3];
                    length = (a << 8) + b;

                    var key = new[]
                    {
                        messageBytes[offset + 4], 
                        messageBytes[offset + 5], 
                        messageBytes[offset + 6],
                        messageBytes[offset + 7]
                    };

                    var decoded = new byte[length];
                    for (var i = 0; i < length; i++)
                    {
                        var realPostion = offset + 8 + i;
                        decoded[i] = (byte) (messageBytes[realPostion] ^ key[i % 4]);
                    }

                    offset += 8 + length;
                    decodedReturn.Add(decoded);
                }
            }
            return decodedReturn;
        }

        private static void CloseConnection()
        {
            _stream.Close();
            _tcpClient.Close();
            _tcpListener.Stop();
        }
    }
}