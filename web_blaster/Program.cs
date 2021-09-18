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
            DeleteFiles(args[2]);
            CopyFiles(args[0] != "Debug", args[1], args[2]);
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

        private static void CopyFiles(bool release, string source, string dest)
        {
            DirectoryInfo di = new DirectoryInfo(source);
            foreach (FileInfo file in di.GetFiles())
            {
                var destination = Path.Combine(dest, file.Name);
                if (release)
                {
                    ProcessFile(file, destination);
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
                CopyFiles(release, dir.FullName, subDest);
            }
        }

        private static void ProcessFile(FileInfo source, string destination)
        {
            switch (source.Extension)
            {
                case ".html":
                    UseMinifiedInHTML(source.FullName, destination);
                    break;
                case ".js":
                    UglifyFile(Uglifytype.uglifyjs, source.FullName, destination);
                    break;
                case ".css":
                    UglifyFile(Uglifytype.uglifycss, source.FullName, destination);
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

        private static void UglifyFile(Uglifytype ugg, string source, string destination)
        {
            Process process = new Process();
            //process.StartInfo.FileName = string.Format("{0}.cmd", ugg);
            process.StartInfo.FileName = string.Format("{0}", ugg);
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
                        if (line.Contains("<script") && line.Contains(".js\"") && !line.Contains("overrides.js") && !line.Contains("vue2-slideout-panel.min.js") && !line.Contains("vue-cookies.js"))
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
                        else if (line.Contains("108thHouse"))
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
