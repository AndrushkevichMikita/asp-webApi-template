using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FS.Shared.Models.Controllers
{
    public class CookieUser
    {
        public int Id { get; set; }
    }

    public class BaseController : ControllerBase
    {
        private CookieUser? _user;

        public CookieUser CurrentUser
        {
            get
            {
                if (_user is not null)
                    return _user;

                _user = new CookieUser
                {
                    Id = Convert.ToInt32(User?.Claims?.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value ?? "0"),
                };
                return _user;
            }
        }
    }
}
