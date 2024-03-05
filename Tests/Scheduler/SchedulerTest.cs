using HelpersCommon.Scheduler;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Tests.Scheduler;

namespace Tests
{
    public class SchedulerTest
    {
        private sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
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
        public async Task CheckScheduledTaskRun()
        {
            // Arrange
            await using var application = new CustomWebApplicationFactory();
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