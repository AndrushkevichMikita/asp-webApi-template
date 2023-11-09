using HelpersCommon.ExceptionHandler;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HelpersCommon.FiltersAndAttributes
{
    /// <summary>
    /// Allow requests for authorised user with specific roles 
    /// OR with extra parameter in query http://{domain}/route?key={DiagAuthorizeKey}
    /// where {DiagAuthorizeKey} see in appsettings.json
    /// </summary>
    public class DiagAuthorizeAttribute : AuthorizeAttribute, IFilterFactory, IAllowAnonymous
    {
        public bool IsReusable => false;

        public DiagAuthorizeAttribute(params object[] roles)
        {
            if (roles.Any(r => r.GetType().BaseType != typeof(Enum)))
                throw new MyApplicationException(ErrorStatus.InvalidData, "Should take enum");

            Roles = string.Join(",", roles.Select(r => Enum.GetName(r.GetType(), r)));
        }

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            var attr = serviceProvider.GetService<DiagAuthorizeFactoryAttribute>();
            attr.Roles = Roles;
            attr.Policy = Policy;
            attr.AuthenticationSchemes = AuthenticationSchemes;

            return attr;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class DiagAuthorizeFactoryAttribute : AuthorizeAttribute, IAuthorizationFilter, IAllowAnonymous
    {
        public string FallbackQueryKey { get; set; }

        public DiagAuthorizeFactoryAttribute(IConfiguration config)
        {
            FallbackQueryKey = config.GetSection("DiagAuthorizeKey").Value;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (context?.HttpContext == null)
                return;

            if (!string.IsNullOrEmpty(FallbackQueryKey))
            {
                context.HttpContext.Request.Query.TryGetValue("key", out var key);
                if (key.ToString() == FallbackQueryKey)
                    return;
            }
#if DEBUG
            else // allow anonymous in DEBUG
                return;
#endif
            if (!string.IsNullOrEmpty(Roles))
            {
                var arr = Roles.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                var isOk = arr.Any(r => context.HttpContext.User.IsInRole(r));
                if (isOk)
                    return;
            }

            if (!context.HttpContext.User.Identity.IsAuthenticated)
                context.Result = new UnauthorizedObjectResult(null);
            else
                context.Result = new ForbidResult();
        }
    }
}
