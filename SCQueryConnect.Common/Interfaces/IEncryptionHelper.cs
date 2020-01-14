using System.Security.Cryptography;
using System.Text;

namespace SCQueryConnect.Common.Interfaces
{
    public interface IEncryptionHelper
    {
        Encoding TextEncoding { get; }

        byte[] Decrypt(string base64CipherText, string entropy, DataProtectionScope scope);
        byte[] Encrypt(byte[] plainTextBytes, out byte[] entropy, DataProtectionScope scope);
    }
}
