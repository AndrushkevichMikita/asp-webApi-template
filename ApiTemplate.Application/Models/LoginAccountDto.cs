using System.ComponentModel.DataAnnotations;

namespace ApiTemplate.Application.Models
{
    public class LoginAccountDto
    {
        public string Email { get; set; }

        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }
}
