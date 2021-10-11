using Manga.IncTrak.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Manga.IncTrak.Manga
{
    public class TextUrls : TextMetric
    {
        private const Int32 MangaStatVersion1 = 1;
        private const int IsNotUrl = -1;

        /// Start Serialized Items 
        private Int32 _isUrl = 0;
        private Int32 _notUrl = 0;
        private bool _urlSchemeCountsStat = false;
        private Dictionary<string, Int32> _urlSchemeCounts = new Dictionary<string, Int32>();
        private bool _urlDomainCountsStat = false;
        private Dictionary<string, Int32> _urlDomainCounts = new Dictionary<string, Int32>();
        private bool _urlFirstPathCountsStat = false;
        private Dictionary<string, Int32> _urlFirstPathCounts = new Dictionary<string, Int32>();
        private bool _urlSecondPathCountsStat = false;
        private Dictionary<string, Int32> _urlSecondPathCounts = new Dictionary<string, Int32>();
        private bool _urlThirdPathCountsStat = false;
        private Dictionary<string, Int32> _urlThirdPathCounts = new Dictionary<string, Int32>();
        private bool _urlPathTermCountStat = false;
        private Dictionary<string, Int32> _urlPathTermCount = new Dictionary<string, Int32>();
        /// End Serialized Items 

        private int[] _queryIndexes = new int[3];

        public TextUrls()
        {
        }

        public object AllStats
        {
            get
            {
                return new
                {
                    Limit = MangaConstants.SmallBucket,
                    SchemeCounts = TopNBuckets(_urlSchemeCounts, MangaConstants.SmallBucket),
                    DomainCounts = TopNBuckets(_urlDomainCounts, MangaConstants.SmallBucket),
                    FirstPathCounts = TopNBuckets(_urlFirstPathCounts, MangaConstants.SmallBucket),
                    SecondPathCounts = TopNBuckets(_urlSecondPathCounts, MangaConstants.SmallBucket),
                    ThirdPathCounts = TopNBuckets(_urlThirdPathCounts, MangaConstants.SmallBucket),
                    PathTermCount = TopNBuckets(_urlPathTermCount, MangaConstants.SmallBucket),
                };
            }
        }
        public override int Version => MangaStatVersion1;
        public bool IsUrl => !(_isUrl == 0 || _notUrl == IsNotUrl);

        public override void Save(IMangaSerializationWriter writer)
        {
            writer.WriteInt32(_isUrl);
            writer.WriteInt32(_notUrl);

            writer.WriteBool(_urlSchemeCountsStat);
            writer.WriteDictStringInt32(_urlSchemeCounts);
            writer.WriteBool(_urlDomainCountsStat);
            writer.WriteDictStringInt32(_urlDomainCounts);
            writer.WriteBool(_urlFirstPathCountsStat);
            writer.WriteDictStringInt32(_urlFirstPathCounts);
            writer.WriteBool(_urlSecondPathCountsStat);
            writer.WriteDictStringInt32(_urlSecondPathCounts);
            writer.WriteBool(_urlThirdPathCountsStat);
            writer.WriteDictStringInt32(_urlThirdPathCounts);
            writer.WriteBool(_urlPathTermCountStat);
            writer.WriteDictStringInt32(_urlPathTermCount);
        }

        public override void Load(int version, IMangaSerializationReader reader, MangaLoadOptions loadOptions)
        {
            _isUrl = reader.ReadInt32();
            _notUrl = reader.ReadInt32();

            _urlSchemeCountsStat = reader.ReadBool();
            _urlSchemeCounts = reader.ReadDictStringInt32();
            _urlDomainCountsStat = reader.ReadBool();
            _urlDomainCounts = reader.ReadDictStringInt32();
            _urlFirstPathCountsStat = reader.ReadBool();
            _urlFirstPathCounts = reader.ReadDictStringInt32();
            _urlSecondPathCountsStat = reader.ReadBool();
            _urlSecondPathCounts = reader.ReadDictStringInt32();
            _urlThirdPathCountsStat = reader.ReadBool();
            _urlThirdPathCounts = reader.ReadDictStringInt32();
            _urlPathTermCountStat = reader.ReadBool();
            _urlPathTermCount = reader.ReadDictStringInt32();

        }

        public bool ProcessPotentialUrl(string data)
        {
            if (_notUrl == IsNotUrl || (_notUrl > MangaConstants.MaxTextDistinct && (_isUrl == 0 || (_notUrl > _isUrl && (_notUrl - _isUrl) > MangaConstants.MaxTextDistinct))))
            {
                _notUrl = IsNotUrl;
            }
            else
            {
                int schemaIndex, dnsIndex, portIndex;
                if (TextStats.IsUrl(data, out schemaIndex, out dnsIndex, out portIndex, _queryIndexes))
                {
                    _isUrl++;

                    if (_urlSchemeCounts != null)
                    {
                        if (!ProcessTextMetric(data.Substring(0, schemaIndex), _urlSchemeCounts))
                            _urlSchemeCounts = null;
                    }

                    int start = schemaIndex + TextStats.DNSOffset;
                    var dns = data.Substring(start, dnsIndex - start);
                    if (_urlDomainCounts != null)
                    {
                        if (!ProcessTextMetric(data.Substring(start, dnsIndex - start), _urlDomainCounts))
                            _urlDomainCounts = null;
                    }

                    if (_urlPathTermCount != null)
                    {
                        var path = data.Substring(dnsIndex);
                        HashSet<string> urlParts = TextStats.SplitUrlPath(path);
                        foreach (string part in urlParts)
                        {
                            if (_urlPathTermCount != null && !ProcessTextMetric(part, _urlPathTermCount))
                                _urlPathTermCount = null;
                        }
                    }

                    if (_urlFirstPathCounts != null && _queryIndexes[0] != 0)
                    {
                        if (!ProcessTextMetric(data.Substring(start, _queryIndexes[0] - start), _urlFirstPathCounts))
                            _urlFirstPathCounts = null;

                        if (_urlSecondPathCounts != null && _queryIndexes[1] != 0)
                        {
                            if (!ProcessTextMetric(data.Substring(start, _queryIndexes[1] - start), _urlSecondPathCounts))
                                _urlSecondPathCounts = null;

                            if (_urlThirdPathCounts != null && _queryIndexes[2] != 0)
                            {
                                if (!ProcessTextMetric(data.Substring(start, _queryIndexes[2] - start), _urlThirdPathCounts))
                                    _urlThirdPathCounts = null;
                            }
                        }
                    }

                    return true;
                }
                else
                {
                    _notUrl++;
                }
            }

            return false;
        }

        private Dictionary<string, int>[] CountDicz => new Dictionary<string, int>[] { _urlSchemeCounts, _urlDomainCounts, _urlFirstPathCounts, _urlSecondPathCounts, _urlThirdPathCounts };
        private TextBuckets[] TextBucketz => new TextBuckets[] { TextBuckets.UrlScheme, TextBuckets.UrlDomain, TextBuckets.UrlFirst, TextBuckets.UrlSecondPart, TextBuckets.UrlThirdPart };
        private bool[] IsStatz => new bool[] { _urlThirdPathCountsStat, _urlSecondPathCountsStat, _urlFirstPathCountsStat, _urlDomainCountsStat, _urlSchemeCountsStat };

        public TextBuckets CalcTextBucketSize()
        {
            if (_urlDomainCounts != null && _urlDomainCounts.Count > MangaConstants.SmallBucket && _urlDomainCounts.Count < MangaConstants.ReasonablBucket)
            {
                return TextBuckets.UrlDomain;
            }
            _urlSchemeCounts = CleanTextMetrix(_urlSchemeCounts);
            _urlDomainCounts = CleanTextMetrix(_urlDomainCounts);
            _urlFirstPathCounts = CleanTextMetrix(_urlFirstPathCounts);
            _urlSecondPathCounts = CleanTextMetrix(_urlSecondPathCounts);
            _urlThirdPathCounts = CleanTextMetrix(_urlThirdPathCounts);
            return ClosestReasonable(TextBuckets.UrlScheme,  CountDicz, TextBucketz);
        }

        public string GetUrlKey(TextBuckets type, string data)
        {
            int schemaIndex, dnsIndex, portIndex;
            if (TextStats.IsUrl(data, out schemaIndex, out dnsIndex, out portIndex, _queryIndexes))
            {
                string key;
                if (type == TextBuckets.UrlScheme)
                {
                    key = data.Substring(0, schemaIndex);
                    if (_urlSchemeCounts.ContainsKey(key))
                        return key;
                }
                else
                {
                    int start = schemaIndex + TextStats.DNSOffset;
                    if (type == TextBuckets.UrlFirst && _queryIndexes[0] != 0)
                    {
                        key = data.Substring(start, _queryIndexes[0] - start);
                        if (_urlFirstPathCounts.ContainsKey(key))
                            return key;
                    }
                    else if (type == TextBuckets.UrlSecondPart && _queryIndexes[1] != 0)
                    {
                        key = data.Substring(start, _queryIndexes[1] - start);
                        if (_urlSecondPathCounts.ContainsKey(key))
                            return key;
                    }
                    else if (type == TextBuckets.UrlThirdPart && _queryIndexes[2] != 0)
                    {
                        key = data.Substring(start, _queryIndexes[2] - start);
                        if (_urlThirdPathCounts.ContainsKey(key))
                            return key;
                    }
                    else if (type == TextBuckets.UrlDomain)
                    {
                        key = data.Substring(start, dnsIndex - start);
                        if (_urlDomainCounts.ContainsKey(key))
                            return key;
                    }
                }
            }

            return MangaConstants.NotSignificant;
        }

        public void AddBuckets(List<TextBuckets> bucketOptions)
        {
            bucketOptions.AddRange(AllowedBucket(CountDicz, TextBucketz, IsStatz));
        }

        public void ClearUrlStats()
        {
            _notUrl = IsNotUrl;

            _urlSchemeCountsStat = ClearToStat(_urlSchemeCounts, MangaConstants.SmallBucket);
            _urlDomainCountsStat  = ClearToStat(_urlDomainCounts, MangaConstants.SmallBucket);
            _urlFirstPathCountsStat  = ClearToStat(_urlFirstPathCounts, MangaConstants.SmallBucket);
            _urlSecondPathCountsStat  = ClearToStat(_urlSecondPathCounts, MangaConstants.SmallBucket);
            _urlThirdPathCountsStat  = ClearToStat(_urlThirdPathCounts, MangaConstants.SmallBucket);
            _urlPathTermCountStat  = ClearToStat(_urlPathTermCount, MangaConstants.SmallBucket);
        }

        public void AddToStats(Dictionary<string, object> stats)
        {
            AddToStats(stats, CountDicz, TextBucketz, IsStatz);
        }
    }
}
