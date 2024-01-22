using ApplicationCore.Entities;
using CommonHelpers;
using HelpersCommon.ExceptionHandler;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace asp_web_api_template.Controllers
{
    [ApiController]
    public class TestController : BaseController<RoleEnum>
    {
        [AllowAnonymous]
        [HttpPost("api/diag/errors")]
        public void FireError()
        {
            throw new MyApplicationException(ErrorStatus.NotFound, "Invalid Data");
        }
    }
}