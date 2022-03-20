using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatrixEase.Manga.Utility
{
    static class AllStopWords
    {
        static HashSet<string> _allStopWords = new HashSet<string>();
        public static HashSet<string> Words { get => _allStopWords; }

        static AllStopWords()
        {
            _allStopWords.UnionWith(DefaultStopWords.Words);
            _allStopWords.UnionWith(LongStopWords.Words);
            _allStopWords.UnionWith(GoogsStopWords.Words);
            _allStopWords.UnionWith(MySqlStopWords.Words);
            _allStopWords = _allStopWords.Distinct().ToHashSet();
        }
    }
}
