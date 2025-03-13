using Org.BouncyCastle.Tls;
using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace CrimsonStainedLands.Connections
{
    public class SslServer
    {
        public ConnectionManager Manager { get; }
        public IPAddress Address {get;}
        public int Port {get;}

        public Socket ListeningSocket {get;private set;}
        
        private CancellationTokenSource cancellationTokenSource;

        public ConnectionManager.ConnectionConnected ConnectionConnectedCallback {get;set;}

        X509Certificate2 certificate;

        public SslServer(ConnectionManager manager, string address, int port, X509Certificate2 certificate, CancellationTokenSource cancellationTokenSource)
        {
            this.Manager = manager;
            Address = IPAddress.Parse(address);
            Port = port;
            this.cancellationTokenSource = cancellationTokenSource;
            this.certificate = certificate;
            //try
            //{
            //    certificate = new X509Certificate2(Settings.X509CertificatePath, Settings.X509CertificatePassword);
            //}
            //catch(Exception ex)
            //{
            //    Game.bug(ex.Message);
            //}
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
            Game.log($"Accepting SSL Connections at {this.Address.ToString()}:{this.Port}");
            while(!cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    var newClientSocket = await ListeningSocket.AcceptAsync(cancellationTokenSource.Token);

                    var connection = new SslConnection(this.Manager, this, newClientSocket, certificate);
                    System.Threading.Thread.Sleep(1);
                }
                catch (Exception ex)
                {
                    Game.bug(ex.Message);
                }
            }

            
        }
    }
}