using System;
using System.Diagnostics.Contracts;
using System.Security.Cryptography;
using System.Text;

namespace FxSsh.Algorithms
{
    [ContractClass(typeof(PublicKeyAlgorithmContract))]
    public abstract class PublicKeyAlgorithm
    {
        public PublicKeyAlgorithm(string key)
        {
            if (!string.IsNullOrEmpty(key))
                ImportKey(key);
        }

        public abstract string Name { get; }
        public virtual string PublicKeyName { get { return Name; } }

        public string GetFingerprint()
        {
            var bytes = SHA256.HashData(CreateKeyAndCertificatesData());
            return Convert.ToBase64String(bytes);
        }

        public byte[] GetSignature(ReadOnlyMemory<byte> signatureData)
        {
            var reader = new SshDataReader(signatureData);
            if (reader.ReadString(Encoding.ASCII) != this.Name)
                throw new CryptographicException("Signature was not created with this algorithm.");

            var signature = reader.ReadBinary();
            return signature;
        }

        public byte[] CreateSignatureData(byte[] data)
        {
            Contract.Requires(data != null);

            return new SshDataWriter()
                .Write(this.Name, Encoding.ASCII)
                .WriteBinary(SignData(data))
                .ToByteArray();
        }

        public abstract void ImportKey(string key);

        public abstract string ExportKey();

        public abstract void LoadKeyAndCertificatesData(byte[] data);

        public abstract byte[] CreateKeyAndCertificatesData();

        public abstract bool VerifyData(byte[] data, byte[] signature);

        public abstract bool VerifyHash(byte[] hash, byte[] signature);

        public abstract byte[] SignData(byte[] data);

        public abstract byte[] SignHash(byte[] hash);
    }
}
