using asp_web_api_template.Data;
using FS.Shared.Scheduler;
using FS.Shared.Settings;
using FS.WebAdmin.SchedulerTasks;
using HelpersCommon.ControllerExtensions;
using HelpersCommon.ExceptionHandler;
using HelpersCommon.Extensions;
using HelpersCommon.FiltersAndAttributes;
using HelpersCommon.Logger;
using HelpersCommon.PipelineExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.OpenApi.Models;
using System.Data;
using System.Net;
using System.Text;

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.WebHost.ConfigureKestrel(x =>
    {
        // WARN: this options works only if run on Linux or *.exe. If you run on IIS see web.config
        x.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(30);
        x.Limits.MaxRequestBodySize = 2 * Config.MaxRequestSizeBytes; // the real limitations see in Startup...FormOptions // https://github.com/dotnet/aspnetcore/issues/20369
    });
    // applying appsettings
    builder.Configuration.ApplyConfiguration();

    builder.Services.AddScheduler(new List<SchedulerItem>
    {
        // new SchedulerItem { TaskType = typeof(SomeName) }, // WARN: It's example of registration
    });

    builder.Services.AddHostedService<SchedulerHostedService>();
    builder.Services.AddScoped<DiagAuthorizeFactoryAttribute>();
    builder.Services.AddScoped<SecureAllowAnonymousAttribute>();
    builder.Services.AddScoped<IAuthorizationHandler, MinPermissionHandler>();
    builder.Services.AddSingleton<HelpersCommon.Logger.ILogger, Logger>();
    builder.Services.Configure<MvcOptions>(x => x.Conventions.Add(new ModelStateValidatorConvension()));
    builder.Services.LimitFormBodySize(Config.MaxRequestSizeBytes);
    builder.Services.AddHealthChecks();

    // Add services to the container.
    //builder.Services.AddDbContext<ApplicationDbContext>(x => x.UseSqlServer(Config.DefaultConnStr)); // for real db
    builder.Services.AddDbContext<ApplicationDbContext>(x => x.UseInMemoryDatabase(Guid.NewGuid().ToString()));  // for in memory db

#if DEBUG
    builder.Services.AddDatabaseDeveloperPageExceptionFilter();
#endif

    builder.Services.AddResponseCompression(options =>
    {
        // it's risky for some attacks: https://docs.microsoft.com/en-us/aspnet/core/performance/response-compression?view=aspnetcore-3.0#compression-with-secure-protocol
        options.EnableForHttps = false;
    });

    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "asp-web-api template" });
    });

    builder.Services.AddSession();
    builder.Services.AddDistributedMemoryCache();
    // identity
    builder.Services.AddDefaultIdentity<TestIdentityUser>(options =>
    {
        //password validation
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequiredLength = 8;
        //User
        options.User.RequireUniqueEmail = true;
        options.User.AllowedUserNameCharacters = null;
        //lockout
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
        options.Lockout.MaxFailedAccessAttempts = 6;
    }).AddRoles<IdentityRole<int>>()
      .AddEntityFrameworkStores<ApplicationDbContext>()
      .AddDefaultTokenProviders();

    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.SlidingExpiration = true;
        options.Cookie.SecurePolicy = Config.IsProd || Config.IsStaging ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(builder.Configuration.GetSection("CookiesSettings").GetValue<int>("ExpirationMinutes"));
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

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy(nameof(MinPermissionHandler), new AuthorizationPolicyBuilder()
               .AddRequirements(new MinPermissionRequirement())
               .Build());
    });

    builder.Services.AddHsts(opt =>
    {
        opt.IncludeSubDomains = true;
        opt.MaxAge = TimeSpan.FromDays(365);
    });

    builder.Services.AddControllers(config =>
    {
        config.Filters.Add(new MaxRequestSizeKBytes(Config.MaxRequestSizeBytes)); // singleton 
    }).AddJsonDefaults();

    builder.Services.AddHttpContextAccessor();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
        app.UseMigrationsEndPoint(); // Error-page with migrations that were not applied
    else
        app.UseHsts();  // The default HSTS value is 30 days.

    app.UseHttpsRedirection();
    app.UseRouting();

    if (!Config.IsProd)
    {
        app.UseSwagger(c =>
        {
            c.RouteTemplate = "api/{documentname}/swagger.json";
        });
        // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
        // specifying the Swagger JSON endpoint.
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/api/v1/swagger.json", "asp-web-api template");
            c.RoutePrefix = "api";
        });
    }

    app.UseAuthentication();
    app.UseAuthorization();
    app.UseSession();

    // logger
    var l = app.Services.GetRequiredService<HelpersCommon.Logger.ILogger>(); // WARN: can get it without new scope because it singleton
    app.LogRequestAndResponse(l);
    app.RegisterExceptionAndLog(l);

    app.UseResponseCaching();
    app.UseResponseCompression();
    app.UseCompressedStaticFiles(app.Environment, app.Services.GetRequiredService<IHttpContextAccessor>());

    app.UseEndpoints(endpoints =>
    {
        // endPoint for SPA
        endpoints.MapFallbackToFile("index.html", new StaticFileOptions
        {
            OnPrepareResponse = x =>
            {
                var httpContext = x.Context;
                var path = httpContext.Request.RouteValues["path"];
                // now you get the original request path
            }
        });

        endpoints.MapHealthChecks("/health");
        endpoints.MapGet("/api/version", async context => await context.Response.WriteAsync(File.ReadAllLines("./appVersion.txt")[0]));
        if (!Config.IsProd)
        {
            // don't allow it for Production side - because it shares internal IP
            endpoints.MapGet("/myip", async context =>
            {
                var ip = context.Connection.RemoteIpAddress?.MapToIPv4()?.ToString();
                var header = context.Request.Headers["X-Forwarded-For"].ToString();
                var str = new StringBuilder();
                str.Append(ip ?? "null");
                str.AppendLine(" - context.Connection.RemoteIpAddress.MapToIPv4()?.ToString()"); // possible IP of AWS LoadBalancer
                str.Append(string.IsNullOrEmpty(header) ? "null" : header);
                str.AppendLine(" - header 'X-Forwarded-For'"); // header returns ogirinal client IP
                await context.Response.WriteAsync(str.ToString());
            });
        }
        endpoints.MapControllers()
                 .RequireAuthorization(new AuthorizeAttribute()) // WARN: Enables global [Authorize] attribute for each controller
                 .RequireAuthorization(nameof(MinPermissionHandler));
    });

    var scope = app.Services.CreateScope().ServiceProvider;
    // scope.GetRequiredService<ApplicationDbContext>().Database.Migrate();
    // adding roles
    var roleManager = scope.GetRequiredService<RoleManager<IdentityRole<int>>>();
    foreach (var role in Enum.GetNames(typeof(RoleEnum)))
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole<int> { Name = role });

    app.Run();
}
catch (Exception ex)
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddSingleton<HelpersCommon.Logger.ILogger, Logger>();

    var app = builder.Build();
    Logger.ErrorCriticalSync("Error during SetupHost", ex);
    app.Run(async (context) =>
    {
        var str = new StringBuilder();
        Logger.ErrorsInMemory.Select(item => item).Reverse().ToList().ForEach(c => str.AppendLine(c.Message));
        await context.Response.WriteAsync(str.ToString());
    });
    app.Run();
}

// to fix running startup during migrations-update
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration.ApplyConfiguration();
        var b = new DbContextOptionsBuilder<ApplicationDbContext>();
        b.UseSqlServer(builder.Configuration.GetSection("ConnectionStrings").GetValue<string>("DefaultConnection"));
        return new ApplicationDbContext(b.Options);
    }
}