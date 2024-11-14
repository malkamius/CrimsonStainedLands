using System;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace CrimsonStainedLands.Connections
{
    public class SslConnection : BaseConnection
    {
        byte[] buffer = new byte[1024];

        public Encoding Encoding { get; set; }
        public ConnectionManager Manager { get; }
        public SslServer Server { get; }
        public NetworkStream BaseStream { get; set; }

        public SslStream Stream { get; set; }

        private void Authenticated(IAsyncResult result)
        {
            try
            {
                this.Stream.EndAuthenticateAsServer(result);
                this.Stream.ReadTimeout = 1;
                this.Status = ConnectionStatus.Connected;
                this.Server.ConnectionConnectedCallback(this, null, null);

            }
            catch
            {
                this.Stream.Dispose();
                this.Status = ConnectionStatus.Disconnected;
            }
        }

        public SslConnection(ConnectionManager manager, SslServer server, Socket socket, X509Certificate2 cert) : base(socket)
        {
            this.Manager = manager;
            this.Server = server;
            this.BaseStream = new System.Net.Sockets.NetworkStream(socket);
            this.Stream = new SslStream(BaseStream);
            this.Status = ConnectionStatus.Authenticating;
            this.Stream.BeginAuthenticateAsServer(cert, Authenticated, this);
        }

        public override byte[] Read()
        {
            try
            {
                if (this.Stream != null && this.Stream.CanRead && (Socket.Poll(1, SelectMode.SelectRead) || Socket.Available > 0))
                {

                    int read = this.Stream.Read(buffer);

                    if (read > 0)
                    {
                        return buffer.Take(read).ToArray();
                    }
                    else
                    {
                        this.Cleanup();
                        return null;
                    }
                }
                else
                    return null;
            }
            catch (IOException ex)
            {
                if (ex.Message.Contains("forcibly closed"))
                    Cleanup();
                return null;
            }
            catch
            {
                this.Cleanup();
                return null;
            }
            finally
            {
            }
        }

        public override int Write(byte[] data)
        {
            try
            {

                if (data.Length > 0)
                {
                    if (data[0] == (byte)TelnetNegotiator.Options.InterpretAsCommand)
                    {
                        this.Stream.Write(data);
                        return data.Length;
                    }
                    else
                    {
                        this.Stream.Write(data);
                        return data.Length;
                    }
                }
                else
                    return 0;
            }
            catch
            {
                this.Cleanup();
                return 0;
            }
        }

        public override BaseConnection.ConnectionStatus Status
        {
            get; set;
        }

        public override void Cleanup()
        {
            this.Status = ConnectionStatus.Disconnected;
            this.Manager.Connections.Remove(this);
            try
            {
                this.Stream.Dispose();
            }
            catch
            {

            }
            try
            {
                this.Socket.Dispose();
            }
            catch
            {
            }
        }
    }
}