using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Manga.IncTrak.Manga
{
    public interface IMangaSerializeInline
    {
        Int32 Version { get; }
        void Save(IMangaSerializationWriter writer );
        void Load(Int32 version, IMangaSerializationReader reader, MangaLoadOptions loadOptions);
    }
}
