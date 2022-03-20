using MatrixEase.Manga.Manga;
using MatrixEase.Manga.Processing;
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

        public static Tuple<string, Guid> VisId(string mangaGuid)
        {
            return Tuple.Create(UserId, new Guid(HttpUtility.UrlDecode(mangaGuid)));
        }

        public static void RunBackroundManagGet(IBackgroundJob job)
        {
            Task.Run(() => {
                using (job) { job.Process(CancellationToken.None); }
                }
            );
        }
    }
}
