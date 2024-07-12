using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using ApiTemplate.Application.Entities;
using Microsoft.Extensions.Configuration;
using ApiTemplate.SharedKernel.Auth;

namespace ApiTemplate.Application.Services
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

        public async Task<string> GenerateJwtTokenAsync(ApplicationUserEntity user)
        {
            var claims = await _applicationUserClaimsPrincipalFactory.GenerateAdjustedClaimsAsync(user);
            var token = JWTAndCookieAuthShema.CreateJWTToken(_configuration, claims.Claims);
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
