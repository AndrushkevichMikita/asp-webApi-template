using Microsoft.AspNetCore.Identity;

namespace ApplicationCore.Entities
{
    public class UserEntity : IdentityUser<int>
    {
        public DateTime Created { get; set; }
        public DateTime LastUpdated { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public RoleEnum Role { get; set; }
        public ICollection<IdentityUserTokenEntity> Tokens { get; set; }
        public bool CheckLocked() => LockoutEnabled && LockoutEnd?.UtcDateTime > DateTime.UtcNow;
    }

    public enum RoleEnum
    {
        SuperAdmin = 1,
        Admin,
    }
}
