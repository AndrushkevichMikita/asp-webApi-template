using ApplicationCore.Entities;
using ApplicationCore.Interfaces;
using ApplicationCore.Models;
using FS.Shared.Models.Controllers;
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
        [HttpPost("api/account/digitCode")]
        public async Task SendDigitCodeByEmail([FromBody] string email)
            => await _account.SendDigitCodeByEmail(email);

        [AllowAnonymous]
        [HttpPost("api/account/signUp")]
        public async Task SignUp(AccountModel model)
            => await _account.SignUp(model);

        [HttpPost("api/account/signOut")]
        public async Task SignOut([FromServices] SignInManager<UserEntity> s)
        {
            await s.SignOutAsync();
        }

        [AuthorizeRoles(RoleEnum.SuperAdmin)]
        [HttpPost("api/account/onlyForSupAdmin")]
        public IActionResult AllowOnlyForSupAdmin()
        {
            return Ok();
        }

        [HttpGet("api/account/authorize")]
        public IActionResult CheckAuthorization()
        {
            var r = User.IsInRole(RoleEnum.SuperAdmin.ToString());
            return Ok(User.Claims.Select(c => c.Value).ToList());
        }
    }
}
