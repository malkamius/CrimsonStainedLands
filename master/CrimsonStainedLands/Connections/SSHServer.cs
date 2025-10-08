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

                var hostKeys = LoadOrGenerateHostKeys();
                foreach (var kvp in hostKeys)
                {
                    server.AddHostKey(kvp.Key, kvp.Value);
                }

                server.ConnectionAccepted += Server_ConnectionAccepted;
                server.ExceptionRaised += Server_ExceptionRaised;
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
                if (service is UserAuthService userAuth)
                {
                    userAuth.UserAuth += (s, args) =>
                    {
                        if(connections.TryGetValue(session, out var connection))
                        {
                            
                            connection.Username = args.Username;
                            connection.Password = args.Password;
                            
                        }

                        args.Result = true;
                        return;
                    };
                }
                else if (service is ConnectionService connService)
                {
                    connService.CommandOpened += (s, args) =>
                    {
                        if (args.ShellType == "shell")
                        {
                            args.Agreed = true;

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
            // Ignore SshConnectionException for unknown x11-req requests, log others
            if (e is FxSsh.SshConnectionException && e.Message.Contains("Unknown request type: x11-req."))
            {
                // Optionally, log as info or ignore completely
                Game.log("SSH Server: Ignored unknown x11-req request from client.");
                return;
            }
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

        private Dictionary<string, string> LoadOrGenerateHostKeys()
        {
            var keys = new Dictionary<string, string>();
            try
            {
                // RSA key (PEM)
                string rsaKeyFile = Path.ChangeExtension(hostKeyFile, ".rsa.pem");
                string rsaPem;
                if (File.Exists(rsaKeyFile))
                {
                    rsaPem = File.ReadAllText(rsaKeyFile);
                }
                else
                {
                    using var rsa = System.Security.Cryptography.RSA.Create(2048);
                    var rsaBytes = rsa.ExportPkcs8PrivateKey();
                    rsaPem = PemEncode("PRIVATE KEY", rsaBytes);
                    File.WriteAllText(rsaKeyFile, rsaPem);
                    File.SetAttributes(rsaKeyFile, FileAttributes.ReadOnly);
                }
                keys["ssh-rsa"] = rsaPem;

                // ECDSA key (nistp256, PEM)
                string ecdsaKeyFile = Path.ChangeExtension(hostKeyFile, ".ecdsa.pem");
                string ecdsaPem;
                if (File.Exists(ecdsaKeyFile))
                {
                    ecdsaPem = File.ReadAllText(ecdsaKeyFile);
                }
                else
                {
                    using var ecdsa = System.Security.Cryptography.ECDsa.Create(System.Security.Cryptography.ECCurve.NamedCurves.nistP256);
                    var ecdsaBytes = ecdsa.ExportPkcs8PrivateKey();
                    ecdsaPem = PemEncode("PRIVATE KEY", ecdsaBytes);
                    File.WriteAllText(ecdsaKeyFile, ecdsaPem);
                    File.SetAttributes(ecdsaKeyFile, FileAttributes.ReadOnly);
                }
                keys["ecdsa-sha2-nistp256"] = ecdsaPem;

                // Ed25519 support is not included here due to .NET/FxSsh limitations

                return keys;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to load or generate host keys", ex);
            }
        }

        // Helper to encode PEM
        private static string PemEncode(string label, byte[] data)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"-----BEGIN {label}-----");
            string base64 = Convert.ToBase64String(data);
            for (int i = 0; i < base64.Length; i += 64)
            {
                builder.AppendLine(base64.Substring(i, Math.Min(64, base64.Length - i)));
            }
            builder.AppendLine($"-----END {label}-----");
            return builder.ToString();
        }
    }
}