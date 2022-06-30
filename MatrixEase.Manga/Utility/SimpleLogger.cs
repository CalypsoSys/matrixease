using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MatrixEase.Manga.Utility
{
    public static class SimpleLogger
    {
        public static void LogError(Exception excp, string message, params object[] args)
        {
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
                string excption = "N/A";
                try
                {
                    StringBuilder output = new StringBuilder();
                    for (int i = 2; excp != null; i++)
                    {
                        string indent = string.Empty.PadLeft(i, '\t');
                        output.AppendFormat("{0}Exception: {1}\r\n", indent, excp.Message);
                        output.AppendFormat("{0}Stack Trace: {1}\r\n", indent, excp.StackTrace.Replace("\r\n", string.Format("\r\n{0}", indent)));
                        excp = excp.InnerException;
                    }
                    output.AppendLine();
                    excption = output.ToString();
                }
                catch
                {
                    if (excp != null)
                        excption = excp.ToString();
                }

                using(StreamWriter loggie = new StreamWriter(MiscHelpers.GetLogFileFile("matrixease_managa.log"), true) )
                {
                    loggie.WriteLine("{0}: {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), messageOut);
                    loggie.WriteLine(excption);
                }
            }
            catch //(Exception excp1)
            {
                // do no harm
            }
        }

        private static object _sync = new object();
        private static DateTime _lastDate = DateTime.MinValue.Date;
        private static string _logFilePath = null;

        /// <summary>
        /// log to a text file located in the sub-directory "logs" where the executing assembly is running
        /// </summary>
        /// <param name="excp"></param>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void Log(Exception excp, string source, string message, params object[] args)
        {
            try
            {
                lock (_sync)
                {
                    Initialize();
                    using (StreamWriter writer = new StreamWriter(_logFilePath, true))
                    {
                        writer.WriteLine("{0}\t{1}\t{2}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                                source, string.Format(message, args));

                        // start at 2, since we want to start at the 3rd position
                        for (int i = 2; excp != null; i++)
                        {
                            string indent = string.Empty.PadLeft(i, '\t');
                            writer.WriteLine("{0}Exception: {1}", indent, excp.Message);
                            writer.WriteLine("{0}Stack Trace: {1}", indent, excp.StackTrace.Replace("\r\n", string.Format("\r\n{0}", indent)));
                            excp = excp.InnerException;
                        }
                    }
                }
            }
            catch
            {
                // eat any exception as we do not want to do any harm when logging
            }
        }

        /// <summary>
        /// log to a text file located in the sub-directory "log" where the service is running
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void Log(string source, string message, params object[] args)
        {
            Log(null, source, message, args);
        }

        /// <summary>
        /// Wraps other logging functions to conform to existing signature
        /// </summary>
        /// <param name="excp"></param>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void Error(string message, Exception excp, params object[] args)
        {
            Log(excp, null, message, args);
        }

        /// <summary>
        /// Wraps other logging functions to conform to existing signature
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void Info(string message, params object[] args)
        {
            Log(null, message, args);
        }

        /// <summary>
        /// initialize the the log file location as the assembly location/logs/assembly name.log
        /// </summary>
        private static void Initialize()
        {
            if (_lastDate != DateTime.Now.Date)
            {

                string logDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "logs");
                string fileName = Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location);
                Directory.CreateDirectory(logDirectory);

                _lastDate = DateTime.Now.Date;
                _logFilePath = string.Format("{0}.{1}.log", Path.Combine(logDirectory, fileName), _lastDate.ToString("yyyyMMdd"));

                DateTime deleteFileBefore = _lastDate.AddDays(7 * -1);
                foreach (string file in Directory.GetFiles(logDirectory, string.Format("{0}.*.log", fileName)))
                {
                    try
                    {
                        string fileDate = Path.GetExtension(Path.GetFileNameWithoutExtension(file));
                        DateTime date = DateTime.ParseExact(fileDate, ".yyyyMMdd", CultureInfo.InstalledUICulture);
                        if (date < deleteFileBefore)
                            File.Delete(file);
                    }
                    catch (Exception excp)
                    {
                        // in case there are strangly named files in the directory
                        Log(excp, "Exception checking log file {0}", file);
                    }
                }
            }
        }
    }
}
