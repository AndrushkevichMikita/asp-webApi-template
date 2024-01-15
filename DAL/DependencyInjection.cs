using ApplicationCore.Entities;
using ApplicationCore.Interfaces;
using CommonHelpers;
using DAL.Interceptors;
using HelpersCommon.ExceptionHandler;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DAL
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

            services.AddDefaultIdentity<UserEntity>(options =>
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
              .AddDefaultTokenProviders();
#if DEBUG
            services.AddDatabaseDeveloperPageExceptionFilter();
#endif
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
