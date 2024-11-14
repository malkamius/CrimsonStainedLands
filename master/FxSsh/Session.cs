using FxSsh.Algorithms;
using FxSsh.Messages;
using FxSsh.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace FxSsh
{
    public class Session : IDynamicInvoker
    {
        private const byte CarriageReturn = 0x0d;
        private const byte LineFeed = 0x0a;
        internal const int MaximumSshPacketSize = LocalChannelDataPacketSize;
        internal const int InitialLocalWindowSize = LocalChannelDataPacketSize * 32;
        internal const int LocalChannelDataPacketSize = 1024 * 32;

        private static readonly RandomNumberGenerator _rng = new RNGCryptoServiceProvider();
        private static readonly Dictionary<byte, Type> _messagesMetadata;
        internal static readonly Dictionary<string, Func<KexAlgorithm>> _keyExchangeAlgorithms =
            new Dictionary<string, Func<KexAlgorithm>>();
        internal static readonly Dictionary<string, Func<string, PublicKeyAlgorithm>> _publicKeyAlgorithms =
            new Dictionary<string, Func<string, PublicKeyAlgorithm>>();
        internal static readonly Dictionary<string, Func<CipherInfo>> _encryptionAlgorithms =
            new Dictionary<string, Func<CipherInfo>>();
        internal static readonly Dictionary<string, Func<HmacInfo>> _hmacAlgorithms =
            new Dictionary<string, Func<HmacInfo>>();
        internal static readonly Dictionary<string, Func<CompressionAlgorithm>> _compressionAlgorithms =
            new Dictionary<string, Func<CompressionAlgorithm>>();

        private readonly object _locker = new object();
        public Socket Socket { get; private set; }
#if DEBUG
        private readonly TimeSpan _timeout = TimeSpan.FromDays(1);
#else
        private readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);
#endif
        private readonly Dictionary<string, string> _hostKey;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private uint _outboundPacketSequence;
        private uint _inboundPacketSequence;
        private uint _outboundFlow;
        private uint _inboundFlow;
        private Algorithms _algorithms = null;
        private ExchangeContext _exchangeContext = null;
        private List<SshService> _services = new List<SshService>();
        private ConcurrentQueue<Message> _blockedMessages = new ConcurrentQueue<Message>();
        private SemaphoreSlim _blockedMessagesSemaphore = new SemaphoreSlim(1);

        public string ServerVersion { get; private set; }
        public string ClientVersion { get; private set; }
        public byte[] SessionId { get; private set; }

        public T GetService<T>() where T : SshService
        {
            return (T)_services.FirstOrDefault(x => x is T);
        }

        static Session()
        {
            _keyExchangeAlgorithms.Add("diffie-hellman-group18-sha512", () => new DiffieHellmanGroupSha512(new DiffieHellman(8192)));
            _keyExchangeAlgorithms.Add("diffie-hellman-group16-sha512", () => new DiffieHellmanGroupSha512(new DiffieHellman(4096)));
            _keyExchangeAlgorithms.Add("diffie-hellman-group14-sha256", () => new DiffieHellmanGroupSha256(new DiffieHellman(2048)));
            _keyExchangeAlgorithms.Add("diffie-hellman-group14-sha1", () => new DiffieHellmanGroupSha1(new DiffieHellman(2048)));
            _keyExchangeAlgorithms.Add("diffie-hellman-group1-sha1", () => new DiffieHellmanGroupSha1(new DiffieHellman(1024)));

            _publicKeyAlgorithms.Add("ssh-rsa", x => new RsaKey(x));
            _publicKeyAlgorithms.Add("ssh-dss", x => new DssKey(x));

            _encryptionAlgorithms.Add("aes128-ctr", () => new CipherInfo(new AesCryptoServiceProvider(), 128, CipherModeEx.CTR));
            _encryptionAlgorithms.Add("aes192-ctr", () => new CipherInfo(new AesCryptoServiceProvider(), 192, CipherModeEx.CTR));
            _encryptionAlgorithms.Add("aes256-ctr", () => new CipherInfo(new AesCryptoServiceProvider(), 256, CipherModeEx.CTR));
            _encryptionAlgorithms.Add("aes128-cbc", () => new CipherInfo(new AesCryptoServiceProvider(), 128, CipherModeEx.CBC));
            _encryptionAlgorithms.Add("3des-cbc", () => new CipherInfo(new TripleDESCryptoServiceProvider(), 192, CipherModeEx.CBC));
            _encryptionAlgorithms.Add("aes192-cbc", () => new CipherInfo(new AesCryptoServiceProvider(), 192, CipherModeEx.CBC));
            _encryptionAlgorithms.Add("aes256-cbc", () => new CipherInfo(new AesCryptoServiceProvider(), 256, CipherModeEx.CBC));

            _hmacAlgorithms.Add("hmac-md5", () => new HmacInfo(new HMACMD5(), 128));
            _hmacAlgorithms.Add("hmac-sha1", () => new HmacInfo(new HMACSHA1(), 160));

            _compressionAlgorithms.Add("none", () => new NoCompression());

            _messagesMetadata = (from t in typeof(Message).Assembly.GetTypes()
                                 let attrib = (MessageAttribute)t.GetCustomAttributes(typeof(MessageAttribute), false).FirstOrDefault()
                                 where attrib != null
                                 select new { attrib.Number, Type = t })
                                 .ToDictionary(x => x.Number, x => x.Type);
        }

        public Session(Socket socket, Dictionary<string, string> hostKey, string serverBanner)
        {
            Contract.Requires(socket != null);
            Contract.Requires(hostKey != null);

            Socket = socket;
            _hostKey = hostKey.ToDictionary(s => s.Key, s => s.Value);
            ServerVersion = serverBanner;
        }

        public event EventHandler<EventArgs> Disconnected;
        public event EventHandler<SshService> ServiceRegistered;
        public event EventHandler<KeyExchangeArgs> KeysExchanged;

        public async Task EstablishConnectionAsync()
        {
            if (!Socket.Connected)
            {
                return;
            }

            SetSocketOptions();

            await SocketWriteProtocolVersionAsync();
            ClientVersion = await SocketReadProtocolVersionAsync();
            if (!Regex.IsMatch(ClientVersion, "SSH-2.0-.+"))
            {
                throw new SshConnectionException(
                    string.Format("Not supported for client SSH version {0}. This server only supports SSH v2.0.", ClientVersion),
                    DisconnectReason.ProtocolVersionNotSupported);
            }

            await ConsiderReExchangeAsync(true);

            try
            {
                while (Socket != null && Socket.Connected)
                {
                    var message = await ReceiveMessageAsync();
                    if (message is UnknownMessage unknownMessage)
                        await SendMessageAsync(unknownMessage.MakeUnimplementedMessage());
                    else
                        await HandleMessageCoreAsync(message);
                }
            }
            finally
            {
                foreach (var service in _services)
                {
                    service.CloseService();
                }

                Disconnected?.Invoke(this, EventArgs.Empty);
            }
        }

        public async Task DisconnectAsync(DisconnectReason reason = DisconnectReason.ByApplication, string description = "Connection terminated by the server.")
        {
            if (reason == DisconnectReason.ByApplication)
            {
                var message = new DisconnectMessage(reason, description);
                await TrySendMessageAsync(message);
            }

            try
            {
                Socket.Shutdown(SocketShutdown.Both);
                Socket.Close();
                Socket.Dispose();
            }
            catch { }

            Disconnected?.Invoke(this, EventArgs.Empty);
        }

        #region Socket operations
        private void SetSocketOptions()
        {
            const int socketBufferSize = 2 * MaximumSshPacketSize;
            Socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
            Socket.LingerState = new LingerOption(enable: false, seconds: 0);
            Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, socketBufferSize);
            Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, socketBufferSize);
        }

        private async Task<string> SocketReadProtocolVersionAsync()
        {
            var buffer = new byte[255];
            var pos = 0;

            using var networkStream = new NetworkStream(Socket);

            while (pos < buffer.Length)
            {
                var rbyte = await networkStream.ReadByteAsync(_cancellationTokenSource.Token);
                if (rbyte == -1)
                {
                    throw new SshConnectionException("Could't read the protocol version", DisconnectReason.ProtocolError);
                }

                buffer[pos] = (byte)rbyte;

                if (pos > 0)
                {
                    if (buffer[pos - 1] == CarriageReturn && buffer[pos] == LineFeed)
                    {
                        return Encoding.ASCII.GetString(buffer, 0, pos - 1);
                    }
                    else if (buffer[pos] == LineFeed) // Non-RFC case
                    {
                        return Encoding.ASCII.GetString(buffer, 0, pos);
                    }
                }
                pos++;
            }
            throw new SshConnectionException("Could't read the protocol version", DisconnectReason.ProtocolError);
        }

        private async Task SocketWriteProtocolVersionAsync()
        {
            await SocketWriteAsync(Encoding.ASCII.GetBytes(ServerVersion + "\r\n"));
        }

        private async Task<byte[]> SocketReadAsync(int length)
        {
            var buffer = new byte[length];
            var pos = 0;
            var msSinceLastData = 0;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token);
            cts.CancelAfter(_timeout);

            try
            {
                while (pos < length)
                {
                    var bytesRead = await Socket.ReceiveAsync(new ArraySegment<byte>(buffer, pos, length - pos), SocketFlags.None);

                    if (!Socket.Connected || bytesRead == 0)
                    {
                        throw new SshConnectionException("Connection lost", DisconnectReason.ConnectionLost);
                    }

                    if (bytesRead == 0 && Socket.Available == 0)
                    {
                        if (msSinceLastData >= _timeout.TotalMilliseconds)
                        {
                            throw new SshConnectionException("Connection lost", DisconnectReason.ConnectionLost);
                        }

                        msSinceLastData += 50;
                        await Task.Delay(50, cts.Token);
                    }
                    else
                    {
                        msSinceLastData = 0;
                    }

                    pos += bytesRead;
                }
            }
            catch (OperationCanceledException)
            {
                throw new SshConnectionException($"Socket operation has timed out after {_timeout.TotalMilliseconds:F0} milliseconds.",
                    DisconnectReason.ConnectionLost);
            }

            return buffer;
        }

        private async Task SocketWriteAsync(byte[] data)
        {
            var pos = 0;
            var length = data.Length;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token);
            cts.CancelAfter(_timeout);

            try
            {
                while (pos < length)
                {
                    var bytesSent = await Socket.SendAsync(new ArraySegment<byte>(data, pos, length - pos), SocketFlags.None);
                    pos += bytesSent;
                }
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.WouldBlock ||
                    ex.SocketErrorCode == SocketError.IOPending ||
                    ex.SocketErrorCode == SocketError.NoBufferSpaceAvailable)
                {
                    await Task.Delay(30, cts.Token);
                }
                else
                    throw new SshConnectionException("Connection lost", DisconnectReason.ConnectionLost);
            }
            catch (OperationCanceledException)
            {
                throw new SshConnectionException($"Socket operation has timed out after {_timeout.TotalMilliseconds:F0} milliseconds.",
                    DisconnectReason.ConnectionLost);
            }
        }

        #endregion

        #region Message operations
        private async Task<Message> ReceiveMessageAsync()
        {
            var useAlg = _algorithms != null;

            var blockSize = (byte)(useAlg ? Math.Max(8, _algorithms.ClientEncryption.BlockBytesSize) : 8);
            var firstBlock = await SocketReadAsync(blockSize);
            if (useAlg)
                firstBlock = _algorithms.ClientEncryption.Transform(firstBlock);

            var packetLength = firstBlock[0] << 24 | firstBlock[1] << 16 | firstBlock[2] << 8 | firstBlock[3];
            var paddingLength = firstBlock[4];
            var bytesToRead = packetLength - blockSize + 4;

            var followingBlocks = await SocketReadAsync(bytesToRead);
            if (useAlg)
                followingBlocks = _algorithms.ClientEncryption.Transform(followingBlocks);

            var fullPacket = firstBlock.Concat(followingBlocks).ToArray();
            var data = fullPacket.Skip(5).Take(packetLength - paddingLength).ToArray();
            if (useAlg)
            {
                var clientMac = await SocketReadAsync(_algorithms.ClientHmac.DigestLength);
                var mac = ComputeHmac(_algorithms.ClientHmac, fullPacket, _inboundPacketSequence);
                if (!clientMac.SequenceEqual(mac))
                {
                    throw new SshConnectionException("Invalid MAC", DisconnectReason.MacError);
                }

                data = _algorithms.ClientCompression.Decompress(data);
            }

            var typeNumber = data[0];
            var implemented = _messagesMetadata.ContainsKey(typeNumber);
            var message = implemented
                ? (Message)Activator.CreateInstance(_messagesMetadata[typeNumber])
                : new UnknownMessage { SequenceNumber = _inboundPacketSequence, UnknownMessageType = typeNumber };

            if (implemented)
                message.Load(data);

            lock (_locker)
            {
                _inboundPacketSequence++;
                _inboundFlow += (uint)packetLength;
            }

            await ConsiderReExchangeAsync();

            return message;
        }

        internal async Task SendMessageAsync(Message message)
        {
            Contract.Requires(message != null);

            if (_exchangeContext != null
                && message.MessageType > 4 && (message.MessageType < 20 || message.MessageType > 49))
            {
                _blockedMessages.Enqueue(message);
                return;
            }

            await _blockedMessagesSemaphore.WaitAsync();
            try
            {
                await SendMessageInternalAsync(message);
            }
            finally
            {
                _blockedMessagesSemaphore.Release();
            }
        }

        private async Task SendMessageInternalAsync(Message message)
        {
            var useAlg = _algorithms != null;

            var blockSize = (byte)(useAlg ? Math.Max(8, _algorithms.ServerEncryption.BlockBytesSize) : 8);
            var payload = message.GetPacket();
            if (useAlg)
                payload = _algorithms.ServerCompression.Compress(payload);

            var paddingLength = (byte)(blockSize - (payload.Length + 5) % blockSize);
            if (paddingLength < 4)
                paddingLength += blockSize;

            var packetLength = (uint)payload.Length + paddingLength + 1;

            var padding = new byte[paddingLength];
            _rng.GetBytes(padding);

            using (var worker = new SshDataWorker())
            {
                worker.Write(packetLength);
                worker.Write(paddingLength);
                worker.Write(payload);
                worker.Write(padding);

                payload = worker.ToByteArray();
            }

            if (useAlg)
            {
                var mac = ComputeHmac(_algorithms.ServerHmac, payload, _outboundPacketSequence);
                payload = _algorithms.ServerEncryption.Transform(payload).Concat(mac).ToArray();
            }

            await SocketWriteAsync(payload);

            lock (_locker)
            {
                _outboundPacketSequence++;
                _outboundFlow += packetLength;
            }

            await ConsiderReExchangeAsync();
        }

        private async Task ConsiderReExchangeAsync(bool force = false)
        {
            var kex = false;
            lock (_locker)
                if (_exchangeContext == null
                    && (force || _inboundFlow + _outboundFlow > 1024 * 1024 * 512)) // 0.5 GiB
                {
                    _exchangeContext = new ExchangeContext();
                    kex = true;
                }

            if (kex)
            {
                var kexInitMessage = LoadKexInitMessage();
                _exchangeContext.ServerKexInitPayload = kexInitMessage.GetPacket();

                await SendMessageAsync(kexInitMessage);
            }
        }

        private async Task ContinueSendBlockedMessagesAsync()
        {
            if (_blockedMessages.Count > 0)
            {
                while (_blockedMessages.TryDequeue(out Message message))
                {
                    await SendMessageInternalAsync(message);
                }
            }
        }

        internal async Task<bool> TrySendMessageAsync(Message message)
        {
            Contract.Requires(message != null);

            try
            {
                await SendMessageAsync(message);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private Message LoadKexInitMessage()
        {
            var message = new KeyExchangeInitMessage();
            message.KeyExchangeAlgorithms = _keyExchangeAlgorithms.Keys.ToArray();
            message.ServerHostKeyAlgorithms = _publicKeyAlgorithms.Keys.ToArray();
            message.EncryptionAlgorithmsClientToServer = _encryptionAlgorithms.Keys.ToArray();
            message.EncryptionAlgorithmsServerToClient = _encryptionAlgorithms.Keys.ToArray();
            message.MacAlgorithmsClientToServer = _hmacAlgorithms.Keys.ToArray();
            message.MacAlgorithmsServerToClient = _hmacAlgorithms.Keys.ToArray();
            message.CompressionAlgorithmsClientToServer = _compressionAlgorithms.Keys.ToArray();
            message.CompressionAlgorithmsServerToClient = _compressionAlgorithms.Keys.ToArray();
            message.LanguagesClientToServer = new[] { "" };
            message.LanguagesServerToClient = new[] { "" };
            message.FirstKexPacketFollows = false;
            message.Reserved = 0;

            return message;
        }
        #endregion

        #region Handle messages
        private async Task HandleMessageCoreAsync(Message message)
        {
            // Note: IDynamicInvoker will need to be updated to support async methods
            await this.InvokeHandleMessageAsync(message);
        }

        private async Task InvokeHandleMessageAsync(Message message)
        {
            this.InvokeHandleMessage(message);
        }

        private async Task HandleMessageAsync(DisconnectMessage message)
        {
            await DisconnectAsync(message.ReasonCode, message.Description);
        }

        private async Task HandleMessageAsync(KeyExchangeInitMessage message)
        {
            await ConsiderReExchangeAsync(true);

            KeysExchanged?.Invoke(this, new KeyExchangeArgs(this)
            {
                CompressionAlgorithmsClientToServer = message.CompressionAlgorithmsClientToServer,
                CompressionAlgorithmsServerToClient = message.CompressionAlgorithmsServerToClient,
                EncryptionAlgorithmsClientToServer = message.EncryptionAlgorithmsClientToServer,
                EncryptionAlgorithmsServerToClient = message.EncryptionAlgorithmsServerToClient,
                KeyExchangeAlgorithms = message.KeyExchangeAlgorithms,
                LanguagesClientToServer = message.LanguagesClientToServer,
                LanguagesServerToClient = message.LanguagesServerToClient,
                MacAlgorithmsClientToServer = message.MacAlgorithmsClientToServer,
                MacAlgorithmsServerToClient = message.MacAlgorithmsServerToClient,
                ServerHostKeyAlgorithms = message.ServerHostKeyAlgorithms
            });

            _exchangeContext.KeyExchange = ChooseAlgorithm(_keyExchangeAlgorithms.Keys.ToArray(), message.KeyExchangeAlgorithms);
            _exchangeContext.PublicKey = ChooseAlgorithm(_publicKeyAlgorithms.Keys.ToArray(), message.ServerHostKeyAlgorithms);
            _exchangeContext.ClientEncryption = ChooseAlgorithm(_encryptionAlgorithms.Keys.ToArray(), message.EncryptionAlgorithmsClientToServer);
            _exchangeContext.ServerEncryption = ChooseAlgorithm(_encryptionAlgorithms.Keys.ToArray(), message.EncryptionAlgorithmsServerToClient);
            _exchangeContext.ClientHmac = ChooseAlgorithm(_hmacAlgorithms.Keys.ToArray(), message.MacAlgorithmsClientToServer);
            _exchangeContext.ServerHmac = ChooseAlgorithm(_hmacAlgorithms.Keys.ToArray(), message.MacAlgorithmsServerToClient);
            _exchangeContext.ClientCompression = ChooseAlgorithm(_compressionAlgorithms.Keys.ToArray(), message.CompressionAlgorithmsClientToServer);
            _exchangeContext.ServerCompression = ChooseAlgorithm(_compressionAlgorithms.Keys.ToArray(), message.CompressionAlgorithmsServerToClient);

            _exchangeContext.ClientKexInitPayload = message.GetPacket();
        }

        private async Task HandleMessageAsync(KeyExchangeDhInitMessage message)
        {
            var kexAlg = _keyExchangeAlgorithms[_exchangeContext.KeyExchange]();
            var hostKeyAlg = _publicKeyAlgorithms[_exchangeContext.PublicKey](_hostKey[_exchangeContext.PublicKey].ToString());
            var clientCipher = _encryptionAlgorithms[_exchangeContext.ClientEncryption]();
            var serverCipher = _encryptionAlgorithms[_exchangeContext.ServerEncryption]();
            var serverHmac = _hmacAlgorithms[_exchangeContext.ServerHmac]();
            var clientHmac = _hmacAlgorithms[_exchangeContext.ClientHmac]();

            var clientExchangeValue = message.E;
            var serverExchangeValue = kexAlg.CreateKeyExchange();
            var sharedSecret = kexAlg.DecryptKeyExchange(clientExchangeValue);
            var hostKeyAndCerts = hostKeyAlg.CreateKeyAndCertificatesData();
            var exchangeHash = ComputeExchangeHash(kexAlg, hostKeyAndCerts, clientExchangeValue, serverExchangeValue, sharedSecret);

            if (SessionId == null)
                SessionId = exchangeHash;

            var clientCipherIV = ComputeEncryptionKey(kexAlg, exchangeHash, clientCipher.BlockSize >> 3, sharedSecret, 'A');
            var serverCipherIV = ComputeEncryptionKey(kexAlg, exchangeHash, serverCipher.BlockSize >> 3, sharedSecret, 'B');
            var clientCipherKey = ComputeEncryptionKey(kexAlg, exchangeHash, clientCipher.KeySize >> 3, sharedSecret, 'C');
            var serverCipherKey = ComputeEncryptionKey(kexAlg, exchangeHash, serverCipher.KeySize >> 3, sharedSecret, 'D');
            var clientHmacKey = ComputeEncryptionKey(kexAlg, exchangeHash, clientHmac.KeySize >> 3, sharedSecret, 'E');
            var serverHmacKey = ComputeEncryptionKey(kexAlg, exchangeHash, serverHmac.KeySize >> 3, sharedSecret, 'F');

            _exchangeContext.NewAlgorithms = new Algorithms
            {
                KeyExchange = kexAlg,
                PublicKey = hostKeyAlg,
                ClientEncryption = clientCipher.Cipher(clientCipherKey, clientCipherIV, false),
                ServerEncryption = serverCipher.Cipher(serverCipherKey, serverCipherIV, true),
                ClientHmac = clientHmac.Hmac(clientHmacKey),
                ServerHmac = serverHmac.Hmac(serverHmacKey),
                ClientCompression = _compressionAlgorithms[_exchangeContext.ClientCompression](),
                ServerCompression = _compressionAlgorithms[_exchangeContext.ServerCompression](),
            };

            var reply = new KeyExchangeDhReplyMessage
            {
                HostKey = hostKeyAndCerts,
                F = serverExchangeValue,
                Signature = hostKeyAlg.CreateSignatureData(exchangeHash),
            };

            await SendMessageAsync(reply);
            await SendMessageAsync(new NewKeysMessage());
        }

        private async Task HandleMessageAsync(NewKeysMessage message)
        {
            await _blockedMessagesSemaphore.WaitAsync();
            try
            {
                lock (_locker)
                {
                    _inboundFlow = 0;
                    _outboundFlow = 0;
                    _algorithms = _exchangeContext.NewAlgorithms;
                    _exchangeContext = null;
                }

                await ContinueSendBlockedMessagesAsync();
            }
            finally
            {
                _blockedMessagesSemaphore.Release();
            }
        }

        private Task HandleMessageAsync(UnimplementedMessage message)
        {
            // Nothing to do here
            return Task.CompletedTask;
        }

        private async Task HandleMessageAsync(ServiceRequestMessage message)
        {
            SshService service = RegisterService(message.ServiceName);
            if (service != null)
            {
                await SendMessageAsync(new ServiceAcceptMessage(message.ServiceName));
                return;
            }
            throw new SshConnectionException(string.Format("Service \"{0}\" not available.", message.ServiceName),
                DisconnectReason.ServiceNotAvailable);
        }

        private async Task HandleMessageAsync(UserauthServiceMessage message)
        {
            var service = GetService<UserauthService>();
            if (service != null)
                await service.HandleMessageCoreAsync(message);
        }

        private async Task HandleMessageAsync(ConnectionServiceMessage message)
        {
            var service = GetService<ConnectionService>();
            if (service != null)
                await service.HandleMessageCoreAsync(message);
        }
        #endregion

        private string ChooseAlgorithm(string[] serverAlgorithms, string[] clientAlgorithms)
        {
            foreach (var client in clientAlgorithms)
                foreach (var server in serverAlgorithms)
                    if (client == server)
                        return client;

            throw new SshConnectionException("Failed to negotiate algorithm.", DisconnectReason.KeyExchangeFailed);
        }

        private byte[] ComputeExchangeHash(KexAlgorithm kexAlg, byte[] hostKeyAndCerts, byte[] clientExchangeValue, byte[] serverExchangeValue, byte[] sharedSecret)
        {
            using (var worker = new SshDataWorker())
            {
                worker.Write(ClientVersion, Encoding.ASCII);
                worker.Write(ServerVersion, Encoding.ASCII);
                worker.WriteBinary(_exchangeContext.ClientKexInitPayload);
                worker.WriteBinary(_exchangeContext.ServerKexInitPayload);
                worker.WriteBinary(hostKeyAndCerts);
                worker.WriteMpint(clientExchangeValue);
                worker.WriteMpint(serverExchangeValue);
                worker.WriteMpint(sharedSecret);

                return kexAlg.ComputeHash(worker.ToByteArray());
            }
        }

        private byte[] ComputeEncryptionKey(KexAlgorithm kexAlg, byte[] exchangeHash, int blockSize, byte[] sharedSecret, char letter)
        {
            var keyBuffer = new byte[blockSize];
            var keyBufferIndex = 0;
            var currentHashLength = 0;
            byte[] currentHash = null;

            while (keyBufferIndex < blockSize)
            {
                using (var worker = new SshDataWorker())
                {
                    worker.WriteMpint(sharedSecret);
                    worker.Write(exchangeHash);

                    if (currentHash == null)
                    {
                        worker.Write((byte)letter);
                        worker.Write(SessionId);
                    }
                    else
                    {
                        worker.Write(currentHash);
                    }

                    currentHash = kexAlg.ComputeHash(worker.ToByteArray());
                }

                currentHashLength = Math.Min(currentHash.Length, blockSize - keyBufferIndex);
                Array.Copy(currentHash, 0, keyBuffer, keyBufferIndex, currentHashLength);

                keyBufferIndex += currentHashLength;
            }

            return keyBuffer;
        }

        private byte[] ComputeHmac(HmacAlgorithm alg, byte[] payload, uint seq)
        {
            using (var worker = new SshDataWorker())
            {
                worker.Write(seq);
                worker.Write(payload);

                return alg.ComputeHash(worker.ToByteArray());
            }
        }

        internal SshService RegisterService(string serviceName, UserauthArgs auth = null)
        {
            Contract.Requires(serviceName != null);

            SshService service = null;
            switch (serviceName)
            {
                case "ssh-userauth":
                    if (GetService<UserauthService>() == null)
                        service = new UserauthService(this);
                    break;
                case "ssh-connection":
                    if (auth != null && GetService<ConnectionService>() == null)
                        service = new ConnectionService(this, auth);
                    break;
            }
            if (service != null)
            {
                ServiceRegistered?.Invoke(this, service);
                _services.Add(service);
            }
            return service;
        }
        
        #region Helper Classes
        private class Algorithms
        {
            public KexAlgorithm KeyExchange;
            public PublicKeyAlgorithm PublicKey;
            public EncryptionAlgorithm ClientEncryption;
            public EncryptionAlgorithm ServerEncryption;
            public HmacAlgorithm ClientHmac;
            public HmacAlgorithm ServerHmac;
            public CompressionAlgorithm ClientCompression;
            public CompressionAlgorithm ServerCompression;
        }

        private class ExchangeContext
        {
            public string KeyExchange;
            public string PublicKey;
            public string ClientEncryption;
            public string ServerEncryption;
            public string ClientHmac;
            public string ServerHmac;
            public string ClientCompression;
            public string ServerCompression;

            public byte[] ClientKexInitPayload;
            public byte[] ServerKexInitPayload;

            public Algorithms NewAlgorithms;
        }
        #endregion
    }

}