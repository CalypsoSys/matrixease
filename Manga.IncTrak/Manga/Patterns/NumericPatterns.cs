using Manga.IncTrak.Processing;
using Manga.IncTrak.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Manga.IncTrak.Manga
{
    public class NumericPatterns : BasePatterns<decimal>
    {
        private const Int32 NumericPatternsVersion1 = 1;
        private const Int32 MinDecimalBucket = 2;

        /// Start Serialized Items 
        private decimal _minBucketSize;
        private decimal _proposedSpread;
        private MangaStat _decimalStat;
        private MangaStat _decimalCountNoValueStat;
        /// End Serialized Items 

        public override decimal MinBucketSize { get => _minBucketSize; }
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
            writer.WriteDecimal(_minBucketSize);
            writer.WriteDecimal(_proposedSpread);
            writer.SaveChild(_decimalStat);
            writer.SaveChild(_decimalCountNoValueStat);
        }

        protected override void LoadDerived(Int32 version, IMangaSerializationReader reader, MangaLoadOptions loadOptions)
        {
            _minBucketSize = reader.ReadDecimal();
            _proposedSpread = reader.ReadDecimal();
            _decimalStat = reader.LoadChild<MangaStat>(new MangaStat(), loadOptions);
            _decimalCountNoValueStat = reader.LoadChild<MangaStat>(new MangaStat(), loadOptions);
        }

        protected override void ProcessData(int row, decimal data, bool hasValue, bool isText, IBackgroundJob status)
        {
            _decimalStat.AddStat(data, hasValue);
            _decimalCountNoValueStat.AddStat(data, true);
        }

        public override bool CalulateBuckets(bool dimension, int totalRows, int maxDisplayRows, IBackgroundJob status)
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
            decimal minBucketSize = range / maxDisplayRows;
            if (avgFractional == 0)
            {
                minBucketSize = Math.Max(MinDecimalBucket, Math.Ceiling(minBucketSize));
            }
            else
            {
                minBucketSize = TruncateDecimal(minBucketSize, avgFractional);
            }
            if ( minBucketSize > _decimalStat.Average )
            {
                minBucketSize = range / minBucketSize;
            }

            if (range < MangaConstants.DecimalBucketThreshold)
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

            if ( _proposedSpread < minBucketSize)
            {
                minBucketSize = _proposedSpread;
            }

            if (avgFractional == 0)
                _minBucketSize = Math.Max(minBucketSize, MinDecimalBucket);
            else
                _minBucketSize = minBucketSize;
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

        public override decimal ReSpread(string name, int totalRows, Dictionary<string, MyBitArray> rows, Dictionary<string, int> rowCounts, decimal? newBucket, IBackgroundJob status)
        {
            if (newBucket.HasValue)
                _proposedSpread = newBucket.Value;

            decimal minDecimal = _decimalStat.MinDecimal;
            int rowIndex = 0;
            foreach (var row in ReadDecimals())
            {
                string key;
                if (row.HasValue)
                {
                    int i = (int)((row.Value - minDecimal) / _proposedSpread);
                    decimal start = minDecimal + (i * _proposedSpread);
                    key = string.Format("{0} - {1}", start, (start + _proposedSpread));
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
                    status.SetStatus(MangaFactoryStatusKey.Analyzing, string.Format("Distributing data into buckets for {0}", name), MangaFactoryStatusState.Running);
                    if (status.IsCancellationRequested)
                    {
                        return -1;
                    }
                }

                ++rowIndex;
            }

            return _proposedSpread;
        }

        public override RawDataReader GetReader()
        {
            return GetReader(DataType.Numeric);
        }
    }
}
