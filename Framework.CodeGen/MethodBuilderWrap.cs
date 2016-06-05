using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace ExpressionTests
{
    public class ParameterInfoWrap : ParameterInfo
    {
        Type _paramType;

        public ParameterInfoWrap(Type paramType)
        {
            _paramType = paramType;
        }

        public override Type ParameterType
        {
            get
            {
                return _paramType;
            }
        }

        public override ParameterAttributes Attributes
        {
            get
            {
                return ParameterAttributes.None;
            }
        }
    }

    public class MethodBuilderWrap : MethodInfo
    {
        MethodBuilder _builder;
        Type _returnType;
        Type[] _paramTypes;
        MethodAttributes _attr;
        ParameterInfo[] _params;
        string _name;

        public MethodBuilderWrap(TypeBuilder tb, string name, MethodAttributes attr, Type returnType, params Type[] parameterTypes)
        {
            _builder = tb.DefineMethod(name, attr, returnType, parameterTypes);
            _returnType = returnType;
            _paramTypes = parameterTypes;
            _attr = attr;
            _name = name;

            _params = new ParameterInfo[_paramTypes.Length];
            int i;
            for (i = 0; i < _paramTypes.Length; ++i)
            {
                _params[i] = new ParameterInfoWrap(_paramTypes[i]);
            }
        }

        public override MethodInfo GetBaseDefinition()
        {
            return this;
        }

        public override ICustomAttributeProvider ReturnTypeCustomAttributes
        {
            get { return null; }
        }

        public override MethodAttributes Attributes
        {
            get { return _attr; }
        }

        public override MethodImplAttributes GetMethodImplementationFlags()
        {
            return MethodImplAttributes.IL;
        }

        public override ParameterInfo[] GetParameters()
        {

            return _params;
        }

        public override Type ReturnType
        {
            get
            {
                return _returnType;
            }
        }

        public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override RuntimeMethodHandle MethodHandle
        {
            get { throw new NotImplementedException(); }
        }

        public override Type DeclaringType
        {
            get { return _builder.DeclaringType; }
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return new object[0];
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            return new object[0];
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return false;
        }

        public override string Name
        {
            get { return _name;  }
        }

        public override Type ReflectedType
        {
            get { throw new NotImplementedException(); }
        }

        public override CallingConventions CallingConvention
        {
            get
            {
                return CallingConventions.Standard;
            }
        }

        public override bool ContainsGenericParameters
        {
            get
            {
                return false;
            }
        }

        public override Type[] GetGenericArguments()
        {
            return new Type[0];
        }

        public override bool IsGenericMethod
        {
            get
            {
                return false;
            }
        }

        public override bool IsGenericMethodDefinition
        {
            get
            {
                return false;
            }
        }

        public override bool IsSecurityCritical
        {
            get
            {
                return false;
            }
        }

        public override bool IsSecuritySafeCritical
        {
            get
            {
                return false;
            }
        }

        public ILGenerator GetILGenerator()
        {
            return _builder.GetILGenerator();
        }

        public MethodBuilder GetMethodBuilder()
        {
            return _builder;
        }
    }
}
