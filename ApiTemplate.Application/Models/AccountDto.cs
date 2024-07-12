using ApiTemplate.Application.Entities;

namespace ApiTemplate.Application.Models
{
    public class AccountDto
    {
        public int Id { get; set; }

        public string Email { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public RoleEnum Role { get; set; }

        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }
}
