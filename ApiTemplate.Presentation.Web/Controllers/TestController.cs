using ApiTemplate.Application.Entities;
using ApiTemplate.SharedKernel;
using ApiTemplate.SharedKernel.ExceptionHandler;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiTemplate.Presentation.Web.Controllers
{
    [ApiController]
    public class TestController : BaseController<RoleEnum>
    {
        [AllowAnonymous]
        [HttpPost("/fireError")]
        public void FireError()
        {
            throw new MyApplicationException(ErrorStatus.NotFound, "Invalid Data");
        }
    }
}