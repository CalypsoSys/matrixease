using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatrixEase.Manga.Utility
{
    public static class StorageHelpers
    {
        public static void SafeFileDelete(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
            catch(Exception excp)
            {
                SimpleLogger.LogError(excp, "Error deleting file {0}", filePath);
            }
        }
        public static List<MyPerformance> FolderSizes(string folderPath)
        {
            List<MyPerformance> perfCounts = new List<MyPerformance>();
            decimal totalSize = 0;
            perfCounts.AddRange( FolderSizes(folderPath, 1, ref totalSize));
            perfCounts.Add(new MyPerformance("total_size", totalSize, folderPath));
            return perfCounts;
        }

        private static List<MyPerformance> FolderSizes(string folderPath, int indent, ref decimal totalSize)
        {
            List<MyPerformance> perfCounts = new List<MyPerformance>();
            DirectoryInfo directory = new DirectoryInfo(folderPath);
            decimal folderSize = 0;
            foreach (var file in directory.GetFiles())
            {
                folderSize += file.Length;
            }
            totalSize += folderSize;
            perfCounts.Add(new MyPerformance(directory.Name, folderSize, folderPath));

            foreach (var folder in directory.GetDirectories())
            {
                perfCounts.AddRange(FolderSizes(folder.FullName, indent + 1, ref totalSize));
            }

            return perfCounts;
        }
    }
}
