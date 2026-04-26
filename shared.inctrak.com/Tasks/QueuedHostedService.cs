using MatrixEase.Web.Common;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MatrixEase.Web.Tasks
{
    public class QueuedHostedService : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly AppSettings _settings;
        private SemaphoreSlim _signals;

        public QueuedHostedService(IBackgroundTaskQueue taskQueue, ILoggerFactory loggerFactory, IOptions<AppSettings> options)
        {
            TaskQueue = taskQueue;
            _logger = loggerFactory.CreateLogger<QueuedHostedService>();
            _settings = options.Value;
            _signals = new SemaphoreSlim(options.Value.MaxConcurrentJobs);
        }

        public IBackgroundTaskQueue TaskQueue { get; }

        protected async override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Queued Hosted Service is starting.");

            while (!cancellationToken.IsCancellationRequested)
            {
                var workItem = await TaskQueue.DequeueAsync(cancellationToken);

                try
                {
                    await _signals.WaitAsync(cancellationToken);
                    _ = Task.Run( () => { 
                        workItem(cancellationToken);
                        _signals.Release();
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                       "Error occurred executing {WorkItem}.", nameof(workItem));
                    ErrorLogWriter.LogError(_settings, ex, "Error occurred executing {0}.", nameof(workItem));
                }
            }

            _logger.LogInformation("Queued Hosted Service is stopping.");
        }
    }
}
