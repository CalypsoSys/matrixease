using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MatrixEase.Manga.Utility
{
    public static class SecretProtector
    {
        private const byte CurrentVersion = 1;
        private const int SaltSize = 16;
        private const int NonceSize = 12;
        private const int TagSize = 16;
        private const int KeySize = 32;
        private const int Pbkdf2Iterations = 100000;

        private static string _configuredSecret;

        public static void Configure(string secret)
        {
            if (string.IsNullOrWhiteSpace(secret))
            {
                throw new InvalidOperationException("MatrixEase protection key is not configured.");
            }

            _configuredSecret = secret;
        }

        public static string ProtectString(string plaintext, string purpose = null)
        {
            if (plaintext == null)
            {
                throw new ArgumentNullException(nameof(plaintext));
            }

            return Protect(Encoding.UTF8.GetBytes(plaintext), purpose);
        }

        public static string Protect(byte[] plaintext, string purpose = null)
        {
            if (plaintext == null)
            {
                throw new ArgumentNullException(nameof(plaintext));
            }

            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
            byte[] ciphertext = new byte[plaintext.Length];
            byte[] tag = new byte[TagSize];
            byte[] key = DeriveKey(GetConfiguredSecret(), salt, purpose);

            try
            {
                using (AesGcm aes = new AesGcm(key, TagSize))
                {
                    aes.Encrypt(nonce, plaintext, ciphertext, tag, GetAssociatedData(purpose));
                }

                using (MemoryStream stream = new MemoryStream())
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(CurrentVersion);
                    writer.Write(salt);
                    writer.Write(nonce);
                    writer.Write(tag);
                    writer.Write(ciphertext);
                    return ToBase64Url(stream.ToArray());
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(key);
            }
        }

        public static string UnprotectString(string protectedValue, string purpose = null)
        {
            return Encoding.UTF8.GetString(Unprotect(protectedValue, purpose));
        }

        public static byte[] Unprotect(string protectedValue, string purpose = null)
        {
            if (string.IsNullOrWhiteSpace(protectedValue))
            {
                throw new ArgumentException("A protected value is required.", nameof(protectedValue));
            }

            byte[] payload = FromBase64Url(protectedValue);
            using (MemoryStream stream = new MemoryStream(payload))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                byte version = reader.ReadByte();
                if (version != CurrentVersion)
                {
                    throw new CryptographicException("Unsupported protected payload version.");
                }

                byte[] salt = reader.ReadBytes(SaltSize);
                byte[] nonce = reader.ReadBytes(NonceSize);
                byte[] tag = reader.ReadBytes(TagSize);
                byte[] ciphertext = reader.ReadBytes((int)(stream.Length - stream.Position));
                byte[] plaintext = new byte[ciphertext.Length];
                byte[] key = DeriveKey(GetConfiguredSecret(), salt, purpose);

                try
                {
                    using (AesGcm aes = new AesGcm(key, TagSize))
                    {
                        aes.Decrypt(nonce, ciphertext, tag, plaintext, GetAssociatedData(purpose));
                    }

                    return plaintext;
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(key);
                }
            }
        }

        private static string GetConfiguredSecret()
        {
            if (string.IsNullOrWhiteSpace(_configuredSecret))
            {
                throw new InvalidOperationException("MatrixEase protection key is not configured.");
            }

            return _configuredSecret;
        }

        private static byte[] DeriveKey(string secret, byte[] salt, string purpose)
        {
            string scopedSecret = string.IsNullOrWhiteSpace(purpose) ? secret : $"{purpose}:{secret}";
            return Rfc2898DeriveBytes.Pbkdf2(scopedSecret, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256, KeySize);
        }

        private static byte[] GetAssociatedData(string purpose)
        {
            return string.IsNullOrWhiteSpace(purpose) ? null : Encoding.UTF8.GetBytes(purpose);
        }

        private static string ToBase64Url(byte[] value)
        {
            return Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static byte[] FromBase64Url(string value)
        {
            string base64 = value.Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 2:
                    base64 += "==";
                    break;
                case 3:
                    base64 += "=";
                    break;
            }

            return Convert.FromBase64String(base64);
        }
    }
}
