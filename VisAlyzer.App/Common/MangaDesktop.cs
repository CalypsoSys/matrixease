using Manga.IncTrak.Manga;
using Manga.IncTrak.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Desktop.Manga.IncTrak.Common
{
    public static class MangaDesktop
    {
        public const string AccessTokenAll = "visalyzer.app";
        public const string UserId = "visalyzer.app";

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
