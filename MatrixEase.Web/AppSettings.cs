namespace MatrixEase.Web
{
    public class AppSettings
    {
        private const int _defaultMaxConcurrentJobs = 10;

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
        public int MaxConcurrentJobs { get; set; }

        public string GetGoogleClientId()
        {
            return GoogleClientId;
        }

        public string GetGoogleClientSecret()
        {
            return GoogleClientSecret;
        }
        public string GetSNMPServer()
        {
            return SNMPServer;
        }
        public string GetSNMPAddress()
        {
            return SNMPAddress;
        }
        public string GetSNMPPassword()
        {
            return SNMPPassword;
        }
        public string GetEmailApiKey()
        {
            return EmailApiKey;
        }

        public string GetEmailFrom()
        {
            return EmailFrom;
        }

        public int GetMaxConcurrentJobs()
        {
            if ( MaxConcurrentJobs > 0 )
            {
                return MaxConcurrentJobs;
            }

            return _defaultMaxConcurrentJobs;
        }
    }
}
