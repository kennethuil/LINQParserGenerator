using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Framework.Parsing
{
    public class ExpressionEqualityComparer<T> : IEqualityComparer<T> where T : Expression
    {
        string CanonicalExpressionString(T x)
        {
            var canon = new CanonicalizingVisitor().Visit(x);
            return canon.ToString();
        }

        public bool Equals(T x, T y)
        {
            if (x == y)
                return true;
            if (x == null)
                return false;
            if (y == null)
                return false;
            return CanonicalExpressionString(x) == CanonicalExpressionString(y);
        }

        public int GetHashCode(T obj)
        {
            return CanonicalExpressionString(obj).GetHashCode();
        }
    }

    public class CanonicalizingVisitor : ExpressionVisitor
    {
        int _paramNum;

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return Expression.Parameter(node.Type, "param" + _paramNum++);
        }
    }
}
