using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Web;
using Desktop.MatrixEase.Manga;
using Desktop.MatrixEase.Manga.Common;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Util.Store;
using MatrixEase.Manga.Manga;
using MatrixEase.Manga.Processing;
using MatrixEase.Manga.Utility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MatrixEase.Manga.com
{
    [Route("/google")]
    public class GoogleController : ControllerBase
    {
        private readonly IOptions<AppSettings> _options;
        private readonly ILogger<GoogleController> _logger;

        public GoogleController(IOptions<AppSettings> options, ILogger<GoogleController> logger)
        {
            _options = options;
            _logger = logger;
        }

        [HttpGet("login")]
        public RedirectResult Get(string matrixease_id)
        {
            return Redirect(new Uri("/index.html", UriKind.Relative).ToString());
        }

        [HttpGet("check_login")]
        public object CheckLogin(string matrixease_id)
        {
            return new { Success =true };
        }

        [HttpGet("manual_login")]
        public RedirectResult ManualLogin(string matrixease_id)
        {
            return Redirect(new Uri("/index.html", UriKind.Relative).ToString());
        }

        [HttpGet("sheet")]
        public object Get(string matrixease_id, string manga_name, int header_row, int header_rows, int max_rows, bool ignore_blank_rows, bool ignore_text_case, bool trim_leading_whitespace, bool trim_trailing_whitespace, string ignore_cols, string sheet_id, string range)
        {
            try
            {
                MangaState.CheckProjectCount(MangaDesktop.UserId);

                MangaInfo mangaInfo = new MangaInfo("", manga_name, header_row, header_rows, max_rows, ignore_blank_rows, ignore_text_case, trim_leading_whitespace, trim_trailing_whitespace, ignore_cols, "google", new Dictionary<string, string> { { MangaConstants.SheetID, sheet_id }, { MangaConstants.SheetRange, range } });

                UserCredential credential = GoogsAuth.AuthenticateLocal(_options.Value.GetGoogleClientId(), _options.Value.GetGoogleClientSecret());
                Guid? mangaGuid = SheetProcessing.ProcessSheet(credential, MangaDesktop.UserId, mangaInfo, MangaDesktop.RunBackroundManagGet);
                if (mangaGuid != null)
                {
                    return new { Success = true, MatrixId = HttpUtility.UrlEncode(mangaGuid.Value.ToString()), StatusData = MangaFactory.StartingStatus("Google Sheet") };
                }
            }
            catch (MatrixEaseException mExcp)
            {
                return new { Success = false, Error = mExcp.Message };
            }
            catch (Google.GoogleApiException googs)
            {
                MyLogger.LogError(googs, "Error API googs clearing cookies");
            }
            catch (Exception excp)
            {
                MyLogger.LogError(excp, "Error accessing googs sheet {0} {1}", sheet_id, range);
            }

            return new { Success = false };
        }
    }
}
