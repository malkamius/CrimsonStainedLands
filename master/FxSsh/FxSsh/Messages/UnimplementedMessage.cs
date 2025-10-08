namespace FxSsh.Messages
{
    [Message("SSH_MSG_UNIMPLEMENTED", MessageNumber)]
    public class UnimplementedMessage : Message
    {
        private const byte MessageNumber = 3;

        public uint SequenceNumber { get; set; }

        public override byte MessageType { get { return MessageNumber; } }

        protected override void OnLoad(SshDataReader reader)
        {
            SequenceNumber = reader.ReadUInt32();
        }

        protected override void OnGetPacket(SshDataWriter writer)
        {
            writer.Write(SequenceNumber);
        }
    }
}
