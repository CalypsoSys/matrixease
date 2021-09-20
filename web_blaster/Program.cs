using System;
using System.Diagnostics;
using System.IO;

namespace web_blaster
{
    class Program
    {
        private enum Uglifytype { uglifycss, uglifyjs };

        static void Main(string[] args)
        {
            DeleteFiles(args[3]);
            CopyFiles(args[0], args[1] != "Debug", args[2], args[3]);
        }

        private static void DeleteFiles(string dest)
        {
            DirectoryInfo di = new DirectoryInfo(dest);
            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                if (dir.Name != "javascript")
                {
                    string subDest = Path.Combine(dest, dir.Name);
                    DeleteFiles(subDest);
                    Directory.Delete(subDest);
                }
            }
        }

        private static void CopyFiles(string os, bool release, string source, string dest)
        {
            DirectoryInfo di = new DirectoryInfo(source);
            foreach (FileInfo file in di.GetFiles())
            {
                var destination = Path.Combine(dest, file.Name);
                if (release)
                {
                    ProcessFile(os, file, destination);
                }
                else
                {
                    file.CopyTo(destination, true);
                }
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                string subDest = Path.Combine(dest, dir.Name);
                Directory.CreateDirectory(subDest);
                CopyFiles(os, release, dir.FullName, subDest);
            }
        }

        private static void ProcessFile(string os, FileInfo source, string destination)
        {
            switch (source.Extension)
            {
                case ".html":
                    UseMinifiedInHTML(source.FullName, destination);
                    break;
                case ".js":
                    if (source.Name == "vis_helper.js")
                    {
                        ClearHelpersJS(source.FullName, destination);
                    }
                    else
                    {
                        UglifyFile(os, Uglifytype.uglifyjs, source.FullName, destination);
                    }
                    break;
                case ".css":
                    UglifyFile(os, Uglifytype.uglifycss, source.FullName, destination);
                    break;
                case ".png":
                case ".svg":
                case ".gif":
                case ".md":
                    source.CopyTo(destination, true);
                    break;
                default:
                    if (source.Extension != string.Empty || source.Name != "LICENSE")
                        throw new Exception("Unknown File Type");
                    break;
            }
        }

        private static void UglifyFile(string os, Uglifytype ugg, string source, string destination)
        {
            Process process = new Process();
            if (os == "Windows_NT")
            {
                process.StartInfo.FileName = string.Format("{0}.cmd", ugg);
            }
            else
            {
                process.StartInfo.FileName = string.Format("{0}", ugg);
            }
            string parameters = string.Empty;
            if (ugg == Uglifytype.uglifyjs)
            {
                destination = destination.Replace(".js", ".min.js");
                parameters = " --compress --mangle ";
            }
            else
            {
                destination = destination.Replace(".css", ".min.css");
            }
            process.StartInfo.Arguments = string.Format("{0} {1} --output {2}", source, parameters, destination);
            process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            process.Start();
            process.WaitForExit();// Waits here for the process to exit.
        }

        private static void UseMinifiedInHTML(string source, string destination)
        {
            using(StreamReader input = new StreamReader(source))
            {
                using (StreamWriter output = new StreamWriter(destination))
                {
                    string line;
                    while ((line = input.ReadLine()) != null)
                    {
                        if (line.Contains("<script") && line.Contains(".js\"") && !line.Contains("overrides.js") && !line.Contains("vis_helper.js") && !line.Contains("vue2-slideout-panel.min.js") && !line.Contains("vue-cookies.js"))
                        {
                            line = line.Replace(".js\"", ".min.js\"");
                            if (line.Contains(".development."))
                            {
                                line = line.Replace(".development.", ".production.");
                            }
                        }
                        else if (line.Contains("<link") && line.Contains(".css\""))
                        {
                            line = line.Replace(".css\"", ".min.css\"");
                        }

                        output.WriteLine(line);
                    }
                }
            }
        }

        private static void ClearHelpersJS(string source, string destination)
        {
            using (StreamReader input = new StreamReader(source))
            {
                using (StreamWriter output = new StreamWriter(destination))
                {
                    string line;
                    while ((line = input.ReadLine()) != null)
                    {
                        if (line.Contains("108thHouse"))
                        {
                            line = line.Replace("108thHouse", "");
                        }
                        else if (line.Contains("1VunkEZX3ajsXMerYXjahOevUd_p88vNrnI9QD2ByGvY"))
                        {
                            line = line.Replace("1VunkEZX3ajsXMerYXjahOevUd_p88vNrnI9QD2ByGvY", "");
                        }

                        output.WriteLine(line);
                    }
                }
            }
        }
    }
}
