using System.ComponentModel.DataAnnotations;

namespace ApplicationCore.Models
{
    public class AccountSignInDto : AccountBaseDto
    {
        [Required]
        public string Password { get; set; }

        public bool? RememberMe { get; set; }
    }
}
