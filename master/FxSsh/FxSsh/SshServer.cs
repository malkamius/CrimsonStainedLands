﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace FxSsh
{
    public class SshServer : IDisposable
    {
        private readonly object _lock = new();
        private readonly List<Session> _sessions = [];
        private readonly Dictionary<string, string> _hostKey = [];
        private bool _isDisposed;
        private bool _started;
        private TcpListener _listenser = null;

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
        public event EventHandler<Exception> ExceptionRaised;

        internal void RaiseException(Exception ex)
        {
            ExceptionRaised?.Invoke(this, ex);
        }

        public void Start()
        {
            lock (_lock)
            {
                CheckDisposed();
                if (_started)
                    throw new InvalidOperationException("The server is already started.");

                _listenser = StartingInfo.LocalAddress == IPAddress.IPv6Any
                    ? TcpListener.Create(StartingInfo.Port) // dual stack
                    : new TcpListener(StartingInfo.LocalAddress, StartingInfo.Port);
                _listenser.ExclusiveAddressUse = false;
                _listenser.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _listenser.Start();
                BeginAcceptSocket();

                _started = true;
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                CheckDisposed();
                if (!_started)
                    throw new InvalidOperationException("The server is not started.");

                _listenser.Stop();

                _isDisposed = true;
                _started = false;

                foreach (var session in _sessions.ToArray())
                {
                    try
                    {
                        session.Disconnect();
                    }
                    catch
                    {
                    }
                }
            }
        }

        public void AddHostKey(string type, string xml)
        {
            Contract.Requires(type != null);
            Contract.Requires(xml != null);

            if (!_hostKey.ContainsKey(type))
                _hostKey.Add(type, xml);
        }

        private void BeginAcceptSocket()
        {
            try
            {
                _listenser.BeginAcceptSocket(AcceptSocket, null);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch
            {
                if (_started)
                    BeginAcceptSocket();
            }
        }

        private void AcceptSocket(IAsyncResult ar)
        {
            try
            {
                var socket = _listenser.EndAcceptSocket(ar);
                Task.Run(() =>
                {
                    var session = new Session(this, socket, _hostKey, StartingInfo.ServerBanner);
                    session.Disconnected += (ss, ee) =>
                    {
                        lock (_lock) _sessions.Remove(session);
                    };
                    lock (_lock)
                        _sessions.Add(session);
                    try
                    {
                        ConnectionAccepted?.Invoke(this, session);
                        session.EstablishConnection();
                    }
                    catch (SshConnectionException ex)
                    {
                        session.Disconnect(ex.DisconnectReason, ex.Message);
                        ExceptionRaised?.Invoke(this, ex);
                    }
                    catch (Exception ex)
                    {
                        session.Disconnect();
                        ExceptionRaised?.Invoke(this, ex);
                    }
                });
            }
            catch
            {
            }
            finally
            {
                BeginAcceptSocket();
            }
        }

        private void CheckDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
        }

        #region IDisposable
        public void Dispose()
        {
            lock (_lock)
            {
                if (_isDisposed)
                    return;
                Stop();
            }
        }
        #endregion
    }
}
