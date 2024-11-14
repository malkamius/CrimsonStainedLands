using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace CrimsonStainedLands.Connections
{
    public class WebsocketConnection : BaseConnection
    {
        private readonly WebSocket webSocket;
        private readonly ConnectionManager manager;
        private readonly WebServer server;
        private readonly byte[] buffer = new byte[1024 * 4];
        private readonly BlockingCollection<byte[]> receivedData = new BlockingCollection<byte[]>();

        public override ConnectionStatus Status { get; set; }

        private CancellationTokenSource cancellationTokenSource;

        public WebsocketConnection(ConnectionManager manager, WebServer server, WebSocket webSocket, CancellationTokenSource cancelTokenSource)
            : base(null) // Socket is not used in WebSocket implementation
        {
            this.manager = manager;
            this.server = server;
            this.webSocket = webSocket;
            this.Status = ConnectionStatus.Connected;
            this.cancellationTokenSource = cancelTokenSource;
        }

        public async Task HandleWebSocketConnection()
        {
            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Binary)  // Changed from Text to Binary
                    {
                        var data = new byte[result.Count];
                        Array.Copy(buffer, data, result.Count);
                        receivedData.Add(data);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            string.Empty,
                            CancellationToken.None);
                        break;
                    }
                }
            }
            catch (WebSocketException)
            {
                // Handle disconnect
            }
            finally
            {
                Cleanup();
            }
        }

        public override byte[] Read()
        {
            if (receivedData.TryTake(out byte[] data))
            {
                return data;
            }
            return new byte[] { };
        }

        public override int Write(byte[] data)
        {
            if (webSocket.State == WebSocketState.Open)
            {
                webSocket.SendAsync(
                    new ArraySegment<byte>(data),
                    WebSocketMessageType.Binary,  // Changed from Text to Binary
                    true,
                    CancellationToken.None).Wait();
                return data.Length;
            }
            return 0;
        }


        public override void Cleanup()
        {
            this.Status = ConnectionStatus.Disconnected;
            try
            {
                webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing connection", this.cancellationTokenSource.Token).Wait();
            }
            catch { }
            try
            {
                webSocket.Dispose();
            }
            catch { }
            manager.Connections.Remove(this);
            server.connections.Remove(this);
        }
    }
}