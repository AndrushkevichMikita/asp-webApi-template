﻿using Microsoft.Extensions.Configuration;

namespace ApiTemplate.SharedKernel
{
    public static class Config
    {
        public static readonly string Env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        public static bool IsDev { get; private set; }
        public static bool IsProd { get; private set; }
        public static bool IsStaging { get; private set; }
        public static bool IsPreStaging { get; private set; }
        public static int MaxRequestSizeBytes { get; set; }

        public static void ApplyConfiguration(this ConfigurationManager c)
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

            c.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
             .AddJsonFile($"appsettings.{Env}.json", optional: true, reloadOnChange: true);

            MaxRequestSizeBytes = c.GetSection("MaxRequestSizeMb").Get<int>() * 1024 * 1024;
        }
    }
}
