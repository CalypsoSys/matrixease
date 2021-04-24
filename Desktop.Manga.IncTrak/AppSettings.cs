using Manga.IncTrak.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Desktop.Manga.IncTrak
{
    public class AppSettings
    {
        public string GoogleClientSecrets { get; set; }

        public string GetGoogleClientSecrets()
        {
            return MiscHelpers.Decrypt(GoogleClientSecrets);
        }
    }
}
