using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Manga.IncTrak.Manga
{
    public static class MangaConstants
    {
        public const int SampleSize = 25;
        public const string Dimension = "Dimension";
        public const string Measure = "Measure";
        public const string NullOrEmpty = "[Null or Empty Values]";
        public const string NotSignificant = "[Not Significant]";
        public const string NotPrefix = "[Not a Prefix]";
        public const string NoKeyWords = "[No keywords]";
        public const string NoPatternFound = "[No Pattern Found]";
        public const int SelectivityThreshold = 25;
        public const int SmallBucketThreshold = 10;
        public const int MaxDistinctfactor = 5;
        public const int MaxTextDistinct = 2500;
        public const int ReasonablBucket = 1000;
        public const int MaximumBucket = 6000;
        public const int SmallBucket = 100;
        public const int SmallTextSize = 100;
        public const string SheetID = "sheet_id";
        public const string SheetRange = "range";
        public const string CsvSeparator = "csv_separator";
        public const string CsvQuote = "csv_quote";
        public const string CsvEscape = "csv_escape";
        public const string CsvNull = "csv_null";
        public const string CsvEol = "csv_eol";
        public const int ThrowOutOnesThreshold = 50000;
        public const int LargeColumn = 40;
    }
}
