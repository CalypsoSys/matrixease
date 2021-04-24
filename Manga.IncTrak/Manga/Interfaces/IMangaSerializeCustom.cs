using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Manga.IncTrak.Manga
{
    public interface IMangaSerializeCustom : IMangaSerializeInline
    {
        void Deserialize(Stream serializationStream);
        void Serialize(Stream serializationStream);
    }
}
