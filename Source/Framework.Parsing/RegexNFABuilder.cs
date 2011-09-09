using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Framework.CodeGen;

namespace Framework.Parsing
{
    public abstract class RegexNFABuilder<TChar> where TChar : IComparable<TChar>, IEquatable<TChar>
    {
        IExpressionHelper _expressionHelper;
        TerminalClassifier<char> _classifierGen;
        ParserGenerator<char> _parserGen;
        public RegexNFABuilder(IExpressionHelper expressionHelper)
        {
            _expressionHelper = expressionHelper;
            _classifierGen = new TerminalClassifier<char>(expressionHelper);
            _parserGen = new ParserGenerator<char>(expressionHelper);
        }

        public RegexGrammar<NFAFragment<TChar>> CreateRegexConversionGrammar(string prefix)
        {
            RegexGrammar<NFAFragment<TChar>> g = new RegexGrammar<NFAFragment<TChar>>();

            // Set actions for rules and terminal matches.

            // Individual characters, escapes, etc.
            g.DigitEscape.NonCapturingAction = GetDigitFragmentExpression();
            g.NonDigitEscape.NonCapturingAction = GetNonDigitFragmentExpression();
            g.WordCharEscape.NonCapturingAction = GetWordCharFragmentExpression();
            g.NonWordCharEscape.NonCapturingAction = GetNonWordCharFragmentExpression();
            g.WhitespaceEscape.NonCapturingAction = GetWhitespaceFragmentExpression();
            g.NonWhitespaceEscape.NonCapturingAction = GetNonWhitespaceFragmentExpression();
            g.SpecificCharMatchRule.Action = GetSpecificCharFragmentExpression();
            
            // NFA/regex compositions.
            
            g.AlternateRule.Action =
                ((a, b) => RegexNFABuilderSupport.Alternate(a, b));
            g.ConcatRule.Action =
                ((a, b) => RegexNFABuilderSupport.Concat(a, b));
            g.OneOrMoreRule.Action =
                ((a) => RegexNFABuilderSupport.OneOrMore(a));
            g.SelectCharRule.Action =
                ((l) => RegexNFABuilderSupport.Select(l));
            g.SelectNotCharRule.Action =
                ((l) => RegexNFABuilderSupport.SelectNot(l));
            g.SelectRangeCharRule.Action = GetRangeFragmentExpression();
            g.ZeroOrMoreRule.Action =
                ((x) => RegexNFABuilderSupport.ZeroOrMore(x));
            g.ZeroOrOneRule.Action =
                ((x => RegexNFABuilderSupport.ZeroOrOne(x)));

            return g;
        }

        // Extensibility points.  Here the subclass tells us how to build NFA fragments that recognize single characters,
        // which depends on how instances of TChar in the stream combine to form characters.
        public abstract Expression<Func<char, NFAFragment<TChar>>> GetSpecificCharFragmentExpression();

        public abstract Expression<Func<NFAFragment<TChar>>> GetWordCharFragmentExpression();

        public abstract Expression<Func<NFAFragment<TChar>>> GetNonWordCharFragmentExpression();

        public abstract Expression<Func<NFAFragment<TChar>>> GetWhitespaceFragmentExpression();

        public abstract Expression<Func<NFAFragment<TChar>>> GetNonWhitespaceFragmentExpression();

        public abstract Expression<Func<NFAFragment<TChar>>> GetDigitFragmentExpression();

        public abstract Expression<Func<NFAFragment<TChar>>> GetNonDigitFragmentExpression();

        public abstract Expression<Func<char, char, NFAFragment<TChar>>> GetRangeFragmentExpression();


        public Expression<Func<string, FiniteAutomatonState<TChar>>> CreateRegexParser(string prefix)
        {
            var g = CreateRegexConversionGrammar(prefix);
            var parseTableBuilder = new LRParseTableBuilder();
            var parseTable = parseTableBuilder.BuildParseTable(g);

            var classifier = _classifierGen.Classifier<RegexNFABuilderSupport.ParserState<TChar>, NFAFragment<TChar>>()
                .HasCurrentCharExprIs(ps => ps.HasCurrentChar())
                .CurrentCharExprIs(ps => ps.CurrentChar())
                .MoveNextCharExprIs(ps => ps.MoveNextChar())
                .MarkPosExprIs(ps => ps.MarkPos())
                .UnmarkPosExprIs(ps => ps.UnmarkPos())
                .GetFromMarkExprIs(ps => ps.GetFromMarkedPos());

            var parser = _parserGen.NewSession<RegexNFABuilderSupport.ParserState<TChar>>()
                .TerminalIs(ps => ps.Terminal)
                .NonTerminalIs(ps => ps.NonTerminal)
                .TerminalValueExprIs<NFAFragment<TChar>>(ps => ps.TerminalValue)
                .TerminalValueExprIs<char>(ps=>ps.TerminalCharValue)
                //.NonTerminalValueExprIs<NFAFragment<TChar>>(ps => ps.NonTerminalValue)
                //.NonTerminalValueExprIs<IList<NFAFragment<TChar>>>(ps => ps.NonTerminalListValue)
                //.NonTerminalValueExprIs<char>(ps=>ps.NonTerminalCharValue)
                .NonTerminalValueExprIs<object>(ps=>ps.NonTerminalValue)
                .NonTerminalValueExprIs<char>(ps=>ps.NonTerminalCharValue)
                .IncludeSymbols(true)
                .Generate("ParseExpr", parseTable, classifier);

            var expString = Expression.Parameter(typeof(string), "regexString");
            var expState = Expression.Parameter(typeof(RegexNFABuilderSupport.ParserState<TChar>), "state");
            var expFragment = Expression.Parameter(typeof(NFAFragment<TChar>), "fragment");

            Expression<Func<string, FiniteAutomatonState<TChar>>> converter =
                Expression.Lambda<Func<string, FiniteAutomatonState<TChar>>>(
                    Expression.Block(new[] { expState, expFragment },
                        Expression.Assign(expState, Expression.New(typeof(RegexNFABuilderSupport.ParserState<TChar>).GetConstructor(new[] { typeof(string) }), expString)),
                        Expression.Invoke(parser, expState),
                        Expression.Assign(expFragment, Expression.Convert(Expression.Property(expState, "NonTerminalValue"), typeof(NFAFragment<TChar>))),
                        Expression.Assign(
                            Expression.Property(Expression.PropertyOrField(expFragment, "End"), "IsAccepting"),
                            Expression.Constant(true)),
                        Expression.PropertyOrField(expFragment, "Begin")), expString);
            return converter;
        }
    }
}
