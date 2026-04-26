using MatrixEase.Web.Common;
using MatrixEase.Manga.Manga;
using MatrixEase.Manga.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;

namespace MatrixEase.Web
{
    public class AuthBaseController : ControllerBase
    {
        private const string MatrixIdPurpose = "MatrixEase.MatrixId";

        protected void CheckMatrixEaseId( string matrixEaseIdIn, bool update)
        {
            string matrixEaseId;
            if (Request.Cookies.TryGetValue("authenticated-accepted-3", out matrixEaseId))
            {
                if (matrixEaseIdIn == matrixEaseId)
                {
                    string idDec = MiscHelpers.Decrypt(matrixEaseId, false);
                    string idDecIn = MiscHelpers.Decrypt(matrixEaseIdIn, false);
                    if (idDec == idDecIn)
                    {
                        if (update)
                        {
                            CookieOptions option = new CookieOptions();
                            option.Expires = DateTime.Now.AddMinutes(30);
                            Response.Cookies.Append("authenticated-accepted-3", matrixEaseId, option);
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

            string mxesEmailClaimId;
            if (Request.Cookies.TryGetValue("MxesEmailClaimId", out mxesEmailClaimId))
            {
                ids.EmailId = MiscHelpers.Decrypt(mxesEmailClaimId, false);

                if (ids.EmailId != null)
                {
                    if (update)
                    {
                        CookieOptions option = new CookieOptions();
                        option.Expires = DateTime.Now.AddMinutes(30);
                        Response.Cookies.Append("MxesEmailClaimId", mxesEmailClaimId, option);
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

        protected MangaAuthType ValidateAccess(Tuple<string, Guid> mxesId, MyIdentities myIds, bool update)
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
                        if (mxesId != null)
                            check = mxesId.Item1;
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
                    SimpleLogger.LogError(excp, "Error decoding auth 1");
                }
                Response.Cookies.Delete("authenticated-accepted-1");
            }

            throw new Exception("Access Denied");
        }

        protected string Encode(string userFolder, Guid mangaGuid)
        {
            try
            {
                List<byte> plainBytes = new List<byte>();
                plainBytes.AddRange(mangaGuid.ToByteArray());
                plainBytes.AddRange(Encoding.UTF8.GetBytes(userFolder));
                return SecretProtector.Protect(plainBytes.ToArray(), MatrixIdPurpose);
            }
            catch(Exception excp)
            {
                SimpleLogger.LogError(excp, "Error encoding MatrixEase ID");
                throw;
            }
        }

        protected Tuple<string, Guid> Decrypt(string mxesId)
        {
            byte[] plainTextBytes = SecretProtector.Unprotect(mxesId, MatrixIdPurpose);
            Guid mangaGuid = new Guid(plainTextBytes.Take(16).ToArray());
            string userFolder = Encoding.UTF8.GetString(plainTextBytes.Skip(16).ToArray());
            return Tuple.Create(userFolder, mangaGuid);
        }
    }
}
