using System.ComponentModel.DataAnnotations;

namespace ApiTemplate.Application.Models
{
    public class AccountSignInDto : AccountBaseDto
    {
        [Required]
        public string Password { get; set; }

        public bool? RememberMe { get; set; }
    }
}
