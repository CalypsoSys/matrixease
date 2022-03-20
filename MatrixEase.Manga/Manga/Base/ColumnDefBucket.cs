using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MatrixEase.Manga.Manga
{
    public class ColumnDefBucket : ColumnDef
    {
        protected override MangaFileType FileType => MangaFileType.bucket;

        public ColumnDefBucket(int index) : base(index)
        {
        }
    }
}
