using ApplicationCore.Entities;
using System.ComponentModel.DataAnnotations;

namespace ApplicationCore.Models
{
    public class AccountBaseDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public RoleEnum Role { get; set; }
    }
}
