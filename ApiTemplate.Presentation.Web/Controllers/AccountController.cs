using ApiTemplate.Application.Entities;
using ApiTemplate.Application.Interfaces;
using ApiTemplate.Application.Models;
using ApiTemplate.Application.Services;
using ApiTemplate.SharedKernel;
using ApiTemplate.SharedKernel.Auth;
using ApiTemplate.SharedKernel.FiltersAndAttributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiTemplate.Presentation.Web.Controllers
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

        [Authorize(AuthenticationSchemes = JWTAndCookieAuthShema.JWTWithNoExpirationSchema)]
        [HttpPost("refreshToken")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDtoModel model)
        {
            var user = await _applicationSignInManager.UserManager.FindByIdAsync(CurrentUser.Id.ToString());

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
        public async Task<IActionResult> SignIn(AccountSignInDto model)
        {
            var (token, refreshToken) = await _account.SignIn(model);
            return Ok(new { token, refreshToken });
        }

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
