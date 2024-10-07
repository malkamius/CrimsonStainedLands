using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
namespace CrimsonStainedLands
{
    public class CertificateChainLoader
    {
        public class PasswordFinder : IPasswordFinder
        {
            private string password;

            public PasswordFinder(string password)
            {
                this.password = password;
            }

            public char[] GetPassword()
            {
                return password.ToCharArray();
            }
        }
        public static X509Certificate2 LoadCertificateWithChain(string certificateChainPath, string privateKeyPath, string password)
        {
            // Read the certificate chain
            var certificateChain = new List<X509Certificate2>();
            using (var reader = new StreamReader(certificateChainPath))
            {
                var pemReader = new PemReader(reader);
                object obj;
                while ((obj = pemReader.ReadObject()) != null)
                {
                    if (obj is Org.BouncyCastle.X509.X509Certificate bcCert)
                    {
                        var certData = bcCert.GetEncoded();
                        var dotNetCert = new X509Certificate2(certData);
                        certificateChain.Add(dotNetCert);
                    }
                }
            }

            if (certificateChain.Count == 0)
            {
                throw new Exception("No certificates found in the chain file.");
            }

            // Load the private key
            AsymmetricKeyParameter privateKey;
            using (var reader = new StreamReader(privateKeyPath))
            {
                var pemReader = new PemReader(reader, new PasswordFinder(password));
                var keyPair = pemReader.ReadObject() as AsymmetricCipherKeyPair;
                privateKey = keyPair?.Private;
            }

            if (privateKey == null)
            {
                throw new Exception("Failed to load private key.");
            }

            // Combine the first certificate (server cert) with the private key
            var serverCert = certificateChain[0];
            //var rsaPrivateKey = DotNetUtilities.ToRSA(privateKey as RsaPrivateCrtKeyParameters);
            var rsaParams = DotNetUtilities.ToRSAParameters(privateKey as RsaPrivateCrtKeyParameters);

            // Create RSA instance
            using (var rsaPrivateKey = RSA.Create())
            {
                rsaPrivateKey.ImportParameters(rsaParams);
                var certWithKey = serverCert.CopyWithPrivateKey(rsaPrivateKey);

                var newcert = new X509Certificate2(certWithKey.Export(X509ContentType.Pfx));
                // Create a collection with the full chain
                var collection = new X509Certificate2Collection(newcert);
                for (int i = 1; i < certificateChain.Count; i++)
                {
                    collection.Add(certificateChain[i]);
                }

                return newcert;
            }
            
        }
    }
}