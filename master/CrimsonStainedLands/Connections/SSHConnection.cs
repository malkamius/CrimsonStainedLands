using System;
using System.Net;
using System.Net.Sockets;
using FxSsh;
using FxSsh.Services;
using System.Threading.Tasks.Dataflow;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;

namespace CrimsonStainedLands.Connections
{
    public class SSHConnection : BaseConnection
    {
        private readonly Session session;
        private readonly BufferBlock<string> inputBuffer;
        private readonly object statusLock = new object();
        private readonly object lineBufferLock = new object();
        private SemaphoreSlim channelLock = new SemaphoreSlim(1, 1);
        private volatile ConnectionStatus currentStatus;
        private Channel currentChannel;
        private StringBuilder currentLineBuffer;

        public string Username { get; set; }
        public string Password { get; set; }
        public ConnectionManager Manager { get; }

        public Channel Channel
        {
            get => currentChannel;
            set
            {
                channelLock.Wait();
                try
                {
                    currentChannel = value;
                }
                finally
                {
                    channelLock.Release();
                }
            }
        }

        public SSHConnection(ConnectionManager manager, Session session) : base(null)
        {
            Manager = manager ?? throw new ArgumentNullException(nameof(manager));
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.Socket = session?.Socket;
            this.RemoteEndPoint = this.Socket.RemoteEndPoint;

            inputBuffer = new BufferBlock<string>(new DataflowBlockOptions
            {
                BoundedCapacity = 1000,
                EnsureOrdered = true
            });

            currentLineBuffer = new StringBuilder();
            Status = ConnectionStatus.Authenticating;
        }

        public override ConnectionStatus Status
        {
            get => currentStatus;
            set
            {
                lock (statusLock)
                {
                    currentStatus = value;
                }
            }
        }

        public override byte[] Read()
        {
            try
            {
                if (inputBuffer.TryReceive(out var line))
                {
                    return Encoding.UTF8.GetBytes(line);
                }
                return null;
            }
            catch (Exception ex)
            {
                Game.log($"SSH connection read error: {ex.Message}");
                return null;
            }
        }

        public override int Write(byte[] data)
        {
            if (data == null || data.Length == 0)
                return 0;

            try
            {
                var channel = Channel;
                if (channel != null)
                {
                    channelLock.Wait();
                    try
                    {
                        channel.SendDataAsync(data).Wait();
                        return data.Length;
                    }
                    finally
                    {
                        channelLock.Release();
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                Game.log($"SSH connection write error: {ex.Message}");
                Status = ConnectionStatus.Disconnected;
                return 0;
            }
        }

        public async Task HandleReceivedDataAsync(byte[] data)
        {
            if (data == null)
                return;

            try
            {
                lock (lineBufferLock)
                {
                    foreach (byte b in data)
                    {
                        if (b == 8) // Backspace
                        {
                            if (currentLineBuffer.Length > 0)
                            {
                                currentLineBuffer.Length--;
                                Write(new byte[] { 8, 32, 8 }); // Backspace, space, backspace to clear character
                            }
                        }
                        else if (b == 13 || b == 10) // CR or LF
                        {
                            //if (currentLineBuffer.Length > 0)
                            //{
                                // Post the current line to the input buffer
                                var line = currentLineBuffer.ToString();
                                //using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                                inputBuffer.Post(line + "\n");
                                currentLineBuffer.Clear();
                            //}
                            
                            Write(new byte[] { 13, 10 }); // Send CRLF for proper line ending
                        }
                        else
                        {
                            currentLineBuffer.Append((char)b);
                            Write(new byte[] { b }); // Echo the character
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Game.log("SSH connection buffer full or operation timed out");
            }
            catch (Exception ex)
            {
                Game.log($"SSH connection buffer error: {ex.Message}");
            }
        }

        public override void Cleanup()
        {
            Status = ConnectionStatus.Disconnected;

            if (channelLock != null)
            {
                try
                {
                    channelLock.Wait();
                    try
                    {
                        var channel = currentChannel;
                        if (channel != null)
                        {
                            channel.SendClose();
                            currentChannel = null;
                        }
                        else
                            session.DisconnectAsync().Wait();
                    }
                    finally
                    {
                        channelLock.Release();
                    }

                    inputBuffer.Complete();
                    lock (lineBufferLock)
                    {
                        currentLineBuffer.Clear();
                    }

                    Manager.Connections.Remove(this);
                }
                catch (Exception ex)
                {
                    Game.log($"SSH connection cleanup error: {ex.Message}");
                }
                finally
                {
                    channelLock.Dispose();
                    channelLock = null;
                }
            }
        }
    }
}