using MatrixEase.Manga.Manga.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MatrixEase.Manga.Utility
{
    public static class MiscHelpers
    {
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

        public static string Encrypt(string cipherText)
        {
            return SecretProtector.ProtectString(cipherText, "MatrixEase.String");
        }

        public static string Encrypt(string cipherText, string passPhrase)
        {
            return SecretProtector.ProtectString(cipherText, $"MatrixEase.String.{passPhrase}");
        }

        public static string Decrypt(string cipherText, bool clear)
        {
            if ( clear )
            {
                return cipherText;
            }

            return SecretProtector.UnprotectString(cipherText, "MatrixEase.String");
        }

        public static string Decrypt(string cipherText, string passPhrase)
        {
            return SecretProtector.UnprotectString(cipherText, $"MatrixEase.String.{passPhrase}");
        }
    }
}
