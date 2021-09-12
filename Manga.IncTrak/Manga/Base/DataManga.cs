//#define PARALLELER
using Manga.IncTrak.Processing;
using Manga.IncTrak.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Manga.IncTrak.Manga
{
    public class DataManga : MangaSerialize
    {
        private const Int32 DataMangaVersion1 = 1;
        /// Start Serialized Items 
        private Int32 _totalRows = 0;
        private List<ColumnDef> _columns = new List<ColumnDef>();
        /// End Serialized Items 
        /// 
        private MangaSettings _settings;
        private MangaFilter _filter;
        private int? _selected = null;
        //private List<RowDef> _rows;

        public DataManga()
        {
        }

        protected override Int32 Version => DataMangaVersion1;
        protected override string Spec => "";
        protected override MangaFileType FileType => MangaFileType.datamap;

        protected override void Save(IMangaSerializationWriter writer)
        {
            writer.WriteInt32(_totalRows);
            Int32 columns = _columns.Count();
            writer.WriteInt32(columns);
            foreach (ColumnDef col in _columns)
            {
                SaveChild(col);
            }
        }

        protected override void Load(Int32 version, IMangaSerializationReader reader, MangaLoadOptions loadOptions)
        {
            _totalRows = reader.ReadInt32();
            Int32 columns = reader.ReadInt32();
            _columns = new List<ColumnDef>(_columns);
            for (int i = 0; i < columns; i++)
            {
                ColumnDef col = new ColumnDef(i);
                if (loadOptions.InlcudeCols == null || loadOptions.InlcudeCols.Contains(i))
                {
                    LoadChild(col, loadOptions);
                }
                _columns.Add(col);
            }
        }

        public int Columns { get => _columns.Count;  }

        public List<UInt32> AddHeaders(bool hasHeader, IList<object> colData, string workFolder, UInt32[] ignoreColIndexes, List<string> ignoreColNames, int calculatedNumberOfCols)
        {
            List<UInt32> ignoreIndexes = new List<UInt32>();

            UInt32 index = 1;
            int colIndex = 1;
            foreach(var header in colData)
            {
                if (index > calculatedNumberOfCols)
                    break;

                string colName;
                if (hasHeader)
                {
                    colName = (header == null ? null : header.ToString());
                    if (string.IsNullOrWhiteSpace(colName))
                    {
                        colName = string.Format("COL_{0}", colIndex);
                    }
                }
                else
                {
                    colName = string.Format("COL_{0}", colIndex);
                }

                if ((ignoreColIndexes == null || ignoreColIndexes.Count() == 0 ||ignoreColIndexes.Contains(index) == false) && 
                    (ignoreColNames == null || ignoreColNames.Count() == 0 || ignoreColNames.Contains(colName.ToLower()) == false))
                {

                    _columns.Add(new ColumnDef(colName, (colIndex-1), workFolder));
                    ++colIndex;
                }
                else
                {
                    ignoreIndexes.Add(index);
                }
                ++index;
            }

            return ignoreIndexes;
        }

        public void AddRow(IList<object> rowData, int row, IBackgroundJob status, List<UInt32> ignoreIndexes, int calculatedNumberOfCols, bool ignoreTextCase, bool trimLeadingWhitespace, bool trimTrailingWhitespace)
        {
            _totalRows++;

#if PARALLELER
            var result = Parallel.ForEach(rowData, (data, state, index) =>
            {
                _columns[(int)index].AddData(data as string, row);
            });
#else
            UInt32 index = 1;
            int colIndex = 0;
            foreach (var data in rowData)
            {
                if (index > calculatedNumberOfCols)
                    break;

                if (ignoreIndexes == null || ignoreIndexes.Count == 0 || ignoreIndexes.Contains(index) == false)
                {
                    var dataCook = data as string;
                    if ( dataCook != null && (trimLeadingWhitespace || trimTrailingWhitespace))
                    {
                        if (trimLeadingWhitespace && trimTrailingWhitespace)
                        {
                            dataCook = dataCook.Trim();
                        }
                        else if (trimLeadingWhitespace)
                        {
                            dataCook = dataCook.TrimStart();
                        }
                        else if (trimTrailingWhitespace)
                        {
                            dataCook = dataCook.TrimEnd();
                        }
                    }
                    _columns[colIndex].AddData(dataCook, row, ignoreTextCase, status);
                    ++colIndex;
                }
                ++index;

                if (status.IsCancellationRequested)
                    break;
            }
#endif
        }

        public void Process(IBackgroundJob status)
        {
            _columns.ForEach(c => c.FinalizeDimensionColumn(_totalRows, status));

            int maxDisplayRows = 0;
            var textCols = _columns.Where(c => c.DataType == DataType.Text);
            if (textCols.Any())
                maxDisplayRows = textCols.Max(c => c.MaxDisplayRows);
            else
                maxDisplayRows = MangaConstants.ReasonablBucket;

            _columns.ForEach(c => c.FinalizeMeasureColumn(_totalRows, maxDisplayRows, status));
        }

        public void ProcessWorkingFolder(string mangaPath)
        {
            _columns.ForEach(c => c.ProcessWorkingFolder(mangaPath));
        }

        private Tuple<int, string> GetColIndexAndNodeIndex(string nodeIdentifier)
        {
            Regex regNode = new Regex(@"^(?<nodeIndex>.*)@(?<colName>.*):(?<colIndex>\d*)$");

            if (regNode.IsMatch(nodeIdentifier))
            {
                Match match = regNode.Match(nodeIdentifier);
                string nodeIndex = match.Result("${nodeIndex}");
                int colIndex;
                if (int.TryParse(match.Result("${colIndex}"), out colIndex))
                {
                    return Tuple.Create(colIndex, nodeIndex);
                }
            }

            return Tuple.Create(-1, string.Empty);
        }

         public MyBitArray GetBitmap(string nodeIdentifier)
        {
            var ret = GetColIndexAndNodeIndex(nodeIdentifier);

            if (ret.Item1 >= 0 && ret.Item2.Length > 0)
            {
                var filterCol = _columns.FirstOrDefault(c => c.Index == ret.Item1);
                if (filterCol != null)
                {
                    return filterCol.GetBitmap(ret.Item2);
                }
            }

            return null;
        }

        public int? Filter(string selectionExpression, MyBitArray bitmap, int ignoreBucketCol)
        {
            if (bitmap != null)
            {
                foreach (var col in _columns)
                {
                    if ( ignoreBucketCol != col.Index)
                        col.Filter(bitmap);
                }

                return bitmap.GetCardinality(); 
            }

            return null;
        }

        internal ColumnDefBucket ReBucketize(int colIndex, bool bucketized, int bucketSize, decimal bucketMod, IBackgroundJob status)
        {
            if (bucketized)
            {
                var bucketCol = _columns.FirstOrDefault(c => c.Index == colIndex);
                if (bucketCol != null)
                {
                    var bucket = bucketCol.ReBucketize(_totalRows, bucketSize, bucketMod, false, status);
                    ApplyBuckets(bucket, colIndex);
                    return bucket;
                }
            }

            return null;
        }

        internal void ApplyFilterToBucket(ColumnDefBucket bucket)
        {
            if (_filter.BitmapFilter != null)
                bucket.Filter(_filter.BitmapFilter);
        }

        public object ReturnVis()
        {
            Dictionary<string, object> vis = new Dictionary<string, object>();
            foreach (var col in _columns)
            {
                vis.Add(col.Name, col.ReturnVis(_selected, _totalRows, _settings.ColAscending));
            }

            if ( _settings.HideColumns == null || _settings.HideColumns.Length == 0)
            {
                _settings.HideColumns = new bool[_columns.Count];
            }

            return new { TotalRows = _totalRows, SelectedRows = _selected ?? _totalRows, Columns = vis,
                ShowLowEqual = _settings.ShowLowEqual,
                ShowLowBound = _settings.ShowLowBound,
                ShowHighEqual = _settings.ShowHighEqual,
                ShowHighBound = _settings.ShowHighBound,
                ShowPercentage = _settings.ShowPercentage.ToString(),
                SelectOperation = _settings.SelectOperation.ToString(), SelectionExpression = _filter.SelectionExpression,
                ColAscending = _settings.ColAscending,
                HideColumns = _settings.HideColumns
            };
        }

        public object ReturnColStats(int columnIndex)
        {
            return _columns[columnIndex].ReturnColStats();
        }

        internal void ApplySettings(MangaSettings settings)
        {
            _settings = settings;
        }

        internal void ApplyFilter(MangaFilter filter, bool applyFilter, int ignoreBucketCol)
        {
            _filter = filter;
            if (applyFilter && filter.HasFilter)
            {
                _selected = Filter(filter.SelectionExpression, filter.BitmapFilter, ignoreBucketCol);
            }
            else
            {
                _selected = null;
            }
        }

        internal void ApplyBuckets(ColumnDefBucket column, int colIndex)
        {
            _columns[colIndex] = column;
        }

        public IEnumerable<int> GetEnumeratAll()
        {
            for (int i = 0; i < _totalRows; i++)
                yield return i;
        }

        public IEnumerable<string> StreamCSV()
        {
            List<RawDataReader> readers = new List<RawDataReader>();
            try
            {
                IEnumerable<int> reader;
                if (_filter != null && _filter.BitmapFilter != null)
                {
                    reader = _filter.BitmapFilter.GetEnumerator();
                }
                else
                {
                    reader = GetEnumeratAll();
                }

                bool first = true;
                StringBuilder line = new StringBuilder();
                int index = 0;
                foreach (var col in _columns)
                {
                    if (_settings.HideColumns == null || _settings.HideColumns.Length == 0 || _settings.HideColumns[index] != true)
                    {
                        readers.Add(col.GetReader());
                        if (first)
                            first = false;
                        else
                            line.Append(",");
                        line.Append(col.Name);
                    }

                    ++index;
                }
                line.AppendLine();
                yield return line.ToString();

                foreach (var row in reader)
                {
                    line.Clear();
                    first = true;
                    foreach (var col in readers)
                    {
                        if (first)
                            first = false;
                        else
                            line.Append(",");
                        line.Append(col.ReadRow(row, true));
                    }
                    line.AppendLine();

                    yield return line.ToString();
                }
            }
            finally
            {
                foreach(var reader in readers)
                {
                    MiscHelpers.SafeDispose(reader);
                }
            }
        }

        public object GetMeasureStats(string selectedNode, int[] colMeasures)
        {
            List<RawDataReader> readers = new List<RawDataReader>();
            try
            {
                IEnumerable<int> reader = GetBitmap(selectedNode).GetEnumerator();

                List<string> measureNames = new List<string>();
                List<MangaStat> stats = new List<MangaStat>();
                foreach (var col in _columns)
                {
                    if (col.DataType == DataType.Numeric && colMeasures.Contains(col.Index))
                    {
                        readers.Add(col.GetReader());
                        stats.Add(new MangaStat());
                        measureNames.Add(col.Name);
                    }
                }

                foreach (var row in reader)
                {
                    int i = 0;
                    foreach (var col in readers)
                    {
                        decimal? val = col.ReadRow(row, false) as decimal?;
                        if (val.HasValue)
                            stats[i].AddStat(val.Value, true);
                        else
                            stats[i].AddStat(0, false);
                        ++i;
                    }
                }

                Dictionary<string, object> measureStats = new Dictionary<string, object>();
                for (int i = 0; i < measureNames.Count; i++)
                {
                    measureStats.Add(measureNames[i], stats[i].AllStats(true));
                }

                return measureStats;
            }
            finally
            {
                foreach (var reader in readers)
                {
                    MiscHelpers.SafeDispose(reader);
                }
            }
        }

        public object GetReportData(int[] colDimensions, int[] colTotMeasures, int[] colAvgMeasures, int[] colCntMeasures, bool filtered)
        {
            Dictionary<int, string> measureNames = new Dictionary<int, string>();
            List<ColumnDef> countCols = new List<ColumnDef>();
            var cols = GetTotalsData(colDimensions, colTotMeasures, colAvgMeasures, colCntMeasures, filtered, measureNames, countCols);

            if (cols != null)
            {
                foreach (var dimCol in cols)
                {
                    return dimCol.BuildReportData(colTotMeasures, colAvgMeasures, measureNames, countCols);
                }
            }

            return null;
        }

        public object GetChartData(int[] colDimensions, int[] colTotMeasures, int[] colAvgMeasures, int[] colCntMeasures, bool filtered)
        {
            Dictionary<int, string> measureNames = new Dictionary<int, string>();
            List<ColumnDef> countCols = new List<ColumnDef>();
            var cols = GetTotalsData(colDimensions, colTotMeasures, colAvgMeasures, colCntMeasures, filtered, measureNames, countCols);

            if (cols != null)
            {
                foreach (var dimCol in cols)
                {
                    return dimCol.BuildGraphData(colTotMeasures, colAvgMeasures, measureNames, countCols);
                }
            }

            return null;
        }

        private List<ColumnDef> GetTotalsData(int[] colDimensions, int[] colTotMeasures, int[] colAvgMeasures, int[] colCntMeasures, bool filtered, Dictionary<int, string> measureNames, List<ColumnDef> countCols)
        {
            Dictionary<int, RawDataReader> readers = new Dictionary<int, RawDataReader>();
            try
            {
                IEnumerable<int> reader;
                if (filtered && _filter != null && _filter.BitmapFilter != null)
                {
                    reader = _filter.BitmapFilter.GetEnumerator();
                }
                else
                {
                    reader = GetEnumeratAll();
                }

                List<ColumnDef> cols = new List<ColumnDef>();
                int[] measures = colTotMeasures.Union(colAvgMeasures).Distinct().ToArray();
                foreach (var col in _columns)
                {
                    if (colDimensions.Contains(col.Index))
                    {
                        col.BuildRowIndexes(measures);
                        cols.Add(col);
                    }
                    else if (colTotMeasures.Contains(col.Index) || colAvgMeasures.Contains(col.Index) )
                    {
                        readers.Add(col.Index, col.GetReader());
                        measureNames.Add(col.Index, col.Name);
                    } 
                    else if (colCntMeasures.Contains(col.Index))
                    {
                        countCols.Add(col);   
                    }
                }

                Dictionary<int, decimal> rowValues = new Dictionary<int, decimal>();
                foreach (var row in reader)
                {
                    foreach (var measureCol in readers)
                    {
                        decimal? val = measureCol.Value.ReadRow(row, false) as decimal?;
                        foreach(var dimCol in cols)
                        {
                            dimCol.AddNodeStat(row, measureCol.Key, val);
                        }
                    }
                }

                return cols;
            }
            finally
            {
                foreach (var reader in readers.Values)
                {
                    MiscHelpers.SafeDispose(reader);
                }
            }
        }

        public object GetNodeData(string selectedNode)
        {
            List<RawDataReader> readers = new List<RawDataReader>();
            try
            {
                IEnumerable<int> reader = GetBitmap(selectedNode).GetEnumerator();

                List<string> columns = new List<string>();
                int index = 0;
                foreach (var col in _columns)
                {
                    if (_settings.HideColumns == null || _settings.HideColumns.Length == 0 || _settings.HideColumns[index] != true)
                    {
                        readers.Add(col.GetReader());
                        columns.Add(col.Name);
                    }
                    ++index;
                }

                List<object> rowData = new List<object>();
                foreach (var row in reader)
                {
                    List<object> cells = new List<object>();
                    foreach (var col in readers)
                    {
                        cells.Add(col.ReadRow(row, false));
                    }
                    rowData.Add(cells);
                }

                return new { columns = columns, data = rowData };
            }
            finally
            {
                foreach (var reader in readers)
                {
                    MiscHelpers.SafeDispose(reader);
                }
            }
        }

        public object GetDuplicateEntries(string selectedNode)
        {
            var ret = GetColIndexAndNodeIndex(selectedNode);
            if (ret.Item1 >= 0 && ret.Item2.Length > 0)
            {
                var filterCol = _columns.FirstOrDefault(c => c.Index == ret.Item1);
                if (filterCol != null)
                {
                    return filterCol.GetDuplicateEntries(ret.Item2);
                }
            }

            return null;
        }
        
        public object GetDependencyDiagram(string selectedNode)
        {
            var selectedBitmap = GetBitmap(selectedNode);
            if (selectedBitmap != null)
            {
                var ret = GetColIndexAndNodeIndex(selectedNode);
                List<Dictionary<string, string>> keys = new List<Dictionary<string, string>>();
                keys.Add(new Dictionary<string, string> { { "name", selectedNode }, { "color", MiscHelpers.GetRandomColor() } });
                List<List<int>> matrix = new List<List<int>>();
                foreach (ColumnDef col in _columns)
                {
                    List<int> counts = new List<int>();
                    if ( col.Index != ret.Item1 )
                    {
                        return col.GetDependencyDiagram(selectedNode, selectedBitmap);
                    }
                }

/*
                List<Dictionary<string, string>> keys = new List<Dictionary<string, string>>
                {
                    new Dictionary<string, string>{{"name","Boystown"},{ "color","#DF151A"} },
                    new Dictionary<string, string>{{ "name","Bridgeport"},{"color","#00DA3C"} },
                    new Dictionary<string, string>{{ "name","Bucktown"},{"color","#FD8603"} },
                    new Dictionary<string, string>{{ "name","Chinatown"},{"color","#00DA3C"} },
                    new Dictionary<string, string>{{ "name","Douglas"},{"color","#00DA3C"} },
                    new Dictionary<string, string>{{ "name","East Village"},{"color","#FD8603"} },
                    new Dictionary<string, string>{{ "name","Edgewater"},{"color","#DF151A"} },
                    new Dictionary<string, string>{{ "name","Gold Coast"},{"color","#DF151A"} },
                    new Dictionary<string, string>{{ "name","Grand Boulevard"},{"color","#00DA3C"} },
                    new Dictionary<string, string>{{ "name","Grant Park"},{"color","#00CBE7"} },
                    new Dictionary<string, string>{{ "name","Greektown"},{"color","#F4F328"} },
                    new Dictionary<string, string>{{ "name","Humboldt Park"},{"color","#FD8603"} },
                    new Dictionary<string, string>{{ "name","Hyde Park"},{"color","#00DA3C"} },
                    new Dictionary<string, string>{{ "name","Kenwood"},{"color","#00DA3C"} },
                    new Dictionary<string, string>{{ "name","Lakeview"},{"color","#DF151A"} },
                    new Dictionary<string, string>{{ "name","Lincoln Park"},{"color","#DF151A"} },
                    new Dictionary<string, string>{{ "name","Lincoln Square"},{"color","#DF151A"} },
                    new Dictionary<string, string>{{ "name","Little Italy/UIC"},{"color","#F4F328"} },
                    new Dictionary<string, string>{{ "name","Little Village"},{"color","#F4F328"} },
                    new Dictionary<string, string>{{ "name","Logan Square"},{"color","#FD8603"} },
                    new Dictionary<string, string>{{ "name","Loop"},{"color","#00CBE7"} },
                    new Dictionary<string, string>{{ "name","Lower West Side"},{"color","#F4F328"} },
                    new Dictionary<string, string>{{ "name","Millennium Park"},{"color","#00CBE7"} },
                    new Dictionary<string, string>{{ "name","Museum Campus"},{"color","#00CBE7"} },
                    new Dictionary<string, string>{{ "name","Near South Side"},{"color","#00CBE7"} },
                    new Dictionary<string, string>{{ "name","North Center"},{"color","#DF151A"} },
                    new Dictionary<string, string>{{ "name","Old Town"},{"color","#DF151A"} },
                    new Dictionary<string, string>{{ "name","Printers Row"},{"color","#00CBE7"} },
                    new Dictionary<string, string>{{ "name","River North"},{"color","#00CBE7"} },
                    new Dictionary<string, string>{{ "name","Rush &amp; Division"},{"color","#00CBE7"} },
                    new Dictionary<string, string>{{ "name","Sheffield/DePaul"},{"color","#DF151A"} },
                    new Dictionary<string, string>{{ "name","Streeterville"},{"color","#00CBE7"} },
                    new Dictionary<string, string>{{ "name","Ukranian Village"},{"color","#F4F328"} },
                    new Dictionary<string, string>{{ "name","United Center"},{"color","#F4F328"} },
                    new Dictionary<string, string>{{ "name","Uptown"},{"color","#DF151A"} },
                    new Dictionary<string, string>{{ "name","Washington Park"},{"color","#00DA3C"} },
                    new Dictionary<string, string>{{ "name","West Loop"},{"color","#00CBE7"} },
                    new Dictionary<string, string>{{ "name","West Town"},{"color","#F4F328"} },
                    new Dictionary<string, string>{{ "name","Wicker Park"},{"color","#FD8603"} },
                    new Dictionary<string, string>{{ "name","Wrigleyville"},{"color","#DF151A"} } 
                };

                List<List<int>> matrix = new List<List<int>> {
                    new List<int> {93, 0, 16, 0, 3, 1, 13, 27, 0, 3, 3, 0, 0, 0, 887, 224, 27, 4, 0, 7, 17, 0, 3, 3, 4, 29, 44, 0, 60, 10, 46, 55, 0, 0, 110, 0, 19, 27, 15, 82 },
                    new List<int> {0, 757, 0, 148, 225, 6, 0, 103, 13, 77, 11, 0, 2, 3, 64, 150, 9, 195, 5, 1, 176, 239, 35, 33, 135, 0, 38, 5, 64, 9, 22, 145, 3, 0, 5, 2, 103, 57, 10, 8},
                    new List<int> {8, 4, 857, 0, 0, 98, 0, 38, 16, 4, 11, 89, 0, 12, 565, 1044, 85, 44, 1, 553, 253, 12, 4, 8, 0, 191, 409, 3, 911, 44, 482, 113, 92, 13, 27, 0, 188, 555, 1504, 95},
                    new List<int> {0, 168, 0, 380, 103, 0, 0, 3, 17, 111, 11, 0, 1, 11, 3, 4, 1, 130, 0, 0, 286, 122, 60, 41, 405, 0, 2, 42, 24, 3, 5, 62, 2, 2, 3, 0, 223, 14, 4, 1},
                    new List<int> {1, 200, 0, 105, 2495, 0, 0, 16, 186, 662, 4, 0, 471, 144, 20, 21, 2, 35, 0, 2, 327, 73, 139, 633, 823, 1, 23, 75, 72, 8, 2, 195, 0, 1, 16, 13, 130, 9, 4, 2},
                    new List<int> {0, 11, 42, 1, 0, 84, 0, 12, 1, 10, 4, 21, 2, 1, 39, 68, 3, 60, 0, 20, 261, 7, 16, 5, 12, 7, 36, 3, 273, 8, 38, 43, 39, 32, 0, 0, 325, 216, 378, 4},
                    new List<int> {19, 1, 1, 0, 0, 0, 105, 9, 0, 2, 0, 0, 0, 0, 101, 75, 60, 0, 0, 2, 3, 0, 0, 7, 1, 8, 4, 0, 4, 1, 4, 9, 0, 0, 217, 0, 0, 1, 0, 21},
                    new List<int> {39, 139, 37, 0, 24, 10, 6, 1945, 6, 724, 10, 3, 12, 9, 859, 2543, 23, 38, 28, 9, 1466, 8, 313, 256, 219, 8, 748, 53, 1300, 337, 340, 2312, 12, 1, 83, 0, 573, 211, 186, 149},
                    new List<int> {0, 18, 15, 24, 144, 2, 0, 7, 345, 34, 6, 0, 71, 89, 6, 7, 0, 80, 0, 1, 174, 23, 37, 26, 94, 0, 6, 17, 106, 8, 8, 33, 2, 4, 3, 28, 138, 24, 10, 4},
                    new List<int> {8, 71, 5, 67, 443, 8, 0, 820, 37, 3382, 26, 3, 156, 36, 324, 1280, 45, 183, 7, 9, 3390, 76, 1340, 2087, 1158, 7, 368, 164, 745, 128, 102, 4828, 7, 4, 46, 1, 951, 151, 54, 28},
                    new List<int> {1, 22, 8, 8, 4, 5, 0, 8, 5, 39, 72, 1, 0, 3, 22, 65, 0, 194, 0, 6, 510, 76, 80, 6, 28, 6, 23, 14, 204, 27, 23, 104, 8, 38, 2, 0, 997, 174, 65, 1},
                    new List<int> {0, 0, 62, 0, 0, 18, 0, 0, 0, 3, 8, 202, 0, 2, 26, 31, 8, 11, 0, 226, 47, 11, 1, 0, 0, 10, 18, 0, 151, 7, 10, 13, 29, 4, 2, 0, 74, 239, 338, 5},
                    new List<int> {0, 3, 0, 4, 439, 2, 0, 11, 56, 171, 0, 0, 1885, 296, 12, 11, 1, 22, 2, 1, 192, 9, 101, 167, 126, 1, 11, 17, 53, 2, 7, 137, 2, 0, 3, 171, 76, 9, 3, 0},
                    new List<int> {0, 2, 4, 6, 149, 3, 0, 17, 77, 56, 3, 0, 362, 329, 14, 37, 0, 37, 1, 3, 220, 25, 40, 67, 119, 3, 22, 9, 105, 23, 9, 72, 2, 2, 6, 10, 152, 28, 20, 0},
                    new List<int> {863, 46, 532, 3, 24, 30, 149, 796, 7, 268, 68, 25, 19, 9, 12953, 6648, 1036, 85, 18, 257, 932, 14, 169, 161, 115, 1563, 1558, 24, 1840, 409, 2235, 1687, 43, 5, 2456, 1, 706, 360, 610, 3157},
                    new List<int> {289, 131, 1062, 8, 19, 62, 94, 2386, 10, 896, 51, 38, 7, 24, 7042, 12583, 233, 174, 49, 334, 2162, 29, 469, 279, 203, 369, 3767, 36, 4149, 1128, 4583, 5931, 73, 17, 1319, 0, 994, 563, 1107, 1572},
                    new List<int> {30, 1, 96, 0, 2, 3, 86, 28, 0, 30, 2, 9, 3, 0, 900, 258, 1846, 3, 2, 86, 63, 4, 20, 15, 11, 556, 41, 1, 79, 13, 143, 111, 20, 1, 829, 0, 23, 30, 84, 227},
                    new List<int> {0, 208, 52, 137, 31, 40, 1, 45, 68, 257, 207, 13, 23, 39, 113, 174, 2, 7033, 10, 38, 2016, 1436, 208, 106, 655, 24, 89, 184, 525, 60, 131, 235, 59, 68, 30, 2, 4635, 485, 260, 14},
                    new List<int> {0, 1, 4, 0, 1, 0, 0, 18, 0, 9, 0, 0, 2, 1, 10, 55, 1, 7, 18, 0, 15, 23, 3, 4, 2, 0, 9, 0, 11, 2, 18, 49, 1, 0, 1, 0, 2, 3, 5, 6},
                    new List<int> {6, 2, 539, 0, 2, 17, 0, 16, 0, 1, 5, 208, 1, 1, 210, 265, 84, 22, 0, 1579, 219, 8, 4, 1, 2, 100, 97, 5, 256, 17, 170, 42, 83, 9, 14, 0, 257, 578, 1034, 45},
                    new List<int> {27, 274, 325, 245, 368, 257, 8, 1955, 183, 3912, 704, 67, 196, 249, 1283, 3314, 111, 2010, 18, 218, 31956, 721, 3807, 2523, 6682, 105, 2260, 1447, 14725, 2707, 624, 11936, 200, 204, 265, 2, 24568, 3191, 1621, 135},
                    new List<int> {0, 232, 6, 159, 73, 7, 0, 9, 28, 93, 42, 11, 9, 35, 14, 40, 0, 1318, 13, 16, 729, 2343, 190, 89, 722, 2, 13, 70, 207, 12, 16, 79, 11, 38, 24, 0, 1030, 275, 64, 6},
                    new List<int> {2, 54, 7, 25, 103, 15, 2, 570, 23, 1369, 43, 4, 82, 38, 220, 728, 28, 116, 2, 6, 3137, 113, 2001, 1334, 839, 6, 218, 147, 1036, 114, 44, 3281, 12, 9, 47, 0, 1207, 202, 74, 14},
                    new List<int> {8, 25, 5, 50, 562, 10, 2, 253, 50, 1987, 14, 0, 160, 62, 189, 285, 15, 119, 4, 2, 1998, 85, 1370, 1923, 941, 2, 121, 173, 422, 72, 39, 2210, 3, 0, 27, 1, 539, 78, 25, 26},
                    new List<int> {7, 170, 2, 526, 920, 8, 3, 260, 95, 1625, 25, 1, 104, 113, 169, 247, 14, 577, 2, 5, 5391, 681, 1103, 1004, 6603, 7, 125, 468, 835, 99, 37, 1397, 3, 7, 137, 17, 1564, 152, 39, 19},
                    new List<int> {34, 0, 166, 0, 0, 3, 9, 12, 0, 4, 10, 16, 3, 0, 1424, 275, 573, 17, 1, 98, 85, 4, 14, 7, 10, 456, 115, 2, 164, 22, 150, 33, 46, 5, 225, 0, 117, 90, 217, 290},
                    new List<int> {44, 38, 409, 2, 16, 24, 8, 781, 8, 262, 46, 6, 8, 21, 1818, 3855, 66, 89, 14, 71, 1909, 6, 157, 104, 89, 185, 3861, 17, 4043, 702, 1230, 1919, 31, 5, 160, 0, 1154, 371, 706, 359},
                    new List<int> {0, 23, 4, 41, 51, 0, 0, 56, 13, 183, 19, 0, 12, 8, 31, 54, 3, 156, 0, 3, 1350, 58, 181, 204, 522, 6, 29, 232, 287, 84, 15, 311, 6, 0, 19, 0, 953, 77, 38, 1},
                    new List<int> {50, 74, 769, 24, 70, 230, 5, 1406, 134, 716, 233, 137, 52, 136, 1974, 4386, 98, 535, 19, 277, 12733, 179, 922, 422, 895, 194, 4007, 329, 15061, 2241, 1272, 6938, 143, 54, 92, 0, 13258, 3801, 2106, 313},
                    new List<int> {26, 28, 62, 3, 9, 8, 0, 334, 13, 98, 28, 4, 0, 15, 471, 1310, 15, 42, 2, 32, 1909, 12, 96, 62, 73, 21, 799, 41, 2304, 855, 168, 1174, 18, 5, 36, 0, 1054, 317, 292, 108},
                    new List<int> {61, 12, 440, 2, 2, 30, 2, 344, 1, 70, 27, 4, 1, 9, 2454, 4723, 176, 153, 23, 236, 339, 18, 48, 37, 23, 223, 1102, 9, 1086, 138, 2994, 378, 15, 4, 133, 0, 462, 220, 576, 653},
                    new List<int> {61, 161, 49, 44, 191, 33, 12, 2385, 55, 3627, 89, 19, 135, 80, 1586, 6267, 89, 236, 47, 37, 9875, 46, 3312, 2131, 1371, 49, 1733, 385, 6685, 1137, 478, 13468, 93, 15, 230, 2, 4491, 1358, 492, 276},
                    new List<int> {0, 1, 109, 2, 0, 39, 1, 12, 2, 13, 5, 39, 1, 1, 58, 81, 11, 83, 0, 93, 182, 41, 7, 3, 6, 37, 50, 1, 150, 12, 27, 105, 142, 44, 2, 0, 277, 331, 719, 7},
                    new List<int> {0, 1, 22, 1, 2, 42, 0, 1, 2, 6, 37, 21, 0, 0, 14, 21, 1, 88, 0, 16, 139, 45, 14, 3, 9, 6, 6, 3, 69, 9, 4, 27, 39, 145, 6, 0, 309, 264, 144, 1},
                    new List<int> {137, 5, 46, 6, 11, 1, 278, 86, 0, 51, 2, 8, 4, 3, 2205, 1303, 759, 26, 1, 23, 207, 20, 28, 42, 145, 229, 156, 12, 100, 52, 129, 191, 4, 0, 3692, 0, 90, 18, 43, 1032},
                    new List<int> {20000, 0, 0, 0, 9, 0, 0, 0, 19, 0, 0, 0, 145, 6, 0, 0, 0, 0, 0, 0, 6, 0, 1, 0, 9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 44, 0, 0, 0, 0},
                    new List<int> {23, 135, 250, 251, 150, 338, 0, 611, 132, 1230, 1178, 68, 92, 175, 986, 1187, 39, 4708, 5, 260, 24464, 1033, 1764, 781, 1892, 118, 1255, 1038, 13450, 1202, 600, 4597, 258, 360, 115, 0, 22492, 4019, 1618, 158},
                    new List<int> {16, 30, 557, 13, 17, 266, 1, 239, 25, 178, 160, 209, 11, 27, 442, 605, 42, 454, 6, 448, 3004, 235, 246, 90, 128, 82, 375, 63, 3622, 329, 240, 1306, 272, 290, 12, 0, 4098, 3889, 2760, 62},
                    new List<int> {26, 13, 1485, 4, 4, 331, 2, 180, 12, 41, 45, 460, 11, 9, 599, 1117, 62, 267, 4, 1302, 1305, 83, 67, 28, 32, 281, 705, 20, 1818, 282, 547, 473, 741, 118, 24, 0, 1428, 2530, 5775, 142},
                    new List<int> {73, 4, 90, 0, 1, 9, 29, 132, 1, 35, 2, 7, 1, 1, 3144, 1341, 229, 12, 5, 67, 88, 0, 25, 22, 25, 319, 352, 7, 305, 104, 654, 240, 10, 3, 1216, 0, 110, 41, 136, 1267 }
                };
*/

                return new { keys = keys, matrix = matrix };
            }

            return null;
        }

        public void CleanupWorkingSet()
        {
            _columns.ForEach(c => c.CleanupWorkingSet());
        }
    }
}
