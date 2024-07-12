using ApiTemplate.SharedKernel.ExceptionHandler;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections;
using System.Linq;

namespace ApiTemplate.SharedKernel.FiltersAndAttributes
{
    /// <summary>
    /// works globally
    /// /// </summary>
    public class ModelEnumValidatorAttribute : ActionFilterAttribute
    {

        private static void ErrorIfEnumInvalid(Type t, object value, string propName)
        {
            if (value is null || !Enum.IsDefined(t, value))
            {
                var dict = Enum.GetValues(t)
                               .Cast<object>()
                               .ToDictionary(t => (int)t, t => t.ToString());

                var message = string.Join(", ", dict.Select(x => x.Key + ": " + x.Value));
                throw new MyApplicationException(ErrorStatus.InvalidData, $"Pointed value '{value ?? "null"}' invalid for '{propName[(propName.IndexOf("+") + 1)..]}'. Valid values : {message}");
            }
        }

        /// <summary>
        /// Validate model for invalid enum
        /// </summary>
        private static void ValidateModel(object t, string propName)
        {
            if (t is null)
                return;

            var curType = t.GetType();
            var isString = curType == typeof(string);
            var isArray = (curType.IsArray || typeof(IEnumerable).IsAssignableFrom(curType)) && !isString;
            if (isArray)
                curType = curType.GetElementType() ?? curType.GetGenericArguments().Single();

            if (curType is null) // case possible for empty array/list/ienumerable
                return;

            curType = Nullable.GetUnderlyingType(curType) ?? curType;

            if (curType.Name.Contains("FormFile")) // skip form files (FormFile & IFromFile)
                return;

            if (curType.IsEnum)
            {
                if (isArray)
                    foreach (var x in ((IEnumerable)t).Cast<object>())
                        ErrorIfEnumInvalid(curType, x, propName);
                else
                    ErrorIfEnumInvalid(curType, t, propName);
            }
            else if (curType is object && curType != typeof(string) && curType != typeof(DateTime))
            {
                var props = curType.GetProperties();
                if (isArray)
                {
                    var arr = ((IEnumerable)t).Cast<object>().ToArray();
                    for (int i = 0; i < props.Length; i++)
                    {
                        for (int k = 0; k < arr.Length; ++k)
                        {
                            var propertyValue = props[i].GetValue(arr[k]);
                            ValidateModel(propertyValue, props[i].Name);
                        }
                    }
                }
                else
                    foreach (var x in props)
                    {
                        var propertyValue = x.GetValue(t);
                        ValidateModel(propertyValue, x.Name);
                    }
            }
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // analyze empty query params. In case if type is not nullalbe and value is null => throw error
            if (context.ActionArguments.Count != context.ActionDescriptor.Parameters.Count)
            {
                foreach (var p in context.ActionDescriptor.Parameters)
                {
                    if (!context.ActionArguments.TryGetValue(p.Name, out object val))
                    {
                        var curType = p.ParameterType;
                        if (Nullable.GetUnderlyingType(curType) is null && curType != typeof(string))
                            throw new MyApplicationException(ErrorStatus.InvalidData, $"Query param '{p.Name}' is required");
                    }
                }
            }
            foreach (var pair in context.ActionArguments)
            {
                var v = context.ActionDescriptor.Parameters.FirstOrDefault(x => x.Name == pair.Key);
                if (v is not null && v.BindingInfo?.BindingSource?.Id == "Services") continue;
                ValidateModel(pair.Value, pair.Value?.ToString());
            }
        }
    }

    public class ModelStateValidatorConvension : IApplicationModelConvention
    {
        public void Apply(ApplicationModel application)
        {
            foreach (var controllerModel in application.Controllers)
            {
                controllerModel.Filters.Add(new ModelEnumValidatorAttribute());
            }
        }
    }
}
