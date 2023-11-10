using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace asp_web_api_template.Data
{
    public class TestIdentityUser : IdentityUser<int>
    {
        public RoleEnum Role { get; set; }
        public bool CheckLocked() => LockoutEnabled && LockoutEnd?.UtcDateTime > DateTime.UtcNow;
    }

    public enum RoleEnum
    {
        SuperAdmin = 1,
        Admin,
    }

    public class ApplicationDbContext : IdentityDbContext<TestIdentityUser,
                                                          IdentityRole<int>,
                                                          int,
                                                          IdentityUserClaim<int>,
                                                          IdentityUserRole<int>,
                                                          IdentityUserLogin<int>,
                                                          IdentityRoleClaim<int>,
                                                          IdentityUserToken<int>>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
    }
}