//#define DEBUG_COLS
using MatrixEase.Manga.Processing;
using MatrixEase.Manga.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;

// if ( data == "1500￼")
namespace MatrixEase.Manga.Manga
{
    public class ColumnDef : MangaSerialize
    {
        private const Int32 ColumnDefVersion1 = 1;
        private static string[] _dateFormats = new string[] {
                    "[dd/MMM/yyyy:HH:mm:ss",
                    "[dd/MMM/yyyy:HH:mm:sszzzz]",
                    "[dd/MMM/yyyy:H:mm:ss",
                    "[dd/MMM/yyyy:H:mm:sszzzz]",
                    "dd/MMM/yyyy:H:mm:ss",
                    "dd/MMM/yyyy:H:mm:sszzzz",
            };

        /// Start Serialized Items 
        private string _name;
        private Int32 _index;
        private DataType _dataType = DataType.Unknown;
        private Int32 _nullEmpty = 0;
        private NumericPatterns _numericPatterns;
        private DatePatterns _datePatterns;
        private TextPatterns _textPatterns;
        private decimal _selectivity = 0;
        private Int32 _distinctValues = 0;
        private Int32 _maxDisplayRows = 0;
        private bool _bucketized = false;
        private bool _onlyBuckets = false;
        private int _curBucketSize = 0;
        private decimal _curBucketMod = 0;
        private Dictionary<string, MyBitArray> _rows = new Dictionary<string, MyBitArray>();
        private Dictionary<string, HashSet<string>> _cellNames = new Dictionary<string, HashSet<string>>();
        private Dictionary<string, Int32> _rowCounts = new Dictionary<string, Int32>();
        /// End Serialized Items 

        // hack unitl I can all allow custom (user specified) datetimes
        private bool _useSpecialDatetTime = false;
        private Dictionary<string, int> _filterCounts = null;
        private IPatterns _patterns = null;

        private List<string> _labels = new List<string>();
        private Dictionary<string, int> _nodeIndexes = new Dictionary<string, int>();
        private Dictionary<int, int> _rowIndexes = new Dictionary<int, int>();
        private Dictionary<int, Dictionary<int, MangaStat>> _measures = new Dictionary<int, Dictionary<int, MangaStat>>();

        public ColumnDef(int index)
        {
            _index = index;
        }

        public ColumnDef(string name, int index, string workFolder)
        {
            _name = name;
            _index = index;
            _numericPatterns = new NumericPatterns(_index, workFolder);
            _datePatterns = new DatePatterns(index, workFolder);
            _textPatterns = new TextPatterns(index, workFolder);
        }

        protected override Int32 Version => ColumnDefVersion1;
        protected override string Spec => _index.ToString();
        protected override MangaFileType FileType => MangaFileType.coldef;

        protected override void Save(IMangaSerializationWriter writer)
        {
            writer.WriteString(_name);
            writer.WriteInt32(_index);
            writer.WriteEnum<DataType>(_dataType);
            writer.WriteInt32(_nullEmpty);
            writer.WriteDecimal(_selectivity);
            writer.WriteInt32(_distinctValues);
            writer.WriteInt32(_maxDisplayRows);
            writer.WriteBool(_bucketized);
            writer.WriteBool(_onlyBuckets);
            writer.WriteInt32(_curBucketSize);
            writer.WriteDecimal(_curBucketMod);

            writer.WriteDictStringInt32(_rowCounts);

            Int32 rows = _rows.Count();
            writer.WriteInt32(rows);
            foreach (var pair in _rows)
            {
                writer.WriteString(pair.Key);
                writer.SaveChild(pair.Value);
            }

            Int32 cellNames = _cellNames.Count();
            writer.WriteInt32(cellNames);
            foreach (var pair in _cellNames)
            {
                writer.WriteString(pair.Key);
                writer.WriteHashString(pair.Value);
            }

            if (_dataType == DataType.Numeric && _numericPatterns != null)
            {
                writer.WriteEnum< DataType>(DataType.Numeric);
                SaveChild(_numericPatterns);
            }
            else if (_dataType == DataType.Date && _datePatterns != null)
            {
                writer.WriteEnum<DataType>(DataType.Date);
                SaveChild(_datePatterns);
            }
            else if (_dataType == DataType.Text && _textPatterns != null)
            {
                writer.WriteEnum<DataType>(DataType.Text);
                SaveChild(_textPatterns);
            }
            else
            {
                writer.WriteEnum<DataType>(DataType.Unknown);
            }
        }

        protected override void Load(Int32 version, IMangaSerializationReader reader, MangaLoadOptions loadOptions)
        {
            _name = reader.ReadString();
            _index = reader.ReadInt32();
            _dataType = reader.ReadEnum<DataType>();
            _nullEmpty = reader.ReadInt32();     
            _selectivity = reader.ReadDecimal();   
            _distinctValues = reader.ReadInt32();
            _maxDisplayRows = reader.ReadInt32();
            _bucketized = reader.ReadBool();    
            _onlyBuckets = reader.ReadBool();   
            _curBucketSize = reader.ReadInt32();
            _curBucketMod = reader.ReadDecimal();

            _rowCounts = reader.ReadDictStringInt32();
            Int32 rows = reader.ReadInt32();
            _rows = new Dictionary<string, MyBitArray>(rows);
            for (int i = 0; i < rows; i++)
            {
                string key = reader.ReadString();
                MyBitArray bits = reader.LoadChild<MyBitArray>(new MyBitArray(0), loadOptions);
                _rows.Add(key, bits);
            }
            
            Int32 cellNames = reader.ReadInt32();
            _cellNames = new Dictionary<string, HashSet<string>>(cellNames);
            for (int i = 0; i < cellNames; i++)
            {
                string key = reader.ReadString();
                HashSet<string> names = reader.ReadHashString();
                _cellNames.Add(key, names);
            }

            DataType dataTypeSaved = reader.ReadEnum<DataType>();
            if (_dataType == DataType.Numeric)
            {
                _numericPatterns = new NumericPatterns(_index);
                LoadChild(_numericPatterns, loadOptions);
            }
            else if (_dataType == DataType.Date)
            {
                _datePatterns = new DatePatterns(_index);
                LoadChild(_datePatterns, loadOptions);
            }
            else if (_dataType == DataType.Text)
            {
                _textPatterns = new TextPatterns(_index);
                LoadChild(_textPatterns, loadOptions);
            }
        }

        private bool DateParse(string dateTimeIn, out DateTime  dateTimeOut)
        {
            if (_useSpecialDatetTime == false && DateTime.TryParse(dateTimeIn, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out dateTimeOut) )
            {
                return true;
            }
            else if (DateTime.TryParseExact(dateTimeIn, _dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out dateTimeOut))
            {
                _useSpecialDatetTime = true;
                return true;
            }

            return false;
        }

#if DEBUG_COLS
        // vehicles
        //private static int[] _debugCols = new int[] { 0, 1, 5, 6, 12, 23, 24, 25 };
        // googls simple
        private static int[] _debugCols = new int[] { 5 };
#endif
        public void AddData(string data, int row, bool ignoreTextCase, IBackgroundJob status)
        {
#if DEBUG_COLS
            if (_debugCols.Contains(_index) == false)
                return;
#endif
            bool isEmpty = false;

            if (string.IsNullOrEmpty(data))
            {
                isEmpty = true;
                _nullEmpty++;
                data = MangaConstants.NullOrEmpty;
            }
            else if (_dataType != DataType.Text)
            {
                bool tryDate = true, tryNumeric = true;
                if (_dataType == DataType.Date)
                {
                    tryNumeric = false;
                }
                else if (_dataType == DataType.Numeric)
                {
                    tryDate = false;
                }

                if (tryNumeric)
                {
                    decimal result;
                    string decimalTest = data.ToString();
                    if (decimalTest.Last() == '%')
                    {
                        decimalTest = decimalTest.Substring(0, data.Length - 1);
                    }
                    if ((_dataType == DataType.Unknown || _dataType == DataType.Numeric) && decimal.TryParse(decimalTest, out result))
                    {
                        _dataType = DataType.Numeric;
                        _numericPatterns.Process(row, result, true, false, status);
                    }
                    else if (_dataType == DataType.Numeric)
                    {
                        _dataType = DataType.Text;
                    }
                }
                if (tryDate)
                {
                    DateTime result;
                    if ((_dataType == DataType.Unknown || _dataType == DataType.Date) && DateParse(data.ToString(), out result))
                    {
                        _dataType = DataType.Date;
                        _datePatterns.Process(row, result.Ticks, true, false, status);
                    }
                    else if (_dataType == DataType.Date)
                    {
                        _dataType = DataType.Text;
                    }
                }
                if (_dataType == DataType.Unknown)
                {
                    _dataType = DataType.Text;
                }
            }

            try
            {
                if (isEmpty == false)
                {
                    _textPatterns.Process(row, data, true, _dataType == DataType.Text, status);
                }
                else
                {
                    if (_dataType == DataType.Numeric)
                        _numericPatterns.Process(row, 0M, false, false, status);
                    else if (_dataType == DataType.Date)
                        _datePatterns.Process(row, 0, false, false, status); 
                    else
                        _textPatterns.Process(row, string.Empty, false, false, status); 
                }

                if (_bucketized == false)
                {
                    if (ignoreTextCase && isEmpty == false && _dataType != DataType.Numeric && _dataType != DataType.Date)
                    {
                        string originalData = data;
                        data = data.ToLower();
                        if (data.Length < MangaConstants.SmallTextSize)
                        {
                            if (_cellNames.ContainsKey(data) == false)
                            {
                                _cellNames.Add(data, new HashSet<string>(new string[] { originalData }));
                            }
                            else
                            {
                                _cellNames[data].Add(originalData);
                            }
                        }
                    }

                    if (_rows.ContainsKey(data) == false)
                    {
                        _rows.Add(data, new MyBitArray());
                        _rowCounts.Add(data, 0);
                    }
                    _rows[data].Set(row);
                    _rowCounts[data] = _rowCounts[data] + 1;
                }

                if (_bucketized == false && _rows.Count > MangaConstants.MaxTextDistinct )
                {
                    _rows = null;
                    _rowCounts = null;
                    _bucketized = true;
                }
            }
            catch(Exception excp)
            {
                SimpleLogger.LogError(excp, "Error adding data to MatrixEase {0} {1}", row, data);
            }
        }

        internal ColumnDefBucket ReBucketize(int totalRows, int? bucketSize, decimal? bucketMod, bool overrideBucket, IBackgroundJob status)
        {
            if (bucketSize.HasValue && overrideBucket == false)
            {
                ColumnDefBucket bucket = new ColumnDefBucket(_index);
                bucket._name = _name;
                bucket._index = _index;
                bucket._dataType = _dataType;
                bucket._nullEmpty = _nullEmpty;
                bucket._numericPatterns = _numericPatterns;
                bucket._datePatterns = _datePatterns;
                bucket._textPatterns = _textPatterns;
                bucket._selectivity = _selectivity;
                bucket._distinctValues = _distinctValues;
                bucket._maxDisplayRows = _maxDisplayRows;
                bucket._onlyBuckets = _onlyBuckets;

                bucket.ReBucketize(totalRows, bucketSize, bucketMod, true, status);
                return bucket;
            }
            else
            {
                _bucketized = true;
                _rows = new Dictionary<string, MyBitArray>();
                _rowCounts = new Dictionary<string, int>();
                _filterCounts = null;
                SetPattern();
                Tuple<int, decimal> bucket = _patterns.ReSpread(_name, totalRows, _rows, _rowCounts, bucketSize, bucketMod, status);
                _curBucketSize = bucket.Item1;
                _curBucketMod = bucket.Item2;
                return null;
            }
        }

        public void FinalizeDimensionColumn(int totalRows, IBackgroundJob status)
        {
            if (_dataType == DataType.Unknown)
            {
                _dataType = DataType.Text;
            }

            _textPatterns.FinalizeCache(_dataType == DataType.Text);
            _datePatterns.FinalizeCache(_dataType == DataType.Date);
            _numericPatterns.FinalizeCache(_dataType == DataType.Numeric);
            bool typeOverride = false;
            if ( _dataType == DataType.Text )
            {
                _numericPatterns = null;
                _datePatterns = null;
                if (_textPatterns.AverageTextLength > MangaConstants.LargeColumn)
                    typeOverride = true;
            } 
            else  if ( _dataType == DataType.Date )
            {
                _textPatterns = null;
                _numericPatterns = null;
            }
            else if ( _dataType == DataType.Numeric )
            {
                _textPatterns = null;
                _datePatterns = null;
            }

            SetPattern();
            _distinctValues = _patterns.DistinctValues;
            _selectivity = (decimal)_distinctValues / (decimal)totalRows * 100M;

            _onlyBuckets = typeOverride || (_distinctValues > MangaConstants.SmallBucket) && (_bucketized || _selectivity > MangaConstants.SelectivityThreshold || _distinctValues > MangaConstants.MaxTextDistinct);
            status.SetStatus(MangaFactoryStatusKey.Analyzing, string.Format("Calulating Distributions for {0}", _name), MangaFactoryStatusState.Running);
            if ( _patterns.CalulateBuckets(true, totalRows, MangaConstants.MaxTextDistinct, _distinctValues, _onlyBuckets, status) )
            {
                if (_onlyBuckets)
                {
                    status.SetStatus(MangaFactoryStatusKey.Analyzing, string.Format("Creating Buckets for {0}", _name), MangaFactoryStatusState.Running);
                    ReBucketize(totalRows, null, null, true, status);
                }
                _maxDisplayRows = _rows.Count;
            }
        }

        public decimal Selectivity
        {
            get { return _selectivity; }
        }

        public DataType DataType
        {
            get { return _dataType; }
        }

        public int MaxDisplayRows
        {
            get { return _maxDisplayRows; }
        }

        public void FinalizeMeasureColumn(int totalRows, int maxDisplayRows, IBackgroundJob status)
        {
            _onlyBuckets = (_distinctValues > MangaConstants.ReasonablBucket) && (_bucketized || _selectivity > MangaConstants.SelectivityThreshold || _distinctValues > (maxDisplayRows * MangaConstants.MaxDistinctfactor) || _distinctValues > MangaConstants.MaxTextDistinct);
            maxDisplayRows = Math.Max(maxDisplayRows, MangaConstants.SmallBucket);
            status.SetStatus(MangaFactoryStatusKey.Analyzing, string.Format("Calulating Distributions for {0}", _name), MangaFactoryStatusState.Running);
            if ( _patterns.CalulateBuckets(false, totalRows, maxDisplayRows, _distinctValues, _onlyBuckets, status) )
            {
                if (_onlyBuckets)
                {
                    status.SetStatus(MangaFactoryStatusKey.Analyzing, string.Format("Creating Buckets for {0}", _name), MangaFactoryStatusState.Running);
                    ReBucketize(totalRows, null, null, true, status);
                }
            }
        }

        private void SetPattern()
        {
            if (_textPatterns != null)
            {
                _patterns = _textPatterns;
            }
            else if (_numericPatterns != null)
            {
                _patterns = _numericPatterns;
            }
            else if (_datePatterns != null)
            {
                _patterns = _datePatterns;
            }
        }

        public void ProcessWorkingFolder(string mangaPath)
        {
            SetPattern();
            _patterns.ProcessWorkingFolder(mangaPath);
        }

        public string Name
        {
            get { return _name; }
        }

        public int Index
        {
            get { return _index; }
        }

        public object ReturnMatrixEase(int? selected, int totalRows, bool colAscending)
        {
            SetPattern();

            string colType = _patterns.ColType;
            object attr = _patterns.Stat;

            if (_filterCounts == null || _filterCounts.Count == 0)
            {
                _filterCounts = _rowCounts;
            }

            List<object> vals = new List<object>();
            IOrderedEnumerable<KeyValuePair<string, int>> nodes;
            if (colAscending)
                nodes = from entry in _filterCounts orderby entry.Value select entry;
            else
                nodes = from entry in _filterCounts orderby entry.Value descending select entry;
            foreach (var val in nodes)
            {
                var totValue = _rowCounts[val.Key];
                if (!selected.HasValue)
                {
                    selected = totalRows;
                }

                int duplicates = 1;
                if (_dataType == DataType.Text && _cellNames != null && _cellNames.ContainsKey(val.Key))
                {
                    duplicates = _cellNames[val.Key].Count();
                }

                vals.Add(new
                {
                    ColumnValue = val.Key,
                    Duplicates = duplicates,
                    TotalPct = (decimal)totValue / (decimal)totalRows * 100M,
                    SelectAllPct = (decimal)val.Value / (decimal)totalRows * 100M,
                    SelectRelPct = (selected.HasValue && selected.Value != 0 ? (decimal?)val.Value / (decimal)selected.Value * 100M : 0),
                    TotalValues = totValue,
                    SelectedValues = val.Value
                }
                );
            }

            return new {Index = _index, ColType = colType, DataType = _dataType.ToString(), NullEmpty = _nullEmpty, Selectivity = _selectivity, DistinctValues = _distinctValues, 
                Bucketized = _bucketized, OnlyBuckets = _onlyBuckets, 
                CurBucketSize = Math.Max(_curBucketSize, _patterns.MinBucketSize), MinBucketSize = _patterns.MinBucketSize, 
                CurBucketMod = Math.Max(_curBucketMod, _patterns.MinBucketMod), MinBucketMod = _patterns.MinBucketMod, 
                AllowedBuckets = _patterns.AllowedBuckets,
                Attributes = attr, Values = vals };
        }

        public object ReturnColStats()
        {
            SetPattern();

            return new
            {
                Name = _name,
                Index = _index,
                ColType = _patterns.ColType,
                DataType = _dataType.ToString(),
                NullEmpty = _nullEmpty,
                Selectivity = _selectivity,
                DistinctValues = _distinctValues,
                Bucketized = _bucketized,
                OnlyBuckets = _onlyBuckets,
                CurBucketSize = Math.Max(_curBucketSize, _patterns.MinBucketSize),
                MinBucketSize = _patterns.MinBucketSize,
                CurBucketMod = Math.Max(_curBucketMod, _patterns.MinBucketMod),
                MinBucketMod = _patterns.MinBucketMod,
                GroupCount = _rowCounts.Count,
                FilterCount = _filterCounts == null ? _rowCounts.Count : _filterCounts.Count,
                Stats = _patterns.DetailedStats,
            };
        }

        public MyBitArray GetBitmap(string nodeIndex)
        {
            if ( _rows.ContainsKey(nodeIndex) )
            {
                return _rows[nodeIndex];
            }

            return null;
        }

        public void Filter(MyBitArray selected)
        {
            _filterCounts = new Dictionary<string, int>();
            foreach (var nodeIndex in _rows.Keys)
            {
                var anded = _rows[nodeIndex].And(selected);
                int newTotal = anded.GetCardinality();
                /*
                int checkTotal = 0;
                for(int i=0;i< anded.Length;i++)
                {
                    if ( anded.Get(i))
                    {
                        ++checkTotal;
                    }
                }
                if ( checkTotal != newTotal)
                {
                    newTotal = checkTotal;
                }
                */
                _filterCounts.Add(nodeIndex, newTotal );
            }
        }

        public RawDataReader GetReader()
        {
            SetPattern();
            return _patterns.GetReader();
        }

        public void BuildRowIndexes(int[] measures)
        {
            int nodeIndex = 0;
            foreach(var node in _rows)
            {
                _labels.Add(node.Key);

                _nodeIndexes.Add(node.Key, nodeIndex);
                var stats = new Dictionary<int, MangaStat>(measures.Select(m => new KeyValuePair<int, MangaStat>(m, new MangaStat())));
                _measures.Add(nodeIndex, stats);
                foreach (var row in node.Value.GetEnumerator())
                {
                    _rowIndexes.Add(row, nodeIndex);
                }
                ++nodeIndex;
            }
        }

        public void AddNodeStat(int row, int measureCol, decimal? val)
        {
            var nodeIndex = _rowIndexes[row];
            var stat = _measures[nodeIndex][measureCol];
            if (val.HasValue)
                stat.AddStat(val.Value, true);
            else
                stat.AddStat(0, false);
        }

        public object BuildReportData(int[] colTotMeasures, int[] colAvgMeasures, Dictionary<int, string> measureNames, List<ColumnDef> countCols)
        {
            Dictionary<string, decimal[]> dataSets = new Dictionary<string, decimal[]>();

            BuildMeasureGraphData(dataSets, colTotMeasures, colAvgMeasures, measureNames);
            BuildCountGraphData(dataSets, countCols);

            List<string> columns = new List<string>();
            columns.Add("Dimension");

            List<object[]> rowData = new List<object[]>();
            foreach(var dim in _labels)
            {
                var row = new object[dataSets.Count + 1];
                row[0] = dim;
                rowData.Add(row);
            }

            int index = 1;
            foreach(var ds in dataSets)
            {
                columns.Add(ds.Key);
                for(int i=0;i<ds.Value.Length;i++)
                {
                    rowData[i][index] = ds.Value[i];
                }
                ++index;
            }

            return new { columns = columns, data = rowData };
        }

        public object BuildGraphData(int[] colTotMeasures, int[] colAvgMeasures, Dictionary<int, string> measureNames, List<ColumnDef> countCols)
        {
            Dictionary<string, decimal[]> dataSets = new Dictionary<string, decimal[]>();

            BuildMeasureGraphData(dataSets, colTotMeasures, colAvgMeasures, measureNames);
            BuildCountGraphData(dataSets, countCols);

            var graphDataSets = new List<object>();
            foreach (var ds in dataSets)
            {
                var graphDs = new { label = ds.Key, data = ds.Value, backgroundColor = MiscHelpers.GetRandomColor(), borderWidth = 3 };
                graphDataSets.Add(graphDs);
            }
            return new { labels = _labels, datasets = graphDataSets };
        }

        private void BuildMeasureGraphData(Dictionary<string, decimal[]> dataSets, int[] colTotMeasures, int[] colAvgMeasures, Dictionary<int, string> measureNames)
        {
            var allMeasures = colTotMeasures.Union(colAvgMeasures).Distinct().ToArray();
            foreach (var measure in allMeasures)
            {
                if (colTotMeasures.Contains(measure))
                {
                    dataSets.Add(string.Format("Total {0}", measureNames[measure]), new decimal[_labels.Count]);
                }
                if (colAvgMeasures.Contains(measure))
                {
                    dataSets.Add(string.Format("Avg {0}", measureNames[measure]), new decimal[_labels.Count]);
                }
            }

            foreach (var name in _labels)
            {
                int index = _nodeIndexes[name];
                foreach (var measure in allMeasures)
                {
                    var stat = _measures[index][measure];
                    if (colTotMeasures.Contains(measure))
                    {
                        string key = string.Format("Total {0}", measureNames[measure]);
                        dataSets[key][index] = stat.Total;
                    }
                    if (colAvgMeasures.Contains(measure))
                    {
                        string key = string.Format("Avg {0}", measureNames[measure]);
                        dataSets[key][index] = stat.Average;
                    }
                }
            }
        }

        private void BuildCountGraphData(Dictionary<string, decimal[]> dataSets, List<ColumnDef> countCols)
        {
            foreach(var colCount in countCols )
            {
                foreach(var node in colCount._rows)
                {
                    var counts = new decimal[_rows.Count];
                    foreach (var name in _labels)
                    {
                        int index = _nodeIndexes[name];

                        counts[index] = _rows[name].Clone().And(node.Value).GetCardinality();
                    }

                    dataSets.Add(string.Format("{0} - {1} Count", colCount.Name, node.Key), counts);
                }
            }
        }

        public object GetDuplicateEntries(string nodeIndex)
        {
            if (_cellNames.ContainsKey(nodeIndex))
            {
                return _cellNames[nodeIndex];
            }

            return null;
        }

        public object GetDependencyDiagram(string name, MyBitArray root)
        {
            List<Dictionary<string, string>> keys = new List<Dictionary<string, string>>();
            keys.Add(new Dictionary<string, string> { { "name", name }, { "color", MiscHelpers.GetRandomColor() } });

            List<int> toThisCell = new List<int>();
            toThisCell.Add(0);
            foreach (var nodeIndex in _rows.Keys)
            {
                var anded = _rows[nodeIndex].And(root);
                int count = anded.GetCardinality();
                if ( count > 0 )
                {
                    keys.Add(new Dictionary<string, string> { { "name", nodeIndex }, { "color", MiscHelpers.GetRandomColor() } });
                    toThisCell.Add(count);
                }
            }
            List<List<int>> matrix = new List<List<int>>();
            for(int i=0; i< keys.Count;i++)
            {
                List<int> toAdd;
                toAdd = Enumerable.Repeat(0, keys.Count).ToList();
                if ( i != 0 )
                {
                    toAdd[0] = toThisCell[i];
                }

                matrix.Add(toAdd);

            }
            return new { keys = keys, matrix = matrix };
        }

        public void CleanupWorkingSet()
        {
            if (_textPatterns != null)
                _textPatterns.FinalizeCache(false);
            if (_datePatterns != null)
                _datePatterns.FinalizeCache(false);
            if (_numericPatterns != null)
                _numericPatterns.FinalizeCache(false);
        }
    }
}
