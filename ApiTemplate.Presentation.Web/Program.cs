using ApiTemplate.Application;
using ApiTemplate.Application.Configuration;
using ApiTemplate.Infrastructure;
using ApiTemplate.SharedKernel;
using ApiTemplate.SharedKernel.FiltersAndAttributes;
using ApiTemplate.SharedKernel.Extensions;
using ApiTemplate.SharedKernel.Scheduler;
using ApiTemplate.SharedKernel.ExceptionHandler;
using ApiTemplate.SharedKernel.PipelineExtensions;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;
using ApiTemplate.SharedKernel.CustomPolicy;

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
    // configure Serilog + Elasticsearch as sink for Serilog
    builder.Host.ConfigureSerilog(builder.Configuration);

    builder.Services.AddScheduler(new List<SchedulerItem>
    {
        // new SchedulerItem { TaskType = typeof(SomeName) }, // WARN: It's example of registration
    });

    builder.Services.Configure<SMTPSettings>(builder.Configuration.GetSection(SMTPSettings.SMTP));

    builder.Services.AddCommonServices();

    builder.Services.AddApplicationServices();

    builder.Services.AddInfrastructureServices(builder.Configuration);

    builder.Services.AddRouting(options => options.LowercaseUrls = true);

    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Version = "v1",
            Title = "API",
            Description = "An ASP.NET Core Web API",
            TermsOfService = new Uri("https://example.com/terms"),
            Contact = new OpenApiContact
            {
                Name = "Example Contact",
                Url = new Uri("https://example.com/contact")
            },
            License = new OpenApiLicense
            {
                Name = "Example License",
                Url = new Uri("https://example.com/license")
            }
        });
        var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
    });

    builder.Services.AddHealthChecks();

    builder.Services.AddResponseCompression(options =>
    {
        // it's risky for some attacks: https://docs.microsoft.com/en-us/aspnet/core/performance/response-compression?view=aspnetcore-3.0#compression-with-secure-protocol
        options.EnableForHttps = false;
    });

    builder.Services.AddSession();

    builder.Services.AddDistributedMemoryCache();

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

    builder.Services.AddHttpLogging(logging =>
    {
        logging.LoggingFields = HttpLoggingFields.All;
    });

    var app = builder.Build();

    app.UseHttpLogging();

    app.AddElasticApm(builder.Configuration);

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

    app.HandleExceptions();

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
                 //.RequireAuthorization(new AuthorizeAttribute()) // WARN: Enables global [Authorize] attribute for each controller
                 .RequireAuthorization(nameof(IsUserLockedAuthHandler));
    });

    await app.Services.ApplyDbMigrations(builder.Configuration);

    app.Run();
}
catch (Exception ex)
{
    var builder = WebApplication.CreateBuilder(args);
    var app = builder.Build();
    var logger = app.Services.GetRequiredService<ILogger<WebApplication>>();
    logger.LogCritical($"Failed to start {Assembly.GetExecutingAssembly().GetName().Name}", ex);
    app.Run(async (context) =>
    {
        await context.Response.WriteAsync(ex.Message + Environment.NewLine + ex.StackTrace);
    });
    app.Run();
}

/// <summary>
/// Make the implicit Program class public so test projects can access it
/// </summary>
public partial class Program { }