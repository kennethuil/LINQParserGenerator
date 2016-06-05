using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Reflection;

namespace Framework.CodeGen
{
    /// <summary>
    /// A service for working with LINQ expressions, lambda expressions, and dynamic assemblies.
    /// </summary>
    public interface IExpressionHelper
    {
        /// <summary>
        /// Given two boolean lambda expressions, return a lambda expression that OR's them.  If the inputs are LINQ-to-Entities-compatible, then so is the result
        /// </summary>
        /// <typeparam name="T">The type of data object passed to the lambda expressions</typeparam>
        /// <param name="first">One of the expressions to be combined</param>
        /// <param name="second">The other expression to be combined</param>
        /// <returns>A lambda expression that is the logical OR of the two input lambda expressions</returns>
        Expression<Func<T, bool>> Or<T>(Expression<Func<T, bool>> first, Expression<Func<T, bool>> second);

        /// <summary>
        /// Given two boolean lambda expressions, return a lambda expression that AND's them.  If the inputs are LINQ-to-Entities-compatible, then so is the result
        /// </summary>
        /// <typeparam name="T">The type of data object passed to the lambda expressions</typeparam>
        /// <param name="first">One of the lambda expressions to be combined</param>
        /// <param name="second">The other lambda expression to be combined</param>
        /// <returns>A lambda expression that is the logical AND of the two input lambda expressions</returns>
        Expression<Func<T, bool>> And<T>(Expression<Func<T, bool>> first, Expression<Func<T, bool>> second);

        /// <summary>
        /// Given two boolean lambda expressions, return a lambda expression that matches anything that matches the first lambda if and only if
        /// it does not match the second lambda.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        Expression<Func<T, bool>> AndNot<T>(Expression<Func<T, bool>> first, Expression<Func<T, bool>> second);

        /// <summary>
        /// Given two simple LINQ Expressions of boolean type, OR them.
        /// </summary>
        /// <param name="first">One of the Expressions to be combined</param>
        /// <param name="second">The other expression to be combined</param>
        /// <returns>An Expression representing the logical OR of the two input Expressions</returns>
        Expression Or(Expression first, Expression second);

        /// <summary>
        /// Given two simple LINQ Expressions of boolean type, OR them.
        /// </summary>
        /// <param name="first">One of the Expressions to be combined</param>
        /// <param name="second">The other expression to be combined</param>
        /// <returns>An Expression representing the logical OR of the two input Expressions</returns>
        Expression And(Expression first, Expression second);

        /// <summary>
        /// Create an invocation expression for a lambda expression.  This allows 'invoking' one lambda expression from another expression tree.
        /// </summary>
        /// <typeparam name="TR">The return type of the lambda expression</typeparam>
        /// <param name="target">The lambda expression</param>
        /// <returns>The invocation expression</returns>
        Expression Invoke<TR>(Expression<Func<TR>> target);

        /// <summary>
        /// Create an invocation expression for a lambda expression and associated parameter expressions.  This allows 'invoking' one lambda expression from another expression tree.
        /// </summary>
        /// <typeparam name="T0">Lambda expression parameter type</typeparam>
        /// <typeparam name="TR">The return type of the lambda expression</typeparam>
        /// <param name="target">The lambda expression</param>
        /// <param name="p0">Expression for a parameter to be passed to the lambda expression</param>
        /// <returns>The invocation expression</returns>
        Expression Invoke<T0, TR>(Expression<Func<T0, TR>> target, Expression p0);

        /// <summary>
        /// Create an invocation expression for a lambda expression and associated parameter expressions.  This allows 'invoking' one lambda expression from another expression tree.
        /// </summary>
        /// <typeparam name="T0">Lambda expression parameter type</typeparam>
        /// <typeparam name="T1">Lambda expression parameter type</typeparam>
        /// <typeparam name="TR">The return type of the lambda expression</typeparam>
        /// <param name="target">The lambda expression</param>
        /// <param name="p0">Expression for a parameter to be passed to the lambda expression</param>
        /// <param name="p1">Expression for a parameter to be passed to the lambda expression</param>
        /// <returns>The invocation expression</returns>
        Expression Invoke<T0, T1, TR>(Expression<Func<T0, T1, TR>> target, Expression p0, Expression p1);

        /// <summary>
        /// Create an invocation expression for a lambda expression and associated parameter expressions.  This allows 'invoking' one lambda expression from another expression tree.
        /// </summary>
        /// <param name="target">The lambda expression</param>
        /// <returns>The invocation expression</returns>
        Expression InvokeAction(Expression<Action> target);

        /// <summary>
        /// Create an invocation expression for a lambda expression and associated parameter expressions.  This allows 'invoking' one lambda expression from another expression tree.
        /// </summary>
        /// <typeparam name="T0">Lambda expression parameter type</typeparam>
        /// <param name="target">The lambda expression</param>
        /// <param name="p0">Expression for a parameter to be passed to the lambda expression</param>
        /// <returns>The invocation expression</returns>
        Expression InvokeAction<T0>(Expression<Action<T0>> target, Expression p0);

        /// <summary>
        /// Create an invocation expression for a lambda expression and associated parameter expressions.  This allows 'invoking' one lambda expression from another expression tree.
        /// </summary>
        /// <typeparam name="T0">Lambda expression parameter type</typeparam>
        /// <typeparam name="T1">Lambda expression parameter type</typeparam>
        /// <param name="target">The lambda expression</param>
        /// <param name="p0">Expression for a parameter to be passed to the lambda expression</param>
        /// <param name="p1">Expression for a parameter to be passed to the lambda expression</param>
        /// <returns>The invocation expression</returns>
        Expression InvokeAction<T0, T1>(Expression<Action<T0, T1>> target, Expression p0, Expression p1);

        /// <summary>
        /// Create an invocation expression for a lambda expression and associated parameter expressions.  This allows 'invoking' one lambda expression from another expression tree.
        /// </summary>
        /// <typeparam name="T0">Lambda expression parameter type</typeparam>
        /// <typeparam name="T1">Lambda expression parameter type</typeparam>
        /// <typeparam name="T2">Lambda expression parameter type</typeparam>
        /// <param name="target">The lambda expression</param>
        /// <param name="p0">Expression for a parameter to be passed to the lambda expression</param>
        /// <param name="p1">Expression for a parameter to be passed to the lambda expression</param>
        /// <param name="p2">Expression for a parameter to be passed to the lambda expression</param>
        /// <returns>The invocation expression</returns>
        Expression InvokeAction<T0, T1, T2>(Expression<Action<T0, T1, T2>> target, Expression p0, Expression p1, Expression p2);

        /// <summary>
        /// Creates a uniquely named type in a dynamic module
        /// </summary>
        /// <param name="mb">The dynamic module that will contain the type</param>
        /// <param name="description">A description of the type.  This will form part of the typename</param>
        /// <returns>The newly created uniquely named type</returns>
        TypeBuilder GetUniqueType(ModuleBuilder mb, string description);

        /// <summary>
        /// Registers a handler to create an assembly with a given name if the assembly is not found
        /// </summary>
        /// <param name="assemblyName">The name of the assembly, without the ".dll" extension</param>
        /// <param name="build">A delegate that will add code to the assembly when it is created in response to a load attempt</param>
        void SetupLinkedDynamicAssembly(String assemblyName, Action<ModuleBuilder> build);

        /// <summary>
        /// Given a superclass with non-default constructors, adds corresponding "passthru" constructors to a subclass TypeBuilder.
        /// </summary>
        /// <param name="subclass">The subclass under construction.</param>
        /// <param name="superclass">The superclass</param>
        void AddPassthruConstructors(TypeBuilder subclassBuilder, Type superclass);

        /// <summary>
        /// Inserts code into one MethodBuilder to call another method.  Each argument of target must be assignable from the corresponding argument in passthru.
        /// The return type of passthrough must be assignable from the return type of target.
        /// Since you cannot use LINQ expressions to populate a nonstatic method, this is useful for populating a nonstatic method that does nothing but call
        /// a static method, which you can populate with a LINQ expression.
        /// If one method is a static method and the other is an instance method, the 'this' parameter of the instance method will correspond to the first declared
        /// parameter of the static method, and the declared parameters of the instance method will correspond to the second and subsequent declared parameters
        /// of the static method.
        /// </summary>
        /// <param name="passthru">The MethodBuilder that needs passthrough code filled in</param>
        /// <param name="target">The method that the passthrough code will call</param>
        /// <param name="numArguments">The number of arguments to be passed.  It *must* match the number of arguments declared by target.  This parameter is needed because incompleted MethodBuilders throw an exception if you try to get their parameter lists.</param>
        void PopulatePassthru(MethodBuilder passthru, MethodInfo target, int numArguments);

        /// <summary>
        /// Inserts code into one ConstructorBuilder to call another method.  Each argument of target must be assignable from the corresponding argument in passthru.
        /// The target method must be declared with a void return.
        /// Since you cannot use LINQ expressions to populate a nonstatic method, this is useful for populating a nonstatic method that does nothing but call
        /// a static method, which you can populate with a LINQ expression.
        /// If target is a static method, the 'this' parameter of the constructor will correspond to the first declared
        /// parameter of the static method, and the declared parameters of the constructor will correspond to the second and subsequent declared parameters
        /// of the static method.
        /// </summary>
        /// <param name="passthru">The ConstructorBuilder that needs passthrough code filled in</param>
        /// <param name="baseConstructor">The default constructor of the superclass of passthru's declaring class</param>
        /// <param name="target">The method that the passthrough code will call</param>
        /// <param name="numArguments">The number of arguments to be passed.  It *must* match the number of arguments declared by target.  This parameter is needed because incompleted MethodBuilders throw an exception if you try to get their parameter lists.</param>
        void PopulatePassthru(ConstructorBuilder passthru, ConstructorInfo baseConstructor, MethodInfo target, int numArguments);

        /// <summary>
        /// Populates the target MethodBuilder and causes it to return immediately to its caller.  The target must be declared to return void.
        /// </summary>
        /// <param name="target">The MethodBuilder to be populated with an immediate return</param>
        void PopulateEmpty(MethodBuilder target);

        Expression BuildInvokeConstantDelegate(Delegate d, Expression expDelegate, params Expression[] args);

        Expression BuildInvokeAction(Action d);

        Expression BuildInvokeAction<T0>(Action<T0> d, Expression p0);

        Expression BuileInvokeAction<T0, T1>(Action<T0, T1> d, Expression p0, Expression p1);

        Expression BuildInvokeAction<T0, T1, T2>(Action<T0, T1, T2> d, Expression p0, Expression p1, Expression p2);

    }
}
