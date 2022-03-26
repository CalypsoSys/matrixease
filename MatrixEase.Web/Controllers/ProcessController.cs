using MatrixEase.Web.Tasks;
using MatrixEase.Manga.Manga;
using MatrixEase.Manga.Processing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;

namespace MatrixEase.Web.Controllers
{
    public abstract class ProcessController : AuthBaseController
    {
        private IBackgroundTaskQueue Queue { get; }

        protected ProcessController(IBackgroundTaskQueue queue)
        {
            Queue = queue;
        }

        protected void RunBackroundManagGet(IBackgroundJob job)
        {
            Queue.QueueBackgroundWorkItem(async token =>
            {
                using (job)
                {
                    job.Process(token);
                }
            });
        }

        protected string GetMangaVis(string matrixease_id, string userId, Guid? mangaGuid)
        {
            if (mangaGuid.HasValue)
            {
                CookieOptions option = new CookieOptions();
                option.Expires = DateTime.Now.AddMinutes(30);
                Response.Cookies.Append("authenticated-accepted-3", matrixease_id, option);

                var mxesId = Encode(userId, mangaGuid.Value);
                return mxesId;
            }

            return null;
        }

        protected void ClearCookies()
        {
            foreach (var cookie in Request.Cookies.Keys.ToList())
            {
                if (cookie != "cookies-accepted-1" && cookie != "MxesEmailClaimId" && cookie != "authenticated-accepted-1" && cookie != "authenticated-accepted-2" && cookie != "authenticated-accepted-3")
                    Response.Cookies.Delete(cookie);
            }
        }
    }
}
