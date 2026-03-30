using ElectronNET.API;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using ElectronNET.API.Entities;
using MatrixEase.Manga.Manga.Serialization;

namespace Desktop.MatrixEase.Manga
{
    public class Program
    {
        private static readonly Dictionary<string, object> _data = new Dictionary<string, object>();

        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("windows")]
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("windows")]
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseElectron(args, ElectronAppReady);
                    webBuilder.UseStartup<Startup>();
                });

        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("windows")]
        public static async Task ElectronAppReady()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WSL_DISTRO_NAME")))
            {
                Electron.App.CommandLine.AppendArgument("--disable-gpu");
                Electron.App.CommandLine.AppendArgument("--disable-software-rasterizer");
            }

            string userPath = await Electron.App.GetPathAsync(PathName.AppData);
            MangaRoot.SetRootFolder(userPath);

            var browserWindow = await Electron.WindowManager.CreateWindowAsync(new BrowserWindowOptions
            {
                Width = 1152,
                Height = 940,
                Show = false,
                Icon = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets\\icon.ico"),
                WebPreferences = new WebPreferences
                {
                    EnableRemoteModule = true,
                    NodeIntegration = true,
                }
            });

            await browserWindow.WebContents.Session.ClearCacheAsync();

            browserWindow.OnReadyToShow += () => browserWindow.Show();
            browserWindow.SetTitle("MatrixEase");
#if !DEBUG_JAVASCRIPT
            browserWindow.RemoveMenu();
#endif

            await Electron.IpcMain.On("open_window", args =>
            {
                var jsonArgs = args as Newtonsoft.Json.Linq.JArray;
                _ = Electron.WindowManager.CreateWindowAsync(new BrowserWindowOptions
                {
                    AutoHideMenuBar = true,
                    Icon = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets\\icon.ico"),
                    WebPreferences = new WebPreferences
                    {
                        EnableRemoteModule = true,
                        NodeIntegration = true,
                    }
                }, jsonArgs[0].ToString());

                _data.Add(jsonArgs[1].ToString(), jsonArgs[2]);
            });

            await Electron.IpcMain.On("opened_window", args =>
            {
                string id = args as string;
                if (_data.ContainsKey(id))
                {
                    foreach (var win in Electron.WindowManager.BrowserWindows)
                    {
                        Electron.IpcMain.Send(win, "send_data", _data[id]);
                    }
                    _data.Remove(id);
                }
            });
        }
    }
}
