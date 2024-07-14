using ApiTemplate.Domain.Entities;
using ApiTemplate.Domain.Interfaces;
using ApiTemplate.Domain.Services;
using ApiTemplate.Infrastructure.Interceptors;
using ApiTemplate.Infrastructure.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ApiTemplate.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());

                if (configuration.GetValue<bool>("IsInMemoryDb"))
                {
                    options.UseInMemoryDatabase(Guid.NewGuid().ToString());
                }
                else
                {
                    var connection = configuration.GetConnectionString("MSSQL") ?? throw new ArgumentNullException("Db connection");
                    options.UseSqlServer(connection);
                }
            });

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

            services.AddScoped(typeof(IRepo<>), typeof(TRepository<>));
            services.AddScoped<ISaveChangesInterceptor, UserEntityInterceptor>();
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
                var db = scope.GetRequiredService<ApplicationDbContext>().Database;
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
            //builder.Configuration.ApplyConfiguration();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>();
            options.UseSqlServer(builder.Configuration.GetConnectionString("MSSQL"));
            return new ApplicationDbContext(options.Options);
        }
    }
}
