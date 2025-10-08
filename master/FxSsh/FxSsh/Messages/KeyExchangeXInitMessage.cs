namespace FxSsh.Messages
{
    [Message("SSH_MSG_KEXDH_INIT,SSH_MSG_KEX_ECDH_INIT", MessageNumber)]
    public class KeyExchangeXInitMessage : Message
    {
        private const byte MessageNumber = 30;

        public override byte MessageType { get { return MessageNumber; } }

        protected override void OnLoad(SshDataReader reader)
        {
        }
    }
}
