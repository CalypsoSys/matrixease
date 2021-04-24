using Manga.IncTrak.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Manga.IncTrak.Processing
{
    public abstract class MangaJobFactory : MyDisposable, IBackgroundJob
    {
        private CancellationToken? _cancellationToken;
        private volatile object _lockStatus = new object();
        private Dictionary<MangaFactoryStatusKey, MangaFactoryStatus> _status = new Dictionary<MangaFactoryStatusKey, MangaFactoryStatus>();

        public abstract void Process(CancellationToken token);
        public abstract void SetStatusExtra(Dictionary<MangaFactoryStatusKey, MangaFactoryStatus> status);

        protected Dictionary<MangaFactoryStatusKey, MangaFactoryStatus> Status 
        {
            get
            {
                foreach (var val in _status.Values)
                    val.UpdateElapsed();
                return _status;
            }
        }

        protected CancellationToken CancelToken
        {
            set
            {
                _cancellationToken = value;
            }
        }

        public static object StartingStatus(string what)
        {
            var startStatus = new Dictionary<MangaFactoryStatusKey, MangaFactoryStatus>();
            startStatus.Add(MangaFactoryStatusKey.PreProcess, new MangaFactoryStatus(string.Format("Initializing {0}", what), MangaFactoryStatusState.Starting));
            return startStatus;
        }

        public bool IsCancellationRequested
        {
            get
            {
                if (_cancellationToken.HasValue)
                    return _cancellationToken.Value.IsCancellationRequested;
                return false;
            }
        }

        public void SetStatus(MangaFactoryStatusKey key, string desc, MangaFactoryStatusState status)
        {
            lock (_lockStatus)
            {
                if (_status.ContainsKey(key) == false)
                    _status.Add(key, new MangaFactoryStatus(desc, status));
                else
                    _status[key].Update(desc, status);

                SetStatusExtra(_status);
            }
        }
    }
}
