using Manga.IncTrak.Manga;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Manga.IncTrak.Processing
{
    public abstract class MangaUploadFactory : MangaFactory
    {
        public abstract void SetUploadWorkFile(string uploadFile);

        public MangaUploadFactory(string userId, MangaInfo mangaInfo) : base(userId, mangaInfo)
        {
        }
    }
}
