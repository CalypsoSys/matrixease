using MatrixEase.Manga.Manga.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MatrixEase.Manga.Utility
{
    public static class MiscHelpers
    {
        // This constant is used to determine the keysize of the encryption algorithm.
        private const int _keysize = 256;

        public static int CalcPercent( int count, int total )
        {
            return (int)CalcPercent((decimal)count, (decimal)total);
        }

        public static long CalcPercent(long count, long total)
        {
            return (long)CalcPercent((decimal)count, (decimal)total);
        }

        public static decimal CalcPercent(decimal count, decimal total)
        {
            if (total != 0)
                return (count * 100) / total;
            return 0;
        }

        public static int SafeDictionaryCount<T1, T2>( Dictionary<T1, T2> input)
        {
            if ( input != null)
            {
                return input.Count;
            }

            return 0;
        }

        public static void SafeDispose(IDisposable dispose)
        {
            try
            {
                if (dispose != null)
                    dispose.Dispose();
            }
            catch
            {
            }
        }

        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // Normalize the domain
                email = Regex.Replace(email, @"(@)(.+)$", DomainMapper,
                                      RegexOptions.None, TimeSpan.FromMilliseconds(200));

                // Examines the domain part of the email and normalizes it.
                string DomainMapper(Match match)
                {
                    // Use IdnMapping class to convert Unicode domain names.
                    var idn = new IdnMapping();

                    // Pull out and process domain name (throws ArgumentException on invalid)
                    string domainName = idn.GetAscii(match.Groups[2].Value);

                    return match.Groups[1].Value + domainName;
                }
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }

            try
            {
                return Regex.IsMatch(email,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                    RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        public static string GetPendingFile(string file)
        {
            string pending = Path.Combine(MangaRoot.Folder, "pending");
            Directory.CreateDirectory(pending);

            return Path.Combine(pending, file);
        }

        public static string GetLogFileFile(string file)
        {
            string pending = Path.Combine(MangaRoot.Folder, "logs");
            Directory.CreateDirectory(pending);

            return Path.Combine(pending, file);
        }

        public static string GetGoogsFile(string file)
        {
            string pending = Path.Combine(MangaRoot.Folder, "googs");
            Directory.CreateDirectory(pending);

            return Path.Combine(pending, file);
        }

        public static string GetPendingAccountFile(string emailAddress)
        {
            return GetPendingFile(HashEmail(emailAddress, true));
        }

        public static string HashEmail(string email, bool upperLower)
        {
            string emailCased = (upperLower ? email.ToUpper() : email.ToLower());
            byte[] crypto2 = MD5.HashData(Encoding.ASCII.GetBytes(emailCased));
            string hash = String.Empty;
            foreach (byte theByte in crypto2)
            {
                hash += theByte.ToString("x2");
            }

            return hash;
        }

        public static string GetRandomColor()
        {
            string letters = "0123456789ABCDEF";
            StringBuilder color = new StringBuilder("#");
            Random rnd = new Random();
            for (int i = 0; i < 6; i++)
            {
                color.Append( letters[rnd.Next(0, 15)] );
            }
            return color.ToString();
        }

        private static Tuple<string, byte[]> GetAppHash()
        {
            string myCode = "9owZPLZ0mu6x8FECb/JnEf470qlpH4ouPMCwMCVGX+XpeUDDjag4hK0vJ9q3ZnYc";
            string companyDesc = Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyDescriptionAttribute>().First().Description;
            byte[] salt = new UnicodeEncoding().GetBytes(Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyCompanyAttribute>().First().Company.Substring(0, 8));

            string passPhrase = Decrypt(myCode, companyDesc, salt);
            return Tuple.Create(passPhrase, salt);
        }

        public static string Encrypt(string cipherText)
        {
            var tup = GetAppHash();

            return Encrypt(cipherText, tup.Item1, tup.Item2);
        }

        public static string Encrypt(string cipherText, string passPhrase)
        {
            var tup = GetAppHash();

            return Encrypt(cipherText, passPhrase, tup.Item2);
        }

        private static string Encrypt(string cipherText, string passPhrase, byte[] initVectorBytes)
        {
            var tup = GetAppHash();
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(cipherText);

            using (PasswordDeriveBytes password = new PasswordDeriveBytes(passPhrase, null))
            {
                byte[] keyBytes = password.GetBytes(_keysize / 8);
                using (RijndaelManaged symmetricKey = new RijndaelManaged())
                {
                    symmetricKey.Mode = CipherMode.CBC;
                    using (ICryptoTransform encryptor = symmetricKey.CreateEncryptor(keyBytes, initVectorBytes))
                    {
                        using (MemoryStream memoryStream = new MemoryStream())
                        {
                            using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                            {
                                cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                                cryptoStream.FlushFinalBlock();
                                byte[] cipherTextBytes = memoryStream.ToArray();
                                return Convert.ToBase64String(cipherTextBytes);
                            }
                        }
                    }
                }
            }
        }

        public static string Decrypt(string cipherText, bool clear)
        {
            if ( clear )
            {
                return cipherText;
            }

            var tup = GetAppHash();

            return Decrypt(cipherText, tup.Item1, tup.Item2);
        }

        public static string Decrypt(string cipherText, string passPhrase)
        {
            var tup = GetAppHash();

            return Decrypt(cipherText, passPhrase, tup.Item2);
        }

        public static string Decrypt(string cipherText, string passPhrase, byte[] initVectorBytes)
        {
            byte[] cipherTextBytes = Convert.FromBase64String(cipherText);

            using (PasswordDeriveBytes password = new PasswordDeriveBytes(passPhrase, null))
            {
                byte[] keyBytes = password.GetBytes(_keysize / 8);
                using (RijndaelManaged symmetricKey = new RijndaelManaged())
                {
                    symmetricKey.Mode = CipherMode.CBC;
                    using (ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, initVectorBytes))
                    {
                        using (MemoryStream memoryStream = new MemoryStream(cipherTextBytes))
                        {
                            using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                            {
                                byte[] plainTextBytes = new byte[cipherTextBytes.Length];
                                int decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                                return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
                            }
                        }
                    }
                }
            }
        }
    }
}
