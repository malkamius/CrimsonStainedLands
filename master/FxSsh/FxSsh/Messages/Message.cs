using System;
using System.Diagnostics.Contracts;

namespace FxSsh.Messages
{
    public abstract class Message
    {
        public abstract byte MessageType { get; }

        protected ReadOnlyMemory<byte> RawBytes { get; set; }

        public void Load(ReadOnlyMemory<byte> bytes)
        {
            RawBytes = bytes;

            var reader = new SshDataReader(bytes);
            var number = reader.ReadByte();
            if (number != MessageType)
                throw new ArgumentException(string.Format("Message type {0} is not valid.", number));

            OnLoad(reader);
        }

        public byte[] GetPacket()
        {
            var writer = new SshDataWriter();
            writer.Write(MessageType);

            OnGetPacket(writer);

            return writer.ToByteArray();
        }

        public static T LoadFrom<T>(Message message) where T : Message, new()
        {
            Contract.Requires(message != null);

            var msg = new T();
            msg.Load(message.RawBytes);
            return msg;
        }

        protected virtual void OnLoad(SshDataReader reader)
        {
            Contract.Requires(reader != null);

            throw new NotSupportedException();
        }

        protected virtual void OnGetPacket(SshDataWriter writer)
        {
            Contract.Requires(writer != null);

            throw new NotSupportedException();
        }
    }
}
