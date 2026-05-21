using MatrixEase.Web.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace MatrixEase.Web.Middleware
{
    public class UnhandledExceptionLogMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly AppSettings _settings;

        public UnhandledExceptionLogMiddleware(RequestDelegate next, IOptions<AppSettings> options)
        {
            _next = next;
            _settings = options.Value ?? new AppSettings();
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception excp)
            {
                string method = context?.Request?.Method ?? "-";
                string path = context?.Request == null ? "-" : AccessLogMiddleware.BuildLogPath(context.Request);
                MatrixEaseErrors.LogError(_settings, excp, "unhandled request {0} {1}", method, path);
                throw;
            }
        }
    }
}
