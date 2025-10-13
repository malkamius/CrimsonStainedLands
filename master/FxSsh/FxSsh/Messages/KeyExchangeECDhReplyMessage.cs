namespace FxSsh.Messages
{
    public class KeyExchangeECDhReplyMessage : KeyExchangeXReplyMessage
    {
        public byte[] HostKey { get; set; }
        public byte[] Q { get; set; }
        public byte[] Signature { get; set; }

        protected override void OnGetPacket(SshDataWriter writer)
        {
            writer.WriteBinary(HostKey);
            writer.WriteBinary(Q);
            writer.WriteBinary(Signature);
        }
    }
}
