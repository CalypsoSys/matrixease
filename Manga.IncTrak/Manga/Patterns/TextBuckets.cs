using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Manga.IncTrak.Manga
{
    public enum TextBuckets
    {
        Natural = 0,
        Prefix1 = 1,
        Prefix2 = 2,
        Prefix3 = 3,
        Prefix4 = 4,
        CoupleWords = 5,
        LotsOfWords = 6,
        UrlThirdPart = 7,
        UrlSecondPart = 8,
        UrlFirst = 10,
        UrlDomain = 11,
        UrlScheme = 12,
        UrlPathParts = 13,
        Length = 14,
    }
}
