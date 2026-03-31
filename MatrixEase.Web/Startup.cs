using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MatrixEase.Manga.Utility;
using MatrixEase.Web.Tasks;
using MatrixEase.Manga.Manga.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MatrixEase.Web
{
    public class Startup
    {
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

            services.AddCors(); // Make sure you call this previous to AddMvc

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

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseCors(
                options => options.WithOrigins("https://my.matrixease.com", "https://matrixease.com", 
                                                "https://www.matrixease.com", "https://localhost:44340",
                                                "http://127.0.0.1:3000", "http://localhost:3000",
                                                "http://127.0.0.1:3001", "http://localhost:3001")
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .AllowCredentials()
            );

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedProto
            });

            if (env.IsDevelopment() == false)
            {
                app.UseHttpsRedirection();
            }
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
                endpoints.MapControllers();

            });

        }
    }
}
