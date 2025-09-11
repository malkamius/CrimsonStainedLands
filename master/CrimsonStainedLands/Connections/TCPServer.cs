using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace CrimsonStainedLands.Connections
{
    public class TCPServer
    {
        public ConnectionManager Manager { get; }
        public IPAddress Address { get; }
        public int Port { get; }

        public Socket ListeningSocket { get; private set; }

        private CancellationTokenSource cancellationTokenSource;

        ConnectionManager.ConnectionConnected ConnectionConnectedCallback { get; set; }

        public TCPServer(ConnectionManager manager, string address, int port, CancellationTokenSource cancellationTokenSource)
        {
            this.Manager = manager;
            Address = IPAddress.Parse(address);
            Port = port;
            this.cancellationTokenSource = cancellationTokenSource;
        }

        public async Task Start(ConnectionManager.ConnectionConnected connectionConnected)
        {
            Game.log("START TCP SERVER");
            ConnectionConnectedCallback = connectionConnected;

            try
            {
                var endpoint = new IPEndPoint(this.Address, this.Port);
                ListeningSocket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                ListeningSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, true);
                ListeningSocket.Bind(endpoint);
                ListeningSocket.Listen(10);
                Game.log($"Accepting Telnet Connections at {this.Address.ToString()}:{this.Port}");
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    var newClientSocket = await ListeningSocket.AcceptAsync(cancellationTokenSource.Token);

                    var connection = new TCPConnection(this.Manager, newClientSocket);
                    ConnectionConnectedCallback(connection, null, null);
                }
            }
            catch (Exception ex)
            {
                Game.bug(ex.Message);
                System.Environment.Exit(1);
            }
        }
    }
}