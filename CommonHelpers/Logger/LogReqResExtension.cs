using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Security.Claims;
using System.Text;

namespace HelpersCommon.Logger
{
    public static class LogReqResExtension
    {
        private static readonly byte[] b = new byte[1] { 1 };
        public static IApplicationBuilder LogRequestAndResponse(this IApplicationBuilder app, ILogger logger)
        {
            app.Use(async delegate (HttpContext context, Func<Task> next)
            {
                var now = DateTime.UtcNow;
                var reqId = (now.Second * 1000 + now.Millisecond + Thread.CurrentThread.ManagedThreadId).ToString("D5") + ":";
                var message = "";
                try
                {
                    var str = new StringBuilder();
                    // user id or anonym
                    var userId = context.User?.Claims?.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
                    str.Append("uid:"); str.Append(userId ?? "null");
                    str.Append(", ");

                    // get cookie sessionId
                    context.Session.Set("Init", b); // session id changes until you actually store something, the id is fixed then. https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state?view=aspnetcore-6.0
                    str.Append("session:"); str.Append(context.Session.Id.Replace("-", ""));
                    str.Append(", ");

                    // device name if header exists
                    context.Request.Headers.TryGetValue("device", out StringValues device);
                    str.Append("dev:"); str.Append('['); str.Append(device); str.Append(']');

                    str.Append(" => ");
                    str.Append(context.Request.Method);
                    str.Append(' ');
                    str.Append(context.Request.Path);
                    if (context.Request.QueryString.HasValue)
                        str.Append(context.Request.QueryString.Value);

                    message = str.ToString();
                    logger.AddHttpTrace(reqId + $"Req, {message}");
                }
                catch (Exception ex)
                {
                    logger.AddError(reqId + "Loger.UserFileLogger exception: " + message, ex);
                }
                try
                {
                    await next.Invoke();
                }
                catch (Exception ex)
                {
                    logger.AddError(reqId + $"Res(err), {message}", ex);
                    throw;
                }

                var code = context?.Response?.StatusCode;
                if (code == 500 || code == null)
                    logger.AddError(reqId + $"Res({code?.ToString() ?? "null"}), {message}");
                else
                    logger.AddHttpTrace(reqId + $"Res({code}), {message}");
            });
            return app;
        }
    }
}
