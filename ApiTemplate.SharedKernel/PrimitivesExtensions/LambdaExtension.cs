using System.Linq.Expressions;
using System.Reflection;

namespace ApiTemplate.SharedKernel.PrimitivesExtensions
{
    public static class LambdaExtension
    {
        // from https://gist.github.com/sandord/400553/6562ebb3cf2767d6c1ad9474d6f04691ab6ca412
        public static string GetMemberName<T>(Expression<Func<T, object>> property)
        {
            MemberExpression memberExpression;
            LambdaExpression lambda = property;

            if (lambda.Body is UnaryExpression)
                memberExpression = (MemberExpression)(((UnaryExpression)lambda.Body).Operand);
            else
                memberExpression = (MemberExpression)lambda.Body;

            return ((PropertyInfo)memberExpression.Member).Name;
        }
    }
}
