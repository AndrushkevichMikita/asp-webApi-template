using ApiTemplate.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace ApiTemplate.Domain
{
    public static class DomainDependencyInjection
    {
        public static IServiceCollection AddDomain(this IServiceCollection services, IConfiguration configuration)
        {
            Assembly.GetExecutingAssembly()
                    .GetTypes()
                    .Where(t => typeof(IDomainConfiguration).IsAssignableFrom(t) && t.IsClass)
                    .ToList()
                    .ForEach(type =>
                    {
                        (Activator.CreateInstance(type) as IDomainConfiguration).Register(services, configuration);
                    });

            return services;
        }
    }
}