using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using MatrixEase.Manga.Manga;
using MatrixEase.Manga.Processing;
using MatrixEase.Manga.Utility;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MatrixEase.Web
{
    [Route("/")]
    public class DefaultController : AuthBaseController
    {
        private readonly IOptions<AppSettings> _options;
        private readonly ILogger<DefaultController> _logger;
        private readonly IWebHostEnvironment _environment;

        public DefaultController(IOptions<AppSettings> options, ILogger<DefaultController> logger, IWebHostEnvironment environment)
        {
            _options = options;
            _logger = logger;
            _environment = environment;
        }

        [HttpGet]
        public IActionResult Get()
        {
            try 
            { 
                return Ok(new
                {
                    Service = "MatrixEase API",
                    Frontend = "Run frontend separately for the shared web UI.",
                    Platform = "web"
                });
            }
            catch (Exception excp)
            {
                SimpleLogger.LogError(excp, "Error home page");
                throw;
            }
        }

        [HttpGet("/js/mxes_init.js")]
        public string MatrixEaseIdDebug()
        {
            try 
            { 
                return MatrixEaseId();
            }
            catch (Exception excp)
            {
                SimpleLogger.LogError(excp, "Error debug ID");
                throw;
            }
        }

        [HttpGet("/js/mxes_init.min.js")]
        public string MatrixEaseIdRelease()
        {
            try
            { 
                return MatrixEaseId();
            }
            catch (Exception excp)
            {
                SimpleLogger.LogError(excp, "Error ID");
                throw;
            }
        }

        [HttpGet("/api/session/bootstrap")]
        public object Bootstrap()
        {
            try
            {
                EnsureDevelopmentAccess();
                var matrixeaseId = IssueMatrixEaseId();
                var cookiesAccepted = Request.Cookies.TryGetValue("cookies-accepted-1", out string cookieValue) && cookieValue == "acceptedxxx";
                var identities = GetMyIdentities(false);
                var hasAccessCookie = Request.Cookies.ContainsKey("authenticated-accepted-1");

                return new
                {
                    Success = true,
                    MatrixEaseId = matrixeaseId,
                    CookiesAccepted = cookiesAccepted,
                    HasCatalogAccess = hasAccessCookie,
                    GoogleSignedIn = string.IsNullOrWhiteSpace(identities.GoogleId) == false,
                    HasEmailIdentity = string.IsNullOrWhiteSpace(identities.EmailId) == false
                };
            }
            catch (Exception excp)
            {
                SimpleLogger.LogError(excp, "Error building session bootstrap");
            }

            return new { Success = false };
        }

        private void EnsureDevelopmentAccess()
        {
            if (_environment.IsDevelopment() == false)
            {
                return;
            }

            if (Request.Cookies.ContainsKey("authenticated-accepted-1"))
            {
                return;
            }

            const string devEmail = "dev@matrixease.local";
            string devEmailHash = MiscHelpers.HashEmail(devEmail, false);

            CookieOptions option = new CookieOptions();
            option.Expires = DateTime.Now.AddHours(8);
            Response.Cookies.Append("cookies-accepted-1", "acceptedxxx", option);
            Response.Cookies.Append("MxesEmailClaimId", MiscHelpers.Encrypt(devEmailHash), option);

            SetAccess(devEmailHash, devEmail, MangaAuthType.Email);
        }

        private string MatrixEaseId()
        {
            try 
            { 
                string matrixease_id = IssueMatrixEaseId();
                return string.Format("document.write('<form><input type=\"hidden\" id=\"matrixease_id\" name=\"matrixease_id\" value=\"{0}\"/></form>');", matrixease_id);
            }
            catch (Exception excp)
            {
                SimpleLogger.LogError(excp, "Error DOC ID");
                throw;
            }
        }

        private string IssueMatrixEaseId()
        {
            CookieOptions option = new CookieOptions();
            option.Expires = DateTime.Now.AddMinutes(30);
            string matrixeaseId = MiscHelpers.Encrypt(Guid.NewGuid().ToString());
            Response.Cookies.Append("authenticated-accepted-3", matrixeaseId, option);

            return matrixeaseId;
        }

        [HttpGet("get_access")]
        public object GetAccessToken(string matrixease_id, string access_token)
        {
            try
            {
                CheckMatrixEaseId(matrixease_id, true);
                var tok = Decrypt(access_token);
                if (MangaState.ValidateUserMangaCatalog(tok) != MangaAuthType.Invalid)
                {
                    return new { Success = true, AccessToken = access_token };
                }
            }
            catch(Exception excp)
            {
                SimpleLogger.LogError(excp, "Error getting access token");
            }
            return new { Success = false, AccessToken = "" };
        }

        [HttpGet("/api/session/access")]
        public object GetAccessTokenApi(string matrixease_id, string access_token)
        {
            return GetAccessToken(matrixease_id, access_token);
        }

        [HttpGet("captcha")]
        public object Captcha(string matrixease_id)
        {
            try
            {
                CheckMatrixEaseId(matrixease_id, true);
                Random rnd = new Random();
                int num1 = rnd.Next(1, 9);
                int num2 = rnd.Next(1, 9);

                CookieOptions option = new CookieOptions();
                option.Expires = DateTime.Now.AddSeconds(35);
                string coded = MiscHelpers.Encrypt(string.Format("{0},{1},{2}", num1, num2, num1 + num2));
                Response.Cookies.Append("authenticated-accepted-2", coded, option);

                return new { num1 = num1, num2 = num2 };
            }
            catch (Exception excp)
            {
                SimpleLogger.LogError(excp, "Error getting captcha");
            }
            return new { num1 = 0, num2 = 0 };
        }

        [HttpGet("/api/session/captcha")]
        public object CaptchaApi(string matrixease_id)
        {
            return Captcha(matrixease_id);
        }

        // Deprecated: email-code sign-in is being removed from the split backend.
        [HttpGet("send_email_code")]
        public object SendEmailCode(string matrixease_id, string email_to_address, string result)
        {
            try
            {
                CheckMatrixEaseId(matrixease_id, true);
                _logger.LogWarning("Email code sign-in is deprecated and disabled for {EmailAddress}", email_to_address);
            }
            catch (Exception excp)
            {
                SimpleLogger.LogError(excp, "Error sending email code {0}", email_to_address);
            }

            return new { Success = false };
        }

        [HttpGet("/api/session/send_email_code")]
        public object SendEmailCodeApi(string matrixease_id, string email_to_address, string result)
        {
            return SendEmailCode(matrixease_id, email_to_address, result);
        }

        [HttpGet("validate_email_code")]
        public object ValidateEmailCode(string matrixease_id, string email_to_address, string emailed_code)
        {
            bool status = false;
            try
            {
                CheckMatrixEaseId(matrixease_id, true);
                if (MiscHelpers.IsValidEmail(email_to_address) )
                {
                    string filePath = MiscHelpers.GetPendingAccountFile(email_to_address);
                    if (System.IO.File.Exists(filePath))
                    {
                        using (new FileLocker(filePath))
                        {
                            using (FileStream pendingStream = new FileStream(filePath, FileMode.Open))
                            {
                                using (BinaryReader pending = new BinaryReader(pendingStream))
                                {
                                    string emailAddress = MiscHelpers.Decrypt(pending.ReadString(), false);
                                    string code = pending.ReadString();
                                    string emailAddressHash = MiscHelpers.Decrypt(pending.ReadString(), false);
                                    if (emailAddress.ToLower() == email_to_address.ToLower() && code == emailed_code && emailAddressHash == MiscHelpers.HashEmail(email_to_address, false))
                                    {
                                        CookieOptions option = new CookieOptions();
                                        option.Expires = DateTime.Now.AddMinutes(30);
                                        string MxesEmailClaimId = MiscHelpers.Encrypt(emailAddressHash);
                                        Response.Cookies.Append("MxesEmailClaimId", MxesEmailClaimId, option);

                                        SetAccess(emailAddressHash, email_to_address, MangaAuthType.Email);
                                        status = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception excp)
            {
                SimpleLogger.LogError(excp, "Error validating email code for {0}", email_to_address);
            }

            return new { Success = status };
        }

        [HttpGet("/api/session/validate_email_code")]
        public object ValidateEmailCodeApi(string matrixease_id, string email_to_address, string emailed_code)
        {
            return ValidateEmailCode(matrixease_id, email_to_address, emailed_code);
        }

        [HttpGet("my_mangas")]
        public object MyMangas(string matrixease_id)
        {
            try
            {
                CheckMatrixEaseId(matrixease_id, false);
                var myIds = GetMyIdentities(false);
                MangaAuthType auth = ValidateAccess(null, myIds, false);
                if (auth != MangaAuthType.Invalid)
                {
                    string userId = MyIdentity(myIds, auth);

                    MangaCatalog cats = MangaState.LoadUserMangaCatalog(userId, new MangaLoadOptions(true));

                    List<object> mangas = new List<object>();
                    List<Guid> loadedMangas = new List<Guid>();
                    foreach(var manga in cats.MyMangas)
                    {
                        var mxesId = Encode(userId, manga.ManagGuid);
                        string viewerPath = new Uri(string.Format("/viewer/{0}", HttpUtility.UrlEncode(mxesId)), UriKind.Relative).ToString();
                        mangas.Add(new {Name= manga.MangaName, 
                            Url = viewerPath,
                            ViewerPath = viewerPath,
                            Original = manga.OriginalName, Type = manga.SheetType, Created =manga.Created, MaxRows = manga.MaxRows,
                            TotalRows = manga.TotalRows, Status = manga.Status});
                        loadedMangas.Add(manga.ManagGuid);
                    }
                    foreach(var manga in MangaFactory.GetPending(userId))
                    {
                        if (loadedMangas.Contains(manga.ManagGuid) == false)
                        {
                            mangas.Add(new
                            {
                                Name = manga.MangaName,
                                Url = "#",
                                ViewerPath = "#",
                                Original = manga.OriginalName,
                                Type = manga.SheetType,
                                Created = manga.Created,
                                MaxRows = manga.MaxRows,
                                TotalRows = "N/A",
                                Status = manga.Status
                            });
                        }

                    }
                    return new { Success = true, MyMangas = mangas };
                }
            }
            catch (Exception excp)
            {
                SimpleLogger.LogError(excp, "Error getting my mangas");
            }

            return new { Success = false };
        }

        [HttpGet("/api/session/my_mangas")]
        public object MyMangasApi(string matrixease_id)
        {
            return MyMangas(matrixease_id);
        }
    }
}
