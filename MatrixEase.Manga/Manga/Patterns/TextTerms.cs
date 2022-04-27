using MatrixEase.Manga.Processing;
using MatrixEase.Manga.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatrixEase.Manga.Manga
{
    public class TextTerms : TextMetric
    {
        private const Int32 MangaStatVersion1 = 1;
        private const int OneWord = 1;
        private const int CoupleOfWordThreshold = 2;
        private const int LotsOfTermThreshold = 5;
        private const int FewWordsThreshold = 20;

        /// Start Serialized Items 
        private MangaStat _allTermsStat;
        private MangaStat _distinctTermsStat;
        private MangaStat _allConsideredTermsStat;
        private MangaStat _distinctConsideredTermsStat;

        private List<string> _termPatterns;
        private Dictionary<string, double> _termWeights;

        private bool _allDocTermCountStat = false;
        private Dictionary<string, Int32> _allDocTermCount = new Dictionary<string, Int32>();
        private Dictionary<Int32, Int32> _docTermCount = new Dictionary<Int32, Int32>();
        private Dictionary<Int32, Dictionary<string, Int32>> _termsCount = new Dictionary<Int32, Dictionary<string, Int32>>();
        /// End Serialized Items 

        public TextTerms()
        {
            _allTermsStat = new MangaStat();
            _distinctTermsStat = new MangaStat();
            _allConsideredTermsStat = new MangaStat();
            _distinctConsideredTermsStat = new MangaStat();
        }
        public object AllStats
        {
            get
            {
                /*
        private Dictionary<string, double> _termWeights;

                 */
                return new
                {
                    AllTermStats = _allTermsStat.AllStats(false),
                    DistinctTermStats = _distinctTermsStat.AllStats(false),
                    AllConsideredTermStats = _allConsideredTermsStat.AllStats(false),
                    DistinctConsideredTermStats = _distinctConsideredTermsStat.AllStats(false),
                    Limit = MangaConstants.SmallBucket,
                    TermCounts = TopNBuckets(_allDocTermCount, MangaConstants.SmallBucket),
                    TermWeights = TopNBuckets(_termWeights, MangaConstants.SmallBucket),
                    CalculatedTermPatterns = _termPatterns
                };
            }
        }


        public override int Version => MangaStatVersion1;

        public override void Save(IMangaSerializationWriter writer)
        {
            writer.SaveChild(_allTermsStat);
            writer.SaveChild(_distinctTermsStat);
            writer.SaveChild(_allConsideredTermsStat);
            writer.SaveChild(_distinctConsideredTermsStat);

            writer.WriteListString(_termPatterns);
            writer.WriteDictStringDouble(_termWeights);

            writer.WriteBool(_allDocTermCountStat);
            writer.WriteDictStringInt32(_allDocTermCount);
            writer.WriteDictInt32Int32(_docTermCount);
            if (_termsCount != null)
            {
                Int32 count = _termsCount.Count();
                writer.WriteInt32(count);
                foreach (var pair in _termsCount)
                {
                    writer.WriteInt32(pair.Key);
                    writer.WriteDictStringInt32(pair.Value);
                }
            }
            else
            {
                Int32 count = 0;
                writer.WriteInt32(count);
            }
        }

        public override void Load(int version, IMangaSerializationReader reader, MangaLoadOptions loadOptions)
        {
            _allTermsStat = reader.LoadChild<MangaStat>(new MangaStat(), loadOptions);
            _distinctTermsStat = reader.LoadChild<MangaStat>(new MangaStat(), loadOptions);
            _allConsideredTermsStat = reader.LoadChild<MangaStat>(new MangaStat(), loadOptions);
            _distinctConsideredTermsStat = reader.LoadChild<MangaStat>(new MangaStat(), loadOptions);

            _termPatterns = reader.ReadListString();
            _termWeights = reader.ReadDictStringDouble();

            _allDocTermCountStat = reader.ReadBool();
            _allDocTermCount = reader.ReadDictStringInt32();

            _docTermCount = reader.ReadDictInt32Int32();

            Int32 termCount = reader.ReadInt32();
            if (termCount != 0)
            {
                _termsCount = new Dictionary<int, Dictionary<string, int>>(termCount);
                for (int i = 0; i < termCount; i++)
                {
                    Int32 key = reader.ReadInt32();
                    var dict = reader.ReadDictStringInt32();
                    _termsCount.Add(key, dict);
                }
            }
        }


        public void ProcessTerms(int row, string data, bool countMe, IBackgroundJob status)
        {
            var allTerms = TextStats.SplitTerms(data);
            int termsCount = allTerms.Count();
            _allTermsStat.AddStat(termsCount, countMe);
            _distinctTermsStat.AddStat(allTerms.Distinct().Count(), countMe);

            var terms = new List<string>(termsCount);
            Dictionary<string, int> termCounts = new Dictionary<string, int>();
            foreach (var term in allTerms)
            {
                if (term.Length > 1 && AllStopWords.Words.Contains(term) == false)
                {
                    terms.Add(term);
                    int count;
                    if (!_allDocTermCount.TryGetValue(term, out count))
                    {
                        _allDocTermCount.Add(term, 1);
                    }
                    else
                    {
                        _allDocTermCount[term] = count + 1;
                    }

                    if (!termCounts.TryGetValue(term, out count))
                    {
                        termCounts.Add(term, 1);
                    }
                    else
                    {
                        termCounts[term] = count + 1;
                    }
                }
            }

            var termCount = terms.Count();
            if (termCount == 0)
            {
                _allConsideredTermsStat.AddStat(0, countMe);
                _distinctConsideredTermsStat.AddStat(0, countMe);
            }
            else
            {
                _allConsideredTermsStat.AddStat(termCount, countMe);
                _distinctConsideredTermsStat.AddStat(terms.Distinct().Count(), countMe);

                _docTermCount.Add(row, termCount);
                _termsCount.Add(row, termCounts);
            }
        }

        public bool AverageUnderTermsThreshold => _allTermsStat.Average <= CoupleOfWordThreshold;
        public bool TotalUnderTermsThreshold => _allTermsStat.TotalCount <= CoupleOfWordThreshold;
        public TextBuckets CalcTextBucketSize(int totalRows, IBackgroundJob status)
        {
            TextBuckets bucket = TextBuckets.Length;
            if (_allTermsStat.Average != OneWord || _allDocTermCount.Count > OneWord)
            {
                if (_allTermsStat.Average <= LotsOfTermThreshold)
                {
                    CalculateTermPatterns(status);
                    bucket = TextBuckets.CoupleWords;
                }

                if (_allDocTermCount.Count > OneWord)
                {
                    bucket = TextBuckets.TermCounts;
                }

                if (_allTermsStat.Average > CoupleOfWordThreshold || _allDocTermCount.Count < FewWordsThreshold)
                {
                    CalculateTermFrequencies(totalRows, status);
                    bucket = TextBuckets.LotsOfWords;
                }
            }

            return bucket;
        }

        private void AddTermPatternOrderedNoRepition(string root, int[] indexes, int level, string[] terms, IBackgroundJob status)
        {
            for (int i = 0; i < terms.Length; i++)
            {
                if (indexes.Contains(i) == false)
                {
                    _termPatterns.Add(root + terms[i]);
                    if (level < terms.Length - 1)
                        AddTermPatternOrderedNoRepition(root + terms[i] + " ", indexes.Append(i).ToArray(), level + 1, terms, status);
                }
            }
        }

        private void CalculateTermPatterns(IBackgroundJob status)
        {
            int take = Math.Max(LotsOfTermThreshold, Convert.ToInt32(Math.Floor(_allTermsStat.Average + .5M)));
            var terms = _allDocTermCount.OrderByDescending(t => t.Value).Take(take).Select(t => t.Key).ToArray();

            _termPatterns = new List<string>();
            AddTermPatternOrderedNoRepition("", new int[0], 0, terms, status);
        }

        private void CalculateTermFrequencies(int totalRows, IBackgroundJob status)
        {
            Dictionary<string, TermWeight> weights = new Dictionary<string, TermWeight>();
            double totalWeights = 0;
            double countWeights = 0;
            foreach (int row in _docTermCount.Keys)
            {
                var totalTermsInDoc = _docTermCount[row];
                foreach (var term in _termsCount[row])
                {
                    double tf = (double)term.Value / (double)totalTermsInDoc;
                    double idf = Math.Log((double)totalRows / (double)_allDocTermCount[term.Key]);
                    double weight = tf * idf;
                    if (weight > 0)
                    {
                        totalWeights += weight;
                        ++countWeights;
                        TermWeight curWeight;
                        if (!weights.TryGetValue(term.Key, out curWeight))
                        {
                            weights.Add(term.Key, new TermWeight(term.Key, weight));
                        }
                        else
                        {
                            curWeight.AddWeight(weight);
                        }
                    }
                }
            }

            //double averageWeight = (totalWeights / countWeights);
            //double weightTest = averageWeight + (averageWeight / 4);
            //int thousanth = totalRows / 1000;
            //double onePercent = totalRows * 1 / 100;
            //double twentyPercent = totalRows * 20 / 100;
            //
            /*
            _termWeights = weights.Where(w => w.Value.Count > onePercent && w.Value.Count < twentyPercent && w.Value.AverageWeigth > weightTest).OrderByDescending(w => w.Value.Count).Take(MangaConstants.SmallBucket).ToDictionary(k => k.Key, v => v.Value.AverageWeigth);
            if (_termWeights.Count < MangaConstants.SmallBucketThreshold)
            {
                _termWeights = weights.Where(w => w.Value.Count < twentyPercent && w.Value.AverageWeigth > weightTest).OrderByDescending(w => w.Value.Count).Take(MangaConstants.SmallBucket).ToDictionary(k => k.Key, v => v.Value.AverageWeigth);
            }
            */

            double loop = 0;
            do
            {
                double percentCheck = totalRows * loop / 100;
                _termWeights = weights.Where(w => w.Value.Count > percentCheck).OrderBy(w => w.Value.AverageWeigth).ToDictionary(k => k.Key, v => v.Value.AverageWeigth);
                loop += .01;
            } while (_termWeights.Count > MangaConstants.ReasonablBucket && loop < 100);
            if (_termWeights.Count > MangaConstants.ReasonablBucket )
            {
                _termWeights = _termWeights.OrderBy(v => v.Value).Take(MangaConstants.ReasonablBucket).ToDictionary(k => k.Key, v => v.Value);
            }
        }

        public void AddBuckets(List<TextBuckets> bucketOptions)
        {
            if ( _termPatterns != null && _termPatterns.Count > 0 )
            {
                bucketOptions.Add(TextBuckets.CoupleWords);
            }
            
            if (_termWeights != null && _termWeights.Count > 0)
            {
                bucketOptions.Add(TextBuckets.LotsOfWords);
            }

            if (_allDocTermCount != null && _allDocTermCount.Count > OneWord)
            {
                bucketOptions.Add(TextBuckets.TermCounts);
            }
        }

        public void ClearTerms()
        {
            _termPatterns = null;
            _termWeights = null;
            _allDocTermCountStat = true;
            _allDocTermCount = null;
            _docTermCount = null;
            _termsCount = null;
        }

        public string GetPatternKey(string data)
        {
            var terms = (from s in TextStats.SplitTerms(data)
                         where s.Length > 1 && AllStopWords.Words.Contains(s) == false
                         select s).ToList();

            while (terms.Count > 0)
            {
                var pattern = string.Join(" ", terms);
                if (_termPatterns.Contains(pattern))
                {
                    return pattern;
                }
                terms.RemoveAt(terms.Count - 1);
            }

            return MangaConstants.NoPatternFound;
        }

        public string GetTermKey(int row)
        {
            if (_termsCount.ContainsKey(row))
            {
                var termsInRow = _termsCount[row];
                var key = _termWeights.Where(t => termsInRow.ContainsKey(t.Key) == true).OrderByDescending(t => t.Value).FirstOrDefault();
                if (key.Key != null)
                    return key.Key;
            }
            return MangaConstants.NoKeyWords;
        }

        public List<string> GetTermCountKeys()
        {
            return _allDocTermCount.OrderByDescending(t => t.Value).Select(t => t.Key).Take(MangaConstants.ReasonablBucket).ToList();
        }

        public string GetTermCountKey(string data, List<string> terms)
        {
            string lower = data.ToLower();
            foreach( string key in terms )
            {
                if (lower.Contains(key))
                    return key;
            }
            return MangaConstants.NoKeyWords;
        }

        public void AddToStats(Dictionary<string, object> stats)
        {
            if (_allTermsStat.TotalCount > 0)
            {
                stats.Add("AvgTerms", _allTermsStat.Average);
                stats.Add("AvgDistinctTerms", _distinctTermsStat.Average);
                stats.Add("AvgConsideredTerms", _allConsideredTermsStat.Average);
                stats.Add("AvgConsideredDistinctTerms", _distinctConsideredTermsStat.Average);

                if (_termPatterns != null && _termPatterns.Count > 0)
                    stats.Add("TermPatterns", _termPatterns.Count);

                if (_termWeights != null && _termWeights.Count > 0)
                    stats.Add("TermWeigths", _termWeights.Count);

                if (_allDocTermCountStat == false && _allDocTermCount != null && _allDocTermCount.Count > 0)
                    stats.Add("DocTermCounts", _allDocTermCount.Count);
            }
        }
    }
}
