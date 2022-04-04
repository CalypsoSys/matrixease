//#define TEST_LIMITS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatrixEase.Manga.Utility
{
    public static class MatrixEaseLicense
    {
        private const int MaxProjects = 5;
        private const string MaxProjectsMessage = "The free version limits the ability to save more than 5 distinct analysis projects for personal use. Contact us to get a license to remove this restriction or to enquire about a Enterprise license.";
        private const int MaxRowsCols = 5000000;
        private const string MaxCellsMessage = "The free version limits a input matrix to a maximum of 5 million cells for personal use. Contact us to get a license to remove this restriction or to enquire about a Enterprise license.";

        public static void CheckProjectCount(int mangas)
        {
#if RELEASE || TEST_LIMITS
            if (mangas >= MaxProjects)
            {
                MyLogger.LogError(null, MaxProjectsMessage);
                throw new MatrixEaseLicenseException(MaxProjectsMessage);
            }
#endif
        }

        public static void CheckCellCount(int cells)
        {
#if RELEASE || TEST_LIMITS
            if (cells >= MaxRowsCols)
            {
                MyLogger.LogError(null, MaxCellsMessage);
                throw new MatrixEaseLicenseException(MaxCellsMessage);
            }
#endif
        }
    }

    public class MatrixEaseLicenseException : ApplicationException
    {
        public MatrixEaseLicenseException(string message) : base(message)
        {
        }
    }
}
