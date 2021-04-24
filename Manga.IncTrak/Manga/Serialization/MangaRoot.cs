using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Manga.IncTrak.Manga.Serialization
{
    public static class MangaRoot
    {
        public static string Folder { get; private set; }

        public static void SetRootFolder(string folder)
        {
            Folder = folder;
        }
    }
}
