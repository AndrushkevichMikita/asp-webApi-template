using HelpersCommon.ControllerExtensions;
using HelpersCommon.FiltersAndAttributes;
using HelpersCommon.PipelineExtensions;
using HelpersCommon.Scheduler;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Exceptions;
using Serilog.Sinks.Elasticsearch;
using System.Reflection;

namespace CommonHelpers
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddCommonServices(this IServiceCollection services)
        {
            services.AddScoped<DiagAuthorizeFactoryAttribute>();
            services.AddScoped<IAuthorizationHandler, MinPermissionHandler>();

            services.AddHostedService<SchedulerHostedService>();

            services.Configure<MvcOptions>(x => x.Conventions.Add(new ModelStateValidatorConvension()));
            services.LimitFormBodySize(Config.MaxRequestSizeBytes);
            return services;
        }

        public static void ConfigureSerilog(this ConfigureHostBuilder host, IConfiguration configuration)
        {
            host.UseSerilog((ctx, lc) => lc
                .Enrich.FromLogContext()
                .Enrich.WithExceptionDetails()
                .Enrich.WithMachineName()
                .WriteTo.Debug()
                .WriteTo.Console()
                .WriteTo.File(@"Logs\log.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 31)
                .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(configuration["ElasticConfiguration:Uri"]))
                {
                    AutoRegisterTemplate = true,
                    IndexFormat = $"{Assembly.GetExecutingAssembly().GetName().Name.ToLower().Replace(".", "-")}-{Config.Env?.ToLower().Replace(".", "-")}-{DateTime.UtcNow:yyyy-MM}"
                })
                .Enrich.WithProperty("Environment", Config.Env)
                .ReadFrom.Configuration(configuration));
        }
    }
}
