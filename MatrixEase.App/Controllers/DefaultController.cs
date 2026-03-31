using Desktop.MatrixEase.Manga.Common;
using MatrixEase.Manga.Manga;
using MatrixEase.Manga.Processing;
using MatrixEase.Manga.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace Desktop.MatrixEase.Manga.Controllers
{
    [Route("/")]
    public class DefaultController : ControllerBase
    {
        private readonly IOptions<AppSettings> _options;
        private readonly ILogger<DefaultController> _logger;

        public DefaultController(IOptions<AppSettings> options, ILogger<DefaultController> logger)
        {
            _options = options;
            _logger = logger;
        }

        [HttpGet]
        public RedirectResult Get()
        {
#if DEBUG
            System.Diagnostics.Debugger.Launch();
#endif
            try 
            { 
                MangaState.SetUserMangaCatalog(MangaDesktop.AccessTokenAll, MangaDesktop.UserId, MangaDesktop.AccessTokenAll, MangaAuthType.Electron);

                CookieOptions optionCookie = new CookieOptions();
                optionCookie.Expires = DateTime.Now.AddYears(1);
                Response.Cookies.Append("cookies-accepted-1", "acceptedxxx", optionCookie);

                CookieOptions optionAuth = new CookieOptions();
                optionAuth.Expires = DateTime.Now.AddYears(1);
                Response.Cookies.Append("authenticated-accepted-1", MangaDesktop.AccessTokenAll, optionAuth);

                return Redirect(new Uri("/index.html", UriKind.Relative).ToString());
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
            return new
            {
                Success = true,
                MatrixEaseId = MangaDesktop.AccessTokenAll,
                CookiesAccepted = Request.Cookies.TryGetValue("cookies-accepted-1", out string cookieValue) && cookieValue == "acceptedxxx",
                HasCatalogAccess = true,
                GoogleSignedIn = true,
                HasEmailIdentity = false
            };
        }

        private string MatrixEaseId()
        {
            try
            { 
                return string.Format("document.write('<form><input type=\"hidden\" id=\"matrixease_id\" name=\"matrixease_id\" value=\"{0}\"/></form>');", MangaDesktop.AccessTokenAll);
            }
            catch (Exception excp)
            {
                SimpleLogger.LogError(excp, "Error DOC ID");
                throw;
            }
        }

        [HttpGet("get_access")]
        public object GetAccessToken(string matrixease_id, string access_token)
        {
            try
            {
                return new { Success = true, AccessToken = access_token };
            }
            catch
            {
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
            return new { num1 = 0, num2 = 0 };
        }

        [HttpGet("/api/session/captcha")]
        public object CaptchaApi(string matrixease_id)
        {
            return Captcha(matrixease_id);
        }

        [HttpGet("send_email_code")]
        public object SendEmailCode(string matrixease_id, string email_to_address, string result)
        {
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
            return new { Success = false };
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
                MangaCatalog cats = MangaState.LoadUserMangaCatalog(MangaDesktop.UserId, new MangaLoadOptions(true));

                List<object> mangas = new List<object>();
                List<Guid> loadedMangas = new List<Guid>();
                foreach (var manga in cats.MyMangas)
                {
                    mangas.Add(new
                    {
                        Name = manga.MangaName,
                        Url = new Uri(string.Format("/matrixease.html?matrixease_id={0}&mxes_id={1}", HttpUtility.UrlEncode(matrixease_id), HttpUtility.UrlEncode(manga.ManagGuid.ToString())), UriKind.Relative).ToString(),
                        Original = manga.OriginalName,
                        Type = manga.SheetType,
                        Created = manga.Created,
                        MaxRows = manga.MaxRows,
                        TotalRows = manga.TotalRows,
                        Status = manga.Status
                    });
                    loadedMangas.Add(manga.ManagGuid);
                }
                foreach (var manga in MangaFactory.GetPending(MangaDesktop.UserId))
                {
                    if (loadedMangas.Contains(manga.ManagGuid) == false)
                    {
                        mangas.Add(new
                        {
                            Name = manga.MangaName,
                            Url = "#",
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
