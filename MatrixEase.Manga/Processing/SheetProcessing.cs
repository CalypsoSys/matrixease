using Google;
using Google.Apis.Http;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using MatrixEase.Manga.Manga;
using MatrixEase.Manga.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatrixEase.Manga.Processing
{
    public static class SheetProcessing
    {
        public delegate void RunBackroundMangaProcess(IBackgroundJob job);

        public static  Guid? ProcessSheet(IConfigurableHttpClientInitializer credential, string googleId, MangaInfo mangaInfo, RunBackroundMangaProcess process)
        {
            string spreadSheetId = mangaInfo.GetExtraInfo(MangaConstants.SheetID);
            string spreadSheetRange = mangaInfo.GetExtraInfo(MangaConstants.SheetRange);

            try
            {
                var service = new SheetsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "MatrixEase",
                });

                // Define request parameters.
                SpreadsheetsResource.GetRequest sheetRequest = service.Spreadsheets.Get(spreadSheetId);
                Spreadsheet sheet = sheetRequest.Execute();
                mangaInfo.OriginalName = sheet.Properties.Title;
                var mangaFactory = new MangaFactoryFromList(googleId, mangaInfo);
                mangaFactory.SetStatus(MangaFactoryStatusKey.PreProcess, "Loading data from Google Sheet", MangaFactoryStatusState.Started);

                SpreadsheetsResource.ValuesResource.GetRequest request = service.Spreadsheets.Values.Get(spreadSheetId, spreadSheetRange);
                ValueRange response = request.Execute();
                IList<IList<object>> values = response.Values;
                mangaFactory.SetStatus(MangaFactoryStatusKey.PreProcess, "Loading data from Google Sheet", MangaFactoryStatusState.Complete);
                if (values != null && values.Count > 0)
                {
                    mangaFactory.SetUploadWorkFile(values);
                    mangaFactory.SetStatus(MangaFactoryStatusKey.Queued, "Queing job for execution", MangaFactoryStatusState.Started);
                    process(mangaFactory);
                    return mangaFactory.WorkSet;
                }
            }
            catch(GoogleApiException googs)
            {
                throw;
            }
            catch (Exception excp)
            {
                SimpleLogger.LogError(excp, "Error processing googs sheet {0} {1}", spreadSheetId, spreadSheetRange);
            }

            return null;
        }

        public static Guid ProcessSheet(string userId, Stream uploadFile, MangaInfo mangaInfo, RunBackroundMangaProcess process)
        {
            MangaState.CheckProjectCount(userId);

            MangaUploadFactory mangaFactory;
            if (mangaInfo.SheetType == "excel")
                mangaFactory = new MangaFactoryFromExcelFile(userId, mangaInfo);
            else
                mangaFactory = new MangaFactoryFromCSVFile(userId, mangaInfo);

            string storeMessage = string.Format("Storing data from {0}", mangaInfo.SheetType);
            mangaFactory.SetStatus(MangaFactoryStatusKey.PreProcess, storeMessage, MangaFactoryStatusState.Started);

            string workFolder = MangaState.MangaWorkFolder(userId);
            string uploadWorkFile = Path.Combine(workFolder, string.Format("{0}.upload", mangaFactory.WorkSet.ToString("N")));
            using (FileStream uploadStream = System.IO.File.Create(uploadWorkFile))
            {
                uploadFile.CopyTo(uploadStream);
            }
            mangaFactory.SetStatus(MangaFactoryStatusKey.PreProcess, storeMessage, MangaFactoryStatusState.Complete);
            mangaFactory.SetUploadWorkFile(uploadWorkFile);

            mangaFactory.SetStatus(MangaFactoryStatusKey.Queued, "Queing job for execution", MangaFactoryStatusState.Started);
            process(mangaFactory);
            return mangaFactory.WorkSet;
        }

    }
}
