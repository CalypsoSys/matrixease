using Desktop.Manga.IncTrak.Common;
using Manga.IncTrak.Manga;
using Manga.IncTrak.Processing;
using Manga.IncTrak.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace Desktop.Manga.IncTrak.Controllers
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
        public string IncTrakIdDebug()
        {
            return IncTrakId();
        }

        [HttpGet("/js/vis_init.min.js")]
        public string IncTrakIdRelease()
        {
            return IncTrakId();
        }

        private string IncTrakId()
        {
            return string.Format("document.write('<form><input type=\"hidden\" id=\"inctrak_id\" name=\"inctrak_id\" value=\"{0}\"/></form>');", MangaDesktop.AccessTokenAll);
        }

        [HttpGet("get_access")]
        public object GetAccessToken(string inctrak_id, string access_token)
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
        public object Captcha(string inctrak_id)
        {
            return new { num1 = 0, num2 = 0 };
        }

        [HttpGet("send_email_code")]
        public object SendEmailCode(string inctrak_id, string email_to_address, string result)
        {
            return new { Success = false };
        }

        [HttpGet("validate_email_code")]
        public object ValidateEmailCode(string inctrak_id, string email_to_address, string emailed_code)
        {
            return new { Success = false };
        }

        [HttpGet("my_mangas")]
        public object MyMangas(string inctrak_id)
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
                        Url = new Uri(string.Format("/visualize.html?inctrak_id={0}&vis_id={1}", HttpUtility.UrlEncode(inctrak_id), HttpUtility.UrlEncode(manga.ManagGuid.ToString())), UriKind.Relative).ToString(),
                        Original = manga.OriginalName,
                        Type = manga.SheetType,
                        Created = manga.Created,
                        MaxRows = manga.MaxRows,
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
