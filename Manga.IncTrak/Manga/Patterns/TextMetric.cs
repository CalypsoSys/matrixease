using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MatrixEase.Manga.Manga
{
    public abstract class TextMetric : IMangaSerializeInline
    {
        public abstract int Version { get; }

        public abstract void Load(int version, IMangaSerializationReader reader, MangaLoadOptions loadOptions);
        public abstract void Save(IMangaSerializationWriter writer);


        protected object TopNBuckets(Dictionary<string, Int32> counts, int limit)
        {
            if (counts != null)
            {
                return new 
                { 
                    Count = counts.Count, 
                    Total = counts.Sum(c => c.Value), 
                    Sample = counts.OrderByDescending(c => c.Value).Take(limit).Select(c => new { Name = c.Key, Value = c.Value }).ToArray() 
                };
            }

            return new { Count = 0, Total =0, Sample = new object[0] };
        }

        // uggggg
        protected object TopNBuckets(Dictionary<string, double> counts, int limit)
        {
            if (counts != null)
            {
                return new
                {
                    Count = counts.Count,
                    Total = counts.Sum(c => c.Value),
                    Sample = counts.OrderByDescending(c => c.Value).Take(limit).Select(c => new { Name = c.Key, Value = c.Value }).ToArray()
                };
            }

            return new { Count = 0, Total = 0, Sample = new object[0] };
        }

        protected Dictionary<string, int> CleanTextMetrix(Dictionary<string, int> textSampleCounts)
        {
            if (textSampleCounts != null)
            {
                if (textSampleCounts.Count > MangaConstants.ReasonablBucket)
                {
                    return textSampleCounts.OrderByDescending(s => s.Value).Take(MangaConstants.ReasonablBucket).ToDictionary(k => k.Key, v => v.Value);
                }
            }

            return textSampleCounts;
        }

        protected bool ProcessTextMetric(string textSample, Dictionary<string, int> textSampleCounts)
        {
            if (textSampleCounts != null)
            {
                bool proceed = textSampleCounts.Count < MangaConstants.ThrowOutOnesThreshold;
                for(int i=1; proceed == false;i++)
                {
                    foreach (var key in textSampleCounts.Where(s => s.Value == i).Select(s => s.Key).ToArray())
                    {
                        textSampleCounts.Remove(key);
                    }

                    proceed = textSampleCounts.Count < MangaConstants.ThrowOutOnesThreshold;
                }
                /*
                if (proceed == false)
                {
                    double avg = textSampleCounts.Average(s => s.Value) / 10;
                    foreach(var key in textSampleCounts.Where(s => s.Value < avg).Select(s => s.Key).ToArray() )
                    {
                        textSampleCounts.Remove(key);
                        proceed = true;
                    }
                }
                */

                if (proceed)
                {
                    int count;
                    if (!textSampleCounts.TryGetValue(textSample, out count))
                    {
                        textSampleCounts.Add(textSample, 1);
                    }
                    else
                    {
                        textSampleCounts[textSample] = count + 1;
                    }

                    return true;
                }
            }

            return false;
        }

        protected TextBuckets ClosestReasonable(TextBuckets bucket, Dictionary<string, int>[] dicts, TextBuckets[] buckets)
        {
            int lastAbs = int.MaxValue;
            for (int i = dicts.Length-1; i != -1; i--)
            {
                if (dicts[i] != null)
                {
                    int curAbs = Math.Abs(dicts[i].Count - MangaConstants.ReasonablBucket);
                    if (curAbs < lastAbs)
                    {
                        bucket = buckets[i];
                        lastAbs = curAbs;
                    }
                }
            }

            return bucket;
        }

        protected bool ClearToStat(Dictionary<string, int> textSampleCounts, int topN)
        {
            if (textSampleCounts != null)
            {
                if (textSampleCounts.Count < MangaConstants.MaxTextDistinct)
                    return false;
                textSampleCounts = textSampleCounts.OrderByDescending(t => t.Value).Take(topN).ToDictionary(k => k.Key, v => v.Value);
            }

            return true;
        }

        protected List<TextBuckets> AllowedBucket(Dictionary<string, int>[] dicts, TextBuckets[] buckets, bool[] flags)
        {
            List<TextBuckets> allowed = new List<TextBuckets>();
            for (int i = 0; i < dicts.Length; i++)
            {
                if (dicts[i] != null && flags[i] == false && dicts[i].Count > 0)
                {
                    allowed.Add(buckets[i]);
                }
            }

            return allowed;
        }

        protected void AddToStats(Dictionary<string, object> stats, Dictionary<string, int>[] dicts, TextBuckets[] buckets, bool[] flags)
        {
            for (int i = 0; i < dicts.Length; i++)
            {
                if (dicts[i] != null && flags[i] == false && dicts[i].Count > 0)
                {
                    stats.Add(buckets[i].ToString(), dicts[i].Count);
                }
            }
        }
    }
}
