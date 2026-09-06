using System;
using System.Linq;
using System.Linq.Expressions;
using Dapper;

namespace NzbDrone.Core.Datastore
{
    public abstract class WhereBuilder : ExpressionVisitor
    {
        public DynamicParameters Parameters { get; protected set; }

        protected static bool IsStaticContains(MethodCallExpression body)
        {
            if (body.Arguments.Count < 2)
            {
                return false;
            }

            var declaringType = body.Method.DeclaringType;
            return declaringType == typeof(Enumerable) || declaringType == typeof(MemoryExtensions);
        }

        protected static Expression UnwrapArrayFromSpan(Expression expression)
        {
            while (true)
            {
                if (expression is UnaryExpression unary &&
                    (unary.NodeType == ExpressionType.Convert || unary.NodeType == ExpressionType.ConvertChecked))
                {
                    expression = unary.Operand;
                    continue;
                }

                if (expression is MethodCallExpression methodCall &&
                    methodCall.Method.Name == "op_Implicit" &&
                    methodCall.Arguments.Count == 1)
                {
                    expression = methodCall.Arguments[0];
                    continue;
                }

                return expression;
            }
        }
    }
}
