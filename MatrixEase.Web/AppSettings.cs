namespace MatrixEase.Web
{
    public class AppSettings
    {
        public string FileSaveLocation { get; set; }
        public string ProtectionKey { get; set; }
        public string GoogleClientId { get; set; }
        public string GoogleClientSecret { get; set; }
        public bool UseSNMP { get; set; }
        public string SNMPServer { get; set; }
        public int SNMPPort { get; set; }
        public string SNMPAddress { get; set; }
        public string SNMPPassword { get; set; }
        public string EmailApiKey { get; set; }
        public string EmailFrom { get; set; }
        public int MaxConcurrentJobs { get; set; } = 10;
    }
}
