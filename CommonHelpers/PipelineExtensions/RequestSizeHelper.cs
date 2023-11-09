using HelpersCommon.ExceptionHandler;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace HelpersCommon.PipelineExtensions
{
    public static class RequestSizeHelper
    {
        public static void LimitFormBodySize(this IServiceCollection services, int maxSizeBytes)
        {
            services.Configure<FormOptions>(x =>
            {
                x.MultipartBodyLengthLimit = maxSizeBytes;
            });
            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage).ToList();
                    throw new MyApplicationException(ErrorStatus.InvalidData, string.Join("\\n", errors));
                };
            });
        }
    }
}
