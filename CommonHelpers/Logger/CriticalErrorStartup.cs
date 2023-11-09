using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text;

namespace HelpersCommon.Logger
{
    public class CriticalErrorStartup
    {
        public static IWebHostEnvironment CurrentEnvironment { get; set; }

        public CriticalErrorStartup(IWebHostEnvironment env)
        {
            CurrentEnvironment = env;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ILogger, Logger>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.Run(async (context) =>
            {
                var str = new StringBuilder();
                Logger.ErrorsInMemory.Select(item => item).Reverse().ToList().ForEach(c => str.AppendLine(c.Message));
                await context.Response.WriteAsync(str.ToString());
            });
        }

        public static void Run(Exception ex)
        {
            try
            {

                Logger.ErrorCriticalSync("Error during SetupHost", ex);
                Host.CreateDefaultBuilder()
                    .ConfigureAppConfiguration(builder =>
                    {
                        builder.Sources.Clear();
                        builder.AddConfiguration(new ConfigurationBuilder().Build());
                    })
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        webBuilder.UseStartup<CriticalErrorStartup>();
                    }).Build().Start();
            }
            catch (Exception)
            {

            }
        }
    }
}
