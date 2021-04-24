using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Manga.IncTrak.Expression
{
    public class Symbol
    {
        public ExpressionConstants ExprType { get; private set; }
        public ExpressionConstants ExprSubType { get; private set; }
        public string Name { get; private set; }
        public Symbol Next { get; private set; }    // link to next

        public static Symbol PutSymbol(string name, ExpressionConstants type, ExpressionConstants subType, Symbol symbol)
        {
            if (symbol == null)
            {
                symbol = new Symbol();
                switch (symbol.ExprType = type)
                {
                    case ExpressionConstants.BOOLEAN:
                        symbol.ExprSubType = subType;
                        break;

                    case ExpressionConstants.ID:
                        switch (symbol.ExprSubType = subType)
                        {
                            case ExpressionConstants.VAR:
                                symbol.Name = name;
                                break;

                            default:
                                break;
                        }
                        break;

                    case ExpressionConstants.DONE:
                    default:
                        break;
                }
            }
            else
                symbol.Next = PutSymbol(name, type, subType, symbol.Next);

            return symbol;
        }
    }
}
