using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Manga.IncTrak.Utility
{
    public static class StringExtensions
    {
        public static string SafeSubString(this string input, int length)
        {
            string data = Encoding.ASCII.GetString(
                               Encoding.Convert(
                                   Encoding.UTF8,
                                   Encoding.GetEncoding(
                                       Encoding.ASCII.EncodingName,
                                       new EncoderReplacementFallback(" "),
                                       new DecoderExceptionFallback()
                                       ),
                                   Encoding.UTF8.GetBytes(input.ToLower())
                               ));
            if (length > data.Length)
                return data;
            else
                return data.Substring(0, length);
        }
    }
}
