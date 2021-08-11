using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Http;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using manga.inctrak.com.Controllers;
using manga.inctrak.com.Tasks;
using Manga.IncTrak.Manga;
using Manga.IncTrak.Processing;
using Manga.IncTrak.Utility;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;

namespace manga.inctrak.com
{
    [Route("/google")]
    public class GoogleController : ProcessController
    {
        private readonly ILogger<GoogleController> _logger;

        public GoogleController(ILogger<GoogleController> logger, IBackgroundTaskQueue queue) : base(queue)
        {
            _logger = logger;
        }

        [Authorize]
        [HttpGet("login")]
        public RedirectResult Get(string inctrak_id)
        {
            try
            {
                CheckIncTrakId(inctrak_id, true);

                var googleId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
                var googleEmail = User.FindFirst(ClaimTypes.Email).Value;

                SetAccess(googleId, googleEmail, MangaAuthType.Googs);

                return Redirect(new Uri("/index.html", UriKind.Relative).ToString());
            }
            catch (Exception excp)
            {
                MyLogger.LogError(excp, "Error login to googs");
                ClearCookies();
                return Redirect(new Uri("/no_access.html", UriKind.Relative).ToString());
            }
        }

        [HttpGet("check_login")]
        public object CheckLogin(string inctrak_id)
        {
            try
            {
                CheckIncTrakId(inctrak_id, true);

                var authResult = HttpContext.AuthenticateAsync();
                authResult.Wait();

                return new { Success = authResult.Result.Succeeded };
            }
            catch (Exception excp)
            {
                MyLogger.LogError(excp, "Error checking googs access");
            }
            return new { Success = false };
        }

        [Authorize]
        [HttpGet("manual_login")]
        public RedirectResult ManualLogin(string inctrak_id)
        {
            try
            {
                CheckIncTrakId(inctrak_id, true);

                return Redirect(new Uri("/index.html", UriKind.Relative).ToString());
            }
            catch (Exception excp)
            {
                MyLogger.LogError(excp, "Error in manual googs login");
            }
            return Redirect(new Uri("/no_access.html", UriKind.Relative).ToString());
        }

        [Authorize]
        [HttpGet("sheet")]
        public object Get(string inctrak_id, string manga_name, int header_row, int header_rows, int max_rows, bool ignore_blank_rows, bool ignore_text_case, bool trim_leading_whitespace, bool trim_trailing_whitespace, string ignore_cols, string sheet_id, string range)
        {
            try
            {
                CheckIncTrakId(inctrak_id, true);
                var myIds = GetMyIdentities(true);
                MangaAuthType auth = ValidateAccess(null, myIds, true);
                if (auth != MangaAuthType.Invalid)
                {
                    string userId = MyIdentity(myIds, auth);

                    Console.WriteLine("Login User {0} Authenicated {1} Type {2}", User.Identity.Name, User.Identity.IsAuthenticated, User.Identity.AuthenticationType);

                    MangaState.CheckProjectCount(userId);

                    MangaInfo mangaInfo = new MangaInfo("", manga_name, header_row, header_rows, max_rows, ignore_blank_rows, ignore_text_case, trim_leading_whitespace, trim_trailing_whitespace, ignore_cols, "google", new Dictionary<string, string> { { MangaConstants.SheetID, sheet_id }, { MangaConstants.SheetRange, range } });

                    var authResult = HttpContext.AuthenticateAsync();
                    authResult.Wait();
                    var accessToken = authResult.Result.Properties.GetTokenValue("access_token");
                    var credential = GoogleCredential.FromAccessToken(accessToken);

                    Guid? mangaGuid = SheetProcessing.ProcessSheet(credential, userId, mangaInfo, RunBackroundManagGet);
                    if (mangaGuid != null)
                    {
                        return new { Success = true, VisId = GetMangaVis(inctrak_id, userId, mangaGuid), StatusData = MangaFactory.StartingStatus("Google Sheet") };
                    }
                }
            }
            catch (VisAlyzerLicenseException licExcp)
            {
                return new { Success = false, Error = licExcp.Message };
            }
            catch (Google.GoogleApiException googs)
            {
                MyLogger.LogError(googs, "Error API googs clearing cookies");
                ClearCookies();
            }
            catch (Exception excp)
            {
                MyLogger.LogError(excp, "Error accessing googs sheet {0} {1}", sheet_id, range);
                ClearCookies();
            }

            return new { Success = false };
        }
    }
}
