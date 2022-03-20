using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MatrixEase.Manga.Manga
{
    public interface IMangaSerializeFile
    {
        void Save(string path);
        bool Load(string path, MangaLoadOptions loadOptions);
    }
}
