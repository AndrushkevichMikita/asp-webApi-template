using ApiTemplate.Application.Entities;
using System.ComponentModel.DataAnnotations;

namespace ApiTemplate.Presentation.Web.Models
{
    public class AccountModel
    {
        [Required]
        public int Id { get; set; }

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
