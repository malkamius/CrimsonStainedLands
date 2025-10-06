using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using FxSsh;
using FxSsh.Services;

namespace CrimsonStainedLands.Connections
{
    public class SSHServer
    {
        private readonly FxSsh.SshServer server;
        private readonly string hostKeyFile;
        private readonly ConnectionManager manager;
        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly ConcurrentDictionary<Session, SSHConnection> connections = new();
        private ConnectionManager.ConnectionConnected connectionConnectedCallback;

        public IPAddress Address { get; }
        public int Port { get; }

        public SSHServer(ConnectionManager manager, string address, int port, CancellationTokenSource cancellationTokenSource, string hostKeyFile = "host.key")
        {
            this.manager = manager;
            this.Address = IPAddress.Parse(address);
            this.Port = port;
            this.cancellationTokenSource = cancellationTokenSource;
            this.hostKeyFile = hostKeyFile;

            var startingInfo = new StartingInfo(this.Address, this.Port, "SSH-2.0-CrimsonStainedLands");
            try
            {
                server = new FxSsh.SshServer(startingInfo);

                var hostKey = LoadOrGenerateHostKey();
                server.AddHostKey("ssh-rsa", hostKey);

                server.ConnectionAccepted += Server_ConnectionAccepted;
                server.ExceptionRasied += Server_ExceptionRaised;
            }
            catch (Exception ex)
            {
                Game.bug(ex.Message);
            }
        }

        private void Server_ConnectionAccepted(object sender, Session session)
        {
            var connection = new SSHConnection(manager, session);
            connections.TryAdd(session, connection);
            
            session.ServiceRegistered += Session_ServiceRegistered;
            session.Disconnected += Session_Disconnected;

            
        }

        private void Session_Disconnected(object sender, EventArgs e)
        {
            if (sender is Session session && connections.TryRemove(session, out var connection))
            {
                connection.Cleanup();
            }
        }

        private void Session_ServiceRegistered(object sender, SshService service)
        {
            if (sender is Session session && connections.TryGetValue(session, out var connection))
            {
                if (service is UserauthService userAuth)
                {
                    userAuth.Userauth += (s, args) =>
                    {
                        if(connections.TryGetValue(session, out var connection))
                        {
                            
                            connection.Username = args.Username;
                            connection.Password = args.Password;
                            
                        }

                        // For development, accept all connections
                        args.Result = true;
                        return;

                        // TODO: Implement proper authentication
                        //if (args.AuthenticationType == "publickey")
                        //{
                        //    args.Result = VerifyPublicKey(args.Username, args.PublicKey);
                        //}
                    };
                }
                else if (service is ConnectionService connService)
                {
                    connService.CommandOpened += (s, args) =>
                    {
                        if (args.ShellType == "shell")
                        {
                            connection.Channel = args.Channel;
                            args.Channel.DataReceived += (sender, data) => connection.HandleReceivedDataAsync(data);
                            connection.Status = BaseConnection.ConnectionStatus.Connected;
                            connectionConnectedCallback?.Invoke(connection, connection.Username, connection.Password);
                        }
                    };
                }
            }
        }

        private void Server_ExceptionRaised(object sender, Exception e)
        {
            Game.log($"SSH Server error: {e.Message}");
        }

        public async Task Start(ConnectionManager.ConnectionConnected connectionConnected)
        {
            connectionConnectedCallback = connectionConnected;
            server.Start();
            Game.log($"Accepting SSH Connections at {Address}:{Port}");

            // Keep the server running until cancellation is requested
            try
            {
                await Task.Delay(-1, cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
            }
            finally
            {
                server.Stop();
            }
        }

        private string LoadOrGenerateHostKey()
        {
            try
            {
                byte[] keyBytes;
                if (File.Exists(hostKeyFile))
                {
                    keyBytes = File.ReadAllBytes(hostKeyFile);
                }
                else
                {
                    using var rsa = new RSACryptoServiceProvider(2048);
                    keyBytes = rsa.ExportCspBlob(true);
                    File.WriteAllBytes(hostKeyFile, keyBytes);
                    File.SetAttributes(hostKeyFile, FileAttributes.ReadOnly);
                }
                return Convert.ToBase64String(keyBytes);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to load or generate host key", ex);
            }
        }
    }
}