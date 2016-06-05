using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Framework.Parsing;

namespace Framework.CodeGen
{
    public abstract class BooleanExpression : Expression
    {
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
                return typeof(bool);
            }
        }

        public override bool CanReduce
        {
            get
            {
                return true;
            }
        }

        public abstract BooleanExpression And(BooleanExpression other);

        public abstract BooleanExpression Or(BooleanExpression other);

        public abstract BooleanExpression Not();

    }

    public class PredicateExpression : BooleanExpression
    {
        public Expression Predicate { get; set; }

        public override Expression Reduce()
        {
            return Predicate;
        }

        public override BooleanExpression And(BooleanExpression other)
        {
            // TODO: Predicate combinations!
            if (!(other is AndExpression))
                return new AndExpression {Members = new HashableSet<BooleanExpression> {this, other}};
            //((AndExpression) other).Members.Add(this);
            //return other;
            var result = new AndExpression { Members = new HashableSet<BooleanExpression> { this } };
            result.Members.UnionWith(((AndExpression)other).Members);
            return result;

        }

        public override BooleanExpression Or(BooleanExpression other)
        {
            // TODO: Predicate combinations!
            if (!(other is OrExpression))
                return new OrExpression { Members = new HashableSet<BooleanExpression> { this, other } };
            //((OrExpression)other).Members.Add(this);
            //return other;
            var result = new OrExpression { Members = new HashableSet<BooleanExpression> { this } };
            result.Members.UnionWith(((OrExpression)other).Members);
            return result;
        }

        public override BooleanExpression Not()
        {
            return new NotExpression { NegatedExpr = this };
        }
    }

    public class AndExpression : BooleanExpression
    {
        public HashableSet<BooleanExpression> Members { get; set; }

        public AndExpression()
        {
            Members = new HashableSet<BooleanExpression>();
        }

        public override BooleanExpression And(BooleanExpression other)
        {
            var result = new AndExpression();
            result.Members.UnionWith(Members);

            if (other is AndExpression)
                result.Members.UnionWith(((AndExpression)other).Members);
            else
                result.Members.Add(other);
            return result;
        }

        public override BooleanExpression Or(BooleanExpression other)
        {
            var result = new OrExpression { Members = new HashableSet<BooleanExpression> { this } };

            if (other is OrExpression)
                result.Members.UnionWith(((OrExpression)other).Members);
            else
                result.Members.Add(other);
            return result;

        }

        public override BooleanExpression Not()
        {
            return new NotExpression { NegatedExpr = this };
        }
    }

    public class OrExpression : BooleanExpression
    {
        public HashableSet<BooleanExpression> Members { get; set; }

        public OrExpression()
        {
            Members = new HashableSet<BooleanExpression>();
        }

        public override BooleanExpression And(BooleanExpression other)
        {
            var result = new AndExpression { Members = new HashableSet<BooleanExpression> { this } };
            if (other is AndExpression)
                result.Members.UnionWith(((AndExpression)other).Members);
            else
                result.Members.Add(other);
            return result;
        }

        public override BooleanExpression Or(BooleanExpression other)
        {
            var result = new OrExpression();
            result.Members.UnionWith(Members);

            if (other is OrExpression)
                result.Members.UnionWith(((OrExpression)other).Members);
            else
                result.Members.Add(other);
            return result;
        }

        public override BooleanExpression Not()
        {
            return new NotExpression { NegatedExpr = this };
        }
    }

    public class NotExpression : BooleanExpression
    {
        public BooleanExpression NegatedExpr { get; set; }

        public override BooleanExpression And(BooleanExpression other)
        {
            return new AndExpression { Members = new HashableSet<BooleanExpression> { this, other } };
        }

        public override BooleanExpression Or(BooleanExpression other)
        {
            return new OrExpression { Members = new HashableSet<BooleanExpression> { this, other } };
        }

        public override BooleanExpression Not()
        {
            return NegatedExpr;
        }
    }

}
