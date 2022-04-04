using MatrixEase.Manga.Manga;
using MatrixEase.Manga.Processing;
using MatrixEase.Manga.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Desktop.MatrixEase.Manga.Common
{
    public static class MangaDesktop
    {
        public const string AccessTokenAll = "matrixease.app";
        public const string UserId = "matrixease.app";

        public static Tuple<string, Guid> MatrixId(string mangaGuid)
        {
            return Tuple.Create(UserId, new Guid(HttpUtility.UrlDecode(mangaGuid)));
        }

        public static void RunBackroundManagGet(IBackgroundJob job)
        {
            Task.Run(() => {
                try
                {
                    using (job)
                    {
                        job.Process(CancellationToken.None);
                    }
                }
                catch(Exception excp)
                {
                    MyLogger.LogError(excp, "Running job");
                }
            });
        }
    }
}
