using ApiTemplate.Application.Entities;
using System.ComponentModel.DataAnnotations;

namespace ApiTemplate.Presentation.Web.Models
{
    public class LoginAccountModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        public bool? RememberMe { get; set; } = false;
    }
}
