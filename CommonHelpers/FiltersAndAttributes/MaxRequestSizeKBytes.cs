using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;

namespace HelpersCommon.FiltersAndAttributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class MaxRequestSizeKBytes : Attribute, IResourceFilter
    {
        public int MaxLengthInKBytes { get; set; }

        public MaxRequestSizeKBytes(int MaxLengthInKBytes)
        {
            this.MaxLengthInKBytes = MaxLengthInKBytes;
        }

        /// <summary>
        /// WARN: If apply globally, as "config.Filters.Add(new MaxRequestSizeBytes(28_766_347)); // singleton",
        /// singleton rule will always apply first, so attribute per route controller will trigger last.
        /// In these scenario MaxLengthInBytes restriction in singleton must be grater than per controller,
        /// to allow the controller to provolute one more time with lesser MaxLengthInBytes
        /// </summary>
        /// <param name="context"></param>
        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            var toLargeRequest = (context.HttpContext.Request.ContentLength / 1024) > MaxLengthInKBytes;
            if (toLargeRequest)
                context.Result = new ContentResult
                {
                    StatusCode = 413,
                    ContentType = "application/json",
                    Content = "{\"errorMessage\":\"The request size is more than " + MaxLengthInKBytes + "kB\"}",
                };
        }

        public void OnResourceExecuted(ResourceExecutedContext context)
        {
        }
    }
}
