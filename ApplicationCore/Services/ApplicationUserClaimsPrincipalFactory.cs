using ApplicationCore.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace ApplicationCore.Services
{
    public class ApplicationUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUserEntity, IdentityRole<int>>
    {
        public ApplicationUserClaimsPrincipalFactory(
            UserManager<ApplicationUserEntity> userManager,
            RoleManager<IdentityRole<int>> roleManager,
            IOptions<IdentityOptions> optionsAccessor)
            : base(userManager, roleManager, optionsAccessor) { }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUserEntity user)
          => await GenerateAdjustedClaimsAsync(user);

        public async Task<ClaimsIdentity> GenerateAdjustedClaimsAsync(ApplicationUserEntity user)
        {
            var identity = await base.GenerateClaimsAsync(user);
            identity.AddClaim(new Claim(ClaimTypes.AuthorizationDecision, user.LockoutEnabled ? (user.LockoutEnd?.UtcDateTime)?.ToString() ?? "" : ""));
            return identity;
        }
    }
}
