using System.Collections.Generic;

namespace MatrixEase.Web
{
    public class RateLimitSettings
    {
        public bool Enabled { get; set; }
        public int PermitLimit { get; set; } = 120;
        public int WindowSeconds { get; set; } = 60;
        public int QueueLimit { get; set; }
    }

    public class AppSettings
    {
        public string FrontendBaseUrl { get; set; } = "http://localhost:3000";
        public string FileSaveLocation { get; set; }
        public string ProtectionKey { get; set; }
        public string GoogleClientId { get; set; }
        public string GoogleClientSecret { get; set; }
        public string SlackFeedbackWebhookUrl { get; set; }
        public string AccessLogPath { get; set; } = "logs/access.log";
        public RateLimitSettings RateLimit { get; set; } = new RateLimitSettings();
        public int MaxConcurrentJobs { get; set; } = 10;
        public List<string> AllowedOrigins { get; set; } = new List<string>();
        public bool RequireGatewaySecret { get; set; }
        public string GatewaySecretHeaderName { get; set; } = "X-Internal-Api-Key";
        public string GatewaySecret { get; set; }
    }
}
