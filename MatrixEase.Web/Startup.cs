using System;
using System.Linq;
using MatrixEase.Web.Common;
using MatrixEase.Web.Middleware;
using MatrixEase.Manga.Utility;
using MatrixEase.Web.Tasks;
using MatrixEase.Manga.Manga.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.RateLimiting;

namespace MatrixEase.Web
{
    public class Startup
    {
        private const string FrontendCorsPolicy = "FrontendOrigins";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            AppSettings settings = Configuration.GetSection("MatrixEase:Web").Get<AppSettings>();
            if (settings == null)
            {
                throw new InvalidOperationException("Missing MatrixEase:Web configuration.");
            }

            SecretProtector.Configure(settings.ProtectionKey);
            services.Configure<AppSettings>(Configuration.GetSection("MatrixEase:Web"));

            MangaRoot.SetRootFolder(settings.FileSaveLocation);

            services.AddHttpClient(nameof(SupabaseTokenValidator));
            services.AddSingleton<RequestContextAccessor>();
            services.AddSingleton<HmacSupabaseTokenValidator>();
            services.AddSingleton<ISupabaseTokenValidator, SupabaseTokenValidator>();

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor |
                    ForwardedHeaders.XForwardedProto |
                    ForwardedHeaders.XForwardedHost;
                options.RequireHeaderSymmetry = false;
            });

            services.AddCors(options =>
            {
                options.AddPolicy(FrontendCorsPolicy, policy =>
                {
                    string[] allowedOrigins = settings.GetAllowedOrigins()
                        .Where(origin => string.IsNullOrWhiteSpace(origin) == false)
                        .ToArray();

                    if (allowedOrigins.Length > 0)
                    {
                        policy.WithOrigins(allowedOrigins)
                            .AllowAnyMethod()
                            .AllowAnyHeader();
                    }
                });
            });

            services.AddRateLimiter(options =>
            {
                RateLimitSettings rateLimit = settings.RateLimit ?? new RateLimitSettings();
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.OnRejected = async (context, token) =>
                {
                    context.HttpContext.Response.Headers["Retry-After"] = Math.Max(1, rateLimit.WindowSeconds).ToString();
                    await context.HttpContext.Response.WriteAsync("Too many requests.", token);
                };
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                {
                    if (rateLimit.Enabled == false)
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
                        PermitLimit = Math.Max(1, rateLimit.PermitLimit),
                        Window = TimeSpan.FromSeconds(Math.Max(1, rateLimit.WindowSeconds)),
                        QueueLimit = Math.Max(0, rateLimit.QueueLimit),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        AutoReplenishment = true
                    });
                });
            });

            services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
            });

            services.AddHostedService<QueuedHostedService>();
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseForwardedHeaders();
            app.UseMiddleware<UnhandledExceptionLogMiddleware>();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
                app.UseHttpsRedirection();
            }

            app.UseMiddleware<AccessLogMiddleware>();
            app.UseRouting();
            app.UseMiddleware<SupabaseAuthMiddleware>();
            app.UseMiddleware<GatewaySecretMiddleware>();
            app.UseCors(FrontendCorsPolicy);

            app.UseRateLimiter();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
                endpoints.MapControllers();

            });
        }
    }
}
