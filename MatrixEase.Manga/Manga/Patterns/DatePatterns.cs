using MatrixEase.Manga.Processing;
using MatrixEase.Manga.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MatrixEase.Manga.Manga
{
    public class DatePatterns : BasePatterns<long>
    {
        private const Int32 DatePatternsVersion1 = 1;
        private const Int32 CentryYears = 100;
        private const Int32 DecadeYears = 10;

        /// Start Serialized Items 
        private DateTimeBuckets _minBucket;
        private DateTimeBuckets _bucket;
        private double _millisecondSpread = 0;
        private MangaStat _dateStat;
        /// End Serialized Items 

        public override int MinBucketSize { get => (int)_minBucket; }
        public override decimal MinBucketMod { get => 0M; }
        public override UInt32[] AllowedBuckets { get => null; }
        public override string ColType { get => MangaConstants.Dimension; }

        public override object Stat 
        {
            get 
            {
                var avg = new DateTime((long)_dateStat.Average);
                var earliest = new DateTime((long)_dateStat.MinDecimal);
                var latest = new DateTime((long)_dateStat.MaxDecimal);

                return new {
                    Count = _dateStat.TotalCount,
                    Earliest = earliest,
                    Latest = latest,
                    Average = avg,
                    Range = (latest - earliest).ToString(),
                    StdDevDays = (avg - new DateTime((long)_dateStat.Average - (long)_dateStat.StandardDeviation)).Days,
                    CoefVarDays = (avg - new DateTime((long)_dateStat.Average - (long)_dateStat.CoefficientOfVariation)).Days
                };
            }
        }
        public override object DetailedStats
        {
            get
            {
                return new { DateStats = Stat };
            }
        }
                
        protected override int TrueDistinctValues { get => Positions; }

        public DatePatterns(int index) : base(index)
        {
        }

        public DatePatterns(int index, string workFolder) : base(index, workFolder)
        {
            _dateStat = new MangaStat();
        }

        protected override MangaFileType FileType => MangaFileType.datepatterns;
        protected override Int32 DerivedVersion => DatePatternsVersion1;

        protected override void SaveDerived(IMangaSerializationWriter writer)
        {
            writer.WriteEnum<DateTimeBuckets>(_minBucket);
            writer.WriteEnum<DateTimeBuckets>(_bucket);
            writer.WriteDouble(_millisecondSpread);
            writer.SaveChild( _dateStat );
        }

        protected override void LoadDerived(Int32 version, IMangaSerializationReader reader, MangaLoadOptions loadOptions)
        {
            _minBucket = reader.ReadEnum<DateTimeBuckets>();
            _bucket = reader.ReadEnum<DateTimeBuckets>();
            _millisecondSpread = reader.ReadDouble();
            _dateStat = reader.LoadChild<MangaStat>(new MangaStat(), loadOptions);
        }

        protected override void ProcessData(int row, long ticks, bool hasValue, bool isText, IBackgroundJob status)
        {
            if (hasValue)
                _dateStat.AddStat( ticks, hasValue );
        }

        public override bool CalulateBuckets(bool dimension, int totalRows, int maxDisplayRows, int distinctValues, bool onlyBuckets, IBackgroundJob status)
        {
            if (dimension)
            {
                _minBucket = CalcDateTimeBucketSize();
                return true;
            }

            return false;
        }

        private DateTimeBuckets CalcDateTimeBucketSize()
        {
            DateTime maxDate = new DateTime((long)_dateStat.MaxDecimal);
            DateTime minDate = new DateTime((long)_dateStat.MinDecimal);
            int years = (maxDate.Year - minDate.Year) + 1;
            if ((years/ CentryYears) > MangaConstants.ReasonablBucket)
                return DateTimeBuckets.Centuries;
            else if ((years/ DecadeYears) > MangaConstants.ReasonablBucket)
                return DateTimeBuckets.Decades;
            else if (years > MangaConstants.ReasonablBucket)
                return DateTimeBuckets.Years;
            else if (years > 1)
                return DateTimeBuckets.Months;
            else
            {
                TimeSpan timeRange = (maxDate - minDate);
                if (timeRange.TotalDays > (365 / 3))
                    return DateTimeBuckets.Weeks;
                else if (timeRange.TotalDays > 6)
                    return DateTimeBuckets.Days;
                else if (timeRange.TotalHours > 12)
                    return DateTimeBuckets.Hours;
                else if (timeRange.TotalMinutes > 9)
                    return DateTimeBuckets.Minutes;
                else if (timeRange.TotalSeconds > 30)
                    return DateTimeBuckets.Seconds;

                var integral = (UInt64)Math.Floor(Math.Log10(timeRange.TotalMilliseconds)) + 1;
                _millisecondSpread = Math.Pow(10, integral);
                if ((_millisecondSpread * MangaConstants.MaxDistinctfactor) > timeRange.TotalMilliseconds)
                {
                    _millisecondSpread /= 10;
                }

                return DateTimeBuckets.CustomMilliSecondRange;
            }
        }

        private IEnumerable<DateTime?> ReadDateTime()
        {
            foreach (var obj in ReadSequential(DataType.Date))
                yield return obj as DateTime?;
        }

        public override Tuple<int, decimal> ReSpread(string name, int totalRows, Dictionary<string, MyBitArray> rows, Dictionary<string, int> rowCounts, int? newBucketSize, decimal? newBucketMod, IBackgroundJob status)
        {
            if (newBucketSize.HasValue)
                _bucket = (DateTimeBuckets)newBucketSize.Value;
            else
                _bucket = _minBucket;

            DateTime minDate = new DateTime((long)_dateStat.MinDecimal);
            int rowIndex = 0;
            foreach (var row in ReadDateTime())
            {
                string key;
                if (row.HasValue)
                {
                    int i;
                    DateTime start;
                    switch (_bucket)
                    {
                        case DateTimeBuckets.CustomMilliSecondRange:
                            i = (int)((row.Value - minDate).TotalMilliseconds / _millisecondSpread);
                            start = minDate.AddMilliseconds(i * _millisecondSpread);
                            key = string.Format("{0} - {1}", start, start.AddMilliseconds(_millisecondSpread));
                            break;
                        case DateTimeBuckets.Seconds:
                            i = (int)((row.Value - minDate).TotalSeconds);
                            start = minDate.AddSeconds(i);
                            key = string.Format("{0} - {1}", start.ToString("yyyy-MM-dd HH:mm:ss"), start.AddSeconds(1).ToString("yyyy-MM-dd HH:mm:ss"));
                            break;
                        case DateTimeBuckets.Minutes:
                            i = (int)((row.Value - minDate).TotalMinutes);
                            start = minDate.AddMinutes(i);
                            key = string.Format("{0} - {1}", start.ToString("yyyy-MM-dd HH:mm"), start.AddMinutes(1).ToString("yyyy-MM-dd HH:mm"));
                            break;
                        case DateTimeBuckets.Hours:
                            i = (int)((row.Value - minDate).TotalHours);
                            start = minDate.AddHours(i);
                            key = string.Format("{0} - {1}", start.ToString("yyyy-MM-dd HH:00"), start.AddHours(1).ToString("yyyy-MM-dd HH:00"));
                            break;
                        case DateTimeBuckets.Days:
                            i = (int)((row.Value - minDate).TotalDays);
                            start = minDate.AddDays(i);
                            key = string.Format("{0} - {1}", start.ToString("yyyy-MM-dd"), start.AddDays(1).ToString("yyyy-MM-dd"));
                            break;
                        case DateTimeBuckets.Weeks:
                            i = (int)((row.Value - minDate).TotalDays) / 7;
                            start = minDate.AddDays(i);
                            start = start.AddDays(-(start.DayOfWeek - DateTimeFormatInfo.CurrentInfo.FirstDayOfWeek)).Date;
                            key = string.Format("{0} - {1}", start.ToString("yyyy-MM-dd"), start.AddDays(6).ToString("yyyy-MM-dd"));
                            break;
                        case DateTimeBuckets.Months:
                            i = ((row.Value.Year - minDate.Year) * 12) + row.Value.Month - minDate.Month;
                            start = minDate.AddMonths(i);
                            key = string.Format("{0} - {1}", start.ToString("yyyy-MM"), start.AddMonths(1).ToString("yyyy-MM"));
                            break;
                        case DateTimeBuckets.Years:
                            i = (row.Value.Year - minDate.Year);
                            start = minDate.AddYears(i);
                            key = string.Format("{0} - {1}", start.ToString("yyyy"), start.AddYears(1).ToString("yyyy"));
                            break;
                        case DateTimeBuckets.Decades:
                            i = (row.Value.Year - minDate.Year) / DecadeYears;
                            start = minDate.AddYears(i * DecadeYears);
                            key = string.Format("{0} - {1}", start.Year, start.AddYears(DecadeYears).Year);
                            break;
                        case DateTimeBuckets.Centuries:
                            i = (row.Value.Year - minDate.Year) / CentryYears;
                            start = minDate.AddYears(i * CentryYears);
                            key = string.Format("{0} - {1}", start.Year, start.AddYears(CentryYears).Year);
                            break;
                        case DateTimeBuckets.MonthOfYear:
                            key = new DateTimeFormatInfo().GetMonthName(row.Value.Month);
                            break;
                        case DateTimeBuckets.DayofWeek:
                            key = row.Value.DayOfWeek.ToString();
                            break;
                        case DateTimeBuckets.HourOfDay:
                            key = row.Value.Hour.ToString();
                            break;
                        case DateTimeBuckets.MinuteOfHour:
                            key = row.Value.Minute.ToString();
                            break;
                        case DateTimeBuckets.SecondOfMinute:
                            key = row.Value.Second.ToString();
                            break;
                        default:
                            key = "Unknown";
                            break;
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

            return Tuple.Create((int)_bucket, 0M);
        }

        public override RawDataReader GetReader()
        {
            return GetReader(DataType.Date);
        }
    }
}
