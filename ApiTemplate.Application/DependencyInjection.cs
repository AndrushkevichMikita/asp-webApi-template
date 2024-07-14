using ApiTemplate.Application.Interfaces;
using ApiTemplate.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ApiTemplate.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddSingleton<ISMTPService, SMTPService>();
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<IEmailTemplateService, EmailTemplateService>();
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            return services;
        }
    }
}
