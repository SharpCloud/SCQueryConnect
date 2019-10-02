using SCQueryConnect.Common.Interfaces;
using System;
using System.Security.Cryptography;
using System.Text;

namespace SCQueryConnect.Common.Helpers
{
    public class DpapiHelper : IEncryptionHelper
    {
        public Encoding TextEncoding { get; } = Encoding.UTF8;

        public byte[] Decrypt(string base64CipherText)
        {
            return ProtectedData.Unprotect(
                Convert.FromBase64String(base64CipherText),
                null,
                DataProtectionScope.LocalMachine);
        }

        public byte[] Encrypt(byte[] plainTextBytes)
        {
            return ProtectedData.Protect(
                plainTextBytes,
                null,
                DataProtectionScope.LocalMachine);
        }
    }
}
