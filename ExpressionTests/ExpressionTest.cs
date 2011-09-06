using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using ExpressionTests;
using NUnit.Framework;

namespace Source.UnitTests.Framework.Parsing.Tests
{
    [TestFixture]
    public class ExpressionTest
    {
        public static int Base()
        {
            throw new ApplicationException("This is a test");
        }

        [Test]
        public void linq_call_methodbuilder()
        {
            AssemblyBuilder ab = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("Test1"), AssemblyBuilderAccess.Run);
            ModuleBuilder modb = ab.DefineDynamicModule("Test1.dll");
            TypeBuilder tb = modb.DefineType("Test1", TypeAttributes.Public);
            var mb = new MethodBuilderWrap(tb, "Test1", MethodAttributes.Public | MethodAttributes.Static, typeof(int), typeof(int));

            var p = Expression.Parameter(typeof(int));

            LabelTarget target = Expression.Label(typeof(int));

            /*
            var body = Expression.Block(
                Expression.IfThen(Expression.GreaterThan(p, Expression.Constant(1)),
                    Expression.Return(target, Expression.Multiply(p, Expression.Call(mb, Expression.Subtract(p, Expression.Constant(1)))))),
                Expression.Label(target, Expression.Call(typeof(ExpressionTest).GetMethod("Base", BindingFlags.Static | BindingFlags.Public))));
            */
            var body = Expression.Call(mb, Expression.Subtract(p, Expression.Constant(1)));
            //var body = Expression.Call(typeof(ExpressionTest).GetMethod("Base", BindingFlags.Static | BindingFlags.Public));
            var lambda = Expression.Lambda<Func<int, int>>(body, true, p);

            lambda.CompileToMethod(mb.GetMethodBuilder());

            var t= tb.CreateType();

            MethodInfo compiled = t.GetMethod("Test1", BindingFlags.Static | BindingFlags.Public);
            var result = compiled.Invoke(null, new object[] { 5 });
            var methodbody = compiled.GetMethodBody();
        }

        [Test]
        public void do_boolean_expressions_reduce()
        {
            ParameterExpression p = Expression.Parameter(typeof(char));

            Expression x = Expression.Or(
                Expression.Equal(p, Expression.Constant('x')),
                Expression.Equal(p, Expression.Constant('x')));
            var canReduce = x.CanReduce;
            var reduced = x.Reduce();

            // Answer: No, boolean expressions do not reduce!
        }

        class TestExpression : Expression
        {
            
            public override bool CanReduce
            {
                get
                {
                    return true;
                }
            }

            public override Expression Reduce()
            {
                return Expression.Constant(24601);
            }
             

            public override ExpressionType NodeType
            {
                get
                {
                    return ExpressionType.Extension;
                }
            }

            public override Type Type
            {
                get
                {
                    return typeof(int);
                }
            }

            protected override Expression Accept(ExpressionVisitor visitor)
            {
                return base.Accept(visitor);
            }
            protected override Expression VisitChildren(ExpressionVisitor visitor)
            {
                return base.VisitChildren(visitor);
            }
        }

        [Test]
        public void test_custom_expression_reduction()
        {
            TestExpression te = new TestExpression();
            var lambda = Expression.Lambda<Func<int>>(te, true);
            var f = lambda.Compile();
            Assert.AreEqual(24601, f());
        }
    }
}
