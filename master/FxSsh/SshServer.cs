using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace FxSsh
{
    public class SshServer : IDisposable, IAsyncDisposable
    {
        private readonly object _lock = new object();
        private readonly List<Session> _sessions = new List<Session>();
        private readonly Dictionary<string, string> _hostKey = new Dictionary<string, string>();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private bool _isDisposed;
        private bool _started;
        private TcpListener _listener = null;

        public SshServer()
            : this(new StartingInfo())
        { }

        public SshServer(StartingInfo info)
        {
            Contract.Requires(info != null);
            StartingInfo = info;
        }

        public StartingInfo StartingInfo { get; private set; }

        public event EventHandler<Session> ConnectionAccepted;
        public event EventHandler<Exception> ExceptionRaised; // Fixed typo in name

        public void Start()
        {
            lock (_lock)
            {
                CheckDisposed();
                if (_started)
                    throw new InvalidOperationException("The server is already started.");

                _listener = StartingInfo.LocalAddress == IPAddress.IPv6Any
                    ? TcpListener.Create(StartingInfo.Port) // dual stack
                    : new TcpListener(StartingInfo.LocalAddress, StartingInfo.Port);

                _listener.ExclusiveAddressUse = false;
                _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _listener.Start();

                // Start accepting connections asynchronously
                _ = AcceptConnectionsAsync(_cancellationTokenSource.Token);

                _started = true;
            }
        }

        public async Task StopAsync()
        {
            lock (_lock)
            {
                CheckDisposed();
                if (!_started)
                    throw new InvalidOperationException("The server is not started.");

                _cancellationTokenSource.Cancel();
                _listener.Stop();

                _isDisposed = true;
                _started = false;
            }

            // Disconnect all sessions in parallel
            var disconnectTasks = _sessions.ToArray().Select(session =>
                DisconnectSessionSafelyAsync(session));

            await Task.WhenAll(disconnectTasks);
        }

        public void Stop()
        {
            StopAsync().GetAwaiter().GetResult();
        }

        private async Task DisconnectSessionSafelyAsync(Session session)
        {
            try
            {
                await session.DisconnectAsync();
            }
            catch (Exception)
            {
                // Ignore exceptions during shutdown
            }
        }

        public void AddHostKey(string type, string xml)
        {
            Contract.Requires(type != null);
            Contract.Requires(xml != null);

            if (!_hostKey.ContainsKey(type))
                _hostKey.Add(type, xml);
        }

        private async Task AcceptConnectionsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var socket = await AcceptSocketAsync(cancellationToken);
                    if (socket != null)
                    {
                        // Handle each connection in a separate task
                        _ = HandleConnectionAsync(socket, cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception)
                {
                    if (_started && !cancellationToken.IsCancellationRequested)
                    {
                        // Brief delay before retrying on error
                        await Task.Delay(100, cancellationToken);
                        continue;
                    }
                    break;
                }
            }
        }

        private async Task<Socket> AcceptSocketAsync(CancellationToken cancellationToken)
        {
            try
            {
                return await Task.Run(async () =>
                {
                    if (_listener == null) return null;
                    return await _listener.AcceptSocketAsync();
                }, cancellationToken);
            }
            catch (ObjectDisposedException)
            {
                return null;
            }
        }

        private async Task HandleConnectionAsync(Socket socket, CancellationToken cancellationToken)
        {
            var session = new Session(socket, _hostKey, StartingInfo.ServerBanner);

            session.Disconnected += (ss, ee) =>
            {
                lock (_lock) _sessions.Remove(session);
            };

            lock (_lock)
                _sessions.Add(session);

            try
            {
                ConnectionAccepted?.Invoke(this, session);
                await session.EstablishConnectionAsync();
            }
            catch (SshConnectionException ex)
            {
                await session.DisconnectAsync(ex.DisconnectReason, ex.Message);
                ExceptionRaised?.Invoke(this, ex);
            }
            catch (Exception ex)
            {
                await session.DisconnectAsync();
                ExceptionRaised?.Invoke(this, ex);
            }
        }

        private void CheckDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
        }

        #region IDisposable and IAsyncDisposable
        public void Dispose()
        {
            DisposeAsync().GetAwaiter().GetResult();
        }

        public async ValueTask DisposeAsync()
        {
            if (_isDisposed)
                return;

            await StopAsync();

            _cancellationTokenSource.Dispose();
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}