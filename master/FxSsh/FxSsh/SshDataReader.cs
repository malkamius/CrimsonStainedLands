using System;
using System.Diagnostics.Contracts;
using System.Text;

namespace FxSsh
{
    public class SshDataReader
    {
        private readonly ReadOnlyMemory<byte> _bytes;
        private int _position;

        public SshDataReader(ReadOnlyMemory<byte> bytes)
        {
            _bytes = bytes;
        }

        public long DataAvailable => _bytes.Length - _position;

        public bool ReadBoolean()
        {
            var num = ReadByte();

            return num != 0;
        }

        public byte ReadByte()
        {
            var span = ReadBytesAsMemory(1).Span;
            return span[0];
        }

        public uint ReadUInt32()
        {
            var span = ReadBytesAsMemory(4).Span;
            return (uint)(span[0] << 24 | span[1] << 16 | span[2] << 8 | span[3]);
        }

        public ulong ReadUInt64()
        {
            var span = ReadBytesAsMemory(8).Span;
            return ((ulong)span[0] << 56 | (ulong)span[1] << 48 | (ulong)span[2] << 40 | (ulong)span[3] << 32 |
                    (ulong)span[4] << 24 | (ulong)span[5] << 16 | (ulong)span[6] << 8 | span[7]);
        }

        public ReadOnlyMemory<byte> ReadBytesAsMemory(int length)
        {
            if (_position + length > _bytes.Length)
                throw new ArgumentOutOfRangeException(nameof(length));

            var bytes = _bytes.Slice(_position, length);
            _position += length;
            return bytes;
        }

        public ReadOnlyMemory<byte> ReadBinaryAsMemory()
        {
            var length = ReadUInt32();
            return ReadBytesAsMemory((int)length);
        }

        public byte[] ReadBytes(int length)
        {
            return ReadBytesAsMemory(length).ToArray();
        }

        public byte[] ReadBinary()
        {
            return ReadBinaryAsMemory().ToArray();
        }

        public string ReadString(Encoding encoding)
        {
            Contract.Requires(encoding != null);

            var span = ReadBinaryAsMemory().Span;
            return encoding.GetString(span);
        }

        public byte[] ReadMpint()
        {
            var span = ReadBinaryAsMemory().Span;

            if (span.Length == 0)
                return new byte[1];

            if (span[0] == 0)
            {
                return span.Slice(1).ToArray();
            }

            return span.ToArray();
        }

        public byte[] GetRemainderBytes()
        {
            return _bytes[_position..].ToArray();
        }
    }
}
