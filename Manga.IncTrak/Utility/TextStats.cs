//#define REGEX_TERM_PARSER
#define JOE_TERM_PARSER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Manga.IncTrak.Utility
{
    public static class TextStats
    {
        enum UrlState { CheckScheme, CheckDNS, CheckPort, CheckQuery };
        private const int MaxTermSize = 25;
        public const int DNSOffset = 3;
        private const int MaxSchemeLength = 64;
        private const int MaxDNSLength = 256;
        private const int MaxPart1Length = 256;
        private const int partGoodPct = 40;

#if REGEX_TERM_PARSER
        public static string[] SplitTerms(string data)
        {
            return Regex.Split(data.ToLower(), "[^A-Za-z]", RegexOptions.Compiled | RegexOptions.Singleline).Where(s => s.Length > 2).ToArray();
        }
#elif JOE_TERM_PARSER
        public static List<string> SplitTerms(string data)
        {
            List<string> output = new List<string>();
            char[] temp = new char[MaxTermSize + 1];
            int pos = 0;
            foreach (char d in data)
            {
                char c = char.ToLower(d);
                if (c >= 'a' && c <= 'z')
                {
                    if (pos < MaxTermSize)
                    {
                        temp[pos] = c;
                        ++pos;
                        temp[pos] = '\0';
                    }
                }
                else
                {
                    if (pos > 2 && pos < MaxTermSize)
                        output.Add(new string(temp, 0, pos));
                    pos = 0;
                }
            }

            if (pos > 2 && pos < MaxTermSize)
                output.Add(new string(temp, 0, pos));

            return output;
        }
#endif

        private static bool IsValidSchemeorDns(char c, bool scheme)
        {
            return char.IsLetterOrDigit(c) || c == '-' || c == '.' || (scheme && c == '+');
        }

        public static bool IsUrl(string data)
        {
            int schemaIndex, dnsIndex, portIndex;
            return IsUrl(data, out schemaIndex, out dnsIndex, out portIndex, null);
        }

        public static bool IsUrl(string data, out int schemaIndex, out int dnsIndex, out int portIndex, int[] queryIndexes)
        {
            UrlState state = UrlState.CheckScheme;
            schemaIndex = -1;
            dnsIndex = -1;
            portIndex = -1;
            if (queryIndexes != null)
                queryIndexes[0] = 0;
            if (data.Length > 3)
            {
                //xx://a.a
                // after 8 failure
                int mod = 0;
                if (data[0] == '"' && data[data.Length - 1] == '"')
                    mod = 1;
                int stop = Math.Min(data.Length-mod, MaxSchemeLength + MaxDNSLength + MaxPart1Length);
                //int[] queryIndexes = new int[32];
                int queryIndex = 0;
                int finalStop = (stop - 1);
                for (int i = mod; i < stop && queryIndex != -1; i++)
                {
                    if (state == UrlState.CheckScheme)
                    {
                        if (i > MaxSchemeLength)
                            return false;
                        else if (!IsValidSchemeorDns(data[i], true))
                        {
                            if (data[i] == ':' && (i + 2) < data.Length && data[i + 1] == '/' && data[i + 2] == '/')
                            {
                                schemaIndex = i;
                                state = UrlState.CheckDNS;
                                i += 2;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    else if (state == UrlState.CheckDNS)
                    {
                        if ((i - schemaIndex) > MaxDNSLength)
                            return false;
                        else if (!IsValidSchemeorDns(data[i], true) || i == finalStop)
                        {
                            if (data[i] == '/' || data[i] == '?' || data[i] == '#' || data[i] == ':' || i == finalStop)
                            {
                                if (data[i] == ':')
                                    state = UrlState.CheckPort;
                                else
                                    state = UrlState.CheckQuery;
                                dnsIndex = i + (i == finalStop ? 1 : 0); ;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    else if (state == UrlState.CheckPort)
                    {
                        if (!char.IsDigit(data[i]) || i == finalStop)
                        {
                            if (data[i] == '/' || data[i] == '?' || data[i] == '#' || i == finalStop)
                            {
                                state = UrlState.CheckQuery;
                                portIndex = i + (i == finalStop ? 1 : 0); ;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    else if (state == UrlState.CheckQuery)
                    {
                        if (data[i] == '/' || data[i] == '?' || data[i] == '&' || i == finalStop)
                        {
                            if (queryIndexes == null)
                            {
                                queryIndex = -1;
                            }
                            else
                            {
                                queryIndexes[queryIndex] = i + (i == finalStop ? 1 : 0);
                                if (queryIndex == queryIndexes.Length - 1)
                                    queryIndex = -1;
                                else
                                {
                                    ++queryIndex;
                                    queryIndexes[queryIndex] = 0;
                                }
                            }
                        }
                    }
                }
            }

            return (state == UrlState.CheckQuery);
        }

        private static bool CheckPathPart(string part)
        {
            int goodParts = 0;
            foreach(char d in part)
            {
                char c = char.ToLower(d);
                if (c >= 'g' && c <= 'z')
                {
                    ++goodParts;
                }
            }

            return ((goodParts * 100) / part.Length) > partGoodPct;
        }

        public static HashSet<string> SplitUrlPath(string data)
        {
            HashSet<string> output = new HashSet<string>();
            int pos = 0;
            int i = 0;
            for (; i < data.Length; i++)
            {
                if (data[i] == '/' || data[i] == '?' || data[i] == '&')
                {
                    if( pos < i)
                    {
                        string part = data.Substring(pos+1, i-(pos+1));
                        if (CheckPathPart(part))
                        {
                            output.Add(part);
                        }
                    }
                    pos = i;
                }
            }

            if (pos < i)
            {
                string part = data.Substring(pos+1, i - (pos + 1));
                if (CheckPathPart(part))
                {
                    output.Add(part);
                }
            }

            return output;
        }

        private static void Test(string url)
        {
            int[] queryIndexes = new int[32];
            int schemaIndex, dnsIndex, portIndex;
            if (IsUrl(url, out schemaIndex, out dnsIndex, out portIndex, queryIndexes))
            {
                var scheme = url.Substring(0, schemaIndex);
                int start = schemaIndex + DNSOffset;
                var dns = url.Substring(start, dnsIndex - start);

                start = dnsIndex + 1;
                if (portIndex != -1)
                {
                    var port = url.Substring(start, portIndex - start);

                    start = portIndex + 1;
                }
                for (int i = 0; i < queryIndexes.Length && queryIndexes[i] != 0; i++)
                {
                    var part = url.Substring(start, queryIndexes[i] - start);
                    start = queryIndexes[i] + 1;
                }
            }
        }
    }
}
