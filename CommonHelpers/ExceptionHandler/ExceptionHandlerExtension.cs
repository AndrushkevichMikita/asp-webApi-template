using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace HelpersCommon.ExceptionHandler
{
    public static class ExceptionHandlerExtension
    {
        public static string ProvideJsonError(string message)
            => "{\"errorMessage\":" + message + "}";

        public static void RegisterExceptionAndLog(this IApplicationBuilder app, HelpersCommon.Logger.ILogger logger)
        {
#if DEBUG
            app.UseDeveloperExceptionPage();
#endif
            app.Use(async (context, next) =>
            {
                try
                {
                    await next();
                }
                catch (MyApplicationException ex)
                {
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = ex.ErrorStatus switch
                    {
                        ErrorStatus.NotFound => StatusCodes.Status404NotFound,
                        ErrorStatus.Forbidden => StatusCodes.Status403Forbidden,
                        ErrorStatus.InvalidData => StatusCodes.Status400BadRequest,
                        ErrorStatus.NotAcceptable => StatusCodes.Status406NotAcceptable,
                        ErrorStatus.NotUnauthorized => StatusCodes.Status401Unauthorized,
                        ErrorStatus.PayloadLarge => StatusCodes.Status413RequestEntityTooLarge,
                        _ => throw ex
                    };

                    var ser = JsonSerializer.Serialize(ex.Message);
                    if (ser.Contains("\\n") && ser.Contains(":line"))
                    {
                        var a = ser.Substring(0, ser.IndexOf(":line"));
                        var b = a.LastIndexOf("\\n");
                        ser = a.Substring(0, b) + "\"";
                    }
                    await context.Response.WriteAsync(ProvideJsonError(ser));
                }
                catch (BadHttpRequestException) { } // https://github.com/dotnet/aspnetcore/issues/23949
                catch (OperationCanceledException ex)
                {
                    context.Response.StatusCode = StatusCodes.Status409Conflict;
                    string json = ProvideJsonError(JsonSerializer.Serialize(ex.Message));
                    await context.Response.WriteAsync(json);
                }
                catch (Exception ex)
                {
                    logger.AddError($"{ex.Message}, {ex.InnerException?.Message}");
#if DEBUG
                    throw ex;
#else
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
                    string json = ProvideJsonError("Something went wrong...");
                    await context.Response.WriteAsync(json);
#endif
                }
            });
        }
    }
}