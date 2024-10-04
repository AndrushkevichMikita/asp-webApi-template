using ApiTemplate.Infrastructure;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Networks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Elasticsearch;
using Testcontainers.MsSql;

namespace ApiTemplate.Presentation.Web.Tests.Integration
{
    public class TestsWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        const string networkAliase = nameof(networkAliase);
        private readonly static INetwork _weatherForecastNetwork = new NetworkBuilder().Build();

        private readonly MsSqlContainer _mssqlContainer = new MsSqlBuilder()
                                                             .WithCleanUp(true)
                                                             .WithPortBinding(1433)
                                                             .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1433))
                                                             .WithNetwork(_weatherForecastNetwork)
                                                             .WithNetworkAliases(networkAliase)
                                                             .Build();

        private readonly ElasticsearchContainer _elasticsearchcontainer = new ElasticsearchBuilder()
                                                                             .WithCleanUp(true)
                                                                             .WithPortBinding(9200)
                                                                             .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(9200))
                                                                             .WithNetwork(_weatherForecastNetwork)
                                                                             .WithNetworkAliases(networkAliase)
                                                                             .Build();
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                var dbContextDescriptor = services.SingleOrDefault(x => x.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (dbContextDescriptor is not null)
                {
                    services.Remove(dbContextDescriptor);
                }
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    var c = _mssqlContainer.GetConnectionString();
                    options.UseSqlServer(c);
                });
            });

            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "IntegrationTests");
            builder.UseEnvironment("IntegrationTests");
        }

        public async Task InitializeAsync()
        {
            await _weatherForecastNetwork.CreateAsync();
            await _mssqlContainer.StartAsync();
        }

        public new async Task DisposeAsync()
        {
            await _mssqlContainer.StopAsync();
            await _weatherForecastNetwork.DisposeAsync();
        }
    }

    /// <summary>
    /// Note: <see cref="TestsWebApplicationFactory" /> class are tied to IAsyncLifetime, <br/>
    /// then the setup and teardown will occur only once per test class, not per [Fact] method
    /// </summary>
    public abstract class BaseIntegrationTest : IClassFixture<TestsWebApplicationFactory>
    {
        public readonly IServiceScope ServicesScope;
        public readonly TestsWebApplicationFactory Factory;
        public readonly HttpClient HTTPClient;

        protected BaseIntegrationTest(TestsWebApplicationFactory factory)
        {
            Factory = factory;
            ServicesScope = factory.Services.CreateScope();
            HTTPClient = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
            });
        }
    }
}
