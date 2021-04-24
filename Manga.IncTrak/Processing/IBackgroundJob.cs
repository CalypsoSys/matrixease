using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Manga.IncTrak.Processing
{
    public interface IBackgroundJob : IDisposable
    {
        void Process(CancellationToken token);
        bool IsCancellationRequested { get; }
        void SetStatus(MangaFactoryStatusKey key, string desc, MangaFactoryStatusState status);
    }
}
