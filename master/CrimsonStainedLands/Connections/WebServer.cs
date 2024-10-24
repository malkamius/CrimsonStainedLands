using CrimsonStainedLands.Web;
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
        public ConnectionManager Manager { get; }

        public IPAddress Address {get;}
        public int Port {get;}

        public Socket ListeningSocket {get;private set;}
        
        private CancellationTokenSource cancellationTokenSource;

        public ConnectionManager.ConnectionConnected ConnectionConnectedCallback {get;set;}

        X509Certificate2 certificate;

        public ConcurrentList<WebsocketConnection> connections = new ConcurrentList<WebsocketConnection>();

        public string WebRoot {get;set;} = "Web";

        public WebServer(ConnectionManager manager, string address, int port, X509Certificate2 certificate, CancellationTokenSource cancellationTokenSource)
        {
            this.Manager = manager;
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

                var connection = new WebsocketConnection(this.Manager, this, newClientSocket, certificate);
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

        private string GetOriginFromRequest(string request)
        {
            var lines = request.Split('\n');
            foreach (var line in lines)
            {
                if (line.StartsWith("Origin: ", StringComparison.OrdinalIgnoreCase))
                {
                    return line.Substring("Origin: ".Length).Trim();
                }
            }
            return string.Empty;
        }


        private async Task handle_connection(WebsocketConnection connection)
        {
            if(connection.Status == BaseConnection.ConnectionStatus.Negotiating) {
                var data = connection.Read();

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
                        string origin = GetOriginFromRequest(request);
                        if (IsWebSocketUpgradeRequest(request))
                        {
                            connections.Remove(connection);
                            await connection.HandleWebSocketUpgradeAsync(request);
                        }
                        else
                        {
                            ServeContentAsync(connection, url, origin);
                        }
                    }
                }
            }
        }

        private void ServeContentAsync(WebsocketConnection connection, string url, string origin)
        {
            try
            {
                url = url.Replace("..", "");
                if (url.StartsWith("/js/") && url.Length > 4 && String.IsNullOrEmpty(Path.GetExtension(url)))
                    url += ".js";

                string filePath = Path.Combine(WebRoot, url.TrimStart('/'));
                
                if (string.IsNullOrEmpty(url.Trim('/')))
                {
                    filePath = Path.Combine(filePath, "client.html");
                }

                if (url == "/who")
                {
                    try
                    { 
                        SendResponse(connection, 200, "OK", "application/json", new WhoListController().GetContent(), origin);
                    }
                    catch (Exception ex)
                    {
                        Game.bug("Error sending response. {0}", ex.Message);
                        SendResponse(connection, 500, "Server Error", "text/html", "An error occurred processing the request.", origin);
                    }

                }
                else if (File.Exists(filePath))
                {
                    try
                    {
                        string content;
                        using (var reader = new StreamReader(filePath, Encoding.UTF8))
                        {
                            content = reader.ReadToEnd();
                        }
                        string contentType = GetContentType(filePath);

                        if (filePath.EndsWith(".html"))
                        {
                            // Apply simple templating
                            content = ApplyTemplate(content);
                        }

                        SendResponse(connection, 200, "OK", contentType, content, origin);
                    }
                    catch (Exception ex)
                    {
                        Game.bug("Error sending response. {0}", ex.Message);
                        SendResponse(connection, 500, "Server Error", "text/html", "An error occurred processing the request.", origin);
                    }
                }
                else
                {
                    SendResponse(connection, 404, "Not Found", "text/html", "<h1>404 - Page Not Found</h1>", origin);
                }
            }
            catch (Exception ex)
            {
                Game.bug("Error serving content. {0}", ex.Message);
                connection.Cleanup();
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

        private void SendResponse(WebsocketConnection connection, int statusCode, string statusText, string contentType, string content, string origin)
        {
            try
            {
                if (connection?.Status != BaseConnection.ConnectionStatus.Disconnected && connection?.Stream != null)
                {
                    // Use consistent UTF-8 encoding for all content
                    byte[] contentBytes = Encoding.UTF8.GetBytes(content);

                    string[] allowedOrigins = new string[]
                    {
                "https://crimsonstainedlands.net",
                "https://kbs-cloud.com",
                "https://games.mywire.org"
                    };

                    string accessControlAllowOrigin = allowedOrigins.Contains(origin) ? origin : allowedOrigins[0];

                    // Add charset and cache control headers
                    string response = $"HTTP/1.1 {statusCode} {statusText}\r\n" +
                                    $"Content-Type: {contentType}; charset=utf-8\r\n" +
                                    $"Content-Length: {contentBytes.Length}\r\n" +
                                    $"Access-Control-Allow-Origin: {accessControlAllowOrigin}\r\n" +
                                    "Access-Control-Allow-Methods: GET, POST, OPTIONS\r\n" +
                                    "Access-Control-Allow-Headers: Content-Type\r\n" +
                                    "Cache-Control: no-cache\r\n" +
                                    "Connection: close\r\n" +
                                    "\r\n";

                    // Use UTF-8 for headers as well
                    byte[] responseBytes = Encoding.UTF8.GetBytes(response);

                    // Write with error checking
                    try
                    {
                        connection.Stream.Write(responseBytes, 0, responseBytes.Length);
                        connection.Stream.Write(contentBytes, 0, contentBytes.Length);
                        connection.Stream.Flush();
                    }
                    catch (Exception ex)
                    {
                        Game.bug($"Error writing to stream: {ex.Message}");
                        throw;
                    }

                    // Clean up in finally block
                    try
                    {
                        connections.Remove(connection);
                    }
                    catch (Exception ex)
                    {
                        Game.bug($"Error removing connection: {ex.Message}");
                    }
                    finally
                    {
                        try
                        {
                            connection.Stream.Close();
                            connection.Cleanup();
                        }
                        catch (Exception ex)
                        {
                            Game.bug($"Error during cleanup: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Game.bug($"SendResponse failed: {ex.Message}");
                // Try to clean up even if we failed
                try
                {
                    connections.Remove(connection);
                    connection?.Stream?.Close();
                    connection?.Cleanup();
                }
                catch { } // Suppress cleanup errors
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