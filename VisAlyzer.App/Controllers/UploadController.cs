using Desktop.Manga.IncTrak.Common;
using Manga.IncTrak.Manga;
using Manga.IncTrak.Processing;
using Manga.IncTrak.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Web;

namespace manga.inctrak.com.Controllers
{
    [Route("/upload_file")]
    public class UploadController : ControllerBase
    {
        private readonly ILogger<UploadController> _logger;

        public UploadController(ILogger<UploadController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = 4294967295)]
        public object PostFormData([FromForm] string inctrak_id, [FromForm] IFormFile file, [FromForm] string manga_name, [FromForm] int header_row, [FromForm] int header_rows, [FromForm] int max_rows, [FromForm] bool ignore_blank_rows, [FromForm] bool ignore_text_case, [FromForm] bool trim_leading_whitespace, [FromForm] bool trim_trailing_whitespace, [FromForm] string ignore_cols, [FromForm] string sheet_type, [FromForm] string csv_separator, [FromForm] string csv_quote, [FromForm] string csv_escape, [FromForm] string csv_null, [FromForm] string csv_eol)
        {
            try
            {
                MangaState.CheckProjectCount(MangaDesktop.UserId);

                using (var input = file.OpenReadStream())
                {
                    MangaInfo mangaInfo = new MangaInfo(file.FileName, manga_name, header_row, header_rows, max_rows, ignore_blank_rows, ignore_text_case, trim_leading_whitespace, trim_trailing_whitespace, ignore_cols, sheet_type, new Dictionary<string, string> { { MangaConstants.CsvSeparator, csv_separator }, { MangaConstants.CsvQuote, csv_quote }, { MangaConstants.CsvEscape, csv_escape }, { MangaConstants.CsvNull, csv_null }, { MangaConstants.CsvEol, csv_eol } });
                    Guid? mangaGuid = SheetProcessing.ProcessSheet(MangaDesktop.UserId, input, mangaInfo, MangaDesktop.RunBackroundManagGet);

                    if (mangaGuid != null)
                        return new { Success = true, VisId = HttpUtility.UrlEncode(mangaGuid.Value.ToString()), StatusData = MangaFactory.StartingStatus("CSV Upload") };
                }
            }
            catch (MatrixEaseLicenseException licExcp)
            {
                return new { Success = false, Error = licExcp.Message };
            }
            catch (Exception excp)
            {
                MyLogger.LogError(excp, "Error uploading sheet {0}", sheet_type);
            }

            return new { Success = false };
        }
    }
}