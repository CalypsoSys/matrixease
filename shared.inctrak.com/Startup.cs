using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MatrixEase.Manga.Utility;
using MatrixEase.Web.Tasks;
using MatrixEase.Web.Middleware;
using MatrixEase.Manga.Manga.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Threading.RateLimiting;

namespace MatrixEase.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            AppSettings settings = LoadAppSettings(Configuration);
            if (string.IsNullOrWhiteSpace(settings.FileSaveLocation))
            {
                throw new InvalidOperationException("Missing AppSettings configuration.");
            }

            SecretProtector.Configure(settings.ProtectionKey);
            services.AddSingleton(settings);
            services.AddOptions<AppSettings>().Configure<IConfiguration>((options, configuration) =>
            {
                BindAppSettings(configuration, options);
            });

            MangaRoot.SetRootFolder(settings.FileSaveLocation);

            services.AddCors();
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = 429;
                options.OnRejected = async (context, token) =>
                {
                    context.HttpContext.Response.Headers["Retry-After"] = Math.Max(1, settings.RateLimit.WindowSeconds).ToString();
                    await context.HttpContext.Response.WriteAsync("Too many requests.", token);
                };
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                {
                    if (settings.RateLimit.Enabled == false)
                    {
                        return RateLimitPartition.GetNoLimiter("disabled");
                    }

                    string partitionKey = httpContext.Request.Headers["CF-Connecting-IP"].ToString();
                    if (string.IsNullOrWhiteSpace(partitionKey))
                    {
                        partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    }

                    return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = Math.Max(1, settings.RateLimit.PermitLimit),
                        Window = TimeSpan.FromSeconds(Math.Max(1, settings.RateLimit.WindowSeconds)),
                        QueueLimit = Math.Max(0, settings.RateLimit.QueueLimit),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        AutoReplenishment = true
                    });
                });
            });

            services
                .AddAuthentication(o =>
                {
                    o.DefaultScheme = IdentityConstants.ApplicationScheme;
                    o.DefaultSignInScheme = IdentityConstants.ExternalScheme;
                })
                .AddCookie(IdentityConstants.ApplicationScheme)
                .AddCookie(IdentityConstants.ExternalScheme)
                .AddGoogle(o =>
                {
                    o.ClientId = settings.GoogleClientId;
                    o.ClientSecret = settings.GoogleClientSecret;
                    o.Scope.Add("https://www.googleapis.com/auth/spreadsheets.readonly");
                    o.SaveTokens = true;

                    o.Events.OnCreatingTicket = ctx =>
                    {
                        List<AuthenticationToken> tokens = ctx.Properties.GetTokens().ToList();

                        ctx.Properties.StoreTokens(tokens);

                        return Task.CompletedTask;
                    };
                });

            services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
            });

            services.AddHostedService<QueuedHostedService>();
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IOptions<AppSettings> appSettingsOptions)
        {
            AppSettings appSettings = appSettingsOptions.Value;

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedProto
            });
            app.UseMiddleware<AccessLogMiddleware>();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            string[] allowedOrigins = appSettings.AllowedOrigins
                .Where(origin => string.IsNullOrWhiteSpace(origin) == false)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (allowedOrigins.Length > 0)
            {
                app.UseCors(options => options
                    .WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
            }

            if (env.IsDevelopment() == false)
            {
                app.UseHttpsRedirection();
            }
            app.UseRouting();

            app.Use(async (context, next) =>
            {
                if (RequestRequiresGatewaySecret(context.Request.Path) == false ||
                    env.IsDevelopment() ||
                    appSettings.RequireGatewaySecret == false ||
                    string.IsNullOrWhiteSpace(appSettings.GatewaySecret))
                {
                    await next();
                    return;
                }

                string headerName = string.IsNullOrWhiteSpace(appSettings.GatewaySecretHeaderName)
                    ? "X-Internal-Api-Key"
                    : appSettings.GatewaySecretHeaderName;

                if (context.Request.Headers.TryGetValue(headerName, out StringValues secretHeader) == false ||
                    secretHeader.Count == 0 ||
                    string.Equals(secretHeader[0], appSettings.GatewaySecret, StringComparison.Ordinal) == false)
                {
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsync("Forbidden");
                    return;
                }

                await next();
            });

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseRateLimiter();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
                endpoints.MapControllers();

            });

        }

        internal static AppSettings LoadAppSettings(IConfiguration configuration)
        {
            AppSettings settings = new AppSettings();
            BindAppSettings(configuration, settings);
            return settings;
        }

        internal static void BindAppSettings(IConfiguration configuration, AppSettings settings)
        {
            configuration.GetSection("AppSettings").Bind(settings);
        }

        private static bool RequestRequiresGatewaySecret(PathString path)
        {
            return path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWithSegments("/google", StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWithSegments("/account", StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWithSegments("/signin-google", StringComparison.OrdinalIgnoreCase);
        }
    }
}
