using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Framework.Parsing;

namespace Framework.Parsing
{
    [Serializable]
    public class Terminal<TChar> : GrammarSymbol
        where TChar : IComparable<TChar>, IEquatable<TChar>
    {
        public FiniteAutomatonState<TChar> InitialState { get; set; }

        // TODO: Include markers for begin/end capture, and have the classifier generator make use of them.

        [NonSerialized]
        LambdaExpression _action;

        /// <summary>
        /// Code to execute when a terminal is successfully matched.  It can optionally take a collection of TChar (or, if TChar is char, a string),
        /// and should return a value of type ValueType.
        /// </summary>
        public LambdaExpression Action
        {
            get { return _action; }
            set { _action = value; }
        }
    }

    [Serializable]
    public class Terminal<TChar, TValue> : Terminal<TChar>
        where TChar : IComparable<TChar>, IEquatable<TChar>
    {
        public new Expression<Func<IEnumerable<TChar>, TValue>> Action
        {
            get { return (Expression<Func<IEnumerable<TChar>, TValue>>)base.Action; }
            set { base.Action = value; }
        }

        public Expression<Func<string, TValue>> StringAction
        {
            get { return (Expression<Func<string, TValue>>)base.Action; }
            set 
            {
                // Can only pass strings into actions when TChar is char.
                if (typeof(TChar) != typeof(char))
                    throw new NotSupportedException();

                base.Action = value; 
            }
        }

        public Expression<Func<TValue>> NonCapturingAction
        {
            get { return (Expression<Func<TValue>>)base.Action; }
            set { base.Action = value; }
        }

        public override Type ValueType
        {
            get
            {
                return typeof(TValue);
            }
            set
            {
                if (value != typeof(TValue))
                    throw new NotSupportedException();
            }
        }
    }
}
