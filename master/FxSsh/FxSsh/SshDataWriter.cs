using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Text;

namespace FxSsh
{
    public class SshDataWriter
    {
        private readonly MemoryStream _ms;

        public SshDataWriter(int expectedCapacity = 4096)
        {
            _ms = new MemoryStream(expectedCapacity);
        }

        public SshDataWriter Write(bool value)
        {
            _ms.WriteByte(value ? (byte)1 : (byte)0);
            return this;
        }

        public SshDataWriter Write(byte value)
        {
            _ms.WriteByte(value);
            return this;
        }

        public SshDataWriter Write(uint value)
        {
            var bytes = new[] { (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)(value & 0xFF) };
            _ms.Write(bytes, 0, 4);
            return this;
        }

        public SshDataWriter Write(ulong value)
        {
            var bytes = new[] {
                (byte)(value >> 56), (byte)(value >> 48), (byte)(value >> 40), (byte)(value >> 32),
                (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)(value & 0xFF)
            };
            _ms.Write(bytes, 0, 8);
            return this;
        }

        public SshDataWriter Write(string str, Encoding encoding)
        {
            Contract.Requires(str != null);
            Contract.Requires(encoding != null);

            var bytes = encoding.GetBytes(str);
            WriteBinary(bytes);
            return this;
        }

        public SshDataWriter WriteMpint(ReadOnlyMemory<byte> data)
        {
            if (data.Length == 1 && data.Span[0] == 0)
            {
                WriteBytes(new byte[4]);
            }
            else
            {
                var length = (uint)data.Length;
                var high = ((data.Span[0] & 0x80) != 0);
                if (high)
                {
                    Write(length + 1);
                    Write((byte)0);
                    WriteBytes(data);
                }
                else
                {
                    Write(length);
                    WriteBytes(data);
                }
            }
            return this;
        }

        public SshDataWriter WriteBytes(ReadOnlyMemory<byte> data)
        {
            _ms.Write(data.Span);
            return this;
        }

        public SshDataWriter WriteBinary(ReadOnlyMemory<byte> data)
        {
            Write((uint)data.Length);
            _ms.Write(data.Span);
            return this;
        }

        public byte[] ToByteArray()
        {
            var buf = _ms.GetBuffer();
            if (buf.Length == _ms.Length)
                return buf;
            return _ms.ToArray();
        }
    }
}
