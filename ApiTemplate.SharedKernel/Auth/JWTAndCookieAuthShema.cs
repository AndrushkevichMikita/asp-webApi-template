using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace ApiTemplate.SharedKernel.Auth
{
    public static class JWTAndCookieAuthShema
    {
        public const string JWTWithNoExpirationSchema = nameof(JWTWithNoExpirationSchema);

        public static JwtSecurityToken CreateJWTToken(IConfiguration configuration, IEnumerable<Claim> claims)
           => new(issuer: configuration["Jwt:Issuer"],
                  audience: configuration["Jwt:Audience"],
                  claims: claims,
                  expires: DateTime.Now.AddMinutes(int.Parse(configuration["Jwt:LifetimeMinutes"])),
                  signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"])), SecurityAlgorithms.HmacSha256));

        public static TokenValidationParameters GetTokenValidationParameters(IConfiguration configuration)
            => new()
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidAudience = configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(configuration["Jwt:Key"]))
            };

        public static bool IsJWTProvided(HttpContext context, out string jwtHeader)
        {
            jwtHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            return jwtHeader?.StartsWith("Bearer ") == true;
        }

        public static IServiceCollection AddAndUseJWTSchemaIfTokenProvided(this IServiceCollection services, IConfiguration configuration, string schema)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultChallengeScheme = "smart";
                options.DefaultAuthenticateScheme = "smart";
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        return Task.CompletedTask;
                    },
                    OnForbidden = context =>
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                        return Task.CompletedTask;
                    }
                };

                options.TokenValidationParameters = GetTokenValidationParameters(configuration);
            })
            // Initially there was an idea to use custom policy to verify jwt token integrity + skip it's expiration time,
            // but due to limitation of asp net core (you can't use [AllowAnonymous] + [Authorize(Policy="SomePolicy")]) -> https://github.com/dotnet/aspnetcore/issues/29377
            .AddJwtBearer(JWTWithNoExpirationSchema, options =>
            {
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        return Task.CompletedTask;
                    },
                    OnForbidden = context =>
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                        return Task.CompletedTask;
                    }
                };

                var tokenValidationParameters = GetTokenValidationParameters(configuration);
                tokenValidationParameters.ValidateLifetime = false; // WARN: Since token can be already expired
                options.TokenValidationParameters = tokenValidationParameters;
            })
            .AddPolicyScheme("smart", "Smart Authentication", options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    if (IsJWTProvided(context, out string jwtHeader))
                    {
                        return JwtBearerDefaults.AuthenticationScheme;
                    }
                    else
                    {
                        return schema;
                    }
                };
            });

            return services;
        }
    }
}
