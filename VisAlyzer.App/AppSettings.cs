using MatrixEase.Manga.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Desktop.MatrixEase.Manga
{
    public class AppSettings
    {
        public bool ClearText { get; set; }
        public string GoogleClientId { get; set; }
        public string GoogleClientSecret { get; set; }

        public string GetGoogleClientId()
        {
            return MiscHelpers.Decrypt(GoogleClientId, ClearText);
        }

        public string GetGoogleClientSecret()
        {
            return MiscHelpers.Decrypt(GoogleClientSecret, ClearText);
        }
    }
}
