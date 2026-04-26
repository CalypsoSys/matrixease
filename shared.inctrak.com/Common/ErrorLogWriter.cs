using System;
using System.Text;

namespace MatrixEase.Web.Common
{
    internal static class ErrorLogWriter
    {
        /// <summary>
        /// Writes an application error to the configured error log and returns the generated error code.
        /// </summary>
        public static string LogError(AppSettings settings, Exception excp, string message, params object[] args)
        {
            string errorCode = Guid.NewGuid().ToString("N").Substring(0, 8);

            try
            {
                FileLogWriter.WriteLine(
                    settings?.ErrorLogPath,
                    BuildErrorEntry(errorCode, excp, message, args));
            }
            catch
            {
                // do no harm
            }

            return errorCode;
        }

        /// <summary>
        /// Writes startup failures to the configured error log path when the host fails before ASP.NET begins serving.
        /// </summary>
        public static void TryLogStartupException(Exception excp, string path)
        {
            try
            {
                string resolvedPath = string.IsNullOrWhiteSpace(path) ? "logs/errors.log" : path;
                FileLogWriter.WriteLine(resolvedPath, BuildStartupExceptionEntry(excp));
            }
            catch
            {
                // do no harm
            }
        }

        internal static string BuildErrorEntry(string errorCode, Exception excp, string message, params object[] args)
        {
            StringBuilder output = new StringBuilder();
            output.AppendFormat(
                "[{0:yyyy-MM-dd HH:mm:ss zzz}] code={1} message={2}\n",
                DateTimeOffset.Now,
                FileLogWriter.SanitizeSingleLine(errorCode),
                FileLogWriter.SanitizeSingleLine(FormatMessage(message, args)));

            for (int i = 0; excp != null; i++)
            {
                string prefix = i == 0 ? "exception" : string.Format("inner_exception_{0}", i);
                output.AppendFormat("\t{0}: {1}\n", prefix, FileLogWriter.SanitizeSingleLine(excp.Message));
                AppendIndentedBlock(output, excp.StackTrace ?? "(no stack trace)", "\t\t");
                excp = excp.InnerException;
            }

            return output.ToString().TrimEnd('\r', '\n');
        }

        internal static string BuildStartupExceptionEntry(Exception excp)
        {
            return string.Format(
                "[{0:yyyy-MM-dd HH:mm:ss zzz}] startup_exception\n{1}",
                DateTimeOffset.Now,
                (excp ?? new Exception("(null exception)")).ToString()).TrimEnd('\r', '\n');
        }

        /// <summary>
        /// Writes a multi-line block with a consistent leading indent on every line.
        /// </summary>
        private static void AppendIndentedBlock(StringBuilder output, string value, string indent)
        {
            string[] lines = (value ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            foreach (string line in lines)
            {
                output.Append(indent);
                output.AppendLine(string.IsNullOrWhiteSpace(line) ? "(blank)" : line.TrimEnd());
            }
        }

        private static string FormatMessage(string message, object[] args)
        {
            try
            {
                return string.Format(message, args);
            }
            catch
            {
                return string.IsNullOrWhiteSpace(message) ? "N/A" : message;
            }
        }
    }
}
