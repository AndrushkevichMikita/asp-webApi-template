using HelpersCommon.ExceptionHandler;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace HelpersCommon.ControllerExtensions
{
    public class UserNotLockedRequirement : IAuthorizationRequirement { }

    public class IsUserLockedAuthHandler : AuthorizationHandler<UserNotLockedRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, UserNotLockedRequirement requirement)
        {
            //check if locked
            var subClaimLock = context.User?.Claims?.FirstOrDefault(x => x.Type == ClaimTypes.AuthorizationDecision);
            var subClaimId = context.User?.Claims?.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
            _ = int.TryParse(subClaimId?.Value, out int userId);
            _ = DateTimeOffset.TryParse(subClaimLock?.Value, out DateTimeOffset lockoutEndDate);

            if (subClaimLock != null && lockoutEndDate.UtcDateTime > DateTime.UtcNow || LockedUsers.Users.TryGetValue(userId, out int id) && subClaimId != null)
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
