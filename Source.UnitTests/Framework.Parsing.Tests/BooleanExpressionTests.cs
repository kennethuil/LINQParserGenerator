using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Framework.CodeGen;
using NUnit.Framework;

namespace Source.UnitTests.Framework.Parsing.Tests
{
    [TestFixture]
    public class BooleanExpressionTests
    {
        [Test]
        public void AndOrCombinations()
        {
            var x = Expression.Parameter(typeof(char), "x");
            var p1 = new PredicateExpression { Predicate = Expression.Equal(x, Expression.Constant('x')) };
            var p2 = new PredicateExpression { Predicate = Expression.Equal(x, Expression.Constant('y')) };
            var p = p1.And(p2);

            var p3 = new PredicateExpression { Predicate = Expression.Equal(x, Expression.Constant('z')) };
            p = p.And(p3);
            var andp = (AndExpression)p;
            foreach (var component in andp.Members)
            {
                Assert.IsInstanceOf<PredicateExpression>(component);
            }

            p = p1.Or(p2).Or(p3);
            var orp = (OrExpression)p;
            foreach (var component in orp.Members)
            {
                Assert.IsInstanceOf<PredicateExpression>(component);
            }
        }
    }
}
