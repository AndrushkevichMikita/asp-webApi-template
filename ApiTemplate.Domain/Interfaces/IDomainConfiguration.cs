using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ApiTemplate.Domain.Interfaces
{
    public interface IDomainConfiguration
    {
        public void Register(IServiceCollection services, IConfiguration configuration);
    }
}
