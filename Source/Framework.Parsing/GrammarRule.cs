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
    }
}
