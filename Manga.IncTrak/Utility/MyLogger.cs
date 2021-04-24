using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Manga.IncTrak.Utility
{
    public static class MyLogger
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

                using(StreamWriter loggie = new StreamWriter(MiscHelpers.GetPendingFile("inctrak_managa.log")) )
                {
                    loggie.WriteLine("{0}: {1}", DateTime.Now.ToString("yyy-MM-dd HH:mm:ss"), messageOut);
                }
            }
            catch //(Exception excp1)
            {
                // do no harm
            }
        }
    }
}
