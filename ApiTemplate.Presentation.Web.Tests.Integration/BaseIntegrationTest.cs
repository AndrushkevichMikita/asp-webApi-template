using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ApiTemplate.Presentation.Web.Tests.Integration
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            builder.UseEnvironment("Development");
        }
    }

    public abstract class BaseIntegrationTest : IClassFixture<CustomWebApplicationFactory>
    {
        public readonly IServiceScope ServicesScope;
        public readonly HttpClient HTTPClient;

        protected BaseIntegrationTest(CustomWebApplicationFactory factory)
        {
            ServicesScope = factory.Services.CreateScope();
            HTTPClient = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
            });
        }
    }
}
