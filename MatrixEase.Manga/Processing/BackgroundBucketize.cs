using MatrixEase.Manga.Manga;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MatrixEase.Manga.Processing
{
    public class BackgroundBucketize : BackgroundAction
    {
        private string _columnName;
        private int _columnIndex;
        private bool _bucketized;
        private int _bucketSize;
        private decimal _bucketMod;

        public BackgroundBucketize(Tuple<string, Guid> mxesId, string columnName, int columnIndex, bool bucketized, int bucketSize, decimal bucketMod) : base(mxesId)
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

            SetStatus(MangaFactoryStatusKey.PreProcess, "Loading MatrixEase for bucketization", MangaFactoryStatusState.Started);
            var manga = MangaState.LoadManga(MatrixId, true, _columnIndex, new MangaLoadOptions(false));
            SetStatus(MangaFactoryStatusKey.PreProcess, "Loaded MatrixEase for bucketization", MangaFactoryStatusState.Complete);

            SetStatus(MangaFactoryStatusKey.Analyzing, "MatrixEase bucketization", MangaFactoryStatusState.Started);
            ColumnDefBucket column = manga.ReBucketize(_columnIndex, _bucketized, _bucketSize, _bucketMod, this);
            SetStatus(MangaFactoryStatusKey.Analyzing, "MatrixEase bucketization", MangaFactoryStatusState.Complete);

            SetStatus(MangaFactoryStatusKey.Saving, "MatrixEase bucketization", MangaFactoryStatusState.Started);
            MangaState.SaveMangaBuckets(MatrixId, _columnIndex, column);
            SetStatus(MangaFactoryStatusKey.Saving, "MatrixEase bucketization", MangaFactoryStatusState.Complete);

            SetStatus(MangaFactoryStatusKey.Processing, "Applying current MatrixEase filters", MangaFactoryStatusState.Started);
            if (column != null)
            {
                manga.ApplyFilterToBucket(column);
            }
            SetStatus(MangaFactoryStatusKey.Processing, "Appled current MatrixEase filters", MangaFactoryStatusState.Complete);

            AddPickup(manga.ReturnMatrixEase());
        }
    }
}
