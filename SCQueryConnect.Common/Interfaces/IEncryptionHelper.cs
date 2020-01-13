using System.Text;

namespace SCQueryConnect.Common.Interfaces
{
    public interface IEncryptionHelper
    {
        Encoding TextEncoding { get; }

        byte[] Decrypt(string base64CipherText, string entropy);
        byte[] Encrypt(byte[] plainTextBytes, out byte[] entropy);
    }
}
