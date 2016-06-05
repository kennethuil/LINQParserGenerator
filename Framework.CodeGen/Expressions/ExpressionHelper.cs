using System;
using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Reflection;
using System.Threading;
using System.Security.Cryptography;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Framework.CodeGen.Expressions
{
    public class ExpressionHelper : IExpressionHelper
    {
        static int _tagNumber = 0;

        Expression<Func<T, bool>> Combine<T>(Expression<Func<T, bool>> first, Expression<Func<T, bool>> second,
            Func<Expression, Expression, Expression> combiner)
        {
            if (first == null)
                return second;
            if (second == null)
                return first;

            // Get the User parameter and the body from each lambda.
            Expression body1 = first.Body;
            Expression body2 = second.Body;

            ParameterExpression p1 = first.Parameters[0];
            ParameterExpression p2 = second.Parameters[0];

            // The new body is the OR of the old bodies.  Make the second body use the first lambda's User parameter.
            VariableSubstituteVisitor vsv = new VariableSubstituteVisitor(p2, p1);
            Expression newBody = combiner(body1, vsv.Visit(body2));

            // Create a new lambda out of the combined body and the User parameter.
            return Expression.Lambda<Func<T, bool>>(newBody, p1);
        }

        public Expression<Func<T, bool>> Not<T>(Expression<Func<T, bool>> expr)
        {
            if (expr == null)
                return null;

            return Expression.Lambda<Func<T, bool>>(
                Expression.Not(expr.Body), expr.TailCall, expr.Parameters);
        }


        public Expression<Func<T, bool>> Or<T>(Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
        {
            return Combine<T>(first, second, this.Or);
        }

        public Expression<Func<T, bool>> And<T>(Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
        {
            return Combine<T>(first, second, this.And);
        }

        public Expression<Func<T, bool>> AndNot<T>(Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
        {
            return And<T>(first, Not<T>(second));
        }

        public Expression Or(Expression first, Expression second)
        {
            if (first == null)
                return second;
            if (second == null)
                return first;
            return Expression.Or(first, second);
        }

        bool IsComparison(BinaryExpression x)
        {
            switch (x.NodeType)
            {
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.GreaterThan:
                    return true;
            }
            return false;
        }

        IComparable Max(IComparable a, IComparable b)
        {
            return a.CompareTo(b) > 0 ? a : b;
        }

        Expression MaxExpr(IComparable a, IComparable b)
        {
            return System.Linq.Expressions.Expression.Constant(Max(a, b));
        }

        IComparable Min(IComparable a, IComparable b)
        {
            return a.CompareTo(b) < 0 ? a : b;
        }

        Expression MinExpr(IComparable a, IComparable b)
        {
            return Expression.Constant(Min(a, b));
        }

        Expression TryAndReduce(BinaryExpression first, BinaryExpression second)
        {
            if (IsComparison(first) && IsComparison(second))
            {
                // make sure all our assumptions are met.
                ConstantExpression ce1 = first.Right as ConstantExpression;
                if (ce1 == null)
                    return null;
                ConstantExpression ce2 = second.Right as ConstantExpression;
                if (ce2 == null)
                    return null;

                IComparable c1 = ce1.Value as IComparable;
                if (c1 == null)
                    return null;
                IComparable c2 = ce2.Value as IComparable;
                if (c2 == null)
                    return null;

                if (first.NodeType == ExpressionType.Equal)
                {
                    if (second.NodeType == ExpressionType.Equal)
                        return (c1.CompareTo(c2) == 0) ? (Expression)first : Expression.Constant(false);
                    if (second.NodeType == ExpressionType.NotEqual)
                        return (c1.CompareTo(c2) != 0) ? (Expression)first : Expression.Constant(false);
                    if (second.NodeType == ExpressionType.LessThan)
                        return (c1.CompareTo(c2) < 0) ? (Expression)first : Expression.Constant(false);
                    if (second.NodeType == ExpressionType.LessThanOrEqual)
                        return (c1.CompareTo(c2) <= 0) ? (Expression)first : Expression.Constant(false);
                    if (second.NodeType == ExpressionType.GreaterThan)
                        return (c1.CompareTo(c2) > 0) ? (Expression)first : Expression.Constant(false);
                    if (second.NodeType == ExpressionType.GreaterThanOrEqual)
                        return (c1.CompareTo(c2) >= 0) ? (Expression)first : Expression.Constant(false);
                }
                else if (first.NodeType == ExpressionType.NotEqual)
                {
                    if (second.NodeType == ExpressionType.NotEqual)
                        return (c1.CompareTo(c2) == 0) ? (Expression)first : null;
                    if (second.NodeType == ExpressionType.LessThan)
                        return (c1.CompareTo(c2) >= 0) ? (Expression)first : null;
                    if (second.NodeType == ExpressionType.LessThanOrEqual)
                        return (c1.CompareTo(c2) > 0) ? first : null;
                    if (second.NodeType == ExpressionType.GreaterThan)
                        return (c1.CompareTo(c2) <= 0) ? first : null;
                    if (second.NodeType == ExpressionType.GreaterThanOrEqual)
                        return (c1.CompareTo(c2) < 0) ? first : null;
                }
                else if (first.NodeType == ExpressionType.LessThan)
                {
                    if (second.NodeType == ExpressionType.LessThan)
                        return Expression.LessThan(first.Left, MinExpr(c1, c2));
                    if (second.NodeType == ExpressionType.LessThanOrEqual)
                        return (c1.CompareTo(c2) <= 0) ? first : second;

                    // TODO: For integers, check for result x == c.
                    if (second.NodeType == ExpressionType.GreaterThan)
                        return (c1.CompareTo(c2) > 0) ? null : Expression.Constant(false);
                    if (second.NodeType == ExpressionType.GreaterThanOrEqual)
                        return (c1.CompareTo(c2) > 0) ? null : Expression.Constant(false);
                }
                else if (first.NodeType == ExpressionType.LessThanOrEqual)
                {
                    if (second.NodeType == ExpressionType.LessThanOrEqual)
                        return Expression.LessThanOrEqual(first.Left, MinExpr(c1, c2));
                    if (second.NodeType == ExpressionType.GreaterThan)
                        return (c1.CompareTo(c2) > 0) ? null : Expression.Constant(false);
                    if (second.NodeType == ExpressionType.GreaterThanOrEqual)
                        return (c1.CompareTo(c2) == 0) ? (Expression)Expression.Equal(first.Left, Expression.Constant(c1))
                            : ((c1.CompareTo(c2) > 0) ? null : Expression.Constant(false));
                }
                else if (first.NodeType == ExpressionType.GreaterThan)
                {
                    if (second.NodeType == ExpressionType.GreaterThan)
                        return Expression.GreaterThan(first.Left, MaxExpr(c1, c2));
                    if (second.NodeType == ExpressionType.GreaterThanOrEqual)
                        return (c1.CompareTo(c2) >= 0) ? first : second;
                }
                else if (first.NodeType == ExpressionType.GreaterThanOrEqual)
                {
                    if (second.NodeType == ExpressionType.GreaterThanOrEqual)
                        return Expression.GreaterThanOrEqual(first.Left, MaxExpr(c1, c2));
                }

                // If we got to this point, swapping the operands and retrying should do the trick
                return TryAndReduce(second, first);
            }

            // TODO: Boolean reductions
            return null;
        }

        Expression TryAndReduce(ConstantExpression ce, Expression other)
        {
            if ((bool)ce.Value)
                return other;
            return ce;
        }

        Expression TryAndReduce(Expression first, Expression second)
        {
            var ce = first as ConstantExpression;
            if (ce != null)
            {
                var result = TryAndReduce(ce, second);
                if (result != null)
                    return result;
            }
            ce = second as ConstantExpression;
            if (ce != null)
            {
                var result = TryAndReduce(first, ce);
                if (result != null)
                    return result;
            }
            
            if (first is BinaryExpression && second is BinaryExpression)
            {
                var result = TryAndReduce((BinaryExpression)first, (BinaryExpression)second);
                if (result != null)
                    return result;
            }
            return null;
        }
           
        public Expression And(Expression first, Expression second)
        {
            if (first == null)
                return second;
            if (second == null)
                return first;

            var result = TryAndReduce(first, second);
            if (result != null)
                return result;
            return Expression.And(first, second);
        }

        public Expression Invoke<TR>(Expression<Func<TR>> target)
        {
            return Expression.Invoke(target);
        }

        public Expression Invoke<T0, TR>(Expression<Func<T0, TR>> target, Expression p0)
        {
            return Expression.Invoke(target, p0);
        }

        public Expression Invoke<T0, T1, TR>(Expression<Func<T0, T1, TR>> target, Expression p0, Expression p1)
        {
            return Expression.Invoke(target, p0, p1);
        }

        public Expression InvokeAction(Expression<Action> target)
        {
            return Expression.Invoke(target);
        }

        public Expression InvokeAction<T0>(Expression<Action<T0>> target, Expression p0)
        {
            return Expression.Invoke(target, p0);
        }

        public Expression InvokeAction<T0, T1>(Expression<Action<T0, T1>> target, Expression p0, Expression p1)
        {
            return Expression.Invoke(target, p0, p1);
        }

        public Expression InvokeAction<T0, T1, T2>(Expression<Action<T0, T1, T2>> target, Expression p0, Expression p1, Expression p2)
        {
            return Expression.Invoke(target, p0, p1, p2);
        }


        public TypeBuilder GetUniqueType(ModuleBuilder mb, string description)
        {
            int tag = Interlocked.Add(ref _tagNumber, 1);
            String name = description + "_" + tag;
            return mb.DefineType(name, TypeAttributes.Public);
        }

        // NOTE: These two functions are duplicated because ILGEnerator.Emit has overloads for MethodINfo and ConstructorInfo, but
        // not for MethodBase (the common base class of MethodInfo & ConstructorInfo)
        void PopulatePassthru(ILGenerator gen, ConstructorInfo target, int numArguments, bool tail)
        {
            if (numArguments > 0)
                gen.Emit(OpCodes.Ldarg_0);
            if (numArguments > 1)
                gen.Emit(OpCodes.Ldarg_1);
            if (numArguments > 2)
                gen.Emit(OpCodes.Ldarg_2);
            if (numArguments > 3)
                gen.Emit(OpCodes.Ldarg_3);
            int i;
            for (i = 4; i < numArguments; ++i)
            {
                gen.Emit(OpCodes.Ldarg_S, (byte)i);
            }
            if (tail)
                gen.Emit(OpCodes.Tailcall);
            gen.Emit(OpCodes.Call, target);
            gen.Emit(OpCodes.Ret);
        }

        void PopulatePassthru(ILGenerator gen, MethodInfo target, int numArguments, bool tail)
        {
            if (numArguments > 0)
                gen.Emit(OpCodes.Ldarg_0);
            if (numArguments > 1)
                gen.Emit(OpCodes.Ldarg_1);
            if (numArguments > 2)
                gen.Emit(OpCodes.Ldarg_2);
            if (numArguments > 3)
                gen.Emit(OpCodes.Ldarg_3);
            int i;
            for (i = 4; i < numArguments; ++i)
            {
                gen.Emit(OpCodes.Ldarg_S, (byte)i);
            }
            if (tail)
                gen.Emit(OpCodes.Tailcall);
            gen.Emit(OpCodes.Call, target);
            gen.Emit(OpCodes.Ret);
        }

        public void PopulatePassthru(MethodBuilder passthru, MethodInfo target, int numArguments)
        {
            ILGenerator gen = passthru.GetILGenerator();
            PopulatePassthru(gen, target, numArguments, true);
        }

        public void PopulatePassthru(ConstructorBuilder passthru, ConstructorInfo baseConstructor, MethodInfo target, int numArguments)
        {
            ILGenerator gen = passthru.GetILGenerator();

            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Call, baseConstructor);

            PopulatePassthru(gen, target, numArguments, true);
        }

        public void PopulateEmpty(MethodBuilder target)
        {
            ILGenerator gen = target.GetILGenerator();
            gen.Emit(OpCodes.Ret);
        }

        public void AddPassthruConstructors(TypeBuilder subclassBuilder, Type superclass)
        {
            //System.Diagnostics.Debugger.Break();
            var constructors = superclass.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            bool hasDefault = false;
            foreach (var c in constructors)
            {
                if (c.IsPrivate)
                    continue;

                ParameterInfo[] p = c.GetParameters();
                Type[] paramTypes = (from x in p select x.ParameterType).ToArray();
                
                ConstructorBuilder cb = subclassBuilder.DefineConstructor(MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                    c.CallingConvention, paramTypes);
                int i;
                for (i = 0; i < paramTypes.Length; ++i)
                {
                    cb.DefineParameter(i + 1, p[i].Attributes, p[i].Name);
                }
                ILGenerator gen = cb.GetILGenerator();
                PopulatePassthru(gen, c, paramTypes.Length + 1, false);
                if (paramTypes.Length == 0)
                    hasDefault = true;
            }
            if (!hasDefault)
            {
            }
        }

        public void SetupLinkedDynamicAssembly(String assemblyName, Action<ModuleBuilder> build)
        {
            Assembly theAssembly = null;
            RandomNumberGenerator rng = RandomNumberGenerator.Create();

            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler((sender, args) =>
            {
                if (!args.Name.StartsWith(assemblyName + ","))
                    return null;

                Assembly result = Interlocked.CompareExchange(ref theAssembly, null, null);
                if (result != null)
                    return result;

                // Create the assembly

                byte[] bUniquifier = new byte[8];
                rng.GetBytes(bUniquifier);
                String strUniquifier = Convert.ToBase64String(bUniquifier).Replace('/','_').Replace('+','_');
                String filename = assemblyName + strUniquifier + ".dll";
                String dir = Path.GetTempPath();
                AssemblyBuilder ab = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.Save, dir);
                ModuleBuilder mod = ab.DefineDynamicModule(filename);

                build(mod);

                ab.Save(filename);
                Assembly loaded = Assembly.LoadFrom(dir + Path.DirectorySeparatorChar + filename);
                result = Interlocked.CompareExchange(ref theAssembly, loaded, null);
                // Result is the *preexisting* value.  If it's null, 'loaded' got put into it and that's what we want to return.
                // Otherwise, the value remained unchanged, result is equal to it, and we return it and discard loaded.
                if (result == null)
                    return loaded;
                return result;
            });
        }


        public PropertyInfo Property<T, TPropType>(Expression<Func<T, TPropType>> expr)
        {
            var body = expr.Body as MemberExpression;
            if (body != null)
            {
                var member = body.Member;
                var prop = member as PropertyInfo;
                if (prop != null)
                    return prop;
            }
            return null;
        }

        public MethodInfo Method<T, TReturn>(Expression<Func<T, TReturn>> expr)
        {
            var body = expr.Body as MethodCallExpression;
            if (body != null)
            {
                var meth = body.Method;
                return meth;
            }
            return null;
        }

        public Expression BuildInvokeConstantDelegate(Delegate d, Expression expDelegate, params Expression[] args)
        {
            // NOTE: Assuming the delegate is not created from a DynamicMethod (or .NET has finally gotten around to supporting reflection on said delegate)
            // TODO: Handle the DynamicMethod case.
            MethodInfo meth = d.Method;
            Object instance = d.Target;
            IEnumerable<Expression> allArgs;
            if (instance == null)
                allArgs = args;
            else
                allArgs = new Expression[] { Expression.PropertyOrField(expDelegate, "Target") }.Concat(args);

            IEnumerable<Expression> finalArgs;
            Expression expInstance;

            if (meth.IsStatic)
            {
                expInstance = allArgs.First();
                finalArgs = allArgs.Skip(1);
            }
            else
            {
                expInstance = null;
                finalArgs = allArgs;
            }
            return Expression.Call(expInstance, meth, finalArgs);
        }

        public Expression BuildInvokeAction(Action d)
        {
            return Expression.Invoke(Expression.Constant(d));
        }

        public Expression BuildInvokeAction<T0>(Action<T0> d, Expression p0)
        {
            return Expression.Invoke(Expression.Constant(d), p0);
        }

        public Expression BuileInvokeAction<T0, T1>(Action<T0, T1> d, Expression p0, Expression p1)
        {
            return Expression.Invoke(Expression.Constant(d), p0, p1);
        }

        public Expression BuildInvokeAction<T0, T1, T2>(Action<T0, T1, T2> d, Expression p0, Expression p1, Expression p2)
        {
            return Expression.Invoke(Expression.Constant(d), p0, p1, p2);
        }
    }
}
