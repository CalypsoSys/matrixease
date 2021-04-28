using Manga.IncTrak.Processing;
using Manga.IncTrak.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Manga.IncTrak.Manga
{
    public class TextPatterns : BasePatterns<string>
    {
        private const Int32 TextPatternsVersion1 = 1;
        private const int SmallTextSize = 15;

        /// Start Serialized Items 
        private TextBuckets _bucket;
        private MangaStat _textLengthStat;
        private UInt32[] _bucketsOptions;

        private TextPrefixes _textPrefixes = new TextPrefixes();
        private TextUrls _textUrls = new TextUrls();
        private TextTerms _textTerms = new TextTerms();
        /// End Serialized Items 

        public override int MinBucketSize { get => (int)_bucket; }
        public override decimal MinBucketMod { get => 0M; }
        public override UInt32[] AllowedBuckets { get => _bucketsOptions; }
        public override string ColType { get => MangaConstants.Dimension; }
        public override object Stat 
        {
            get
            {
                Dictionary<string, object> stats = new Dictionary<string, object>();
                stats.Add("IsUrl", _textUrls.IsUrl);
                stats.Add("AvgTextLength", _textLengthStat.Average);
                stats.Add("ShortestText", _textLengthStat.MinDecimal);
                stats.Add("LongestText", _textLengthStat.MaxDecimal);

                if ( _textUrls.IsUrl)
                {
                    _textUrls.AddToStats(stats);
                }
                else
                {
                    _textPrefixes.AddToStats(stats);
                }
                _textTerms.AddToStats(stats);

                return stats;
            }
        }

        public override object DetailedStats
        {
            get
            {
                if (_textUrls.IsUrl)
                {
                    return new
                    {
                        IsUrl = _textUrls.IsUrl,
                        TextLength = _textLengthStat.AllStats(false),
                        UrlStats = _textUrls.AllStats
                    };
                }
                else
                {
                    return new
                    {
                        IsUrl = _textUrls.IsUrl,
                        TextLength = _textLengthStat.AllStats(false),
                        PrefixStats = _textPrefixes.AllStats,
                        TermStats = _textTerms.AllStats,
                    };
                }
            }
        }

        protected override int TrueDistinctValues { get => Positions; }

        public TextPatterns(int index) : base(index)
        {
        }

        public TextPatterns(int index, string workFolder) : base(index, workFolder)
        {
            _textLengthStat = new MangaStat();
        }

        protected override MangaFileType FileType => MangaFileType.textpatterns;
        protected override Int32 DerivedVersion => TextPatternsVersion1;

        protected override void SaveDerived(IMangaSerializationWriter writer)
        {
            writer.WriteEnum<TextBuckets>(_bucket);
            writer.SaveChild(_textLengthStat);

            writer.WriteArrayUInt32s(_bucketsOptions);

            writer.SaveChild(_textPrefixes);
            writer.SaveChild(_textUrls);
            writer.SaveChild(_textTerms);
        }

        protected override void LoadDerived(Int32 version, IMangaSerializationReader reader, MangaLoadOptions loadOptions)
        {
            _bucket = reader.ReadEnum<TextBuckets>();
            _textLengthStat = reader.LoadChild<MangaStat>(new MangaStat(), loadOptions);

            _bucketsOptions = reader.ReadArrayUInt32s();

            _textPrefixes = reader.LoadChild<TextPrefixes>(new TextPrefixes(), loadOptions);
            _textUrls = reader.LoadChild<TextUrls>(new TextUrls(), loadOptions);
            _textTerms = reader.LoadChild<TextTerms>(new TextTerms(), loadOptions);
        }

        protected override void ProcessData(int row, string data, bool hasValue, bool isText, IBackgroundJob status)
        {
            if (hasValue)
            {
                _textLengthStat.AddStat(data.Length, hasValue);

                _textPrefixes.ProcessPrefixes(data);
                if (isText)
                {
                    if ( _textUrls.ProcessPotentialUrl(data) == false )
                        _textTerms.ProcessTerms(row, data, hasValue, status);
                }
            }
        }

        public override bool CalulateBuckets(bool dimension, int totalRows, int maxDisplayRows, IBackgroundJob status)
        {
            if (dimension)
            {
                _bucket = CalcTextBucketSize(totalRows, status);

                List<TextBuckets> bucketOptions = new List<TextBuckets>();
                if (_textUrls.IsUrl)
                {
                    _textUrls.AddBuckets(bucketOptions);
                }
                else
                {
                    _textPrefixes.AddBuckets(bucketOptions);
                }
                _textTerms.AddBuckets(bucketOptions);
                bucketOptions.Add(TextBuckets.Length);

                _bucketsOptions = bucketOptions.Select(b => (UInt32)b).ToArray();

                return true;
            }

            return false;
        }

        public TextBuckets CalcTextBucketSize(int totalRows, IBackgroundJob status)
        {
            if (_textUrls.IsUrl)
            {
                _textPrefixes.ClearPrefixStats(true);
                _textTerms.ClearTerms();

                return _textUrls.CalcTextBucketSize();
            }
            else
            {
                _textUrls.ClearUrlStats();
                _textPrefixes.ClearPrefixStats(false);

                TextBuckets prefixBucket = _textPrefixes.CalcTextBucketSize();
                TextBuckets termBucket = TextBuckets.Length;
                if (_textTerms.TotalUnderTermsThreshold)
                    _textTerms.ClearTerms();
                else
                    termBucket = _textTerms.CalcTextBucketSize(totalRows, status);

                if ( DistinctValues <= MangaConstants.SmallBucket)
                {
                    return TextBuckets.Natural;
                }
                else 
                {
                    if (_textTerms.AverageUnderTermsThreshold || _textLengthStat.Average < SmallTextSize)
                    {
                        if (DistinctValues > MangaConstants.ReasonablBucket)
                        {
                            return prefixBucket;
                        }
                        return TextBuckets.Natural;
                    }
                    else
                    {
                        return termBucket;
                    }
                }
            }
        }

        private IEnumerable<string> ReadText()
        {
            foreach (var obj in ReadSequential(DataType.Text))
                yield return obj as string;
        }

        public override Tuple<int, decimal> ReSpread(string name, int totalRows, Dictionary<string, MyBitArray> rows, Dictionary<string, int> rowCounts, int? newBucketSize, decimal? newBucketMod, IBackgroundJob status)
        {
            if (newBucketSize.HasValue)
                _bucket = (TextBuckets )newBucketSize.Value;

            SortedDictionary<decimal, string> textLenghBuckets = null;
            if (_bucket == TextBuckets.Length)
            {
                textLenghBuckets = _textLengthStat.BuildAverageBasedBuckets(MangaConstants.SmallBucket, "Length", 0);
            }

            int rowIndex = 0;
            foreach (var row in ReadText())
            {
                string key;
                if (string.IsNullOrEmpty(row) == false )
                {
                    key = null;
                    switch (_bucket)
                    {
                        case TextBuckets.Natural:
                            key = row;
                            break;
                        case TextBuckets.Prefix1:
                        case TextBuckets.Prefix2:
                        case TextBuckets.Prefix3:
                        case TextBuckets.Prefix4:
                            key = _textPrefixes.GetPrefixKey(_bucket, row);
                            break;
                        case TextBuckets.CoupleWords:
                            key = _textTerms.GetPatternKey(row);
                            break;
                        case TextBuckets.LotsOfWords:
                            key = _textTerms.GetTermKey(rowIndex);
                            break;
                        case TextBuckets.UrlThirdPart:
                        case TextBuckets.UrlSecondPart:
                        case TextBuckets.UrlFirst:
                        case TextBuckets.UrlDomain:
                        case TextBuckets.UrlScheme:
                            key = _textUrls.GetUrlKey(_bucket, row);
                            break;
                        case TextBuckets.Length:
                            key = _textLengthStat.GetAverageBasedBucket(textLenghBuckets, row.Length);
                            break;
                    }
                }
                else
                {
                    key = MangaConstants.NullOrEmpty;
                }

                if (rows.ContainsKey(key) == false)
                {
                    rows.Add(key, new MyBitArray(totalRows));
                    rowCounts.Add(key, 0);
                }
                rows[key].Set(rowIndex);
                rowCounts[key] = rowCounts[key] + 1;

                if ((rowIndex % TaskConstants.StatusUpdateCheckFreq) == 0)
                {
                    status.SetStatus(MangaFactoryStatusKey.Analyzing, string.Format("Distributing data into buckets for {0}", name), MangaFactoryStatusState.Running);
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
            return GetReader(DataType.Text);
        }
    }
}
