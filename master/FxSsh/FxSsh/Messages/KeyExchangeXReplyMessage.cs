namespace FxSsh.Messages
{
    [Message("SSH_MSG_KEXDH_REPLY,SSH_MSG_KEX_ECDH_REPLY", MessageNumber)]
    public class KeyExchangeXReplyMessage : Message
    {
        private const byte MessageNumber = 31;

        public override byte MessageType { get { return MessageNumber; } }

        protected override void OnGetPacket(SshDataWriter writer)
        {
        }
    }
}
