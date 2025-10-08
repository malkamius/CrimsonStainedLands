using System;
using System.Diagnostics.Contracts;
using System.Security.Cryptography;
using System.Text;

namespace FxSsh.Algorithms
{
    public class EcdsaKey : PublicKeyAlgorithm
    {
        private readonly ECDsa _algorithm = ECDsa.Create();
        private readonly HashAlgorithmName _sha;
        private readonly string _curveName;

        public EcdsaKey(string curveName, string key)
            : base(key)
        {
            Contract.Requires(curveName == "nistp256" || curveName == "nistp384" || curveName == "nistp521");

            _curveName = curveName;
            var noKey = string.IsNullOrEmpty(key);
            if (curveName == "nistp256")
            {
                if (noKey) _algorithm = ECDsa.Create(ECCurve.NamedCurves.nistP256);
                _sha = HashAlgorithmName.SHA256;
            }
            else if (curveName == "nistp384")
            {
                if (noKey) _algorithm = ECDsa.Create(ECCurve.NamedCurves.nistP384);
                _sha = HashAlgorithmName.SHA384;
            }
            else if (curveName == "nistp521")
            {
                if (noKey) _algorithm = ECDsa.Create(ECCurve.NamedCurves.nistP521);
                _sha = HashAlgorithmName.SHA512;
            }
        }

        public override string Name
        {
            get { return $"ecdsa-sha2-{_curveName}"; }
        }

        public override void ImportKey(string key)
        {
            _algorithm.ImportFromPem(key);
        }

        public override string ExportKey()
        {
            return _algorithm.ExportPkcs8PrivateKeyPem();
        }

        public override void LoadKeyAndCertificatesData(byte[] data)
        {
            var reader = new SshDataReader(data);
            if (reader.ReadString(Encoding.ASCII) != this.Name
                || reader.ReadString(Encoding.ASCII) != _curveName)
                throw new CryptographicException("Key and certificates were not created with this algorithm.");

            var bytesQ = reader.ReadBinaryAsMemory();
            var readerQ = new SshDataReader(bytesQ);
            if (readerQ.ReadByte() != 0x04)
                throw new CryptographicException("Curve point compression is not supported.");
            var fieldSize = bytesQ.Length / 2;
            var args = _algorithm.ExportParameters(false);
            args.Q.X = readerQ.ReadBytes(fieldSize);
            args.Q.Y = readerQ.ReadBytes(fieldSize);

            _algorithm.ImportParameters(args);
        }

        public override byte[] CreateKeyAndCertificatesData()
        {
            var args = _algorithm.ExportParameters(false);
            var bytesQ = new SshDataWriter(1 + args.Q.X.Length + args.Q.Y.Length)
                .Write(0x04)
                .WriteBytes(args.Q.X)
                .WriteBytes(args.Q.Y)
                .ToByteArray();
            return new SshDataWriter(12 + this.Name.Length + _curveName.Length + bytesQ.Length)
                .Write(this.Name, Encoding.ASCII)
                .Write(_curveName, Encoding.ASCII)
                .WriteBinary(bytesQ)
                .ToByteArray();
        }

        public override bool VerifyData(byte[] data, byte[] signature)
        {
            var sig = SignatureBlobToP1363(signature);
            return _algorithm.VerifyData(data, sig, _sha, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        }

        public override bool VerifyHash(byte[] hash, byte[] signature)
        {
            var sig = SignatureBlobToP1363(signature);
            return _algorithm.VerifyHash(hash, sig, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        }

        private byte[] SignatureBlobToP1363(byte[] signatureBlob)
        {
            var reader = new SshDataReader(signatureBlob);
            var r = reader.ReadMpint();
            var s = reader.ReadMpint();
            var fieldSize = (_algorithm.KeySize + 7) >> 3;
            // equal to (int)Math.Ceiling((double)_algorithm.KeySize / 8);
            //_algorithm.KeySize == 256 ? 32 :
            //_algorithm.KeySize == 384 ? 48 :
            //_algorithm.KeySize == 521 ? 66 :
            //throw new InvalidDataException();
            var bytes = new byte[fieldSize * 2];
            Array.Copy(r, 0, bytes, fieldSize - r.Length, r.Length);
            Array.Copy(s, 0, bytes, fieldSize + fieldSize - s.Length, s.Length);
            return bytes;
        }

        public override byte[] SignData(byte[] data)
        {
            var sig = _algorithm.SignData(data, _sha, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
            return P1363ToSignatureBlob(sig);
        }

        public override byte[] SignHash(byte[] hash)
        {
            var sig = _algorithm.SignHash(hash, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
            return P1363ToSignatureBlob(sig);
        }

        private byte[] P1363ToSignatureBlob(byte[] p1363Bytes)
        {
            var fieldSize = p1363Bytes.Length / 2;
            var bytes = p1363Bytes.AsMemory();
            return new SshDataWriter(8 + p1363Bytes.Length)
                .WriteMpint(bytes[..fieldSize])
                .WriteMpint(bytes[fieldSize..])
                .ToByteArray();
        }
    }
}
