using ApiTemplate.Infrastructure;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Networks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
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
                                                             .WithNetwork(_weatherForecastNetwork)
                                                             .WithNetworkAliases(networkAliase)
                                                             .Build();

        private readonly ElasticsearchContainer _elasticsearchcontainer = new ElasticsearchBuilder()
                                                                             .WithPortBinding(9200)
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
                    options.UseSqlServer(_mssqlContainer.GetConnectionString());
                });
            });

            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            builder.UseEnvironment("Development");
        }

        public async Task InitializeAsync()
        {
            await _weatherForecastNetwork.CreateAsync().ConfigureAwait(false);
            await _mssqlContainer.StartAsync().ConfigureAwait(false);
            await _elasticsearchcontainer.StartAsync().ConfigureAwait(false);
        }

        public new async Task DisposeAsync()
        {
            await _mssqlContainer.DisposeAsync();
            await _elasticsearchcontainer.DisposeAsync();
            await _weatherForecastNetwork.DisposeAsync();
        }

        public static async Task WaitUntilContainerIsReadyAsync(MsSqlContainer container, int retries = 10, int delay = 2000)
        {
            using var httpClient = new HttpClient();
            for (int i = 0; i < retries; i++)
            {
                try
                {
                    // Example of checking if the database is ready by trying to open a connection
                    using var connection = new SqlConnection(container.GetConnectionString());
                    await connection.OpenAsync();
                    return;
                }
                catch
                {
                    await Task.Delay(delay);
                }
            }

            throw new Exception("The SQL Server container is not ready.");
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
