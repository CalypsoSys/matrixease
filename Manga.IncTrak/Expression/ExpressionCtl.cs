
using Manga.IncTrak.Manga;
using Manga.IncTrak.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Manga.IncTrak.Expression
{
    public class ExpressionCtl
    {
        /* symbol table itself */
        private List<Entry> _symTable = new List<Entry>();
        private Entry _lastEntry = null;
        private string _expresion;
        private short _position = 0;
        private ExpressionConstants _lookAhead;
        private Symbol _first;
        private List<string> _stack = new List<string>();
        private List<string> _errors = new List<string>();

        public ExpressionCtl(string buffer)
        {
            _expresion = buffer;
            _position = 0;

            Init();
            Parse();
            ValidateExpression(_first, null, null);
        }

        public string[] Stack
        {
            get { return _stack.ToArray(); }
        }

        public string[] Errors
        {
            get { return _errors.ToArray(); }
        }

        /* loads keywords into symtable */
        private void Init()
        {
            foreach (Entry pEntry in Entry.KeyWords)
            {
                Insert(pEntry.Lexan, pEntry.Token);
            }
        }

        /* returns position of entry for s */
        private ExpressionConstants LookUp(string lexan)
        {
            foreach (var symbol in _symTable)
            {
                if (symbol.Lexan == lexan)
                {
                    _lastEntry = symbol;
                    return symbol.Token;
                }
            }

            return ExpressionConstants.NONE;
        }

        /* returns position of entry for s */
        private ExpressionConstants Insert(string lexan, ExpressionConstants token)
        {
            _lastEntry = new Entry(lexan, token);
            _symTable.Add(_lastEntry);
            return token;
        }

        private void Error(string message)
        {
            _errors.Add(string.Format("{0}:{1}", message, _position));
        }

        /* parse & translate expression list */
        private void Parse()
        {
            _lookAhead = Lexan();

            while (_lookAhead != ExpressionConstants.DONE && _errors.Count == 0)
            {
                Booly();
                _first = Symbol.PutSymbol(null, ExpressionConstants.DONE, 0, _first);
            }
        }

        /* deal with expression */
        private void Booly()
        {
            Factor();

            while (true && _errors.Count == 0)
            {
                switch (_lookAhead)
                {
                    /* boolean operators */
                    case ExpressionConstants.AND:
                    case ExpressionConstants.OR:
                        ExpressionConstants sT = _lookAhead;
                        Match(_lookAhead);
                        Factor();
                        Emit(sT);
                        break;

                    default:
                        return;
                }
            }
        }

        private void Factor()
        {
            switch (_lookAhead)
            {
                case ExpressionConstants.NOT:
                    Match(ExpressionConstants.NOT);
                    Booly();
                    Emit(ExpressionConstants.NOT);
                    break;

                case ExpressionConstants.LEFT_PAREN:
                    Match(ExpressionConstants.LEFT_PAREN);
                    Booly();
                    Match(ExpressionConstants.RIGHT_PAREN);
                    break;

                case ExpressionConstants.ID:
                    Emit(_lookAhead);
                    Match(_lookAhead);
                    break;

                case ExpressionConstants.DONE:
                    return;

                default:
                    Error(string.Format("Factor: syntax error: {0}", _lookAhead));
                    break;
            }
        }

        private void Match(ExpressionConstants ec)
        {
            if (_lookAhead == ec)
                _lookAhead = Lexan();
            else
                Error(string.Format("Match: syntax error: {0} {1}", _lookAhead, ec));
        }

        private ExpressionConstants GetString(char nextChar)
        {
            char quote = nextChar;
            StringBuilder buffer = new StringBuilder();
            for (nextChar = GetChar(); nextChar != quote; nextChar = GetChar())
            {
                buffer.Append(nextChar);
            }

            ExpressionConstants ec = LookUp(buffer.ToString());
            if (ec == ExpressionConstants.NONE)
            {
                ec = Insert(buffer.ToString(), ExpressionConstants.ID);
            }
            return ec;
        }

        private ExpressionConstants GetToken(char nextChar)
        {
            StringBuilder buffer = new StringBuilder();
            while (char.IsLetterOrDigit(nextChar) || nextChar == ':' || nextChar == '@')
            {
                buffer.Append(nextChar);
                nextChar = GetChar();
            }

            if (nextChar != char.MinValue)
                UnGetC();

            ExpressionConstants ec = LookUp(buffer.ToString());
            if (ec == ExpressionConstants.NONE)
            {
                ec = Insert(buffer.ToString(), ExpressionConstants.ID);
            }

            return ec;
        }

        /* lexical analyser */
        private ExpressionConstants Lexan()
        {
            while (true && _errors.Count == 0)
            {
                char nextChar = GetChar();

                if (nextChar == char.MinValue)
                {
                    return ExpressionConstants.DONE;
                }
                else if (!char.IsWhiteSpace(nextChar))    // ignore whitespace
                {
                    if (nextChar == '\'' || nextChar == '"')
                    {
                        return GetString(nextChar);
                    }
                    else if (char.IsLetterOrDigit(nextChar))
                    {
                        return GetToken(nextChar);
                    }
                    else if (nextChar == '(')
                    {
                        return ExpressionConstants.LEFT_PAREN;
                    }
                    else if (nextChar == ')')
                    {
                        return ExpressionConstants.RIGHT_PAREN;
                    }
                    else
                    {
                        return ExpressionConstants.NONE;
                    }
                }
            }

            return ExpressionConstants.DONE;
        }

        private char GetChar()
        {
            char nextChar = char.MinValue;
            if (_position < _expresion.Length)
            {
                nextChar = _expresion[_position];
                _position++;
            }

            return nextChar;
        }

        private void UnGetC()
        {
            _position--;
        }

        /* generate output */
        private void Emit(ExpressionConstants ec)
        {
            switch (ec)
            {
                /* boolean operators */
                case ExpressionConstants.AND:
                case ExpressionConstants.OR:
                case ExpressionConstants.NOT:
                    _first = Symbol.PutSymbol(null, ExpressionConstants.BOOLEAN, ec, _first);
                    break;

                case ExpressionConstants.ID:
                    _first = Symbol.PutSymbol(_lastEntry.Lexan, ExpressionConstants.ID, ExpressionConstants.VAR, _first);
                    break;

                default:
                    Error(string.Format("Emit: unexpected token: {0}", ec));
                    break;
            }
        }

        private void ValidateExpression(Symbol symbol, DataManga manga, Stack<MyBitArray> bits)
        {
            if (symbol != null)
            {
                switch (symbol.ExprType)
                {
                    case ExpressionConstants.BOOLEAN:
                        switch (symbol.ExprSubType)
                        {
                            case ExpressionConstants.AND:
                                _stack.Add("AND");
                                if (manga != null)
                                {
                                    var bitmap1 = bits.Pop();
                                    var bitmap2 = bits.Pop();
                                    bits.Push(bitmap1.And(bitmap2));
                                }
                                break;

                            case ExpressionConstants.OR:
                                _stack.Add("OR");
                                if (manga != null)
                                {
                                    var bitmap1 = bits.Pop();
                                    var bitmap2 = bits.Pop();
                                    bits.Push(bitmap1.Or(bitmap2));
                                }
                                break;

                            case ExpressionConstants.NOT:
                                _stack.Add("NOT");
                                if (manga != null)
                                {
                                    var bitmap = bits.Pop();
                                    bits.Push(bitmap.Not());
                                }
                                break;

                            default:
                                Error(string.Format("PrintLink: Error: {0} {1}", symbol.ExprType, symbol.ExprSubType));
                                break;
                        }
                        break;

                    case ExpressionConstants.ID:
                        switch (symbol.ExprSubType)
                        {
                            case ExpressionConstants.VAR:
                                _stack.Add(symbol.Name);
                                if ( manga != null)
                                {
                                    var bitmap = manga.GetBitmap(symbol.Name);
                                    if (bitmap == null)
                                    {
                                        Error(string.Format("PrintLink: Error: {0}", symbol.Name));
                                    }
                                    else
                                    { 
                                        bits.Push(bitmap.Clone() as MyBitArray);
                                    }
                                }
                                break;

                            default:
                                Error(string.Format("PrintLink: Error: {0} {1}", symbol.ExprType, symbol.ExprSubType));
                                break;
                        }
                        break;

                    case ExpressionConstants.DONE:
                        _stack.Add("DONE");
                        break;

                    default:
                        Error(string.Format("PrintLink: Error: {0} {1}", symbol.ExprType, symbol.ExprSubType));
                        break;
                }
                ValidateExpression(symbol.Next, manga, bits);
            }
        }

        public MyBitArray ProcessBitmaps(DataManga manga)
        {
            Stack<MyBitArray> bits = new Stack<MyBitArray>();
            ValidateExpression(_first, manga, bits);
            return bits.Pop();
        }
    }
}
