using MatrixEase.Manga.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MatrixEase.Manga.Manga
{
    public class TextPrefixes : TextMetric
    {
        private const Int32 MangaStatVersion1 = 1;

        /// Start Serialized Items 
        private bool _textPrefix1CountsStat = false;
        private Dictionary<string, Int32> _textPrefix1Counts = new Dictionary<string, Int32>();
        private bool _textPrefix2CountsStat = false;
        private Dictionary<string, Int32> _textPrefix2Counts = new Dictionary<string, Int32>();
        private bool _textPrefix3CountsStat = false;
        private Dictionary<string, Int32> _textPrefix3Counts = new Dictionary<string, Int32>();
        private bool _textPrefix4CountsStat = false;
        private Dictionary<string, Int32> _textPrefix4Counts = new Dictionary<string, Int32>();
        /// End Serialized Items 

        public TextPrefixes()
        {
        }

        public object AllStats
        {
            get
            {
                return new
                {
                    Limit = MangaConstants.SmallBucket,
                    Prefix1 = TopNBuckets(_textPrefix1Counts, MangaConstants.SmallBucket),
                    Prefix2 = TopNBuckets(_textPrefix2Counts, MangaConstants.SmallBucket),
                    Prefix3 = TopNBuckets(_textPrefix3Counts, MangaConstants.SmallBucket),
                    Prefix4 = TopNBuckets(_textPrefix4Counts, MangaConstants.SmallBucket),
                };
            }
        }


        public override int Version => MangaStatVersion1;

        public override void Save(IMangaSerializationWriter writer)
        {
            writer.WriteBool(_textPrefix1CountsStat);
            writer.WriteDictStringInt32(_textPrefix1Counts);
            writer.WriteBool(_textPrefix2CountsStat);
            writer.WriteDictStringInt32(_textPrefix2Counts);
            writer.WriteBool(_textPrefix3CountsStat);
            writer.WriteDictStringInt32(_textPrefix3Counts);
            writer.WriteBool(_textPrefix4CountsStat);
            writer.WriteDictStringInt32(_textPrefix4Counts);
        }

        public override void Load(int version, IMangaSerializationReader reader, MangaLoadOptions loadOptions)
        {
            _textPrefix1CountsStat = reader.ReadBool();
            _textPrefix1Counts = reader.ReadDictStringInt32();
            _textPrefix2CountsStat = reader.ReadBool();
            _textPrefix2Counts = reader.ReadDictStringInt32();
            _textPrefix3CountsStat = reader.ReadBool();
            _textPrefix3Counts = reader.ReadDictStringInt32();
            _textPrefix4CountsStat = reader.ReadBool();
            _textPrefix4Counts = reader.ReadDictStringInt32();
        }

        public void ProcessPrefixes(string data)
        {
            var subString = data.SafeSubString(4);
            if (_textPrefix4Counts != null && subString.Length >= 4)
            {
                if (!ProcessTextMetric(subString.Substring(0, 4), _textPrefix4Counts))
                    _textPrefix4Counts = null;
            }
            if (_textPrefix3Counts != null && subString.Length >= 3)
            {
                if (!ProcessTextMetric(subString.Substring(0, 3), _textPrefix3Counts))
                    _textPrefix3Counts = null;
            }
            if (_textPrefix2Counts != null && subString.Length >= 2)
            {
                if (!ProcessTextMetric(subString.Substring(0, 2), _textPrefix2Counts))
                    _textPrefix2Counts = null;
            }
            if (_textPrefix1Counts != null && subString.Length >= 1)
            {
                if (!ProcessTextMetric(subString.Substring(0, 1), _textPrefix1Counts))
                    _textPrefix1Counts = null;
            }
        }

        private Dictionary<string, int>[] CountDicz => new Dictionary<string, int>[] { _textPrefix1Counts, _textPrefix2Counts, _textPrefix3Counts, _textPrefix4Counts };
        private TextBuckets[] TextBucketz => new TextBuckets[] { TextBuckets.Prefix1, TextBuckets.Prefix2, TextBuckets.Prefix3, TextBuckets.Prefix4 };
        private bool[] IsStatz => new bool[] { _textPrefix1CountsStat, _textPrefix2CountsStat, _textPrefix3CountsStat, _textPrefix4CountsStat };

        public TextBuckets CalcTextBucketSize()
        {
            _textPrefix1Counts = CleanTextMetrix(_textPrefix1Counts);
            _textPrefix2Counts = CleanTextMetrix(_textPrefix2Counts);
            _textPrefix3Counts = CleanTextMetrix(_textPrefix3Counts);
            _textPrefix4Counts = CleanTextMetrix(_textPrefix4Counts);
            return ClosestReasonable(TextBuckets.Prefix1, CountDicz, TextBucketz);
        }

        public string GetPrefixKey(TextBuckets type, string data)
        {
            string key = string.Empty;
            if (type == TextBuckets.Prefix1)
            {
                key = data.SafeSubString(1);
                if (_textPrefix1Counts.ContainsKey(key))
                    return key;
            }
            else if (type == TextBuckets.Prefix2)
            {
                key = data.SafeSubString(2);
                if (_textPrefix2Counts.ContainsKey(key))
                    return key;
            }
            else if (type == TextBuckets.Prefix3)
            {
                key = data.SafeSubString(3);
                if (_textPrefix3Counts.ContainsKey(key))
                    return key;
            }
            else if (type == TextBuckets.Prefix4)
            {
                key = data.SafeSubString(4);
                if (_textPrefix4Counts.ContainsKey(key))
                    return key;
            }

            return MangaConstants.NotPrefix;
        }

        public void AddBuckets(List<TextBuckets> bucketOptions)
        {
            bucketOptions.AddRange(AllowedBucket(CountDicz, TextBucketz, IsStatz));
        }

        public void ClearPrefixStats(bool forceClear)
        {
            if (forceClear)
            {
                _textPrefix1CountsStat = true;
                _textPrefix1Counts = null;
                _textPrefix2CountsStat = true;
                _textPrefix2Counts = null;
                _textPrefix3CountsStat = true;
                _textPrefix3Counts = null;
                _textPrefix4CountsStat = true;
                _textPrefix4Counts = null;
            }
            else
            {
                _textPrefix1CountsStat = ClearToStat(_textPrefix1Counts, MangaConstants.SmallBucket);
                _textPrefix2CountsStat = ClearToStat(_textPrefix2Counts, MangaConstants.SmallBucket);
                _textPrefix3CountsStat = ClearToStat(_textPrefix3Counts, MangaConstants.SmallBucket);
                _textPrefix4CountsStat = ClearToStat(_textPrefix4Counts, MangaConstants.SmallBucket);
            }
        }

        public void AddToStats(Dictionary<string, object> stats)
        {
            AddToStats(stats, CountDicz, TextBucketz, IsStatz);
        }
    }
}
