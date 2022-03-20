//#define VB_TEXT_PARSER
//#define REGEX_TERM_PARSER
#define JOE_CVS_PARSER
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatrixEase.Manga.Utility
{
    public class CsvParser : MyDisposable
    {
#if JOE_CVS_PARSER
        private const int ReadBufferSize = 102400;
        private StreamReader _input;
        private long _fileSize;
        private long _filePos = 0;
        private char _csvSeparator = ',';
        private char _csvQuote = '"';
        private char _csvEscape = '"';
        private char _csvNull = '\0';
        private char _csvEOL1 = '\r';
        private char _csvEOL2 = '\n';

#elif VB_TEXT_PARSER
        private TextFieldParser _parser;
#endif

        public CsvParser(StreamReader input, char csvSeparator, char csvQuote, char csvEscape, char csvNull, char csvEOL1, char csvEOL2)
        {
#if JOE_CVS_PARSER
            _input = input;
            _fileSize = input.BaseStream.Length;
            _csvSeparator = csvSeparator;
            _csvQuote = csvQuote;
            _csvEscape = csvEscape;
            _csvNull = csvNull;
            _csvEOL1 = csvEOL1;
            _csvEOL2 = csvEOL2;
#elif VB_TEXT_PARSER
            _parser = new TextFieldParser(input);
            _parser.Delimiters = new string[] { "," };
            _parser.HasFieldsEnclosedInQuotes = true;
#endif
        }

        public long PctRead
        {
            get
            {
                return _filePos == 0 ? 0 : ((_filePos * 100) / _fileSize);
            }
        }

        protected override void DisposeManaged()
        {
#if JOE_CVS_PARSER
#elif VB_TEXT_PARSER
            if (_parser != null)
            {
                _parser.Close();
            }
#endif
        }

        protected override void DisposeUnManaged()
        {
        }

#if JOE_CVS_PARSER
        private List<object> SplitLine(char[] buffer, int readSize, ref int pos, out bool eol, out bool eob)
        {
            List<object> output = new List<object>();
            StringBuilder temp = new StringBuilder();
            eol = false;
            eob = false;
            bool inQuote = false;
            for (; pos < readSize && eol == false; pos++)
            {
                char c = buffer[pos];
                if ((c == _csvSeparator || c == _csvEOL1 || c == _csvEOL2) && inQuote == false)
                {
                    output.Add(temp.ToString());
                    temp.Clear();
                    if (c == _csvEOL1 && pos < readSize - 1 && buffer[pos + 1] == _csvEOL2)
                    {
                        ++pos;
                        eol = true;
                    }
                    else if (c == _csvEOL2)
                    {
                        eol = true;
                    }
                }
                else
                {
                    if (c == _csvQuote)
                    {
                        if (inQuote)
                        {
                            if ((pos + 1) < readSize && buffer[pos + 1] == _csvEscape)
                            {
                                temp.Append(c);
                                ++pos;
                            }
                            else
                            {
                                inQuote = false;
                            }
                        }
                        else
                        {
                            inQuote = true;
                        }
                    }
                    else
                    {
                        temp.Append(c);
                    }
                }
            }

            if (temp.Length > 0)
            {
                output.Add(temp.ToString());
            }

            if (!eol)
                eob = true;

            return output;
        }

        public IEnumerable<List<object>> ReadParseLine()
        {
            int i = 0;
            char[] buffer = new char[ReadBufferSize];
            bool readMore = true;
            int lastPos = ReadBufferSize;
            while (readMore)
            {
                int offset = ReadBufferSize - lastPos;
                Array.Copy(buffer, lastPos, buffer, 0, offset);
                int read = _input.ReadBlock(buffer, offset, lastPos);
                _filePos += read;

                int bufSize;
                if (read == 0 || read < lastPos)
                {
                    readMore = false;
                    bufSize = offset + read;
                }
                else
                {
                    bufSize = ReadBufferSize;
                }

                bool eob = false;
                int pos = 0;
                while (eob == false)
                {
                    bool eol;
                    List<object> row = SplitLine(buffer, bufSize, ref pos, out eol, out eob);
                    if (eol || (eob == true && readMore == false && row != null && row.Count != 0))
                    {
                        lastPos = pos;

                        //SplitTerms(data);

                        yield return row;

                        ++i;
                    }
                }
            }
        }
#elif VB_TEXT_PARSER
        public IEnumerable<IList<object>> ReadParseLine()
        {
            string[] row;
            while ((row = _parser.ReadFields()) != null)
            {
                yield return row;
            }
        }
#endif
    }
}
