using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Framework.Parsing
{
    public class LR1Item<TChar> where TChar : IComparable<TChar>, IEquatable<TChar>
    {
        int _hashCode;

        public GrammarRule Rule { get; set; }

        public int DotPosition { get; set; }

        public Terminal<TChar> Lookahead { get; set; }

        public override bool Equals(object obj)
        {
            LR1Item<TChar> other = obj as LR1Item<TChar>;
            if (other == null)
                return false;

            if (!Rule.Equals(other.Rule))
                return false;
            if (DotPosition != other.DotPosition)
                return false;
            return Lookahead == other.Lookahead;
        }

        int HashCombine(int first, int second)
        {
            return (first + second).GetHashCode();
        }

        public override int GetHashCode()
        {
            if (_hashCode == 0)
            {
                _hashCode = Rule.GetHashCode();

                _hashCode = HashCombine(_hashCode, DotPosition.GetHashCode());
                _hashCode = HashCombine(_hashCode, Lookahead.GetHashCode());
            }

            return _hashCode;
        }
        public GrammarSymbol SymbolAfterDot
        {
            get
            {
                if (DotPosition >= Rule.RightHandSide.Count)
                {
                    // TODO: Epsilon?
                    return null;
                }
                return Rule.RightHandSide[DotPosition];
            }
        }

        public IList<GrammarSymbol> SymbolsAfterDot
        {
            get
            {
                return Rule.RightHandSide.Skip(DotPosition).ToList();
            }
        }

        public LR1Item<TChar> MoveDotRightOne()
        {
            return new LR1Item<TChar> { Rule = this.Rule, DotPosition = this.DotPosition + 1, Lookahead = this.Lookahead };
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Rule.LeftHandSide);
            sb.Append(" -> ");
            int i = 0;
            foreach (var symbol in Rule.RightHandSide)
            {
                if (DotPosition == i)
                {
                    sb.Append("(dot)");
                }
                sb.Append(symbol);
                sb.Append(" ");
                ++i;
            }
            if (DotPosition == Rule.RightHandSide.Count)
            {
                sb.Append("(dot)");
            }
            sb.Append(", " + Lookahead);
            return sb.ToString();
        }
    }
}
