using MatrixEase.Manga.Manga;
using MatrixEase.Manga.Manga.Serialization;
using MatrixEase.Manga.Processing;
using MatrixEase.Manga.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Dynamic;
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
                        dynamic matrixTest = new ExpandoObject();
                        MyStopWatch stopWatch = MyStopWatch.StartNew("total_test_time", "MatrixEase Test Run");

                        stopWatch.StartSubTime("init_time", "MatrixEase Initialization");
                        MangaInfo mangaInfo = new MangaInfo(row[2], fileName, headerRow, headerRows, maxRows, ignoreBlankRows, ignoreTextCase, trimLeadingWhitespace, trimTrailingWhitespace, ignoreCols, sheetType, sheetSpec);
                        Guid? mangaGuid = SheetProcessing.ProcessSheet("matrixease.tester", input.BaseStream, mangaInfo, RunBackroundManagGet);
                        
                        var myManga = Tuple.Create(MatrixEaseIdentifier, mangaGuid.Value);
                        stopWatch.StartSubTime("load_display_matrix", "Loading the MatrixEase for Display");
                        string mangaName;
                        var mangaDisplay = MangaState.LoadManga(myManga, true, -1, new MangaLoadOptions(true), out mangaName);
                        var matrixDisplayData = mangaDisplay.ReturnMatrixEase();
                        _perfStats.Add(stopWatch.StopSubTime());

                        matrixTest.MatrixDisplayData = matrixDisplayData;

                        dynamic matrix = JsonConvert.DeserializeObject<ExpandoObject>(JsonConvert.SerializeObject(matrixDisplayData), new ExpandoObjectConverter());

                        List<int> measures = new List<int>();
                        foreach (dynamic col in matrix.Columns)
                        {
                            int index = (int)col.Value.Index;
                            if (col.Value.ColType == "Measure")
                                measures.Add(index);
                        }

                        stopWatch.StartSubTime("load_full_matrix", "Loading the MatrixEase full");
                        var manga = MangaState.LoadManga(myManga, true, -1, new MangaLoadOptions(false), out mangaName);
                        _perfStats.Add(stopWatch.StopSubTime());

                        Dictionary<string, object> columns = new Dictionary<string, object>();
                        stopWatch.StartSubTime("load_columndata", "Loading the MatrixEase Columns");
                        foreach (dynamic col in matrix.Columns)
                        {
                            Dictionary<string, object> colData = new Dictionary<string, object>();
                            int index = (int)col.Value.Index;
                            stopWatch.StartSubTime("load_column_stats", "Loading the MatrixEase Columns Stats");
                            colData.Add("col_stats", manga.ReturnColStats(index));

                            double lastHighestPct = 0;
                            string columnValue = "";
                            foreach(var node in col.Value.Values)
                            {
                                double totalPct = node.TotalPct;
                                if (totalPct > lastHighestPct)
                                {
                                    columnValue = node.ColumnValue;
                                    if (totalPct > 40)
                                        break;
                                    lastHighestPct = totalPct;
                                }
                            }
                            _perfStats.Add(stopWatch.StopSubTime());

                            var selectedNode = string.Format("{0}@{1}:{2}", columnValue, col.Key, index);
                            if (measures.Count > 0)
                            {
                                colData.Add("col_measures", manga.GetMeasureStats(selectedNode, measures.ToArray()));
                            }
                            /* TODO
                            GetColumnMeasures pneumonia@finding:4
                            GetNodeRows
                            GetDependencyDiagram

                            filter
                            bucketize
                            export
                            */
                            columns.Add(col.Key, colData);
                        }
                        _perfStats.Add(stopWatch.StopSubTime());
                        matrixTest.ColumnData = columns;

                        stopWatch.StartSubTime("delete_matrix", "Delete the MatrixEase");
                        MangaState.DeleteManga(myManga);
                        _perfStats.Add(stopWatch.StopSubTime());

                        _perfStats.Add(stopWatch.Stop());

                        matrixTest.PerformanceStats = _perfStats;

                        MangaFactory.GetStatus(MatrixEaseIdentifier, mangaGuid.Value);

                        string json = JsonConvert.SerializeObject(matrixTest);

                        System.IO.File.WriteAllText(Path.ChangeExtension(Path.Combine(_testsPath, Path.GetFileNameWithoutExtension(fileName)), "base"), json);
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
