using Microsoft.AspNetCore.Builder;

namespace ApiTemplate.SharedKernel.PipelineExtensions
{
    public static class SecurityHeaders
    {
        public static IApplicationBuilder ConfigureSecurityHeaders(this IApplicationBuilder app)
        {
            app.UseForwardedHeaders();
            app.Use(async (context, next) =>
            {
                context.Response.Headers.Add("Referrer-Policy", "same-origin");
                context.Response.Headers.Add("X-Xss-Protection", "1; mode=block");
                context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                context.Response.Headers.Add("Content-Security-Policy", "img-src * 'self' blob: data:;");
                context.Response.Headers.Add("Permissions-Policy", "camera=(), geolocation=(), microphone=()");
                await next();
            });
            app.UseHsts();
            return app;
        }
    }
}
