using MatrixEase.Manga.Manga;
using MatrixEase.Manga.Manga.Serialization;
using MatrixEase.Manga.Processing;
using MatrixEase.Manga.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.ListViewItem;

namespace MatrixEase.Tester
{
    public partial class Form1 : Form
    {
        public const string MatrixEaseIdentifier = "matrixease.tester";
        private const string _specFile = @"C:\CalypsoSystems\mangadata\matrixease\tester\tester.spec";
        private const string _testsPath = @"C:\CalypsoSystems\mangadata\matrixease\tester\tests";
        private const int _specSize = 4;

        private static List<MyPerformance> _perfStats = new List<MyPerformance>();

        public Form1()
        {
            InitializeComponent();

            MangaRoot.SetRootFolder(_testsPath);
            MangaState.SetPerformanceLogger(PerformanceLogger);
            MangaState.SetUserMangaCatalog(MatrixEaseIdentifier, MatrixEaseIdentifier, MatrixEaseIdentifier, MangaAuthType.Testing);

            foreach (var row in Specs())
            {
                _testsLst.Items.Add(new ListViewItem(row.Take(_specSize).ToArray()));
            }
        }

        private void _browseBtn_Click(object sender, EventArgs e)
        {
            if (_openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                _spec1Txt.Text = _openFileDialog.FileName;
            }
        }

        private void _addBtn_Click(object sender, EventArgs e)
        {
            List<string> parts = new List<string>();
            parts.Add(_typeCmb.SelectedItem as string);
            parts.Add(_sepTxt.Text);
            parts.Add(_spec1Txt.Text);
            parts.Add(_spec2Txt.Text);

            if (string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[2]) || parts[1].Length > 1)
            {
                MessageBox.Show("Invalid Entries, try again");
            }
            else
            {
                _testsLst.Items.Add(new ListViewItem(parts.ToArray()));
            }
        }

        private void _saveBtn_Click(object sender, EventArgs e)
        {
            using (StreamWriter spec = new StreamWriter(_specFile))
            {
                foreach(ListViewItem item in _testsLst.Items)
                {
                    List<string> parts = new List<string>();
                    foreach (ListViewSubItem cell in item.SubItems)
                    {
                        spec.Write("\"{0}\",", cell.Text);
                    }
                    spec.WriteLine();
                }
            }
        }

        private void _runBtn_Click(object sender, EventArgs e)
        {
            foreach (var row in Specs())
            {
                if (row[0] == "CSV")
                {
                    using (var input = new StreamReader(row[2]))
                    {
                        var fileName = Path.GetFileName(row[2]);
                        int headerRow = 1;
                        int headerRows = 1;
                        int maxRows = 0;
                        bool ignoreBlankRows = true;
                        bool ignoreTextCase = true;
                        bool trimLeadingWhitespace = true;
                        bool trimTrailingWhitespace = true;
                        string ignoreCols = null;
                        string sheetType = "csv";
                        var sheetSpec = new Dictionary<string, string> { { MangaConstants.CsvSeparator, row[1] }, { MangaConstants.CsvQuote, "\"" }, { MangaConstants.CsvEscape, "\"" }, { MangaConstants.CsvNull, "" }, { MangaConstants.CsvEol, "\r\n" } };

                        _perfStats = new List<MyPerformance>();
                        MyStopWatch stopWatch = MyStopWatch.StartNew("total_test_time", "MatrixEase Test Run");

                        stopWatch.StartSubTime("init_time", "MatrixEase Initialization");
                        MangaInfo mangaInfo = new MangaInfo(row[2], fileName, headerRow, headerRows, maxRows, ignoreBlankRows, ignoreTextCase, trimLeadingWhitespace, trimTrailingWhitespace, ignoreCols, sheetType, sheetSpec);
                        Guid? mangaGuid = SheetProcessing.ProcessSheet("matrixease.tester", input.BaseStream, mangaInfo, RunBackroundManagGet);
                        
                        var myManga = Tuple.Create(MatrixEaseIdentifier, mangaGuid.Value);
                        stopWatch.StartSubTime("load_display_matrix", "Loading the MatrixEase");
                        string mangaName;
                        var manga = MangaState.LoadManga(myManga, true, -1, new MangaLoadOptions(true), out mangaName);
                        var matrixDisplayData = manga.ReturnMatrixEase();
                        _perfStats.Add(stopWatch.StopSubTime());

                        stopWatch.StartSubTime("delete_matrix", "Delete the MatrixEase");
                        MangaState.DeleteManga(myManga);
                        _perfStats.Add(stopWatch.StopSubTime());

                        _perfStats.Add(stopWatch.Stop());

                        MangaFactory.GetStatus(MatrixEaseIdentifier, mangaGuid.Value);
                    }
                }
            }
        }

        private IEnumerable<IList<string>> Specs()
        {
            using (StreamReader spec = new StreamReader(_specFile))
            {
                using (CsvParser parser = new CsvParser(spec, ',', '"', '"', '\0', '\r', '\n'))
                {
                    foreach (var row in parser.ReadParseLine())
                    {
                        List<string> parts = new List<string>();
                        foreach (string cell in row)
                        {
                            parts.Add(cell);
                        }
                        yield return parts;
                    }
                }
            }
        }

        public static void RunBackroundManagGet(IBackgroundJob job)
        {
            using (job)
            {
                job.Process(CancellationToken.None);
            }
        }

        private static void PerformanceLogger(string mangaName, MyPerformance perf)
        {
            _perfStats.Add(perf);
        }
    }
}
