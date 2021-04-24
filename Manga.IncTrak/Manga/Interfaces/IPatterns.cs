using Manga.IncTrak.Processing;
using Manga.IncTrak.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Manga.IncTrak.Manga
{
    public interface IPatterns
    {
        bool CalulateBuckets(bool dimension, int totalRows, int maxDisplayRows, IBackgroundJob status);
        decimal ReSpread(string name, int totalRows, Dictionary<string, MyBitArray> rows, Dictionary<string, int> rowCounts, decimal? newBucket, IBackgroundJob status);
        void ProcessWorkingFolder(string mangaPath);
        int DistinctValues { get; }
        decimal MinBucketSize { get; }
        UInt32[] AllowedBuckets { get; }
        string ColType { get; }
        object Stat { get; }
        object DetailedStats { get; }
        RawDataReader GetReader();
    }
}
