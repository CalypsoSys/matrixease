using Google.Apis.Sheets.v4.Data;
using MatrixEase.Manga.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MatrixEase.Tester
{
    internal class TestStuff
    {
        private const int _keysize = 256;
        static private string sessionGuid = Guid.NewGuid().ToString();

        public static void Test()
        {
            var mangaGuid = Guid.NewGuid();
            //var userFolder = "100749084436597619452";
            var userFolder = "1007490844365976";
            var mxesId = Encode(userFolder, mangaGuid);

            var check = Decrypt(mxesId);

            Console.WriteLine(check);
        }
        static private Tuple<string, byte[]> GetAppHash()
        {
            byte[] salt = new ASCIIEncoding().GetBytes("Calypso Systems,");



            return Tuple.Create(sessionGuid, salt);
        }
        static protected string Encode(string userFolder, Guid mangaGuid)
        {
            try
            {
                var tup = GetAppHash();
                byte[] initVectorBytes = tup.Item2;
                List<byte> plainBytes = new List<byte>();
                plainBytes.AddRange(mangaGuid.ToByteArray());
                plainBytes.AddRange(Encoding.UTF8.GetBytes(userFolder));

                byte[] plainTextBytes = plainBytes.ToArray();

                using (PasswordDeriveBytes password = new PasswordDeriveBytes(tup.Item1, null))
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
            catch (Exception excp)
            {
                SimpleLogger.LogError(excp, "Error encoding MatrixEase ID");
                throw;
            }
        }

        static protected Tuple<string, Guid> Decrypt(string mxesId)
        {
            var tup = GetAppHash();
            byte[] initVectorBytes = tup.Item2;
            byte[] cipherTextBytes = Convert.FromBase64String(mxesId);

            using (PasswordDeriveBytes password = new PasswordDeriveBytes(tup.Item1, null))
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
                                byte[] guidBytes = new byte[16];
                                int guidByteCount = cryptoStream.Read(guidBytes, 0, 16);
                                Guid mangaGuid = new Guid(guidBytes);

                                byte[] userFolderBytes = new byte[cipherTextBytes.Length - 16];
                                int userFolderByteCount = cryptoStream.Read(userFolderBytes, 0, userFolderBytes.Length);
                                string userFolder = Encoding.UTF8.GetString(userFolderBytes, 0, userFolderByteCount);

                                return Tuple.Create(userFolder, mangaGuid);
                            }
                        }
                    }
                }
            }
        }

    }
}
