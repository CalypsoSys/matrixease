using ExcelDataReader;
using System;
using System.IO;
using System.Text;

namespace ExcelReader
{
    class Program
    {
        private const int Mod = 100000;
        private static string TestFileXls = @"C:\Users\Joe\Downloads\vehicles.csv\vehicles_xls.xls";
        //private static string TestFileXls = @"C:\Users\Joe\Downloads\vehicles.csv\simple_test_xls.xls";
        private static string TestFileXlsx = @"C:\Users\Joe\Downloads\vehicles.csv\vehicles_xlsx.xlsx";

        static void Main(string[] args)
        {
            Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            var filePath = TestFileXls;

            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
            {
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    reader.Reset();
                    int i = 0;
                    do
                    {
                        while (reader.Read()) //Each ROW
                        {
                            object[] data = new object[reader.FieldCount];
                            for (int column = 0; column < reader.FieldCount; column++)
                            {
                                data[column] = reader.GetValue(column);//Get Value returns object
                            }
                            if ((i % Mod) == 0)
                                Console.WriteLine("at {0}", i);
                            ++i;
                        }
                    } while (reader.NextResult()); //Move to NEXT SHEET

                }
            }
        }
    }
}
