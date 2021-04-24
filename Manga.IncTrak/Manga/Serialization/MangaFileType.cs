using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Manga.IncTrak.Manga
{
    public enum MangaFileType
    {
        catalog,
        datamap,
        settings,
        coldef,
        coldata,
        colindex,
        colnulls,
        textpatterns,
        datepatterns,
        numericpatterns,
        filter,
        bucket,
        mangainfo
    }
}
