using System.Linq.Expressions;

namespace Quacklibs.AzureDevopsCli.Core.Behavior
{
    public static class DefaultParmeters
    {
        public static TProp GetSettingSafe<T, TProp>(this T target, Expression<Func<T, TProp>> propertySelector)
        {
            string selectedPropertyName = "";

            if (propertySelector.Body is UnaryExpression unary && unary.Operand is MemberExpression member)
            {
                selectedPropertyName = member.Member.Name;
            }
            if (propertySelector.Body is MemberExpression memberExpr)
            {
                selectedPropertyName = memberExpr.Member.Name;
            }

            var result = propertySelector.Compile().Invoke(target);

            if (result is null || result is "")
            {
                AnsiConsole.Write($"\n Parameter {selectedPropertyName} is requested, but was not filled");
            }

            return result;
        }
    }
}