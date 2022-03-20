using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Desktop.MatrixEase.Manga;
using Desktop.MatrixEase.Manga.Common;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Http;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using MatrixEase.Manga.Manga;
using MatrixEase.Manga.Processing;
using MatrixEase.Manga.Utility;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting.Internal;
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
        public RedirectResult Get(string inctrak_id)
        {
            return Redirect(new Uri("/index.html", UriKind.Relative).ToString());
        }

        [HttpGet("check_login")]
        public object CheckLogin(string inctrak_id)
        {
            return new { Success =true };
        }

        [HttpGet("manual_login")]
        public RedirectResult ManualLogin(string inctrak_id)
        {
            return Redirect(new Uri("/index.html", UriKind.Relative).ToString());
        }

        [HttpGet("sheet")]
        public object Get(string inctrak_id, string manga_name, int header_row, int header_rows, int max_rows, bool ignore_blank_rows, bool ignore_text_case, bool trim_leading_whitespace, bool trim_trailing_whitespace, string ignore_cols, string sheet_id, string range)
        {
            try
            {
                MangaState.CheckProjectCount(MangaDesktop.UserId);

                MangaInfo mangaInfo = new MangaInfo("", manga_name, header_row, header_rows, max_rows, ignore_blank_rows, ignore_text_case, trim_leading_whitespace, trim_trailing_whitespace, ignore_cols, "google", new Dictionary<string, string> { { MangaConstants.SheetID, sheet_id }, { MangaConstants.SheetRange, range } });

                UserCredential credential;
                string[] Scopes = { SheetsService.Scope.SpreadsheetsReadonly };
                using (var stream = new MemoryStream())
                {
                    using (var writer = new StreamWriter(stream))
                    {
                        //writer.Write("{   \"installed\": {    \"client_id\": \"911488426711-66kvhfetkk359ch9jat4ft9rd1i3184b.apps.googleusercontent.com\",    \"project_id\": \"optioneeplan\",    \"auth_uri\": \"https://accounts.google.com/o/oauth2/auth\",    \"token_uri\": \"https://oauth2.googleapis.com/token\",    \"auth_provider_x509_cert_url\": \"https://www.googleapis.com/oauth2/v1/certs\",    \"client_secret\": \"AM_GPRWKKVafgGZmvtAi24oK\",    \"redirect_uris\": [ \"urn:ietf:wg:oauth:2.0:oob\", \"http://localhost\" ]  }}");

                        var clientSecret = GoogsAuth.GoogsJson.Replace("{", "{{").Replace("}", "}}").Replace("#PARAM1#", "{0}").Replace("#PARAM2#", "{1}").Replace("'", "\"");
                        writer.Write(string.Format(clientSecret, _options.Value.GetGoogleClientId(), _options.Value.GetGoogleClientSecret()));
                        writer.Flush();
                        stream.Position = 0;
                        // The file token.json stores the user's access and refresh tokens, and is created
                        // automatically when the authorization flow completes for the first time.
                        string credPath = MiscHelpers.GetGoogsFile("token.json");
                        credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                            GoogleClientSecrets.Load(stream).Secrets,
                            Scopes,
                            "user",
                            CancellationToken.None,
                            new FileDataStore(credPath, true)).Result;
                    }
                }

                Guid? mangaGuid = SheetProcessing.ProcessSheet(credential, MangaDesktop.UserId, mangaInfo, MangaDesktop.RunBackroundManagGet);
                if (mangaGuid != null)
                {
                    return new { Success = true, VisId = HttpUtility.UrlEncode(mangaGuid.Value.ToString()), StatusData = MangaFactory.StartingStatus("Google Sheet") };
                }
            }
            catch (MatrixEaseLicenseException licExcp)
            {
                return new { Success = false, Error = licExcp.Message };
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
