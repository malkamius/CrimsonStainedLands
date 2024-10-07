using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace CrimsonStainedLands.Connections
{

    public class WebsocketConnection : BaseConnection
    {
        byte[] buffer = new byte[1024];

        public Encoding Encoding {get;set;}
        public WebServer Server { get; }
        public NetworkStream BaseStream {get;set;}
        
        public SslStream Stream {get;set;}

        public StringBuilder Request {get;set;} = new StringBuilder();

        private bool _useCompression = false;

        private DeflateStream _compressor;
        private DeflateStream _decompressor;

        private bool _clientNoContext = false;
        private bool _serverNoContext = false;

        public bool Upgraded = false;
        List<byte> messageBuffer = new List<byte>();

        private void Authenticated(IAsyncResult result)
        {
            try
            {
                this.Stream.EndAuthenticateAsServer(result);
                this.Stream.ReadTimeout = 1;
                this.Status = ConnectionStatus.Negotiating;

            }
            catch
            {
                this.Stream.Dispose();
                this.Status = ConnectionStatus.Disconnected;
            }
        }

        public WebsocketConnection(WebServer server, Socket socket, X509Certificate2 cert) : base(socket)
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
                        if (!Upgraded) return buffer.Take(read).ToArray();
                        else
                        {
                            messageBuffer.AddRange(buffer.Take(read));
                            var messages = new StringBuilder();
                            while (messageBuffer.Count > 0)
                            {
                                var (message, opcode, consumed) = DecodeWebSocketFrame(messageBuffer.ToArray());

                                if (consumed == 0) break; // Not enough data for a complete frame

                                messageBuffer.RemoveRange(0, consumed);

                                if (opcode == 8) // Close frame
                                {
                                    Console.WriteLine("Received close frame");
                                    await SendCloseFrameAsync();
                                    Cleanup();
                                }
                                else if (opcode == 9) // Ping frame
                                {
                                    await SendPongFrameAsync(message);
                                }
                                else if (opcode == 10) // Pong frame
                                {
                                    Console.WriteLine("Received pong");
                                }
                                else if (message.Length > 0)
                                {
                                    messages.Append(message);
                                }
                            }
                            return Encoding.UTF8.GetBytes(messages.ToString());
                        }
                    }
                    else
                    {
                        this.Cleanup();

                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            catch(IOException ex)
            {
                return null;
            }
            catch
            {
                this.Cleanup();
                
                return null;
            }
        }

        public override async Task<int> Write(byte[] data) 
        {
            try
            {

                if(data.Length > 0)
                {
                    if(Upgraded)
                        await SendWebSocketMessageAsync(data);
                    else
                    {
                        await this.Stream.WriteAsync(data);
                    }
                    return data.Length;
                    //if(data[0] == (byte) TelnetNegotiator.Options.InterpretAsCommand)
                    //{
                    //    await this.Stream.WriteAsync(data);
                    //    return data.Length;
                    //}
                    //else
                    //{
                    //    await this.Stream.WriteAsync(data);
                    //    return data.Length;
                    //}
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
            get;set;
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

        public async Task HandleWebSocketUpgradeAsync(string request)
        {
            if (Stream != null)
            {
                Game.log("Handling WebSocket upgrade request");

                // Extract the Sec-WebSocket-Key
                string secWebSocketKey = Regex.Match(request, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim();

                // Check if the client supports permessage-deflate
                _useCompression = request.Contains("permessage-deflate");

                // Compute the Sec-WebSocket-Accept value
                string secWebSocketAccept = ComputeWebSocketAcceptValue(secWebSocketKey);

                // Prepare and send the WebSocket upgrade response
                string response = "HTTP/1.1 101 Switching Protocols\r\n" +
                                "Connection: Upgrade\r\n" +
                                "Upgrade: websocket\r\n" +
                                $"Sec-WebSocket-Accept: {secWebSocketAccept}\r\n";

                if (_useCompression)
                {
                    response += "Sec-WebSocket-Extensions: permessage-deflate; server_no_context_takeover; client_no_context_takeover\r\n";
                    //response += "Sec-WebSocket-Extensions: permessage-deflate;\r\n";
                    _clientNoContext = true;
                    _serverNoContext = true;
                    InitializeCompression();

                }

                response += "\r\n";

                byte[] responseBytes = Encoding.ASCII.GetBytes(response);
                await this.Stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                await this.Stream.FlushAsync();

                Game.log("WebSocket connection established" + (_useCompression ? " with compression" : ""));
                Upgraded = true;
                Server.ConnectionConnectedCallback(this);
            }
        }

        private string ComputeWebSocketAcceptValue(string secWebSocketKey)
        {
            const string Guid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            string concatenated = secWebSocketKey + Guid;
            byte[] sha1Hash = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(concatenated));
            return Convert.ToBase64String(sha1Hash);
        }

        private void InitializeCompression()
        {
            var compressorStream = new MemoryStream();
            _compressor = new DeflateStream(compressorStream, CompressionMode.Compress, true);

            var decompressorStream = new MemoryStream();
            _decompressor = new DeflateStream(decompressorStream, CompressionMode.Decompress, true);
        }

        private (string message, int opcode, int consumed) DecodeWebSocketFrame(byte[] buffer)
        {
            if (buffer.Length < 2) return (string.Empty, 0, 0);

            bool fin = (buffer[0] & 0b10000000) != 0;
            bool rsv1 = (buffer[0] & 0b01000000) != 0; // Compression flag
            int opcode = buffer[0] & 0b00001111;
            bool mask = (buffer[1] & 0b10000000) != 0;
            long msglen = buffer[1] & 0b01111111;
            int offset = 2;

            Console.WriteLine($"Frame info: FIN={fin}, RSV1={rsv1}, Opcode={opcode}, MASK={mask}, Initial length={msglen}");

            if (msglen == 126)
            {
                if (buffer.Length < 4) return (string.Empty, 0, 0);
                msglen = BitConverter.ToUInt16(new byte[] { buffer[3], buffer[2] }, 0);
                offset = 4;
            }
            else if (msglen == 127)
            {
                if (buffer.Length < 10) return (string.Empty, 0, 0);
                msglen = BitConverter.ToInt64(new byte[] { buffer[9], buffer[8], buffer[7], buffer[6], buffer[5], buffer[4], buffer[3], buffer[2] }, 0);
                offset = 10;
            }

            Console.WriteLine($"Message length: {msglen}, Data offset: {offset}");

            if (mask && buffer.Length < offset + 4) return (string.Empty, 0, 0);
            if (buffer.Length < offset + msglen) return (string.Empty, 0, 0);

            byte[] decoded = new byte[msglen];
            if (mask)
            {
                byte[] masks = new byte[4] { buffer[offset], buffer[offset + 1], buffer[offset + 2], buffer[offset + 3] };
                offset += 4;

                for (long i = 0; i < msglen; ++i)
                    decoded[i] = (byte)(buffer[offset + i] ^ masks[i % 4]);
            }
            else
            {
                Array.Copy(buffer, offset, decoded, 0, (int)msglen);
            }

            if (rsv1 && _useCompression)
            {
                decoded = DecompressMessage(decoded);
            }

            int totalConsumed = offset + (int)msglen;
            return (Encoding.UTF8.GetString(decoded), opcode, totalConsumed);
        }

        private async Task SendCloseFrameAsync()
        {
            byte[] closeFrame = new byte[] { 0x88, 0x00 }; // FIN bit set, opcode 8 (close frame), no payload
            if (this.Stream != null)
            {
                await this.Stream.WriteAsync(closeFrame, 0, closeFrame.Length);
                await this.Stream.FlushAsync();
            }
        }

        private async Task SendPongFrameAsync(string message)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            byte[] pongFrame = new byte[messageBytes.Length + 2];
            pongFrame[0] = 0xA0; // FIN bit set, opcode 10 (pong frame)
            pongFrame[1] = (byte)messageBytes.Length;
            Array.Copy(messageBytes, 0, pongFrame, 2, messageBytes.Length);
            if (this.Stream != null)
            {
                await this.Stream.WriteAsync(pongFrame, 0, pongFrame.Length);
                await this.Stream.FlushAsync();
            }
        }

        private async Task SendWebSocketMessageAsync(byte[] messageBytes)
        {
            messageBytes = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(messageBytes));
            if (_useCompression)
            {
                messageBytes = CompressMessage(messageBytes);
            }

            byte[] frame = new byte[10 + messageBytes.Length];
            frame[0] = 0b10000001; // FIN bit set, opcode 1 (text frame)

            if (_useCompression)
            {
                frame[0] |= 0b01000000; // Set RSV1 bit for compression
            }

            if (Stream != null)
            {
                if (messageBytes.Length <= 125)
                {
                    frame[1] = (byte)messageBytes.Length;
                    Array.Copy(messageBytes, 0, frame, 2, messageBytes.Length);
                    await this.Stream.WriteAsync(frame, 0, 2 + messageBytes.Length);
                }
                else if (messageBytes.Length <= 65535)
                {
                    frame[1] = 126;
                    frame[2] = (byte)((messageBytes.Length >> 8) & 255);
                    frame[3] = (byte)(messageBytes.Length & 255);
                    Array.Copy(messageBytes, 0, frame, 4, messageBytes.Length);
                    await this.Stream.WriteAsync(frame, 0, 4 + messageBytes.Length);
                }
                else
                {
                    frame[1] = 127;
                    frame[2] = (byte)((messageBytes.Length >> 56) & 255);
                    frame[3] = (byte)((messageBytes.Length >> 48) & 255);
                    frame[4] = (byte)((messageBytes.Length >> 40) & 255);
                    frame[5] = (byte)((messageBytes.Length >> 32) & 255);
                    frame[6] = (byte)((messageBytes.Length >> 24) & 255);
                    frame[7] = (byte)((messageBytes.Length >> 16) & 255);
                    frame[8] = (byte)((messageBytes.Length >> 8) & 255);
                    frame[9] = (byte)(messageBytes.Length & 255);
                    Array.Copy(messageBytes, 0, frame, 10, messageBytes.Length);
                    await this.Stream.WriteAsync(frame, 0, 10 + messageBytes.Length);
                }

                await this.Stream.FlushAsync();
            }
        }

        private byte[] CompressMessage(byte[] data)
        {
            if (_compressor == null)
                return data;

            if (_serverNoContext)
                return CompressMessageNoContext(data);

            try
            {
                var outputStream = new MemoryStream();
                _compressor.BaseStream.Position = 0;
                _compressor.BaseStream.SetLength(0);
                _compressor.Write(data, 0, data.Length);
                _compressor.Flush();

                var compressed = ((MemoryStream)_compressor.BaseStream).ToArray();

                // Remove the last 4 bytes (00 00 FF FF)
                if (compressed.Length >= 4 &&
                    compressed[compressed.Length - 4] == 0x00 &&
                    compressed[compressed.Length - 3] == 0x00 &&
                    compressed[compressed.Length - 2] == 0xFF &&
                    compressed[compressed.Length - 1] == 0xFF)
                {
                    return compressed.Take(compressed.Length - 4).ToArray();
                }

                return compressed;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Compression error: {ex.Message}");
                return data; // Return original data if compression fails
            }
        }

        private byte[] CompressMessageNoContext(byte[] data)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var deflateStream = new DeflateStream(memoryStream, CompressionMode.Compress))
                {
                    deflateStream.Write(data, 0, data.Length);
                    deflateStream.Flush();
                }
                var result = memoryStream.ToArray();
                if (result.Length >= 4 &&
                    result[result.Length - 4] == 0x00 &&
                    result[result.Length - 3] == 0x00 &&
                    result[result.Length - 2] == 0xFF &&
                    result[result.Length - 1] == 0xFF)
                {
                    return result.Take(result.Length - 4).ToArray();
                }
                else
                {
                    return result.AsQueryable().Append((byte)0).ToArray();
                }
            }
        }

        private byte[] DecompressMessage(byte[] data)
        {
            if (_decompressor == null || data.Length == 0)
                return data;

            if (_clientNoContext)
                return DecompressMessageNoContext(data);

            try
            {
                var outputStream = new MemoryStream();
                _decompressor.BaseStream.Position = 0;
                _decompressor.BaseStream.SetLength(0);
                _decompressor.BaseStream.Write(data, 0, data.Length);
                _decompressor.BaseStream.Position = 0;

                _decompressor.CopyTo(outputStream);

                // Add the "00 00 FF FF" trailer if it's not present
                byte[] decompressed = outputStream.ToArray();
                if (decompressed.Length < 4 ||
                    decompressed[decompressed.Length - 4] != 0x00 ||
                    decompressed[decompressed.Length - 3] != 0x00 ||
                    decompressed[decompressed.Length - 2] != 0xFF ||
                    decompressed[decompressed.Length - 1] != 0xFF)
                {
                    outputStream.Write(new byte[] { 0x00, 0x00, 0xFF, 0xFF }, 0, 4);
                }

                return outputStream.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Decompression error: {ex.Message}");
                return data; // Return original data if decompression fails
            }
        }

        private byte[] DecompressMessageNoContext(byte[] data)
        {
            try
            {
                using (var memoryStream = new MemoryStream(data))
                using (var deflateStream = new DeflateStream(memoryStream, CompressionMode.Decompress))
                using (var resultStream = new MemoryStream())
                {
                    deflateStream.CopyTo(resultStream);
                    return resultStream.ToArray();
                }
            }
            catch
            {
                return data;
            }
        }
    }
}