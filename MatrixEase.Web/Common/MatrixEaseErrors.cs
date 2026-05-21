using System;
using System.Text;

namespace MatrixEase.Web.Common
{
    internal static class MatrixEaseErrors
    {
        public static string LogError(AppSettings settings, Exception excp, string message, params object[] args)
        {
            string errorCode = DateTime.UtcNow.Ticks.ToString("x");
            try
            {
                string messageOut = "N/A";
                try
                {
                    messageOut = string.Format(message, args);
                }
                catch
                {
                    if (string.IsNullOrWhiteSpace(message) == false)
                        messageOut = message;
                }

                StringBuilder output = new StringBuilder();
                output.AppendFormat("[{0:yyyy-MM-dd HH:mm:ss zzz}] code={1} message={2}\n", DateTimeOffset.Now, errorCode, FileLogWriter.SanitizeSingleLine(messageOut));

                for (int i = 0; excp != null; i++)
                {
                    string prefix = i == 0 ? "exception" : string.Format("inner_exception_{0}", i);
                    output.AppendFormat("\t{0}: {1}\n", prefix, FileLogWriter.SanitizeSingleLine(excp.Message));
                    AppendIndentedBlock(output, excp.StackTrace ?? "(no stack trace)", "\t\t");
                    excp = excp.InnerException;
                }

                output.AppendLine();
                FileLogWriter.WriteLine(settings?.GetErrorLogPath(), output.ToString().TrimEnd('\r', '\n'));
            }
            catch
            {
                // do no harm
            }

            return string.Format("Unknown error occurred code: [{0}], try again?", errorCode);
        }

        private static void AppendIndentedBlock(StringBuilder output, string value, string indent)
        {
            string[] lines = (value ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            foreach (string line in lines)
            {
                output.Append(indent);
                output.AppendLine(string.IsNullOrWhiteSpace(line) ? "(blank)" : line.TrimEnd());
            }
        }
    }
}
