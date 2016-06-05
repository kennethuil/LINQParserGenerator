using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Framework.Parsing
{
    [Serializable]
    public class GrammarRule
    {
        public NonTerminal LeftHandSide { get; set; }
        public IList<GrammarSymbol> RightHandSide { get; set; }
        public String Name { get; set; }

        int _hashCode;


        // TODO: Action should support semantic actions that are represented by
        // objects other than LINQ expressions.
        [NonSerialized]
        LambdaExpression _action;
        public LambdaExpression Action
        {
            get { return _action; }
            set { _action = value; }
        }

        public override bool Equals(object obj)
        {
            GrammarRule other = obj as GrammarRule;
            if (obj == null)
                return false;

            if (LeftHandSide != other.LeftHandSide)
                return false;

            if (RightHandSide.Count != other.RightHandSide.Count)
                return false;

            int i;
            for (i = 0; i < RightHandSide.Count; ++i)
            {
                if (RightHandSide[i] != other.RightHandSide[i])
                    return false;
            }
            return true;
        }

        int HashCombine(int first, int second)
        {
            return (first + second).GetHashCode();
        }

        public override int GetHashCode()
        {
            if (_hashCode == 0)
            {
                // Compute hash code
                _hashCode = LeftHandSide.GetHashCode();
                foreach (var symbol in RightHandSide)
                {
                    _hashCode = HashCombine(_hashCode, symbol.GetHashCode());
                }
            }
            return _hashCode;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(LeftHandSide);
            sb.Append(" -> ");
            foreach (var symbol in RightHandSide)
            {
                sb.Append(symbol);
                sb.Append(" ");
            }
            return sb.ToString();
        }

        public static GrammarRule Create(NonTerminal left, params GrammarSymbol[] right)
        {
            return new GrammarRule { LeftHandSide = left, RightHandSide = right };
        }
    }

    [Serializable]
    public class GrammarRule<TValue> : GrammarRule
    {
        public new Expression<Func<TValue>> Action
        {
            get
            {
                return (Expression<Func<TValue>>)base.Action;
            }
            set
            {
                base.Action = value;
            }
        }

        public void SetStatefulAction<TParseState>(Expression<Func<TParseState, TValue>> action)
        {
            base.Action = action;
        }

        public new static GrammarRule<TValue> Create(NonTerminal left, params GrammarSymbol[] right)
        {
            return new GrammarRule<TValue> { LeftHandSide = left, RightHandSide = right };
        }
    }

    [Serializable]
    public class GrammarRule<TParam0, TValue> : GrammarRule
    {
        public new Expression<Func<TParam0, TValue>> Action
        {
            get
            {
                return (Expression<Func<TParam0, TValue>>)base.Action;
            }
            set
            {
                base.Action = value;
            }
        }

        public void SetStatefulAction<TParseState>(Expression<Func<TParseState, TParam0, TValue>> action)
        {
            base.Action = action;
        }

        public new static GrammarRule<TParam0, TValue> Create(NonTerminal left, params GrammarSymbol[] right)
        {
            return new GrammarRule<TParam0, TValue> { LeftHandSide = left, RightHandSide = right };
        }
    }

    [Serializable]
    public class GrammarRule<TParam0, TParam1, TValue> : GrammarRule
    {
        public new Expression<Func<TParam0, TParam1, TValue>> Action
        {
            get
            {
                return (Expression<Func<TParam0, TParam1, TValue>>)base.Action;
            }
            set
            {
                base.Action = value;
            }
        }

        public void SetStatefulAction<TParseState>(Expression<Func<TParseState, TParam0, TParam1, TValue>> action)
        {
            base.Action = action;
        }

        public new static GrammarRule<TParam0, TParam1, TValue> Create(NonTerminal left, params GrammarSymbol[] right)
        {
            return new GrammarRule<TParam0, TParam1, TValue> { LeftHandSide = left, RightHandSide = right };
        }
    }

    [Serializable]
    public class GrammarRule<TParam0, TParam1, TParam2, TValue> : GrammarRule
    {
        public new Expression<Func<TParam0, TParam1, TParam2, TValue>> Action
        {
            get
            {
                return (Expression<Func<TParam0, TParam1, TParam2, TValue>>)base.Action;
            }
            set
            {
                base.Action = value;
            }
        }

        public void SetStatefulAction<TParseState>(Expression<Func<TParseState, TParam0, TParam1, TParam2, TValue>> action)
        {
            base.Action = action;
        }

        public new static GrammarRule<TParam0, TParam1, TParam2, TValue> Create(NonTerminal left, params GrammarSymbol[] right)
        {
            return new GrammarRule<TParam0, TParam1, TParam2, TValue> { LeftHandSide = left, RightHandSide = right };
        }
    }

    [Serializable]
    public class GrammarRule<TParam0, TParam1, TParam2, TParam3, TValue> : GrammarRule
    {
        public new Expression<Func<TParam0, TParam1, TParam2, TParam3, TValue>> Action
        {
            get
            {
                return (Expression<Func<TParam0, TParam1, TParam2, TParam3, TValue>>)base.Action;
            }
            set
            {
                base.Action = value;
            }
        }

        public void SetStatefulAction<TParseState>(Expression<Func<TParseState, TParam0, TParam1, TParam2, TParam3, TValue>> action)
        {
            base.Action = action;
        }

        public new static GrammarRule<TParam0, TParam1, TParam2, TParam3, TValue> Create(NonTerminal left, params GrammarSymbol[] right)
        {
            return new GrammarRule<TParam0, TParam1, TParam2, TParam3, TValue> { LeftHandSide = left, RightHandSide = right };
        }
    }

    [Serializable]
    public class GrammarRule<TParam0, TParam1, TParam2, TParam3, TParam4, TValue> : GrammarRule
    {
        public new Expression<Func<TParam0, TParam1, TParam2, TParam3, TParam4, TValue>> Action
        {
            get
            {
                return (Expression<Func<TParam0, TParam1, TParam2, TParam3, TParam4, TValue>>)base.Action;
            }
            set
            {
                base.Action = value;
            }
        }

        public void SetStatefulAction<TParseState>(Expression<Func<TParseState, TParam0, TParam1, TParam2, TParam3, TParam4, TValue>> action)
        {
            base.Action = action;
        }

        public new static GrammarRule<TParam0, TParam1, TParam2, TParam3, TParam4, TValue> Create(NonTerminal left, params GrammarSymbol[] right)
        {
            return new GrammarRule<TParam0, TParam1, TParam2, TParam3, TParam4, TValue> { LeftHandSide = left, RightHandSide = right };
        }
    }

}
