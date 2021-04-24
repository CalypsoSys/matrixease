using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Manga.IncTrak.Manga
{
    public class MangaLoadOptions
    {
        public bool ForDisplayOnly { get; private set; }
        public int[] InlcudeCols { get; set; }

        public MangaLoadOptions(bool forDisplayOnly )
        {
            ForDisplayOnly = forDisplayOnly;
        }
    }
}
