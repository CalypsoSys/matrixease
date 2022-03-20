using MatrixEase.Manga.Manga;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MatrixEase.Manga.Processing
{
    public class MangaFactoryFromList : MangaFactory
    {
        private IList<IList<object>> _rows;

        public MangaFactoryFromList(string userId, MangaInfo mangaInfo) : base(userId, mangaInfo)
        {
        }

        public void SetUploadWorkFile(IList<IList<object>> rows)
        {
            _rows = rows;
        }

        protected override void Cleanup()
        {
        }

        protected override IEnumerable<IList<object>> RowIterator(IBackgroundJob status)
        {
            int rowCount = _rows.Count;
            foreach (IList<object> row in _rows)
            {
                var statusCheck = IsStatusCheck(status);
                if (statusCheck == RowStatus.Break)
                    break;
                else if (statusCheck == RowStatus.Check)
                    SendStatus(rowCount, -1);

                yield return row;
            }
        }
    }
}
