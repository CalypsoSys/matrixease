using System;
using System.Collections.Generic;
using MatrixEase.Manga.Manga;
using MatrixEase.Manga.Processing;
using MatrixEase.Manga.Utility;
using MatrixEase.Web.Common;
using MatrixEase.Web.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MatrixEase.Web.Controllers
{
    [Route("api/matrixease/upload")]
    public class UploadController : ProcessController
    {
        private const long MaxUploadBytes = 100L * 1024L * 1024L;

        private readonly ILogger<UploadController> _logger;
        private readonly RequestContextAccessor _requestContextAccessor;

        public UploadController(ILogger<UploadController> logger, IBackgroundTaskQueue queue, RequestContextAccessor requestContextAccessor) : base(queue)
        {
            _logger = logger;
            _requestContextAccessor = requestContextAccessor;
        }

        [HttpPost]
        [RequestSizeLimit(MaxUploadBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
        public object PostFormData([FromForm] IFormFile file, [FromForm] string manga_name, [FromForm] int header_row, [FromForm] int header_rows, [FromForm] int max_rows, [FromForm] bool ignore_blank_rows, [FromForm] bool ignore_text_case, [FromForm] bool trim_leading_whitespace, [FromForm] bool trim_trailing_whitespace, [FromForm] string ignore_cols, [FromForm] string sheet_type, [FromForm] string csv_separator, [FromForm] string csv_quote, [FromForm] string csv_escape, [FromForm] string csv_null, [FromForm] string csv_eol)
        {
            SupabaseIdentity identity = _requestContextAccessor.GetSupabaseIdentity(HttpContext);
            if (identity.IsAuthenticated() == false)
            {
                return Unauthorized(new { Success = false, Error = "You must sign in before uploading MatrixEase data." });
            }

            if (file == null || file.Length <= 0)
            {
                return BadRequest(new { Success = false, Error = "A file is required." });
            }

            try
            {
                string userId = identity.ExternalIdentity;
                MangaState.CheckProjectCount(userId);

                using (var input = file.OpenReadStream())
                {
                    MangaInfo mangaInfo = new MangaInfo(file.FileName, manga_name, header_row, header_rows, max_rows, ignore_blank_rows, ignore_text_case, trim_leading_whitespace, trim_trailing_whitespace, ignore_cols, sheet_type, new Dictionary<string, string> { { MangaConstants.CsvSeparator, csv_separator }, { MangaConstants.CsvQuote, csv_quote }, { MangaConstants.CsvEscape, csv_escape }, { MangaConstants.CsvNull, csv_null }, { MangaConstants.CsvEol, csv_eol } });
                    Guid? mangaGuid = SheetProcessing.ProcessSheet(userId, input, mangaInfo, RunBackroundManagGet);

                    if (mangaGuid != null)
                    {
                        return new { Success = true, MatrixId = Encode(userId, mangaGuid.Value), StatusData = MangaFactory.StartingStatus("CSV Upload") };
                    }
                }
            }
            catch (MatrixEaseException mExcp)
            {
                return new { Success = false, Error = mExcp.Message };
            }
            catch (Exception excp)
            {
                SimpleLogger.LogError(excp, "Error uploading sheet {0}", sheet_type);
            }

            return new { Success = false };
        }
    }
}
