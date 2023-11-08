using HelpersCommon.ExceptionHandler;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HelpersCommon.ControllerExtensions
{
    public class MinPermissionRequirement : IAuthorizationRequirement { }

    public class MinPermissionHandler : AuthorizationHandler<MinPermissionRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, MinPermissionRequirement requirement)
        {
            //check if locked
            var subClaimLock = context.User?.Claims?.FirstOrDefault(x => x.Type == ClaimTypes.AuthorizationDecision);
            var subClaimId = context.User?.Claims?.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
            _ = int.TryParse(subClaimId?.Value, out int userId);
            _ = bool.TryParse(subClaimLock?.Value, out bool isLocked);

            if (subClaimLock != null && isLocked == true || LockedUsers.Users.TryGetValue(userId, out int id) && subClaimId != null)
                throw new MyApplicationException(ErrorStatus.Forbidden, "User is locked");

            context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }

    public static class LockedUsers
    {
        public static ConcurrentDictionary<int, int> Users = new();
    }
}
