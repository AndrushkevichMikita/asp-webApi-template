using ApiTemplate.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.DataAnnotations;

namespace ApiTemplate.Domain.Configuration
{
    public class SMTPConfiguration : IDomainConfiguration
    {
        [Required]
        public string UserName { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string Host { get; set; }

        [Required]
        public int Port { get; set; }

        [Required]
        public string EmailFrom { get; set; }

        public void Register(IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<SMTPConfiguration>(configuration.GetSection(nameof(SMTPConfiguration)));
        }
    }
}
