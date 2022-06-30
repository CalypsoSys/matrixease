//#define TEST_LIMITS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        private static bool _overrideLicense = false;
        public static void OverrideLicense()
        {
            if ( Assembly.GetEntryAssembly().FullName == "MatrixEase.Tester, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null" )
            {
                _overrideLicense = true;
            }
        }

        public static void CheckProjectCount(int mangas)
        {
            if (_overrideLicense)
            {
                return;
            }
#if RELEASE || TEST_LIMITS
            if (mangas >= MaxProjects)
            {
                SimpleLogger.LogError(null, MaxProjectsMessage);
                throw new MatrixEaseException(MaxProjectsMessage);
            }
#endif
        }

        public static void CheckCellCount(int cells)
        {
            if ( _overrideLicense )
            {
                return;
            }
#if RELEASE || TEST_LIMITS
            if (cells >= MaxRowsCols)
            {
                SimpleLogger.LogError(null, MaxCellsMessage);
                throw new MatrixEaseException(MaxCellsMessage);
            }
#endif
        }
    }
}
