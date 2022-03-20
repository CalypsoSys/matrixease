using MatrixEase.Manga.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MatrixEase.Manga.com
{
    public class AppSettings
    {
        public bool ClearText { get; set; }
        public string FileSaveLocation { get; set; }
        public string GoogleClientId { get; set; }
        public string GoogleClientSecret { get; set; }
        public bool UseSNMP { get; set; }
        public string SNMPServer { get; set; }
        public int SNMPPort { get; set; }
        public string SNMPAddress { get; set; }
        public string SNMPPassword { get; set; }
        public string EmailApiKey { get; set; }
        public string EmailFrom { get; set; }

        public string GetGoogleClientId()
        {
            return MiscHelpers.Decrypt(GoogleClientId, ClearText);
        }

        public string GetGoogleClientSecret()
        {
            return MiscHelpers.Decrypt(GoogleClientSecret, ClearText);
        }
        public string GetSNMPServer()
        {
            return MiscHelpers.Decrypt(SNMPServer, ClearText);
        }
        public string GetSNMPAddress()
        {
            return MiscHelpers.Decrypt(SNMPAddress, ClearText);
        }
        public string GetSNMPPassword()
        {
            return MiscHelpers.Decrypt(SNMPPassword, ClearText);
        }
        public string GetEmailApiKey()
        {
            return MiscHelpers.Decrypt(EmailApiKey, ClearText);
        }

        public string GetEmailFrom()
        {
            return MiscHelpers.Decrypt(EmailFrom, ClearText);
        }
    }
}
