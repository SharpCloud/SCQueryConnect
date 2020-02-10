using SCQueryConnect.Common.Interfaces;
using SCQueryConnect.Helpers;
using SCQueryConnect.Interfaces;
using System;
using System.Security.Cryptography;
using System.Text;

namespace SCQueryConnect.Services
{
    public class PasswordStorage : IPasswordStorage
    {
        public const string Password = "Password";
        public const string ProxyPassword = "ProxyPassword";

        private const string Dpapi = "Dpapi";
        private const string Entropy = "Entropy";

        private readonly IEncryptionHelper _encryptionHelper;

        public PasswordStorage(IEncryptionHelper encryptionHelper)
        {
            _encryptionHelper = encryptionHelper;
        }

        public string LoadPassword(string key)
        {
            var regPassword = SaveHelper.RegRead($"{key}{Dpapi}", string.Empty);
            var regPasswordEntropy = SaveHelper.RegRead($"{key}{Dpapi}{Entropy}", null);
            try
            {
                return _encryptionHelper.TextEncoding.GetString(
                    _encryptionHelper.Decrypt(
                        regPassword,
                        regPasswordEntropy,
                        DataProtectionScope.CurrentUser));
            }
            catch (CryptographicException ex) when (ex.Message.Contains("The parameter is incorrect"))
            {
                // Fallback method for backwards compatibility
                regPassword = SaveHelper.RegRead(key, string.Empty);
                
                SaveHelper.RegDelete(key);

                return Encoding.Default.GetString(
                    Convert.FromBase64String(regPassword));
            }
        }

        public void SavePassword(string key, string password)
        {
            SaveHelper.RegWrite($"{key}{Dpapi}", Convert.ToBase64String(
                _encryptionHelper.Encrypt(
                    _encryptionHelper.TextEncoding.GetBytes(password),
                    out var entropy,
                    DataProtectionScope.CurrentUser)));

            SaveHelper.RegWrite($"{key}{Dpapi}{Entropy}", Convert.ToBase64String(entropy));
        }
    }
}
