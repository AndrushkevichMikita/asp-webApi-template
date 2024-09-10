using ApiTemplate.SharedKernel;
using ApiTemplate.SharedKernel.Extensions;
using ApiTemplate.SharedKernel.FiltersAndAttributes;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.OpenApi.Models;
using System.Reflection;

namespace ApiTemplate.Presentation.Web
{
    public static class WebDependencyInjection
    {
        public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAutoMapper(Assembly.GetExecutingAssembly());

            services.AddControllers(config =>
                    {
                        config.Filters.Add(new MaxRequestSizeKBytes(Config.MaxRequestSizeBytes)); // singleton 
                    }).AddJsonDefaults();

            services.AddRouting(options => options.LowercaseUrls = true)
                    .AddHttpContextAccessor()
                    .AddHttpLogging(logging =>
                    {
                        logging.LoggingFields = HttpLoggingFields.All;
                    })
                    .AddHsts(opt =>
                    {
                        opt.IncludeSubDomains = true;
                        opt.MaxAge = TimeSpan.FromDays(365);
                    })
                    .AddResponseCompression(options =>
                    {
                        // it's risky for some attacks: https://docs.microsoft.com/en-us/aspnet/core/performance/response-compression?view=aspnetcore-3.0#compression-with-secure-protocol
                        options.EnableForHttps = false;
                    })
                    .AddSwaggerGen(c =>
                    {
                        c.SwaggerDoc("v1", new OpenApiInfo
                        {
                            Version = "v1",
                            Title = "API",
                            Description = "An ASP.NET Core Web API",
                            TermsOfService = new Uri("https://example.com/terms"),
                            Contact = new OpenApiContact
                            {
                                Name = "Example Contact",
                                Url = new Uri("https://example.com/contact")
                            },
                            License = new OpenApiLicense
                            {
                                Name = "Example License",
                                Url = new Uri("https://example.com/license")
                            }
                        });
                        var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                        c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
                    })
                    .AddHealthChecks();

            return services;
        }
    }
}