using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using ApplicationCore.Entities;
using Microsoft.Extensions.Configuration;

namespace ApplicationCore.Services
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
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = await _applicationUserClaimsPrincipalFactory.GenerateAdjustedClaimsAsync(user);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims.Claims,
                expires: DateTime.Now.AddMinutes(int.Parse(_configuration["Jwt:LifetimeMinutes"])),
                signingCredentials: creds);

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
