using FS.Shared.Models.Controllers;
using HelpersCommon.FiltersAndAttributes;
using HelpersCommon.Logger;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using ILogger = HelpersCommon.Logger.ILogger;

namespace asp_web_api_template.Controllers
{
    public enum RoleEnum
    {
        SuperAdmin = 1,
        Admin,
    }

    [DiagAuthorize(RoleEnum.SuperAdmin, RoleEnum.Admin)]
    [ApiController]
    public class TestController : BaseController
    {
        private readonly ILogger _logger;

        public TestController(ILogger logger)
        {
            _logger = logger;
        }

        [HttpGet("api/diag/errors")]
        public string ErrorInMemoryGet([FromQuery] string key)
        {
            var log = Logger.ErrorsInMemory;
            if (log.Count == 1)
                return "No errors";

            var str = new StringBuilder();
            log.Select(item => item).Reverse().ToList().ForEach(x => str.AppendLine(x.Message));
            return str.ToString();
        }
    }
}