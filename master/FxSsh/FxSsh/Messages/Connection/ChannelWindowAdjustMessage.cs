
namespace FxSsh.Messages.Connection
{
    [Message("SSH_MSG_CHANNEL_WINDOW_ADJUST", MessageNumber)]
    public class ChannelWindowAdjustMessage : ConnectionServiceMessage
    {
        private const byte MessageNumber = 93;

        public uint RecipientChannel { get; set; }
        public uint BytesToAdd { get; set; }

        public override byte MessageType { get { return MessageNumber; } }

        protected override void OnLoad(SshDataReader reader)
        {
            RecipientChannel = reader.ReadUInt32();
            BytesToAdd = reader.ReadUInt32();
        }

        protected override void OnGetPacket(SshDataWriter writer)
        {
            writer.Write(RecipientChannel);
            writer.Write(BytesToAdd);
        }
    }
}
