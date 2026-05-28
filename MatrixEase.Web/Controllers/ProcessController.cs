using System;
using MatrixEase.Manga.Processing;
using MatrixEase.Manga.Utility;
using MatrixEase.Web.Tasks;

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
                try
                {
                    using (job)
                    {
                        job.Process(token);
                    }
                }
                catch (Exception excp)
                {
                    SimpleLogger.LogError(excp, "Running job");
                }
            });
        }
    }
}
