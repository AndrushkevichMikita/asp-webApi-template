using ApiTemplate.Domain.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ApiTemplate.Domain.Services
{
    public class ApplicationSignInManager : SignInManager<ApplicationUserEntity>
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUserEntity> _userManager;
        private readonly ApplicationUserClaimsPrincipalFactory _applicationUserClaimsPrincipalFactory;

        public ApplicationSignInManager(UserManager<ApplicationUserEntity> userManager,
            IHttpContextAccessor contextAccessor,
            IUserClaimsPrincipalFactory<ApplicationUserEntity> claimsFactory,
            IConfiguration configuration,
            IOptions<IdentityOptions> optionsAccessor,
            ILogger<SignInManager<ApplicationUserEntity>> logger,
            IAuthenticationSchemeProvider schemes,
            IUserConfirmation<ApplicationUserEntity> confirmation,
            ApplicationUserClaimsPrincipalFactory applicationUserClaimsPrincipalFactory)
            : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
        {
            _userManager = userManager;
            _configuration = configuration;
            _applicationUserClaimsPrincipalFactory = applicationUserClaimsPrincipalFactory;
        }

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

        public static JwtSecurityToken CreateJWTToken(IConfiguration configuration, IEnumerable<Claim> claims)
          => new(issuer: configuration["Jwt:Issuer"],
                 audience: configuration["Jwt:Audience"],
                 claims: claims,
                 expires: DateTime.Now.AddMinutes(int.Parse(configuration["Jwt:LifetimeMinutes"])),
                 signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"])), SecurityAlgorithms.HmacSha256));

        public async Task<string> GenerateJwtTokenAsync(ApplicationUserEntity user)
        {
            var claims = await _applicationUserClaimsPrincipalFactory.GenerateAdjustedClaimsAsync(user);
            var token = CreateJWTToken(_configuration, claims.Claims);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<string> GenerateRefreshTokenAsync(ApplicationUserEntity user)
        {
            var refreshToken = Guid.NewGuid().ToString();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.Now.AddDays(int.Parse(_configuration["Jwt:RefreshTokenExpiryDays"]));
            await _userManager.UpdateAsync(user);

            return refreshToken;
        }
    }
}
