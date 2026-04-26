using System.Collections.Generic;

namespace MatrixEase.Web
{
    public class AppSettings
    {
        public string FrontendBaseUrl { get; set; } = "http://localhost:3000";
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
        public List<string> AllowedOrigins { get; set; } = new List<string>();
        public bool RequireGatewaySecret { get; set; }
        public string GatewaySecretHeaderName { get; set; } = "X-Internal-Api-Key";
        public string GatewaySecret { get; set; }
    }
}
