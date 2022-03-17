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
        private char _csvSeparator = ',';
        private char _csvQuote = '"';
        private char _csvEscape = '"';
        private char _csvNull = '\0';
        private char _csvEOL1 = '\r';
        private char _csvEOL2 = '\n';

        public MangaFactoryFromCSVFile(string userId, MangaInfo mangaInfo) : base(userId, mangaInfo)
        {
            switch (mangaInfo.GetExtraInfo(MangaConstants.CsvSeparator))
            {
                case "tab":
                    _csvSeparator = '\t';
                    break;
                case "space":
                    _csvSeparator = ' ';
                    break;
                case "pipe":
                    _csvSeparator = '|';
                    break;
                case "colon":
                    _csvSeparator = ':';
                    break;
                case "semicolon":
                    _csvSeparator = ';';
                    break;
                case "comma":
                default:
                    _csvSeparator = ',';
                    break;
            }
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
            using (CsvParser parser = new CsvParser(_rows, _csvSeparator, _csvQuote, _csvEscape, _csvNull, _csvEOL1, _csvEOL2))
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
