using ApiTemplate.SharedKernel.ExceptionHandler;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;

namespace ApiTemplate.SharedKernel.FiltersAndAttributes
{
    public class AuthorizeRoles : AuthorizeAttribute
    {
        public AuthorizeRoles(params object[] roles)
        {
            if (roles.Any(r => r.GetType().BaseType != typeof(Enum)))
                throw new MyApplicationException(ErrorStatus.InvalidData, "AuthorizeRoles should take enum");

            Roles = string.Join(",", roles.Select(r => Enum.GetName(r.GetType(), r)));
        }
    }
}