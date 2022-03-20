using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatrixEase.Manga.Utility
{
    static class GoogsStopWords
    {
        public static HashSet<string> Words = new HashSet<string>
        {
            "i",
            "a",
            "about",
            "an",
            "are",
            "as",
            "at",
            "be",
            "by",
            "com",
            "for",
            "from",
            "how",
            "in",
            "is",
            "it",
            "of",
            "on",
            "or",
            "that",
            "the",
            "this",
            "to",
            "was",
            "what",
            "when",
            "where",
            "who",
            "will",
            "with",
            "the",
            "www"
        };
    }
}
