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
                this.Server.ConnectionConnectedCallback(this);

            }
            catch
            {
                this.Stream.Dispose();
                this.Status = ConnectionStatus.Disconnected;
            }
        }

        public SslConnection(SslServer server, Socket socket, X509Certificate2 cert) : base(socket)
        {
            this.Server = server;
            this.BaseStream = new System.Net.Sockets.NetworkStream(socket);
            this.Stream = new SslStream(BaseStream);
            this.Status = ConnectionStatus.Authenticating;
            this.Stream.BeginAuthenticateAsServer(cert, Authenticated, this);
        }

        public override async Task<byte[]> Read()
        {
            try
            {
                if (this.Stream.CanRead)
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
            catch (IOException)
            {
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

        public override async Task<int> Write(byte[] data)
        {
            try
            {

                if (data.Length > 0)
                {
                    if (data[0] == (byte)TelnetNegotiator.Options.InterpretAsCommand)
                    {
                        await this.Stream.WriteAsync(data);
                        return data.Length;
                    }
                    else
                    {
                        await this.Stream.WriteAsync(data);
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