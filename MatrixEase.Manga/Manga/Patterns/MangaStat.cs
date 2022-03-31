using MatrixEase.Manga.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace MatrixEase.Manga.Manga
{
    public class MangaStat : IMangaSerializeInline
    {
        private const Int32 MangaStatVersion1 = 1;

        /// Start Serialized Items 
        private Int32 _totalCount = 0;
        private decimal _minDecimal = decimal.MaxValue;
        private decimal _maxDecimal = decimal.MinValue;
        private decimal _totalDecimal = 0;
        private BigInteger _totalSqr = 0;
        private UInt64 _totalIntegral = 0;
        private UInt64 _totalFractional = 0;
        //private List<UInt64> _firstSignificance = new List<UInt64>(new UInt64[32]);
        /// End Serialized Items 

        public MangaStat()
        {
        }

        public object AllStats(bool number)
        {
            if (number)
            {
                return new
                {
                    Count = _totalCount,
                    Min = MinDecimal,
                    Max = MaxDecimal,
                    Average = Average,
                    Range = Range,
                    StandardDeviation = StandardDeviation,
                    CoefficientOfVariation = CoefficientOfVariation,
                    Total = _totalDecimal,
                    TotalSqr = _totalSqr.ToString(),
                    TotalIntegral = _totalIntegral,
                    AvgIntegral = AvgIntegral,
                    TotalFractional = _totalFractional,
                    AvgFractional = AvgFractional
                };
            }
            else
            {
                return new
                {
                    Count = _totalCount,
                    Min = MinDecimal,
                    Max = MaxDecimal,
                    Average = Average,
                    Range = Range,
                    StandardDeviation = StandardDeviation,
                    CoefficientOfVariation = CoefficientOfVariation,
                    Total = _totalDecimal,
                    TotalSqr = _totalSqr.ToString()
                };
            }
        }

        public int Version => MangaStatVersion1;

        public void Save(IMangaSerializationWriter writer)
        {
            writer.WriteInt32(_totalCount);
            writer.WriteDecimal(_minDecimal == decimal.MaxValue ? 0 : _minDecimal);
            writer.WriteDecimal(_maxDecimal == decimal.MinValue ? 0 : _maxDecimal);
            writer.WriteDecimal(_totalDecimal);
            writer.WriteArrayBytes(_totalSqr.ToByteArray());
            writer.WriteUInt64(_totalIntegral);
            writer.WriteUInt64(_totalFractional);
        }

        public void Load(int version, IMangaSerializationReader reader, MangaLoadOptions loadOptions)
        {
            _totalCount = reader.ReadInt32();
            _minDecimal = reader.ReadDecimal();
            _maxDecimal = reader.ReadDecimal();
            _totalDecimal = reader.ReadDecimal();
            _totalSqr = new BigInteger(reader.ReadArrayBytes());
            _totalIntegral = reader.ReadUInt64();
            _totalFractional = reader.ReadUInt64();
        }

        public void AddStat(decimal input, bool hasValue)
        {
            _totalCount++;

            if (hasValue)
            {
                _maxDecimal = Math.Max(_maxDecimal, input);
                _minDecimal = Math.Min(_minDecimal, input);
                if (input != 0)
                {
                    _totalDecimal += input;
                    var bigInt = new BigInteger(input);
                    _totalSqr += (bigInt * bigInt);

                    int[] bits = decimal.GetBits(input);
                    UInt64 fractional = (UInt64)((bits[3] >> 16) & 0x7F);

                    if (fractional > 0)
                    {
                        _totalFractional += fractional;
                        /*
                        decimal test = (input - Math.Truncate(input)) * Convert.ToDecimal(Math.Pow(10, fractional));

                        int significance = (int)(fractional - (UInt64)Math.Floor(Math.Log10((double)test))) - 1;
                        if (significance >= 0 || significance < 32)
                        {
                            _firstSignificance[significance] = _firstSignificance[significance] + 1;
                        }
                        */
                    }

                    _totalIntegral += (UInt64)Math.Floor(Math.Log10((double)input)) + 1;
                }
            }
        }

        public decimal TotalCount => _totalCount;
        public decimal Average
        {
            get
            {
                if (_totalCount != 0)
                    return _totalDecimal / _totalCount;
                return 0;
            }
        }

        public decimal StandardDeviation
        {
            get
            {
                try
                {
                    if (_totalCount != 0 && _totalCount - 1 != 0)
                    { 
                        BigInteger totalCount = new BigInteger(_totalCount);
                        BigInteger totalDecimal = new BigInteger(_totalDecimal);
                        BigInteger one = new BigInteger(1);
                        double d = (double)(((totalCount * _totalSqr) - (totalDecimal * totalDecimal)) / (totalCount * (totalCount - one)));
                        return (decimal)Math.Sqrt(d);
                    }
                }
                catch(Exception excp)
                {
                    MyLogger.LogError(excp, "Error calculating std dev");
                }
                return 0;
            }
        }

        public decimal CoefficientOfVariation
        {
            get
            {
                if (_totalCount != 0 && _totalDecimal != 0)
                    return StandardDeviation / Average * 100M;
                return 0;
            }
        }
        
        public decimal MinDecimal { get => _minDecimal == decimal.MaxValue ? 0 : _minDecimal; set => _minDecimal = value; }
        public decimal MaxDecimal { get => _maxDecimal == decimal.MinValue ? 0 : _maxDecimal; set => _maxDecimal = value; }
        public decimal Range { get => _totalCount != 0 ? Math.Abs(MaxDecimal - MinDecimal) : 0; }
        public UInt64 AvgIntegral { get => _totalCount != 0 ? (UInt64)Math.Floor((_totalIntegral / (decimal)_totalCount) + .5M) : 0; }
        public UInt64 AvgFractional { get => _totalCount != 0 ? _totalFractional / (UInt64)_totalCount : 0; }
        public decimal Total { get => _totalDecimal; set => _totalDecimal = value; }

        private string GetAverageBasedKey(decimal low, decimal high, string prePend, int decimalPlaces)
        {
            string format = string.Format("{{0}} {{1:F{0}}} to {{2:F{0}}}", decimalPlaces);
            return string.Format(format, prePend, low, high);
        }

        public SortedDictionary<decimal, string> BuildAverageBasedBuckets(int maxBuckets, string prePend, int decimalPlaces)
        {
            SortedDictionary<decimal, string> averageBasedBuckets = new SortedDictionary<decimal, string>();
            decimal stdDev = Decimal.Floor(StandardDeviation + .5M);
            decimal lowRange = MinDecimal;
            decimal lowLow = Average - stdDev;
            decimal lowHigh = Average;
            decimal highRange = MaxDecimal;
            decimal hightLow = Average + 1;
            decimal highHigh = Average + stdDev;

            bool isSet = true;
            while (averageBasedBuckets.Count < maxBuckets && isSet == true)
            {
                isSet = false;
                if (lowLow < lowRange)
                    lowLow = lowRange;
                if (lowHigh > lowRange)
                {
                    isSet = true;
                    averageBasedBuckets.Add(lowHigh, GetAverageBasedKey(lowLow, lowHigh, prePend, decimalPlaces));
                    lowHigh = lowLow - 1;
                    lowLow = lowHigh - stdDev;
                }
                if (highHigh > highRange)
                    highHigh = highRange;
                if (hightLow < highRange)
                {
                    isSet = true;
                    averageBasedBuckets.Add(highHigh, GetAverageBasedKey(hightLow, highHigh, prePend, decimalPlaces));
                    hightLow = highHigh + 1;
                    highHigh = hightLow + stdDev;
                }
            }

            if (lowLow > lowRange)
            {
                averageBasedBuckets.Add(lowLow - 1, GetAverageBasedKey(lowRange, lowLow - 1, prePend, decimalPlaces));
            }

            if (highHigh < highRange)
            {
                averageBasedBuckets.Add(highRange, GetAverageBasedKey(highHigh + 1, highRange, prePend, decimalPlaces));
            }

            return averageBasedBuckets;
        }

        public string GetAverageBasedBucket(SortedDictionary<decimal, string> averageBasedBuckets, decimal number)
        {
            foreach (decimal key in averageBasedBuckets.Keys)
            {
                if (number <= key)
                    return averageBasedBuckets[key];
            }

            return MangaConstants.NoKeyWords;
        }
    }
}
