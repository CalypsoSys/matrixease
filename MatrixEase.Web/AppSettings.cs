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
        private static readonly string[] DefaultAllowedOrigins = new[]
        {
            "https://app.matrixease.com",
            "https://matrixease.com",
            "https://www.matrixease.com",
            "https://localhost:44340",
            "http://127.0.0.1:5173",
            "http://localhost:5173"
        };

        public string FileSaveLocation { get; set; }
        public string ProtectionKey { get; set; }
        public int MaxConcurrentJobs { get; set; } = 10;
        public string FrontendBaseUrl { get; set; }
        public string[] AllowedOrigins { get; set; }
        public bool RequireGatewaySecret { get; set; }
        public string GatewaySecretHeaderName { get; set; } = "X-Internal-Api-Key";
        public string GatewaySecret { get; set; }
        public RateLimitSettings RateLimit { get; set; } = new RateLimitSettings();
        public string AccessLogPath { get; set; } = "logs/access.log";
        public string ErrorLogPath { get; set; } = "logs/errors.log";
        public string SupabaseUrl { get; set; }
        public string SupabaseAnonKey { get; set; }
        public string SupabaseJwtSecret { get; set; }
        public string SupabaseAudience { get; set; } = "authenticated";
        public string SlackFeedbackWebhookUrl { get; set; }

        public string GetAccessLogPath()
        {
            return AccessLogPath;
        }

        public string GetErrorLogPath()
        {
            return ErrorLogPath;
        }

        public string[] GetAllowedOrigins()
        {
            if (AllowedOrigins == null || AllowedOrigins.Length == 0)
                return DefaultAllowedOrigins;

            return AllowedOrigins;
        }

        public string GetSupabaseUrl()
        {
            return SupabaseUrl;
        }

        public string GetSupabaseAnonKey()
        {
            return SupabaseAnonKey;
        }

        public string GetSupabaseJwtSecret()
        {
            return SupabaseJwtSecret;
        }

        public string GetSupabaseAudience()
        {
            return string.IsNullOrWhiteSpace(SupabaseAudience) ? "authenticated" : SupabaseAudience;
        }

        public string GetSlackFeedbackWebhookUrl()
        {
            return SlackFeedbackWebhookUrl;
        }
    }
}
