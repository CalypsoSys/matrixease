using MatrixEase.Manga.Expression;
using MatrixEase.Manga.Manga;
using MatrixEase.Manga.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MatrixEase.Manga.Processing
{
    public class BackgroundFilter : BackgroundAction
    {
        private string _selectionExpression;

        public BackgroundFilter(Tuple<string, Guid> mxesId, string selectionExpression) : base(mxesId)
        {
            _selectionExpression = selectionExpression;
        }

        public override void SetStatusExtra(Dictionary<MangaFactoryStatusKey, MangaFactoryStatus> status)
        {
        }

        public override void Process(CancellationToken cancellationToken)
        {
            CancelToken = cancellationToken;
            SetStatus(MangaFactoryStatusKey.Queued, "Filter execution started", MangaFactoryStatusState.Complete);

            SetStatus(MangaFactoryStatusKey.PreProcess, "Loading MatrixEase for filtering", MangaFactoryStatusState.Started);
            DataManga manga = MangaState.LoadManga(MatrixId, false, -1, new MangaLoadOptions(false));
            SetStatus(MangaFactoryStatusKey.PreProcess, "Loading MatrixEase for filtering", MangaFactoryStatusState.Complete);

            if (string.IsNullOrWhiteSpace(_selectionExpression) == false)
            {
                SetStatus(MangaFactoryStatusKey.Analyzing, "MatrixEase filter", MangaFactoryStatusState.Started);
                ExpressionCtl exprCtl = new ExpressionCtl(_selectionExpression);
                MyBitArray bitmap = exprCtl.ProcessBitmaps(manga);
                SetStatus(MangaFactoryStatusKey.Analyzing, "MatrixEase filter", MangaFactoryStatusState.Complete);

                if (bitmap != null)
                {
                    SetStatus(MangaFactoryStatusKey.Saving, "MatrixEase filter", MangaFactoryStatusState.Started);
                    MangaFilter mangaFilter = MangaState.SaveMangaFilter(MatrixId, _selectionExpression, bitmap);
                    SetStatus(MangaFactoryStatusKey.Saving, "MatrixEase filter", MangaFactoryStatusState.Complete);

                    SetStatus(MangaFactoryStatusKey.Processing, "Applying MatrixEase filters", MangaFactoryStatusState.Started);
                    manga.ApplyFilter(mangaFilter, true, -1);
                    SetStatus(MangaFactoryStatusKey.Processing, "Applying MatrixEase filters", MangaFactoryStatusState.Complete);
                }
            }
            else
            {
                SetStatus(MangaFactoryStatusKey.Saving, "MatrixEase filter", MangaFactoryStatusState.Started);
                MangaFilter mangafilter = MangaState.SaveMangaFilter(MatrixId, "", null);
                SetStatus(MangaFactoryStatusKey.Saving, "MatrixEase filter", MangaFactoryStatusState.Complete);

                SetStatus(MangaFactoryStatusKey.Processing, "Applying MatrixEase filters", MangaFactoryStatusState.Started);
                manga.ApplyFilter(mangafilter, false, -1);
                SetStatus(MangaFactoryStatusKey.Processing, "Applying MatrixEase filters", MangaFactoryStatusState.Complete);
            }

            AddPickup(manga.ReturnMatrixEase());
        }
    }
}
