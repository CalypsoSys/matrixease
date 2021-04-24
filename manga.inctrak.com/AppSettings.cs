using Manga.IncTrak.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace manga.inctrak.com
{
    public class AppSettings
    {
        public string GoogleClientId { get; set; }
        public string GoogleClientSecret { get; set; }
        public string EmailApiKey { get; set; }
        public string EmailFrom { get; set; }

        public string GetGoogleClientId()
        {
            return MiscHelpers.Decrypt(GoogleClientId);
        }

        public string GetGoogleClientSecret()
        {
            return MiscHelpers.Decrypt(GoogleClientSecret);
        }

        public string GetEmailApiKey()
        {
            return MiscHelpers.Decrypt(EmailApiKey);
        }

        public string GetEmailFrom()
        {
            return MiscHelpers.Decrypt(EmailFrom);
        }
    }
}
