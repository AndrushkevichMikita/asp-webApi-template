using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ApiTemplate.SharedKernel
{
    public class CookieUser<T> where T : Enum
    {
        public int Id { get; set; }
        public T Role { get; set; }
        public string Email { get; set; }
        public DateTime? LockoutEndDate { get; set; }
    }

    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public abstract class BaseController<T> : ControllerBase where T : Enum
    {
        private CookieUser<T> _user;

        public CookieUser<T> CurrentUser
        {
            get
            {
                if (_user is null)
                {
                    var i = (User?.Identity as ClaimsIdentity).Claims;

                    var id = i.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
                    var role = i.FirstOrDefault(x => x.Type == ClaimTypes.Role)?.Value;
                    var email = i.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value;
                    var lockoutEndDate = i.FirstOrDefault(x => x.Type == ClaimTypes.AuthorizationDecision)?.Value;

                    if (!string.IsNullOrWhiteSpace(id))
                        _user = new CookieUser<T>
                        {
                            Email = email!,
                            Id = int.Parse(id),
                            Role = (T)Enum.Parse(typeof(T), role!),
                            LockoutEndDate = string.IsNullOrWhiteSpace(lockoutEndDate) ? null : DateTimeOffset.Parse(lockoutEndDate).UtcDateTime,
                        };
                }
                return _user;
            }
        }
    }
}
