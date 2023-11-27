using HelpersCommon.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ILogger = HelpersCommon.Logger.ILogger;

namespace HelpersCommon.FiltersAndAttributes
{
    /// <summary>
    /// For anonym requests to be secure enough need to include encodedKey in header [Anonym]
    /// UI must encodeKey per each request (twice calls with the same key forbiden)
    /// If mobileTime <> serverTime more than 5minutes - returns forbidden
    /// </summary>
    public class SecureAllowAnonym : AuthorizeAttribute, IFilterFactory, IAllowAnonymous
    {
        public bool IsReusable => false;
        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            var attr = serviceProvider.GetService<SecureAllowAnonymousAttribute>();
            attr.Policy = Policy;
            attr.AuthenticationSchemes = AuthenticationSchemes;
            return attr;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class SecureAllowAnonymousAttribute : AuthorizeAttribute, IAuthorizationFilter, IAllowAnonymous
    {
        private static readonly FastCache<string, int> _cache = new();
        private readonly ILogger _logger;
        static int AnonymDiffMinutes;
        static int AnonymKey;

        public SecureAllowAnonymousAttribute(IConfiguration config, ILogger logger)
        {
            _logger = logger;
            if (AnonymKey == 0)
            {
                AnonymKey = config.GetValue<int>("AnonymKey");
                AnonymDiffMinutes = config.GetValue<int>("AnonymDiffMinutes");
            }
        }

        // encodeKey: (key: number, startYear = 2020): string => {
        //   const now = Date.now(); // must return UTC value
        //   const ms = now - new Date(startYear, 0, 1).valueOf(); // integer milliseconds as difference between Now and StartYear
        //   const random = btoa(Math.random().toString().substring(2)); // random integer string
        //   let s = "";
        //   const txt = ms.toString();
        //   const obfusKey = txt.charCodeAt(txt.length - 1) - 48; // required to change first numbers because these are not changable in general
        //   console.warn(obfusKey);
        //   for (let i = 0; i < txt.length; ++i) {
        //     const num = txt.charCodeAt(i) - 48 + (key % (i + 1));
        //     const c = num + 97 + obfusKey;
        //     s += String.fromCharCode(c);
        //     s += random[i];
        //   }

        //   let sum = key;
        //   for (let i = 0; i < s.length; ++i) {
        //     sum += s.charCodeAt(i);
        //   }
        //   return s + String.fromCharCode((sum % (90 - 65)) + 65); // get char between 65-90 >>> A-Z
        // },

        public static DateTime DecodeTime(string queryKey, int hashKey, int? startYear = 2020)
        {
            var sum = hashKey;
            for (short i = 0; i < queryKey.Length - 1; ++i)
                sum += queryKey[i];

            var exp = sum % (90 - 65) + 65;
            if (exp != queryKey[^1])
                return DateTime.MinValue; // last symbol is checksum; if not valid than return false

            long ms = 0;
            var lastNum = queryKey.Length - 3;
            var obfusKey = (queryKey[lastNum] - 97 - hashKey % (lastNum / 2 + 1)) / 2; // required to change first numbers because these are not changable in general
            for (var (i, n) = (0, 0); i < queryKey.Length - 1; i += 2, ++n)
            {
                var num = queryKey[i] - 97 - hashKey % (n + 1) - obfusKey;
                ms = ms * 10 + num;
            }

            var dt = new DateTime(startYear.Value, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(ms);
            return dt;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (context?.HttpContext == null)
                return;
#if DEBUG
            return; // allow skip Anonym key validation in DEBUG
#endif
#pragma warning disable CS0162 // Unreachable code detected
            if (!context.HttpContext.Request.Headers.TryGetValue("Anonym", out var key))
                _logger.AddError($"Header [Anonym] is missed. Request rejected");
            else
            {
                var extractedTime = DecodeTime(key, AnonymKey);
                var timeDiffMinutes = Math.Abs((extractedTime - DateTime.UtcNow).TotalMinutes);
                var isValid = timeDiffMinutes < AnonymDiffMinutes;
                if (isValid)
                {
                    var alreadyInCache = !_cache.TryAdd(key, value: 0, ttl: TimeSpan.FromMinutes(AnonymDiffMinutes));
                    if (!alreadyInCache)
                        return;

                    _logger.AddError($"Header [Anonym]:'{key}' used earlier. ExtractedUtcTime: {extractedTime.ToString("o")}, timeDiffMinutes: {timeDiffMinutes} > ${AnonymDiffMinutes}");
                }
                else
                    _logger.AddError($"Header [Anonym]:'{key}' invalid. ExtractedUtcTime: {extractedTime.ToString("o")}, timeDiffMinutes: {timeDiffMinutes} > ${AnonymDiffMinutes}");
            }
#pragma warning restore CS0162 // Unreachable code detected

            context.Result = new ForbidResult();
        }
    }
}
