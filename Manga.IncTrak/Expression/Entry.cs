using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Manga.IncTrak.Expression
{
    /* symbol table entry */
    public class Entry
    {
        public static Entry[] KeyWords = new Entry[] {
            new Entry("OR", ExpressionConstants.OR),
            new Entry("AND", ExpressionConstants.AND),
            new Entry("NOT", ExpressionConstants.NOT),
        };

        public string Lexan
        {
            get; private set;
        }

        public ExpressionConstants Token
        {
            get; private set;
        }

        public Entry(string lexan, ExpressionConstants token)
        {
            Lexan = lexan;
            Token = token;
        }
    }
}
