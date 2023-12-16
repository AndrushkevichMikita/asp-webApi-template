using ApplicationCore.Entities;
using System.ComponentModel.DataAnnotations;

namespace ApplicationCore.Models
{
    public class AccountModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        public RoleEnum Role { get; set; }
        public bool? RememberMe { get; set; }
    }
}
