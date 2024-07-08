using ApplicationCore.Entities;
using ApplicationCore.Interfaces;
using ApplicationCore.Models;
using ApplicationCore.Services;
using CommonHelpers;
using HelpersCommon.FiltersAndAttributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace asp_web_api_template.Controllers
{
    public class AccountController : BaseController<RoleEnum>
    {
        private readonly IAccountService _account;
        private readonly ApplicationSignInManager _applicationSignInManager;

        public AccountController(IAccountService account, ApplicationSignInManager applicationSignInManager)
        {
            _account = account;
            _applicationSignInManager = applicationSignInManager;
        }

        [AllowAnonymous]
        [HttpPost("refreshToken")]
        public async Task<IActionResult> RefreshToken([FromServices] IConfiguration configuration, [FromBody] RefreshTokenDtoModel model)
        {
            var tokenValidationParameters = ApplicationSignInManager.GetTokenValidationParameters(configuration);
            tokenValidationParameters.ValidateLifetime = false; // WARN: Since token can be already expired

            var principal = new JwtSecurityTokenHandler().ValidateToken(model.Token, tokenValidationParameters, out SecurityToken securityToken);
            if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return Unauthorized();
            }

            var username = principal.Identity.Name;
            var user = await _applicationSignInManager.UserManager.FindByNameAsync(username);

            if (user == null || user.RefreshToken != model.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.Now)
            {
                return Unauthorized();
            }

            var newToken = await _applicationSignInManager.GenerateJwtTokenAsync(user);
            var newRefreshToken = await _applicationSignInManager.GenerateRefreshTokenAsync(user);
            return Ok(new { token = newToken, refreshToken = newRefreshToken });
        }

        [AllowAnonymous]
        [HttpPost("signIn")]
        public async Task SignIn(AccountSignInDto model)
            => await _account.SignIn(model);

        [AllowAnonymous]
        [HttpPost("digitCode")]
        public async Task SendDigitCodeByEmail([FromBody] string email)
            => await _account.SendDigitCodeByEmail(email);

        [AllowAnonymous]
        [HttpPut("digitCode")]
        public async Task ConfirmDigitCode([FromBody] string code)
            => await _account.ConfirmDigitCode(code);

        [AllowAnonymous]
        [HttpPost("signUp")]
        public async Task SignUp(AccountSignInDto model)
            => await _account.SignUp(model);

        /// <summary>
        /// Signs the current user out of the application.
        /// </summary>
        /// <returns></returns>
        [HttpPost("signOut")]
        public new async Task<SignOutResult> SignOut()
        {
            base.SignOut();
            await _account.SignOut();
            return new SignOutResult();
        }

        [AuthorizeRoles(RoleEnum.SuperAdmin)]
        [HttpPost("onlyForSupAdmin")]
        public IActionResult AllowOnlyForSupAdmin()
            => Ok();

        [HttpGet("authorize")]
        public IActionResult CheckAuthorization()
        {
            var r = User.IsInRole(RoleEnum.SuperAdmin.ToString());
            return Ok(User.Claims.Select(c => c.Value).ToList());
        }

        /// <summary>
        /// Get current authenticated user
        /// </summary>
        /// <returns></returns>
        [HttpGet()]
        public async Task<AccountBaseDto> GetCurrent()
            => await _account.GetCurrent(CurrentUser.Id);

        /// <summary>
        /// Delete user if password verification is successful
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        [HttpDelete()]
        public async Task Delete([FromBody] string password)
            => await _account.Delete(password, CurrentUser.Id);
    }
}
