using ApplicationCore.Entities;
using ApplicationCore.Interfaces;
using ApplicationCore.Services;
using CommonHelpers;
using HelpersCommon.ExceptionHandler;
using Infrastructure.Interceptors;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Text;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<ISaveChangesInterceptor, UserEntityInterceptor>();

            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());

                if (configuration.GetValue<bool>("IsInMemoryDb")) options.UseInMemoryDatabase(Guid.NewGuid().ToString());
                else
                {
                    var section = configuration.GetSection("ConnectionStrings");
                    var useDb = Enum.Parse<UseDb>(Environment.GetEnvironmentVariable("UseDb") ?? configuration.GetValue<UseDb>(nameof(UseDb)).ToString());
                    var connection = useDb switch
                    {
                        UseDb.Azure => Environment.GetEnvironmentVariable("DOTNET_AzureDb") ?? section.GetValue<string>("AzureDb"),
                        UseDb.AWS => Environment.GetEnvironmentVariable("DOTNET_AWSDb") ?? section.GetValue<string>("AWSDb"),
                        UseDb.Docker => Environment.GetEnvironmentVariable("DOTNET_DockerDb") ?? section.GetValue<string>("DockerDb"),
                        _ => section.GetValue<string>("MSSQLDb")
                    } ?? throw new MyApplicationException(ErrorStatus.InvalidData, "Db connection");

                    options.UseSqlServer(connection);
                }
            });

            services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
            services.AddDefaultIdentity<ApplicationUserEntity>(options =>
            {
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
                options.User.AllowedUserNameCharacters = null;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
                options.Lockout.MaxFailedAccessAttempts = 6;
            }).AddRoles<IdentityRole<int>>()
              .AddEntityFrameworkStores<ApplicationDbContext>()
              .AddDefaultTokenProviders()
              .AddClaimsPrincipalFactory<ApplicationUserClaimsPrincipalFactory>();
#if DEBUG
            services.AddDatabaseDeveloperPageExceptionFilter();
#endif
            services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(1);
                options.Cookie.SecurePolicy = Config.IsProd || Config.IsStaging ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    return Task.CompletedTask;
                };
            });

            services.AddAuthentication(options =>
            {
                options.DefaultChallengeScheme = "smart";
                options.DefaultAuthenticateScheme = "smart";
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Events.OnAuthenticationFailed = context =>
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnForbidden = context =>
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    return Task.CompletedTask;
                };
                options.TokenValidationParameters = ApplicationSignInManager.GetTokenValidationParameters(configuration);
            })
            .AddPolicyScheme("smart", "Smart Authentication", options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                    if (authHeader?.StartsWith("Bearer ") == true)
                    {
                        return JwtBearerDefaults.AuthenticationScheme;
                    }
                    return IdentityConstants.ApplicationScheme; // It's default shema that defined in Identity
                };
            });

            return services;
        }

        public static async Task ApplyDbMigrations(this IServiceProvider servicesProvider, IConfiguration configuration)
        {
            var scope = servicesProvider.CreateScope().ServiceProvider;
            if (!configuration.GetValue<bool>("IsInMemoryDb"))
            {
                var db = scope.GetRequiredService<IApplicationDbContext>().ProvideContext().Database;
                if ((await db.GetPendingMigrationsAsync()).Any())
                    await db.MigrateAsync();
            }
            // adding roles
            var roleManager = scope.GetRequiredService<RoleManager<IdentityRole<int>>>();
            foreach (var role in Enum.GetNames(typeof(RoleEnum)))
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole<int> { Name = role });
        }
    }

    // to fix running startup during migrations-update
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Configuration.ApplyConfiguration();
            var b = new DbContextOptionsBuilder<ApplicationDbContext>();
            b.UseSqlServer(builder.Configuration.GetSection("ConnectionStrings").GetValue<string>("MSSQLDb"));
            return new ApplicationDbContext(b.Options);
        }
    }
}
