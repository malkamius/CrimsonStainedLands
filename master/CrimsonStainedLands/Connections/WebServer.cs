using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CrimsonStainedLands.Connections
{
    public class WebServer
    {
        

        public IPAddress Address {get;}
        public int Port {get;}

        public Socket ListeningSocket {get;private set;}
        
        private CancellationTokenSource cancellationTokenSource;

        public ConnectionManager.ConnectionConnected ConnectionConnectedCallback {get;set;}

        X509Certificate2 certificate;

        private ConcurrentList<WebsocketConnection> connections = new ConcurrentList<WebsocketConnection>();

        public string WebRoot {get;set;} = "I:\\web";

        public WebServer(string address, int port, X509Certificate2 certificate, CancellationTokenSource cancellationTokenSource)
        {
            Address = IPAddress.Parse(address);
            Port = port;
            this.cancellationTokenSource = cancellationTokenSource;
            this.certificate = certificate;
        }

        public async Task Start(ConnectionManager.ConnectionConnected connectionConnected)
        {
            if(certificate == null)
            {
                return;
            }
            ConnectionConnectedCallback = connectionConnected;

            ListeningSocket = new Socket( SocketType.Stream, ProtocolType.Tcp );
            ListeningSocket.Bind(new IPEndPoint(this.Address, this.Port));
            ListeningSocket.Listen(10);
            Game.log($"Accepting Web Connections at {this.Address.ToString()}:{this.Port}");
            var handler = Task.Run(HandleWebConnections);
            while(!cancellationTokenSource.IsCancellationRequested)
            {
                var newClientSocket = await ListeningSocket.AcceptAsync(cancellationTokenSource.Token);

                var connection = new WebsocketConnection(this, newClientSocket, certificate);
                //ConnectionConnectedCallback(connection);
                connections.Add(connection);
            }
            await handler.WaitAsync(cancellationTokenSource.Token);
        }

        private async Task HandleWebConnections()
        {
            while (!cancellationTokenSource.IsCancellationRequested)
                foreach (var connection in connections.ToArray())
                    try
                    {
                        await handle_connection(connection);
                    }
                    catch
                    {
                        connection.Cleanup();
                        connections.Remove(connection);
                    }
        }

        private bool IsWebSocketUpgradeRequest(string request)
        {
            return request.Contains("Connection: Upgrade", StringComparison.OrdinalIgnoreCase) &&
                request.Contains("Upgrade: websocket", StringComparison.OrdinalIgnoreCase);
        }
        private async Task handle_connection(WebsocketConnection connection)
        {
            if(connection.Status == BaseConnection.ConnectionStatus.Negotiating) {
                var data = await connection.Read();

                if (data != null)
                {
                    connection.Request.Append(System.Text.Encoding.ASCII.GetString(data));

                    if (connection.Request.Length > 4096)
                    {
                        connections.Remove(connection);
                        connection.Cleanup();
                    }
                    else
                    {
                        var request = connection.Request.ToString();
                        var requestLines = request.Split('\n');
                        var requestLine = requestLines[0].Split(' ');
                        var method = requestLine[0];
                        var url = requestLine[1];

                        if (IsWebSocketUpgradeRequest(request))
                        {
                            connections.Remove(connection);
                            await connection.HandleWebSocketUpgradeAsync(request);
                        }
                        else
                        {
                            await ServeContentAsync(connection, url);
                        }
                    }
                }
            }
        }

        private async Task ServeContentAsync(WebsocketConnection connection, string url)
        {
            if(url.StartsWith("/js/") && url.Length > 4 && String.IsNullOrEmpty(Path.GetExtension(url)))
                url += ".js";

            string filePath = Path.Combine(WebRoot, url.TrimStart('/'));
            
            if (string.IsNullOrEmpty(url.Trim('/')))
            {
                filePath = Path.Combine(filePath, "client.html");
            }

            
            
            if (File.Exists(filePath))
            {
                string content = File.ReadAllText(filePath);
                string contentType = GetContentType(filePath);

                // Apply simple templating
                content = ApplyTemplate(content);

                await SendResponse(connection, 200, "OK", contentType, content);
            }
            else
            {
                await SendResponse(connection, 404, "Not Found", "text/html", "<h1>404 - Page Not Found</h1>");
            }
        }

        private string ApplyTemplate(string content)
        {
            // Simple templating system - replace placeholders with values
            content = content.Replace("{{CurrentTime}}", DateTime.Now.ToString());
            content = content.Replace("{{ServerName}}", "My SSL Server");
            // Add more replacements as needed
            return content;
        }

        private async Task SendResponse(WebsocketConnection connection, int statusCode, string statusText, string contentType, string content)
        {
            if(connection.Status != BaseConnection.ConnectionStatus.Disconnected) {
                byte[] contentBytes = Encoding.UTF8.GetBytes(content);
                string response = $"HTTP/1.1 {statusCode} {statusText}\r\n" +
                                $"Content-Type: {contentType}\r\n" +
                                $"Content-Length: {contentBytes.Length}\r\n" +
                                "Connection: close\r\n\r\n";

                byte[] responseBytes = Encoding.ASCII.GetBytes(response);
                await connection.Write(responseBytes);
                await connection.Write(contentBytes);
                connections.Remove(connection);
                connection.Stream.Flush();
                connection.Stream.Close();
                connection.Cleanup();

            }
        }

        private string GetContentType(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            switch (ext)
            {
                case ".htm":
                case ".html":
                    return "text/html";
                case ".css":
                    return "text/css";
                case ".js":
                    return "application/javascript";
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                case ".png":
                    return "image/png";
                default:
                    return "application/octet-stream";
            }
        }

        


    }

}