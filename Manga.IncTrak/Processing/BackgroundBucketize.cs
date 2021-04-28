using Manga.IncTrak.Manga;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Manga.IncTrak.Processing
{
    public class BackgroundBucketize : BackgroundAction
    {
        private string _columnName;
        private int _columnIndex;
        private bool _bucketized;
        private int _bucketSize;
        private decimal _bucketMod;

        public BackgroundBucketize(Tuple<string, Guid> visId, string columnName, int columnIndex, bool bucketized, int bucketSize, decimal bucketMod) : base(visId)
        {
            _columnName = columnName;
            _columnIndex = columnIndex;
            _bucketized = bucketized;
            _bucketSize = bucketSize;
            _bucketMod = bucketMod;
        }

        public override void SetStatusExtra(Dictionary<MangaFactoryStatusKey, MangaFactoryStatus> status)
        {
        }

        public override void Process(CancellationToken cancellationToken)
        {
            CancelToken = cancellationToken;
            SetStatus(MangaFactoryStatusKey.Queued, "Bucketization execution started", MangaFactoryStatusState.Complete);

            SetStatus(MangaFactoryStatusKey.PreProcess, "Loading Manga for bucketization", MangaFactoryStatusState.Started);
            var manga = MangaState.LoadManga(VisId, true, _columnIndex, new MangaLoadOptions(false));
            SetStatus(MangaFactoryStatusKey.PreProcess, "Loaded Manga for bucketization", MangaFactoryStatusState.Complete);

            SetStatus(MangaFactoryStatusKey.Analyzing, "Manga bucketization", MangaFactoryStatusState.Started);
            ColumnDefBucket column = manga.ReBucketize(_columnIndex, _bucketized, _bucketSize, _bucketMod, this);
            SetStatus(MangaFactoryStatusKey.Analyzing, "Manga bucketization", MangaFactoryStatusState.Complete);

            SetStatus(MangaFactoryStatusKey.Saving, "Manga bucketization", MangaFactoryStatusState.Started);
            MangaState.SaveMangaBuckets(VisId, _columnIndex, column);
            SetStatus(MangaFactoryStatusKey.Saving, "Manga bucketization", MangaFactoryStatusState.Complete);

            SetStatus(MangaFactoryStatusKey.Processing, "Applying current Manga filters", MangaFactoryStatusState.Started);
            if (column != null)
            {
                manga.ApplyFilterToBucket(column);
            }
            SetStatus(MangaFactoryStatusKey.Processing, "Appled current Manga filters", MangaFactoryStatusState.Complete);

            AddPickup(manga.ReturnVis());
        }
    }
}
