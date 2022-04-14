using MatrixEase.Web.Tasks;
using MatrixEase.Manga.Manga;
using MatrixEase.Manga.Processing;
using MatrixEase.Manga.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace MatrixEase.Web.Controllers
{
    [Route("/upload_file")]
    public class UploadController : ProcessController
    {
        private readonly ILogger<UploadController> _logger;

        public UploadController(ILogger<UploadController> logger, IBackgroundTaskQueue queue) : base(queue)
        {
            _logger = logger;
        }

        [HttpPost]
        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = 4294967295)]
        public object PostFormData([FromForm] string matrixease_id, [FromForm] IFormFile file,  [FromForm] string manga_name, [FromForm] int header_row, [FromForm] int header_rows, [FromForm] int max_rows, [FromForm] bool ignore_blank_rows, [FromForm] bool ignore_text_case, [FromForm] bool trim_leading_whitespace, [FromForm] bool trim_trailing_whitespace, [FromForm] string ignore_cols, [FromForm] string sheet_type, [FromForm] string csv_separator, [FromForm] string csv_quote, [FromForm] string csv_escape, [FromForm] string csv_null, [FromForm] string csv_eol)
        {
            try
            {
                CheckMatrixEaseId(matrixease_id, true);
                var myIds = GetMyIdentities(true);
                MangaAuthType auth = ValidateAccess(null, myIds, true);
                if (auth != MangaAuthType.Invalid)
                {
                    string userId = MyIdentity(myIds, auth);

                    MangaState.CheckProjectCount(userId);

                    using (var input = file.OpenReadStream())
                    {
                        MangaInfo mangaInfo = new MangaInfo(file.FileName, manga_name, header_row, header_rows, max_rows, ignore_blank_rows, ignore_text_case, trim_leading_whitespace, trim_trailing_whitespace, ignore_cols, sheet_type, new Dictionary<string, string> { { MangaConstants.CsvSeparator, csv_separator }, { MangaConstants.CsvQuote, csv_quote }, { MangaConstants.CsvEscape, csv_escape }, { MangaConstants.CsvNull, csv_null }, { MangaConstants.CsvEol, csv_eol } });
                        Guid? mangaGuid = SheetProcessing.ProcessSheet(userId, input, mangaInfo, RunBackroundManagGet);

                        if (mangaGuid != null)
                            return new { Success = true, MatrixId = GetMangaVis(matrixease_id, userId, mangaGuid), StatusData = MangaFactory.StartingStatus("CSV Upload") };
                    }
                }
            }
            catch (MatrixEaseException mExcp)
            {
                return new { Success = false, Error = mExcp.Message };
            }
            catch (Exception excp)
            {
                MyLogger.LogError(excp, "Error uploading sheet {0}", sheet_type);
            }

            return new { Success = false };
        }
    }
}