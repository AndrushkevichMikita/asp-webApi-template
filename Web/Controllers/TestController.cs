using CommonHelpers;
using HelpersCommon.ExceptionHandler;
using HelpersCommon.Logger;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Text;

namespace asp_web_api_template.Controllers
{
    [ApiController]
    public class TestController : BaseController
    {
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