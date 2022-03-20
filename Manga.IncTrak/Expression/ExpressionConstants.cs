using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MatrixEase.Manga.Expression
{
    public enum ExpressionConstants
    {
        NONE = -1,

        ID = 200,
        VAR = 202,

        BOOLEAN = 450,
        AND = 451,
        OR = 452,
        NOT = 453,

        LEFT_PAREN = 500,
        RIGHT_PAREN = 501,

        DONE = 1000,
    }
}
