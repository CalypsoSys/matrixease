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
                MyLogger.LogError(excp, "Error deleting file {0}", filePath);
            }
        }
        public static string FolderSizes(string folderPath)
        {
            decimal totalSize = 0;
            var output = FolderSizes(folderPath, 1, ref totalSize);
            return string.Format("Total Size: {0:n0}\r\n{1}", totalSize, output);
        }

        private static string FolderSizes(string folderPath, int indent, ref decimal totalSize)
        {
            StringBuilder output = new StringBuilder();
            DirectoryInfo directory = new DirectoryInfo(folderPath);
            decimal folderSize = 0;
            foreach (var file in directory.GetFiles())
            {
                folderSize += file.Length;
            }
            totalSize += folderSize;
            output.AppendFormat("{0}{1}: {2:n0}\r\n", string.Empty.PadLeft(indent, ' '), directory.Name, folderSize);

            foreach (var folder in directory.GetDirectories())
            {
                output.Append(FolderSizes(folder.FullName, indent + 1, ref totalSize));
            }

            return output.ToString();
        }

    }
}
