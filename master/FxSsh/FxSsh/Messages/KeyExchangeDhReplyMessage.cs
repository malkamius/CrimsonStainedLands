namespace FxSsh.Messages
{
    public class KeyExchangeDhReplyMessage : KeyExchangeXReplyMessage
    {
        public byte[] HostKey { get; set; }
        public byte[] F { get; set; }
        public byte[] Signature { get; set; }

        protected override void OnGetPacket(SshDataWriter writer)
        {
            writer.WriteBinary(HostKey);
            writer.WriteMpint(F);
            writer.WriteBinary(Signature);
        }
    }
}
