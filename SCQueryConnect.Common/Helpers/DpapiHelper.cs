using SCQueryConnect.Common.Interfaces;
using System;
using System.Security.Cryptography;
using System.Text;

namespace SCQueryConnect.Common.Helpers
{
    public class DpapiHelper : IEncryptionHelper
    {
        public Encoding TextEncoding { get; } = Encoding.UTF8;

        public byte[] Decrypt(string base64CipherText, string entropy, DataProtectionScope scope)
        {
            byte[] bytes;
            
            var entropyBytes = string.IsNullOrWhiteSpace(entropy)
                ? null
                : Convert.FromBase64String(entropy);

            try
            {
                bytes = ProtectedData.Unprotect(
                    Convert.FromBase64String(base64CipherText),
                    entropyBytes,
                    scope);
            }
            catch (Exception)
            {
                bytes = new byte[0];
            }

            return bytes;
        }

        public byte[] Encrypt(byte[] plainTextBytes, out byte[] entropy, DataProtectionScope scope)
        {
            entropy = CreateRandomEntropy();

            return ProtectedData.Protect(
                plainTextBytes,
                entropy,
                scope);
        }

        private byte[] CreateRandomEntropy()
        {
            var entropy = new byte[100];
            new RNGCryptoServiceProvider().GetBytes(entropy);
            return entropy;
        }
    }
}
