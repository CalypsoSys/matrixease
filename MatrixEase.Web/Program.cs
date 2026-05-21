using System;
using System.IO;
using MatrixEase.Web.Common;
using MatrixEase.Manga.Utility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace MatrixEase.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception excp)
            {
                SimpleLogger.LogError(excp, "Error loading anything");
                TryLogStartupException(excp);
                Console.WriteLine(excp);
            }
        }

        private static void TryLogStartupException(Exception excp)
        {
            string path = Environment.GetEnvironmentVariable("MatrixEase__Web__ErrorLogPath");
            if (string.IsNullOrWhiteSpace(path))
                path = Path.Combine("logs", "errors.log");

            string message = string.Format("[{0:yyyy-MM-dd HH:mm:ss zzz}] startup_exception\n{1}", DateTimeOffset.Now, excp);
            FileLogWriter.WriteLine(path, message.TrimEnd('\r', '\n'));
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
