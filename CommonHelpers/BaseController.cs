using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CommonHelpers
{
    public class CookieUser<T> where T : struct
    {
        public int Id { get; set; }
        public T Role { get; set; }
        public string Email { get; set; }
    }

    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public abstract class BaseController<T> : ControllerBase where T : struct
    {
        private CookieUser<T>? _user;
        /// <summary>
        /// Return current authenticated user
        /// </summary>
        public CookieUser<T>? CurrentUser
        {
            get
            {
                if (_user is null)
                {
                    var i = (User?.Identity as ClaimsIdentity).Claims;

                    var id = i.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
                    var role = i.FirstOrDefault(x => x.Type == ClaimTypes.Role)?.Value;
                    var email = i.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value;

                    if (!string.IsNullOrWhiteSpace(id))
                        _user = new CookieUser<T>
                        {
                            Email = email!,
                            Id = int.Parse(id),
                            Role = Enum.Parse<T>(role!),
                        };
                }

                return _user;
            }
        }
    }
}
