using Manga.IncTrak.Manga;
using Manga.IncTrak.Utility;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Manga.IncTrak.Processing
{
    public class MangaFactoryFromCSVFile : MangaUploadFactory
    {
        private string _uploadFile;
        private StreamReader _rows;

        public MangaFactoryFromCSVFile(string userId, MangaInfo mangaInfo) : base(userId, mangaInfo)
        {
        }

        public override void SetUploadWorkFile(string uploadFile)
        {
            _uploadFile = uploadFile;
            _rows = new StreamReader(_uploadFile);
        }

        protected override void Cleanup()
        {
            MiscHelpers.SafeDispose(_rows);
            StorageHelpers.SafeFileDelete(_uploadFile);
        }

        protected override IEnumerable<IList<object>> RowIterator(IBackgroundJob status)
        {
            using (CsvParser parser = new CsvParser(_rows))
            {
                foreach(var row in parser.ReadParseLine())
                {
                    var statusCheck = IsStatusCheck(status);
                    if (statusCheck == RowStatus.Break )
                        break;
                    else if (statusCheck == RowStatus.Check )
                        SendStatus(-1, parser.PctRead);

                    yield return row;
                }
            }
        }
    }
}
