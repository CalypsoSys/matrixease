using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Util.Store;
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
using System.Text.RegularExpressions;
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
        private const int _specSize = 6;

        private static Regex _cleanName = new Regex("[^a-zA-Z0-9 -]");
        private static List<MyPerformance> _perfStats;
        private static CancellationTokenSource _tokenSource;
        private BackgroundWorker _processWorker = new BackgroundWorker();

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

            _processWorker.WorkerReportsProgress = true;
            _processWorker.WorkerSupportsCancellation = true;
            _processWorker.ProgressChanged += _processWorker_ProgressChanged;
            _processWorker.DoWork += _processWorker_DoWork;
            _processWorker.RunWorkerCompleted += _processWorker_RunWorkerCompleted;
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


        private void _canelBtn_Click(object sender, EventArgs e)
        {
            _canelBtn.Enabled = false;
            _processWorker.CancelAsync();
        }

        private void EnDisAll(bool enable)
        {
            _canelBtn.Enabled = !enable;
            _testsLst.Enabled = enable;
            _typeCmb.Enabled = enable;
            _sepTxt.Enabled = enable;
            _spec1Txt.Enabled = enable;
            _spec2Txt.Enabled = enable;
            _browseBtn.Enabled = enable;
            _addBtn.Enabled = enable;
            _saveBtn.Enabled = enable;
            _runBtn.Enabled = enable;
            _baseLineChk.Enabled = enable;
        }

        private void _runBtn_Click(object sender, EventArgs e)
        {
            EnDisAll(false);
            _outputTxt.Clear();
            _processWorker.RunWorkerAsync(_baseLineChk.Enabled);
            _tabCtrl.SelectedIndex = 1;
        }

        void _processWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            bool isBaseline = (bool)e.Argument;
            _tokenSource = new CancellationTokenSource();
            try
            {
                foreach (var row in Specs())
                {
                    CheckCacellation();

                    _processWorker.ReportProgress(0, string.Format("Processing {0}", row[2]));

                    int headerRow = 1;
                    int headerRows = 1;
                    int maxRows = 0;
                    bool ignoreBlankRows = true;
                    bool ignoreTextCase = true;
                    bool trimLeadingWhitespace = true;
                    bool trimTrailingWhitespace = true;
                    string ignoreCols = null;
                    string sheetType = row[0].ToLower();

                    _perfStats = new List<MyPerformance>();
                    dynamic matrixTest = new ExpandoObject();
                    MyStopWatch stopWatch = MyStopWatch.StartNew("total_test_time", "MatrixEase Test Run");

                    stopWatch.StartSubTime("init_time", "MatrixEase Initialization");
                    Dictionary<string, object> extraDetails = new Dictionary<string, object>();
                    extraDetails.Add("type", row[0]);
                    extraDetails.Add("separator", row[1]);
                    extraDetails.Add("location", row[2]);
                    extraDetails.Add("spec", row[3]);
                    Guid? mangaGuid = null;
                    string testName = null;
                    if (sheetType == "csv" || sheetType == "excel")
                    {
                        testName = Path.GetFileNameWithoutExtension(row[2]);
                        FileInfo fi = new FileInfo(row[2]);
                        extraDetails.Add("file_size", fi.Length);

                        var sheetSpec = new Dictionary<string, string> { { MangaConstants.CsvSeparator, row[1] }, { MangaConstants.CsvQuote, "\"" }, { MangaConstants.CsvEscape, "\"" }, { MangaConstants.CsvNull, "" }, { MangaConstants.CsvEol, "\r\n" } };
                        MangaInfo mangaInfo = new MangaInfo(row[2], testName, headerRow, headerRows, maxRows, ignoreBlankRows, ignoreTextCase, trimLeadingWhitespace, trimTrailingWhitespace, ignoreCols, sheetType, sheetSpec);
                        using (var input = new StreamReader(row[2]))
                        {
                            mangaGuid = SheetProcessing.ProcessSheet(MatrixEaseIdentifier, input.BaseStream, mangaInfo, RunBackroundManagGet);
                        }
                    }
                    else if (sheetType == "google")
                    {
                        var sheetSpec = new Dictionary<string, string> { { MangaConstants.SheetID, row[2] }, { MangaConstants.SheetRange, row[3] } };
                        MangaInfo mangaInfo = new MangaInfo("", row[2], headerRow, headerRows, maxRows, ignoreBlankRows, ignoreTextCase, trimLeadingWhitespace, trimTrailingWhitespace, ignoreCols, sheetType, sheetSpec);

                        UserCredential credential = GoogsAuth.AuthenticateLocal(row[4], row[5]);
                        mangaGuid = SheetProcessing.ProcessSheet(credential, MatrixEaseIdentifier, mangaInfo, RunBackroundManagGet);
                        
                        testName = _cleanName.Replace(mangaInfo.OriginalName, "");
                    }
                    CheckCacellation();
                    matrixTest.ExtraDetails = extraDetails;

                    var myManga = Tuple.Create(MatrixEaseIdentifier, mangaGuid.Value);
                    stopWatch.StartSubTime("load_display_matrix", "Loading the MatrixEase for Display");
                    string mangaName;
                    var mangaDisplay = MangaState.LoadManga(myManga, true, -1, new MangaLoadOptions(true), out mangaName);
                    CheckCacellation();

                    var matrixDisplayData = mangaDisplay.ReturnMatrixEase();
                    CheckCacellation();

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
                        CheckCacellation();

                        Dictionary<string, object> colData = new Dictionary<string, object>();
                        int index = (int)col.Value.Index;
                        stopWatch.StartSubTime("load_column_stats", "Loading the MatrixEase Columns Stats");
                        colData.Add("col_stats", manga.ReturnColStats(index));

                        double lastHighestPct = 0;
                        string nodeToMeasure = "";
                        string nodeForRows = "";
                        foreach (var node in col.Value.Values)
                        {
                            double totalPct = node.TotalPct;
                            if (totalPct > lastHighestPct)
                            {
                                nodeToMeasure = node.ColumnValue;
                                if (totalPct > 40)
                                    break;
                                lastHighestPct = totalPct;
                            }
                        }
                        _perfStats.Add(stopWatch.StopSubTime());

                        var selectedNode = string.Format("{0}@{1}:{2}", nodeToMeasure, col.Key, index);
                        if (measures.Count > 0)
                        {
                            CheckCacellation();
                            colData.Add("col_measures", manga.GetMeasureStats(selectedNode, measures.ToArray()));
                        }
                        /* TODO
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

                    string ext = isBaseline ? "base" : DateTime.Now.ToString("yyyyMMddHHmmss");
                    System.IO.File.WriteAllText(Path.ChangeExtension(Path.Combine(_testsPath, testName), ext), json);
                }
            }
            catch(Exception excp)
            {
                _processWorker.ReportProgress(0, excp.ToString());
            }
        }

        private void CheckCacellation()
        {
            if (_processWorker.CancellationPending)
            {
                _tokenSource.Cancel();
                _processWorker.ReportProgress(0, "Cancelling");
                throw new OperationCanceledException();
            }
        }

        void _processWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            try
            {
                _outputTxt.AppendText(e.UserState as string);
            }
            catch (Exception excp)
            {
                _outputTxt.AppendText(excp.ToString());
            }
            _outputTxt.AppendText("\r\n");
        }

        void _processWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            EnDisAll(true);
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
                job.Process(_tokenSource.Token);
            }
        }

        private static void PerformanceLogger(string mangaName, MyPerformance perf)
        {
            _perfStats.Add(perf);
        }
    }
}
