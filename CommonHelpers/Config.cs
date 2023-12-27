using HelpersCommon.Logger;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace CommonHelpers
{
    public enum UseDb
    {
        Azure = 1,
        MSSQL,
        AWS,
        Docker,
    }

    public static class Config
    {
        /// <summary>
        /// Use Config.RootServiceProvider = app.ApplicationServices in Startup.Configure
        /// </summary>
        public static IServiceProvider RootServiceProvider { get; set; }
        public static readonly string Env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        public static bool IsDev { get; private set; }
        public static bool IsProd { get; private set; }
        public static bool IsStaging { get; private set; }
        public static bool IsPreStaging { get; private set; }
        public static string DefaultConnStr { get; set; }
        public static int MaxRequestSizeBytes { get; set; }

        public static void ApplyConfiguration(this ConfigurationManager c)
        {
            try
            {
                if (string.IsNullOrEmpty(Env))
                    throw new ArgumentNullException("Running app without ASPNETCORE_ENVIRONMENT isn't allowed");

                switch (Env.ToLower())
                {
                    case "prod": IsProd = true; break;
                    case "staging": IsStaging = true; break;
                    case "prestaging": IsPreStaging = true; break;
                    default: IsDev = true; break;
                }

                c.AddJsonFile("appsettings.shared.json", optional: false, reloadOnChange: true)
                 .AddJsonFile($"appsettings.shared.{Env}.json", optional: true, reloadOnChange: true)
                 .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                 .AddJsonFile($"appsettings.{Env}.json", optional: true, reloadOnChange: true);

                MaxRequestSizeBytes = c.GetSection("MaxRequestSizeMb").Get<int>() * 1024 * 1024;

                Logger.Instance(Options.Create(c.GetSection(LoggerSettings.Logger).Get<LoggerSettings>()));// set logger settings, no error will be caught in memory or(and) file until this line is reached
                Logger.Info($"Current environment : {Env}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }
    }
}
