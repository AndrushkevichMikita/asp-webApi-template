using ApplicationCore.Entities;
using ApplicationCore.Interfaces;
using ApplicationCore.Models;
using CommonHelpers;
using HelpersCommon.FiltersAndAttributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace asp_web_api_template.Controllers
{
    public class AccountController : BaseController<RoleEnum>
    {
        private readonly IAccountService _account;

        public AccountController(IAccountService account)
        {
            _account = account;
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
        [AllowAnonymous]
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
