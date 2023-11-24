using FS.Shared.Models.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace asp_web_api_template.Controllers
{
    [ApiController]
    public class AccountController : BaseController
    {
        [AllowAnonymous]
        [HttpPost("api/account/signUp")]
        public IActionResult SignUp()
        {
            return Ok();
        }
    }
}
