using ApiTemplate.Application.Entities;
using ApiTemplate.Application.Interfaces;
using ApiTemplate.Application.Repository;
using ApiTemplate.Application.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace ApiTemplate.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddSingleton<ISMTPService, SMTPService>();
            services.AddScoped<ApplicationSignInManager, ApplicationSignInManager>();
            services.AddScoped<ApplicationUserClaimsPrincipalFactory, ApplicationUserClaimsPrincipalFactory>();
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<IEmailTemplateService, EmailTemplateService>();
            services.AddScoped(typeof(IRepo<>), typeof(RepositoryBase<>));
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            return services;
        }
    }
}
