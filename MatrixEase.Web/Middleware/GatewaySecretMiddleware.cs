using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace MatrixEase.Web.Middleware
{
    public class GatewaySecretMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly AppSettings _settings;
        private readonly IWebHostEnvironment _environment;

        public GatewaySecretMiddleware(RequestDelegate next, IOptions<AppSettings> options, IWebHostEnvironment environment)
        {
            _next = next;
            _settings = options.Value ?? new AppSettings();
            _environment = environment;
        }

        public async Task Invoke(HttpContext context)
        {
            if (ShouldRequireGatewaySecret(context.Request.Path) == false ||
                _environment.IsDevelopment() ||
                _settings.RequireGatewaySecret == false ||
                string.IsNullOrWhiteSpace(_settings.GatewaySecret))
            {
                await _next(context);
                return;
            }

            string headerName = string.IsNullOrWhiteSpace(_settings.GatewaySecretHeaderName)
                ? "X-Internal-Api-Key"
                : _settings.GatewaySecretHeaderName;

            if (context.Request.Headers.TryGetValue(headerName, out var providedSecret) == false ||
                string.Equals(providedSecret, _settings.GatewaySecret, StringComparison.Ordinal) == false)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            await _next(context);
        }

        internal static bool ShouldRequireGatewaySecret(PathString path)
        {
            return path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase);
        }
    }
}
