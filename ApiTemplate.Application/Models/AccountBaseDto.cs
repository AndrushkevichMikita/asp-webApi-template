using ApiTemplate.Application.Entities;
using System.ComponentModel.DataAnnotations;

namespace ApiTemplate.Application.Models
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
