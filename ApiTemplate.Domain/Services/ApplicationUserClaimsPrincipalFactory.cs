using ApiTemplate.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace ApiTemplate.Domain.Services
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
            if (user.LockoutEnabled && user.LockoutEnd.HasValue)
                identity.AddClaim(new Claim(ClaimTypes.AuthorizationDecision, user.LockoutEnd.Value.UtcDateTime.ToString()));

            return identity;
        }
    }
}
