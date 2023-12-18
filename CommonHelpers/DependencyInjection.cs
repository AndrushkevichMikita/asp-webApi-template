using HelpersCommon.ControllerExtensions;
using HelpersCommon.FiltersAndAttributes;
using HelpersCommon.Logger;
using HelpersCommon.PipelineExtensions;
using HelpersCommon.Scheduler;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace CommonHelpers
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddCommonServices(this IServiceCollection services)
        {
            services.AddScoped<DiagAuthorizeFactoryAttribute>();
            services.AddScoped<SecureAllowAnonymousAttribute>();
            services.AddScoped<IAuthorizationHandler, MinPermissionHandler>();
            services.AddSingleton<ILogger, Logger>();

            services.AddHostedService<SchedulerHostedService>();

            services.Configure<MvcOptions>(x => x.Conventions.Add(new ModelStateValidatorConvension()));
            services.LimitFormBodySize(Config.MaxRequestSizeBytes);
            return services;
        }
    }
}
