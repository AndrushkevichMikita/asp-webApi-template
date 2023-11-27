using Microsoft.AspNetCore.Identity;

namespace ApplicationCore.Entities
{
    public class UserEntity : IdentityUser<int>
    {
        public RoleEnum Role { get; set; }
        public bool CheckLocked() => LockoutEnabled && LockoutEnd?.UtcDateTime > DateTime.UtcNow;
    }

    public enum RoleEnum
    {
        SuperAdmin = 1,
        Admin,
    }
}
