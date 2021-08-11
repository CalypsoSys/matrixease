using manga.inctrak.com.Tasks;
using Manga.IncTrak.Manga;
using Manga.IncTrak.Processing;
using Manga.IncTrak.Utility;
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

namespace manga.inctrak.com.Controllers
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
        public object PostFormData([FromForm] string inctrak_id, [FromForm] IFormFile file,  [FromForm] string manga_name, [FromForm] int header_row, [FromForm] int header_rows, [FromForm] int max_rows, [FromForm] bool ignore_blank_rows, [FromForm] bool ignore_text_case, [FromForm] bool trim_leading_whitespace, [FromForm] bool trim_trailing_whitespace, [FromForm] string ignore_cols, [FromForm] string sheet_type)
        {
            try
            {
                CheckIncTrakId(inctrak_id, true);
                var myIds = GetMyIdentities(true);
                MangaAuthType auth = ValidateAccess(null, myIds, true);
                if (auth != MangaAuthType.Invalid)
                {
                    string userId = MyIdentity(myIds, auth);

                    MangaState.CheckProjectCount(userId);

                    using (var input = file.OpenReadStream())
                    {
                        MangaInfo mangaInfo = new MangaInfo(file.FileName, manga_name, header_row, header_rows, max_rows, ignore_blank_rows, ignore_text_case, trim_leading_whitespace, trim_trailing_whitespace, ignore_cols, sheet_type, null);
                        Guid? mangaGuid = SheetProcessing.ProcessSheet(userId, input, mangaInfo, RunBackroundManagGet);

                        if (mangaGuid != null)
                            return new { Success = true, VisId = GetMangaVis(inctrak_id, userId, mangaGuid), StatusData = MangaFactory.StartingStatus("CSV Upload") };
                    }
                }
            }
            catch (VisAlyzerLicenseException licExcp)
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