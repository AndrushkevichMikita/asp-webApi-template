using asp_web_api_template.Data;
using FS.Shared.Models.Controllers;
using HelpersCommon.ExceptionHandler;
using HelpersCommon.FiltersAndAttributes;
using HelpersCommon.Logger;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text;

namespace asp_web_api_template.Controllers
{
    [ApiController]
    public class TestController : BaseController
    {
        [AllowAnonymous]
        [HttpPost("api/user/signUp")]
        public async Task SignIn([FromQuery] bool isSupAdmin,
            [FromServices] UserManager<TestIdentityUser> manager,
            [FromServices] RoleManager<IdentityRole<int>> roleManager,
            [FromServices] SignInManager<TestIdentityUser> signManager)
        {
            var toDelete = await manager.Users.FirstOrDefaultAsync();
            if (toDelete is not null) await manager.DeleteAsync(toDelete);

            var toInsert = new TestIdentityUser
            {
                Email = "test@gmail.com",
                Role = isSupAdmin ? RoleEnum.SuperAdmin : RoleEnum.Admin,
                UserName = "test" + new Random().Next().ToString(),
            };

            await roleManager.CreateAsync(new IdentityRole<int>
            {
                Id = (int)RoleEnum.SuperAdmin,
                Name = RoleEnum.SuperAdmin.ToString()
            });
            await roleManager.CreateAsync(new IdentityRole<int>
            {
                Id = (int)RoleEnum.Admin,
                Name = RoleEnum.Admin.ToString()
            });

            await manager.CreateAsync(toInsert);
            await manager.AddToRoleAsync(toInsert, toInsert.Role.ToString());
            var i = await manager.FindByEmailAsync("test@gmail.com");
            await signManager.SignInAsync(i, false);
        }

        [AuthorizeRoles(RoleEnum.SuperAdmin)]
        [HttpPost("api/user/onlyForSupAdmin")]
        public IActionResult AllowOnlyForSupAdmin()
        {
            return Ok();
        }

        [HttpPost("api/user/signOut")]
        public async Task SignOut([FromServices] SignInManager<TestIdentityUser> s)
        {
            await s.SignOutAsync();
        }

        [HttpPost("api/user/authorize")]
        public IActionResult CheckAuthorization()
        {
            var r = User.IsInRole(RoleEnum.SuperAdmin.ToString());
            return Ok(User.Claims.Select(c => c.Value).ToList());
        }

        [AllowAnonymous]
        [HttpPost("api/diag/errors")]
        public void CheckError()
        {
            throw new MyApplicationException(ErrorStatus.InvalidData, "Invalid Data");
        }

        [AllowAnonymous]
        [HttpGet("api/diag/errors")]
        public string ErrorInMemoryGet()
        {
            var log = Logger.ErrorsInMemory;
            if (log.Count < 1)
                return "No errors";

            var str = new StringBuilder();
            log.Select(item => item).Reverse().ToList().ForEach(x => str.AppendLine(x.Message));
            return str.ToString();
        }
    }
}