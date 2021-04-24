using ExcelDataReader;
using Manga.IncTrak.Manga;
using Manga.IncTrak.Utility;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Manga.IncTrak.Processing
{
    public class MangaFactoryFromExcelFile : MangaUploadFactory
    {
        private string _uploadFile;
        private FileStream _stream;

        public MangaFactoryFromExcelFile(string userId, MangaInfo mangaInfo) : base(userId, mangaInfo)
        {
            Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        }

        public override void SetUploadWorkFile(string uploadFile)
        {
            _uploadFile = uploadFile;
            _stream = File.Open(_uploadFile, FileMode.Open, FileAccess.Read);
        }

        protected override void Cleanup()
        {
            MiscHelpers.SafeDispose(_stream);
            StorageHelpers.SafeFileDelete(_uploadFile);
        }

        protected override IEnumerable<IList<object>> RowIterator(IBackgroundJob status)
        {
            using (var reader = ExcelReaderFactory.CreateReader(_stream))
            {
                int rowCout = reader.RowCount;
                while (reader.Read()) //Each ROW
                {
                    var statusCheck = IsStatusCheck(status);
                    if (statusCheck == RowStatus.Break)
                        break;
                    else if (statusCheck == RowStatus.Check)
                        SendStatus(rowCout, -1);

                    object[] row = new object[reader.FieldCount];
                    for (int column = 0; column < reader.FieldCount; column++)
                    {
                        var data = reader.GetValue(column);//Get Value returns object
                        if (data != null)
                            row[column] = data.ToString(); // TDOD
                    }

                    // FUTURE????? while (reader.NextResult()); //Move to NEXT SHEET

                    yield return row;
                }
            }
        }
    }
}
