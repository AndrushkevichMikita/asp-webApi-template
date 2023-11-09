using HelpersCommon.PrimitivesExtensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace HelpersCommon.FiltersAndAttributes
{
    public static class SecureAllowAnonymWebClient
    {
        /// <summary>
        /// Replace {{uitoken}} in index.html to AnonymKey from settings
        /// </summary>
        public static void UISetAnonymKey(this IWebHostEnvironment host, IConfiguration config, HelpersCommon.Logger.ILogger logger)
        {
            if (host.WebRootPath is null)
                return; // Client ui not builded 

            var path = Path.Combine(host.WebRootPath, "index.html");
            if (File.Exists(path))
            {
                var key = config.GetValue<int>("AnonymKey");
                if (key == 0)
                    logger.AddError("Missed AnonymKey in appsettings");

                var text = File.ReadAllText(path);
                text = text.ReplaceFirst("{{uitoken}}", key.ToString());
                File.WriteAllText(path, text);
            }
        }
    }
}
