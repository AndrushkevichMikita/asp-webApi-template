using HelpersCommon.Scheduler;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Scheduler
{
    public class SchedulerTest : BaseIntegrationTest
    {
        public SchedulerTest(CustomWebApplicationFactory f) : base(f) { }

        private sealed class ApplicationFactory : CustomWebApplicationFactory
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
                SchedulerExtension.StartWithMinuteBegin = false;
                builder.ConfigureServices(services =>
                {
                    services.AddScheduler(new List<SchedulerItem>
                    {
                        new() { TaskType = typeof(TestScheduledTask) }
                    });
                    services.AddHostedService<SchedulerHostedService>();
                });
                builder.UseEnvironment("Development");
            }
        }

        [Fact]
        public async Task Should_Run_At_Least_Once()
        {
            // Arrange
            await using var application = new ApplicationFactory();
            // Act
            application.CreateClient();
            // give background service some time to run
            await Task.Delay(100);
            var ranAtLeastOnce = SchedulerExtension.TaskList.Any(x => x.CurrentTimeStart == DateTime.MaxValue && x.TaskType == typeof(TestScheduledTask));
            // Assert
            Assert.True(ranAtLeastOnce);
        }
    }
}