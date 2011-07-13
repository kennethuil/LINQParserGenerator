using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace Framework.CodeGen.Expressions
{
    public class VariableSubstituteVisitor : ExpressionVisitor
    {
        ParameterExpression _find;
        ParameterExpression _replaceWith;

        public VariableSubstituteVisitor(ParameterExpression find, ParameterExpression replaceWith)
        {
            _find = find;
            _replaceWith = replaceWith;
        }

        /// <summary>
        /// If the given variable matches _find, return _replaceWith.  Otherwise, continue default visitation behavior.
        /// </summary>
        /// <param name="node">The expression node being visited</param>
        /// <returns></returns>
        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (node == _find)
                return _replaceWith;

            return base.VisitParameter(node);
        }
    }
}
