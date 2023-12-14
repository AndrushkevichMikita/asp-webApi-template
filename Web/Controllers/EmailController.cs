using ApplicationCore.Interfaces;
using ApplicationCore.Models;
using FS.Shared.Models.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace asp_web_api_template.Controllers
{
    public class EmailController : BaseController
    {
        private readonly IAccountService _account;

        public EmailController(IAccountService account)
        {
            _account = account;
        }

        [AllowAnonymous]
        [HttpPost("api/email/digitCode")]
        public async Task SignIn(AccountModel model)
            => await _account.SignIn(model);
    }
}
