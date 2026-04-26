using System;
using System.IO;
using System.Text;

namespace MatrixEase.Web.Common
{
    internal static class FileLogWriter
    {
        private static readonly object _sync = new object();

        public static void WriteLine(string path, string line)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            try
            {
                string fullPath = Path.GetFullPath(path);
                string directory = Path.GetDirectoryName(fullPath);
                if (string.IsNullOrWhiteSpace(directory) == false)
                {
                    Directory.CreateDirectory(directory);
                }

                lock (_sync)
                {
                    File.AppendAllText(fullPath, line + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch (Exception)
            {
                // Do no harm in the request pipeline.
            }
        }

        public static string SanitizeSingleLine(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "-";
            }

            return value.Replace("\r", " ").Replace("\n", " ").Trim();
        }
    }
}
