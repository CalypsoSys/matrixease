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

        public void AddRow(IList<object> rowData, int row, IBackgroundJob status, List<UInt32> ignoreIndexes, int calculatedNumberOfCols)
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

                if (ignoreIndexes.Count == 0 || ignoreIndexes.Contains(index) == false)
                {
                    _columns[colIndex].AddData(data as string, row, status);
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

            int maxDisplayRows = _columns.Where(c => c.DataType == DataType.Text).Max(c => c.MaxDisplayRows);

            _columns.ForEach(c => c.FinalizeMeasureColumn(_totalRows, maxDisplayRows, status));
        }

        public void ProcessWorkingFolder(string mangaPath)
        {
            _columns.ForEach(c => c.ProcessWorkingFolder(mangaPath));
        }

        public MyBitArray GetBitmap(string nodeIdentifier)
        {
            Regex regNode = new Regex(@"^(?<nodeIndex>.*)@(?<colName>.*):(?<colIndex>\d*)$");

            if (regNode.IsMatch(nodeIdentifier))
            {
                Match match = regNode.Match(nodeIdentifier);
                string nodeIndex = match.Result("${nodeIndex}");
                int colIndex;
                if (int.TryParse(match.Result("${colIndex}"), out colIndex))
                {
                    var filterCol = _columns.FirstOrDefault(c => c.Index == colIndex);
                    if (filterCol != null)
                    {
                        return filterCol.GetBitmap(nodeIndex);
                    }
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

        public void CleanupWorkingSet()
        {
            _columns.ForEach(c => c.CleanupWorkingSet());
        }
    }
}
