using manga.inctrak.com.Common;
using Manga.IncTrak.Manga;
using Manga.IncTrak.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace manga.inctrak.com
{
    public class AuthBaseController : ControllerBase
    {
        private const int _keysize = 256;
        private const string _seesionKey = "mangaed";

        protected void CheckIncTrakId( string incTrakIdIn, bool update)
        {
            string incTrakId;
            if (Request.Cookies.TryGetValue("authenticated-accepted-3", out incTrakId))
            {
                if (incTrakIdIn == incTrakId)
                {
                    string idDec = MiscHelpers.Decrypt(incTrakId, false);
                    string idDecIn = MiscHelpers.Decrypt(incTrakIdIn, false);
                    if (idDec == idDecIn)
                    {
                        if (update)
                        {
                            CookieOptions option = new CookieOptions();
                            option.Expires = DateTime.Now.AddMinutes(30);
                            Response.Cookies.Append("authenticated-accepted-3", incTrakId, option);
                        }
                        return;
                    }
                }
                Response.Cookies.Delete("authenticated-accepted-3");
            }
            throw new Exception();
        }

        protected string MyIdentity(MyIdentities myIds, MangaAuthType auth)
        {
            string userId;
            if (auth == MangaAuthType.Email)
                userId = myIds.EmailId;
            else if (auth == MangaAuthType.Googs)
                userId = myIds.GoogleId;
            else
                throw new Exception("Bad auth type");

            return userId;
        }

        protected MyIdentities GetMyIdentities(bool update)
        {
            MyIdentities ids = new MyIdentities();
            var googleId = User.FindFirst(ClaimTypes.NameIdentifier);
            if (googleId != null)
                ids.GoogleId = googleId.Value;

            string visEmailClaimId;
            if (Request.Cookies.TryGetValue("VisEmailClaimId", out visEmailClaimId))
            {
                ids.EmailId = MiscHelpers.Decrypt(visEmailClaimId, false);

                if (ids.EmailId != null)
                {
                    if (update)
                    {
                        CookieOptions option = new CookieOptions();
                        option.Expires = DateTime.Now.AddMinutes(30);
                        Response.Cookies.Append("VisEmailClaimId", visEmailClaimId, option);
                    }
                }
            }

            return ids;
        }

        protected void SetAccess(string userId, string userEmail, MangaAuthType authType)
        {
            Guid accessToken = Guid.NewGuid();
            MangaState.SetUserMangaCatalog(accessToken.ToString("N"), userId, userEmail, authType);
            string authAccepted = Encode(userId, accessToken);

            CookieOptions option = new CookieOptions();
            option.Expires = DateTime.Now.AddHours(1);
            Response.Cookies.Append("authenticated-accepted-1", authAccepted, option);
        }

        protected MangaAuthType ValidateAccess(Tuple<string, Guid> visId, MyIdentities myIds, bool update)
        {
            string accessToken;
            if (Request.Cookies.TryGetValue("authenticated-accepted-1", out accessToken))
            {
                try
                {
                    var tok = Decrypt(accessToken);
                    MangaAuthType auth = MangaState.ValidateUserMangaCatalog(tok);
                    if (auth != MangaAuthType.Invalid)
                    {
                        string check = null;
                        if (visId != null)
                            check = visId.Item1;
                        else if (auth == MangaAuthType.Googs)
                            check = myIds.GoogleId;
                        else if (auth == MangaAuthType.Email)
                            check = myIds.EmailId;
                        if (check != null && check == tok.Item1)
                        {
                            if (update)
                            {
                                CookieOptions option = new CookieOptions();
                                option.Expires = DateTime.Now.AddHours(1);
                                Response.Cookies.Append("authenticated-accepted-1", accessToken, option);
                            }

                            return auth;
                        }
                    }
                }
                catch(Exception excp)
                {
                    MyLogger.LogError(excp, "Error decoding auth 1");
                }
                Response.Cookies.Delete("authenticated-accepted-1");
            }

            throw new Exception("Access Denied");
        }

        private Tuple<string, byte[]> GetAppHash()
        {
            byte[] salt = new ASCIIEncoding().GetBytes(Assembly.GetEntryAssembly().GetCustomAttributes<AssemblyCompanyAttribute>().First().Company.Substring(0, 16));

            string sessionGuid;
            if (Request.Cookies.TryGetValue(_seesionKey, out sessionGuid) == false)
            {
                sessionGuid = Guid.NewGuid().ToString();
            }
            else
            {
                sessionGuid = Request.Cookies[_seesionKey];
                Response.Cookies.Delete(_seesionKey);
            }
            CookieOptions option = new CookieOptions();
            option.Expires = DateTime.Now.AddHours(1);
            Response.Cookies.Append(_seesionKey, sessionGuid, option);

            return Tuple.Create(sessionGuid, salt);
        }

        protected string Encode(string userFolder, Guid mangaGuid)
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
            catch(Exception excp)
            {
                MyLogger.LogError(excp, "Error encoding visid");
                throw;
            }
        }

        protected Tuple<string, Guid> Decrypt(string visId)
        {
            var tup = GetAppHash();
            byte[] initVectorBytes = tup.Item2;
            byte[] cipherTextBytes = Convert.FromBase64String(visId);

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

                                byte[] userFolderBytes = new byte[cipherTextBytes.Length-16];
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
