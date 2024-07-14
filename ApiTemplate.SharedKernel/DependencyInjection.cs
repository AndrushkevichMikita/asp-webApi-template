using ApiTemplate.SharedKernel.CustomPolicy;
using ApiTemplate.SharedKernel.FiltersAndAttributes;
using ApiTemplate.SharedKernel.PipelineExtensions;
using ApiTemplate.SharedKernel.Scheduler;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace ApiTemplate.SharedKernel
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddSharedKernel(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy(nameof(IsUserLockedAuthHandler), policy => policy.Requirements.Add(new UserNotLockedRequirement()));
            });

            services.AddScoped<DiagAuthorizeFactoryAttribute>();
            services.AddScoped<IAuthorizationHandler, IsUserLockedAuthHandler>();

            services.AddHostedService<SchedulerHostedService>();

            services.Configure<MvcOptions>(x => x.Conventions.Add(new ModelStateValidatorConvension()));
            services.LimitFormBodySize(Config.MaxRequestSizeBytes);
            return services;
        }
    }
}
