using MatrixEase.Manga.Processing;
using MatrixEase.Manga.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace MatrixEase.Manga.Manga
{
    public class NumericPatterns : BasePatterns<decimal>
    {
        private const Int32 NumericPatternsVersion1 = 1;
        private const Int32 MinDecimalBucket = 2;

        /// Start Serialized Items 
        private int _minBucketSize;
        private decimal _minBucketMod;
        private decimal _proposedSpread;
        private MangaStat _decimalStat;
        private MangaStat _decimalCountNoValueStat;
        /// End Serialized Items 

        public override int MinBucketSize { get => _minBucketSize; }
        public override decimal MinBucketMod { get => _minBucketMod; }
        public override UInt32[] AllowedBuckets { get => null; }
        public override string ColType { get => MangaConstants.Measure; }
        public override object Stat 
        { 
            get => new { 
                Smallest = _decimalStat.MinDecimal, 
                Largest = _decimalStat.MaxDecimal, 
                Average = _decimalStat.Average, 
                StandardDeviation = _decimalStat.StandardDeviation,
                CoefficientOfVariation = _decimalStat.CoefficientOfVariation
            }; 
        }
        public override object DetailedStats
        {
            get
            {
                return new { NumericStats = _decimalStat.AllStats(true), NumericStatsIncludeNoValue = _decimalCountNoValueStat.AllStats(true) };
            }
        }

        protected override int TrueDistinctValues { get => Positions; }

        public NumericPatterns(int index) : base(index)
        {
        }

        public NumericPatterns(int index, string workFolder) : base(index, workFolder)
        {
            _decimalStat = new MangaStat();
            _decimalCountNoValueStat = new MangaStat();
        }

        protected override MangaFileType FileType => MangaFileType.numericpatterns;
        protected override Int32 DerivedVersion => NumericPatternsVersion1;

        protected override void SaveDerived(IMangaSerializationWriter writer)
        {
            writer.WriteInt32(_minBucketSize);
            writer.WriteDecimal(_minBucketMod);
            writer.WriteDecimal(_proposedSpread);
            writer.SaveChild(_decimalStat);
            writer.SaveChild(_decimalCountNoValueStat);
        }

        protected override void LoadDerived(Int32 version, IMangaSerializationReader reader, MangaLoadOptions loadOptions)
        {
            _minBucketSize = reader.ReadInt32();
            _minBucketMod = reader.ReadDecimal();
            _proposedSpread = reader.ReadDecimal();
            _decimalStat = reader.LoadChild<MangaStat>(new MangaStat(), loadOptions);
            _decimalCountNoValueStat = reader.LoadChild<MangaStat>(new MangaStat(), loadOptions);
        }

        protected override void ProcessData(int row, decimal data, bool hasValue, bool isText, IBackgroundJob status)
        {
            _decimalStat.AddStat(data, hasValue);
            _decimalCountNoValueStat.AddStat(data, true);
        }

        public override bool CalulateBuckets(bool dimension, int totalRows, int maxDisplayRows, int distinctValues, bool onlyBuckets, IBackgroundJob status)
        {
            if (dimension == false)
            {
                CalcNumericBucketSize(maxDisplayRows);
                return true;
            }

            return false;
        }

        public decimal TruncateDecimal(decimal value, UInt64 precision)
        {
            decimal step = Convert.ToDecimal(Math.Pow(10, precision));
            decimal tmp = Math.Truncate(step * value);
            return tmp / step;
        }

        private void CalcNumericBucketSize(int maxDisplayRows)
        {
            decimal range = _decimalStat.Range;
            UInt64 avgIntegral = _decimalStat.AvgIntegral;
            UInt64 avgFractional = _decimalStat.AvgFractional;
            decimal minBucketMod = range / maxDisplayRows;
            if (avgFractional == 0)
            {
                minBucketMod = Math.Max(MinDecimalBucket, Math.Ceiling(minBucketMod));
            }
            else
            {
                minBucketMod = TruncateDecimal(minBucketMod, avgFractional);
            }
            if ( minBucketMod > _decimalStat.Average )
            {
                minBucketMod = range / minBucketMod;
            }

            if (range < MangaConstants.SmallBucketThreshold)
            {
                if (avgIntegral > 1 && range > MangaConstants.MaxDistinctfactor)
                {
                    _proposedSpread = 1;
                }
                else
                {
                    _proposedSpread = 1;
                }
            }
            else
            {
                //int avgDigits = ((digits(range) + digits(_minDecimal) + digits(_maxDecimal) + digits(_totalDecimal / totalRows)) / 4) - 1;
                _proposedSpread = Convert.ToDecimal(Math.Pow(10, avgIntegral));
                while ((_proposedSpread * MangaConstants.MaxDistinctfactor) > range)
                {
                    _proposedSpread /= 10;
                }
            }

            if ( _proposedSpread < minBucketMod)
            {
                minBucketMod = _proposedSpread;
            }

            if (avgFractional == 0)
                _minBucketMod = Math.Max(minBucketMod, MinDecimalBucket);
            else
                _minBucketMod = minBucketMod;
        }

        private int digits(decimal numba)
        {
            int digits = 0;
            numba = Math.Abs(numba);
            while (numba > 0)
            {
                ++digits;
                numba = decimal.Floor(numba / 10);
            }

            return digits;
        }

        private IEnumerable<decimal?> ReadDecimals()
        {
            foreach (var obj in ReadSequential(DataType.Numeric))
                yield return obj as decimal?;
        }

        public override Tuple<int, decimal> ReSpread(string name, int totalRows, Dictionary<string, MyBitArray> rows, Dictionary<string, int> rowCounts, int? newBucketSize, decimal? newBucketMod, IBackgroundJob status)
        {
            if (newBucketMod.HasValue)
                _proposedSpread = newBucketMod.Value;

            NumericBuckets bucket;
            SortedDictionary<decimal, string> averageLengthBuckets = null;
            if (newBucketSize.HasValue && newBucketSize.Value == (int)NumericBuckets.FromAverage)
            {
                bucket = NumericBuckets.FromAverage;
                averageLengthBuckets = _decimalStat.BuildAverageBasedBuckets(Convert.ToInt32(_proposedSpread), "Value", 2);
            }
            else
            {
                bucket = NumericBuckets.Range;
            }


            decimal minDecimal = _decimalStat.MinDecimal;
            int rowIndex = 0;
            foreach (var row in ReadDecimals())
            {
                string key;
                if (row.HasValue)
                {
                    if (bucket == NumericBuckets.FromAverage)
                    {
                        key = _decimalStat.GetAverageBasedBucket(averageLengthBuckets, row.Value);
                    }
                    else
                    {
                        int i = (int)((row.Value - minDecimal) / _proposedSpread);
                        decimal start = minDecimal + (i * _proposedSpread);
                        if (row.Value > 10)
                            key = string.Format("{0:F2} - {1:F2}", start, (start + _proposedSpread));
                        else
                            key = string.Format("{0:F4} - {1:F4}", start, (start + _proposedSpread));
                    }
                }
                else
                {
                    key = MangaConstants.NullOrEmpty;
                }
                if (rows.ContainsKey(key) == false)
                {
                    rows.Add(key, new MyBitArray());
                    rowCounts.Add(key, 0);
                }
                rows[key].Set(rowIndex);
                rowCounts[key] = rowCounts[key] + 1;

                if ((rowIndex % TaskConstants.StatusUpdateCheckFreq) == 0)
                {
                    int pct = MiscHelpers.CalcPercent(rowIndex, totalRows);
                    status.SetStatus(MangaFactoryStatusKey.Analyzing, string.Format("Distributing data into buckets for {0} {1}%", name, pct), MangaFactoryStatusState.Running);
                    if (status.IsCancellationRequested)
                    {
                        return Tuple.Create(-1, 0M);
                    }
                }

                ++rowIndex;
            }

            return Tuple.Create((int)bucket, _proposedSpread);
        }

        public override RawDataReader GetReader()
        {
            return GetReader(DataType.Numeric);
        }
    }
}
