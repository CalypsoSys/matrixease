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


            MangaState.SetUserMangaCatalog(MangaDesktop.AccessTokenAll, MangaDesktop.UserId, MangaDesktop.AccessTokenAll, MangaAuthType.Electron);

            CookieOptions optionCookie = new CookieOptions();
            optionCookie.Expires = DateTime.Now.AddYears(1);
            Response.Cookies.Append("cookies-accepted-1", "acceptedxxx", optionCookie);

            CookieOptions optionAuth = new CookieOptions();
            optionAuth.Expires = DateTime.Now.AddYears(1);
            Response.Cookies.Append("authenticated-accepted-1", MangaDesktop.AccessTokenAll, optionAuth);

            return Redirect(new Uri("/index.html", UriKind.Relative).ToString());
        }

        [HttpGet("/js/vis_init.js")]
        public string MatrixEaseIdDebug()
        {
            return MatrixEaseId();
        }

        [HttpGet("/js/vis_init.min.js")]
        public string MatrixEaseIdRelease()
        {
            return MatrixEaseId();
        }

        private string MatrixEaseId()
        {
            return string.Format("document.write('<form><input type=\"hidden\" id=\"matrixease_id\" name=\"matrixease_id\" value=\"{0}\"/></form>');", MangaDesktop.AccessTokenAll);
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

        [HttpGet("captcha")]
        public object Captcha(string matrixease_id)
        {
            return new { num1 = 0, num2 = 0 };
        }

        [HttpGet("send_email_code")]
        public object SendEmailCode(string matrixease_id, string email_to_address, string result)
        {
            return new { Success = false };
        }

        [HttpGet("validate_email_code")]
        public object ValidateEmailCode(string matrixease_id, string email_to_address, string emailed_code)
        {
            return new { Success = false };
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
                        Url = new Uri(string.Format("/visualize.html?matrixease_id={0}&vis_id={1}", HttpUtility.UrlEncode(matrixease_id), HttpUtility.UrlEncode(manga.ManagGuid.ToString())), UriKind.Relative).ToString(),
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
                MyLogger.LogError(excp, "Error getting my mangas");
            }

            return new { Success = false };
        }
    }
}
