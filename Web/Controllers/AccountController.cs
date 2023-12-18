using ApplicationCore.Entities;
using ApplicationCore.Interfaces;
using ApplicationCore.Models;
using CommonHelpers;
using HelpersCommon.FiltersAndAttributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace asp_web_api_template.Controllers
{
    [ApiController]
    public class AccountController : BaseController
    {
        private readonly IAccountService _account;

        public AccountController(IAccountService account)
        {
            _account = account;
        }

        [AllowAnonymous]
        [HttpPost("api/account/signIn")]
        public async Task SignIn(AccountModel model)
            => await _account.SignIn(model);

        [AllowAnonymous]
        [HttpGet("api/account/digitCode")]
        public async Task SendDigitCodeByEmail(string email)
            => await _account.SendDigitCodeByEmail(email);

        [AllowAnonymous]
        [HttpPost("api/account/digitCode")]
        public async Task ConfirmDigitCode([FromBody] string code)
            => await _account.ConfirmDigitCode(code);

        [AllowAnonymous]
        [HttpPost("api/account/signUp")]
        public async Task SignUp(AccountModel model)
            => await _account.SignUp(model);

        [HttpPost("api/account/signOut")]
        public async Task SignOut([FromServices] SignInManager<UserEntity> s)
            => await s.SignOutAsync();

        [AuthorizeRoles(RoleEnum.SuperAdmin)]
        [HttpPost("api/account/onlyForSupAdmin")]
        public IActionResult AllowOnlyForSupAdmin()
            => Ok();

        [HttpGet("api/account/authorize")]
        public IActionResult CheckAuthorization()
        {
            var r = User.IsInRole(RoleEnum.SuperAdmin.ToString());
            return Ok(User.Claims.Select(c => c.Value).ToList());
        }
    }
}
