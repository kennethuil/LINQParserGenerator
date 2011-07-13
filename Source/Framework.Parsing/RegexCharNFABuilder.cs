using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Framework.CodeGen;


namespace Framework.Parsing
{
    public class RegexCharNFABuilder : RegexNFABuilderGen
    {
        static IDictionary<char, Expression<Func<char, bool>>> _specificCharExpr = new Dictionary<char, Expression<Func<char, bool>>>();
        static Expression<Func<char, bool>> _any = (c) => true;
        static Expression<Func<char, bool>> _word = (c) => char.IsLetterOrDigit(c);
        static Expression<Func<char, bool>> _notWord = (c) => !char.IsLetterOrDigit(c);
        static Expression<Func<char, bool>> _whitespace = (c) => char.IsWhiteSpace(c);
        static Expression<Func<char, bool>> _notWhitespace = (c) => !char.IsWhiteSpace(c);
        static Expression<Func<char, bool>> _digit = (c) => char.IsDigit(c);
        static Expression<Func<char, bool>> _notDigit = (c) => !char.IsDigit(c);

        public class NFAConversionState : StringInput
        {
            public NFAConversionState(string str)
                : base(str)
            {
            }

            public int CurrentTerminal { get; set; }
            public int CurrentNonTerminal { get; set; }
            public NFAFragment<char> CurrentTerminalValue { get; set; }
            public NFAFragment<char> CurrentNonTerminalValue { get; set; }
            public List<NFAFragment<char>> CurrentNonTerminalListValue { get; set; }
            public FiniteAutomatonState<char> CurrentRegexBeginState { get; set; }
        }

        public RegexCharNFABuilder(IExpressionHelper expressionHelper) : base(expressionHelper)
        {
        }

        public void SetupImplies(TerminalClassifier<char> classifier)
        {
            classifier.AddImplies(_word, _any);
            classifier.AddImplies(_notWord, _any);
            classifier.AddImplies(_whitespace, _any);
            classifier.AddImplies(_notWhitespace, _any);
            classifier.AddImplies(_digit, _any);
            classifier.AddImplies(_notDigit, _any);

            classifier.AddImplies(_word, _notWhitespace);
            classifier.AddImplies(_whitespace, _notWord);
            classifier.AddImplies(_whitespace, _notDigit);
            classifier.AddImplies(_digit, _notWhitespace);
            classifier.AddImplies(_notWord, _notDigit);

            foreach (var entry in _specificCharExpr)
            {
                if (char.IsLetterOrDigit(entry.Key))
                {
                    classifier.AddImplies(entry.Value, _word);
                    classifier.AddImplies(entry.Value, _notWhitespace);
                }
                else
                {
                    classifier.AddImplies(entry.Value, _notWord);
                    classifier.AddImplies(entry.Value, _notDigit);
                }
                if (char.IsDigit(entry.Key))
                {
                    classifier.AddImplies(entry.Value, _digit);
                    classifier.AddImplies(entry.Value, _word);
                    classifier.AddImplies(entry.Value, _notWhitespace);
                }
                else
                {
                    classifier.AddImplies(entry.Value, _notDigit);
                }
                if (char.IsWhiteSpace(entry.Key))
                {
                    classifier.AddImplies(entry.Value, _whitespace);
                }
                else
                {
                    classifier.AddImplies(entry.Value, _notWhitespace);
                }

            }
        }

        public static Expression<Func<char,bool>> SpecificCharMatchExpr(char ch)
        {
            Expression<Func<char, bool>> expr;
            if (_specificCharExpr.TryGetValue(ch, out expr))
                return expr;

            var expCh = Expression.Parameter(typeof(char),"ch");
            expr = Expression.Lambda<Func<char,bool>>(
                Expression.Equal(expCh, Expression.Constant(ch)), expCh);
            _specificCharExpr[ch] = expr;
            return expr;

        }

        public static NFAFragment<char> OtherChar(string ch)
        {
            Expression<Func<char, bool>> expr;
            if (ch[0] == '.')
                expr = _any;
            expr = SpecificCharMatchExpr(ch[0]);

            return SingleCharExpressionFragment(expr);
        }

        public static NFAFragment<char> SingleCharEscape(string esc)
        {
            var escapeChar = esc[1];
            Expression<Func<char, bool>> expr;
            switch (escapeChar)
            {
                case 'a':
                    expr = SpecificCharMatchExpr((char)7);
                    break;
                case 'b':
                    expr = SpecificCharMatchExpr((char)8);
                    break;
                case 't':
                    expr = SpecificCharMatchExpr((char)9);
                    break;
                case 'r':
                    expr = SpecificCharMatchExpr((char)13);
                    break;
                case 'v':
                    expr = SpecificCharMatchExpr((char)11);
                    break;
                case 'f':
                    expr = SpecificCharMatchExpr((char)12);
                    break;
                case 'n':
                    expr = SpecificCharMatchExpr((char)10);
                    break;
                case 'e':
                    expr = SpecificCharMatchExpr((char)0x1b);
                    break;
                default:
                    expr = SpecificCharMatchExpr(escapeChar);
                    break;
            }

            return SingleCharExpressionFragment(expr);
        }

        public static NFAFragment<char> CharClassEscape(string esc)
        {
            var escapeChar = esc[1];
            Expression<Func<char, bool>> expr = null;

            switch (escapeChar)
            {
                case 'w':
                    expr = _word;
                    break;
                case 'W':
                    expr = _notWord;
                    break;
                case 's':
                    expr = _whitespace;
                    break;
                case 'S':
                    expr = _notWhitespace;
                    break;
                case 'd':
                    expr = _digit;
                    break;
                case 'D':
                    expr = _notDigit;
                    break;
            }
            return SingleCharExpressionFragment(expr);
        }

        private static NFAFragment<char> SingleCharExpressionFragment(Expression<Func<char, bool>> expr)
        {
            var newEnd = new FiniteAutomatonState<char>
                             {
                             };
            return new NFAFragment<char>
                       {
                           Begin = new FiniteAutomatonState<char>
                                       {
                                           Transitions = new[] {
                                                                   new FiniteAutomatonStateTransition<char> {
                                                                                                                CharacterMatchExpression = expr,
                                                                                                                Target = newEnd
                                                                                                            }
                                                               }
                                       },
                                       End = newEnd

                       };
        }

        public RegexGrammar CreateRegexConversionGrammar(string prefix)
        {
            RegexGrammar g = new RegexGrammar();
            // Value types for symbols.
            g.CharClassEscape.ValueType = typeof(NFAFragment<char>);
            g.CharList.ValueType = typeof(List<NFAFragment<char>>);
            g.ConcatExpr.ValueType = typeof(NFAFragment<char>);
            g.Expr.ValueType = typeof(NFAFragment<char>);
            g.OtherChar.ValueType = typeof(NFAFragment<char>);
            g.QuantExpr.ValueType = typeof(NFAFragment<char>);
            g.SelectChar.ValueType = typeof(NFAFragment<char>);
            g.SelectNotChar.ValueType = typeof(NFAFragment<char>);
            g.SelectRangeChar.ValueType = typeof(NFAFragment<char>);
            g.SimpleChar.ValueType = typeof(NFAFragment<char>);
            g.SingleChar.ValueType = typeof(NFAFragment<char>);
            g.SingleCharEscape.ValueType = typeof(NFAFragment<char>);
            g.SpecificChar.ValueType = typeof(NFAFragment<char>);
            g.SubExpr.ValueType = typeof(NFAFragment<char>);

            g.OtherChar.Action = ((Expression<Func<string, NFAFragment<char>>>)((s) => OtherChar(s)));
            g.SingleCharEscape.Action = ((Expression<Func<string, NFAFragment<char>>>)((s) => SingleCharEscape(s)));
            g.CharClassEscape.Action = ((Expression<Func<string, NFAFragment<char>>>)((s) => CharClassEscape(s)));

            g.AlternateRule.Action = ((Expression<Func<NFAFragment<char>, NFAFragment<char>, NFAFragment<char>>>)
                ((a, b) => Alternate(a, b)));
            g.ConcatRule.Action = ((Expression<Func<NFAFragment<char>, NFAFragment<char>, NFAFragment<char>>>)
                ((a, b) => Concat(a, b)));
            g.OneOrMoreRule.Action = ((Expression<Func<NFAFragment<char>, NFAFragment<char>>>)
                ((a) => OneOrMore(a)));
            g.SelectCharListSingleonRule.Action = ((Expression<Func<NFAFragment<char>, List<NFAFragment<char>>>>)
                ((a) => new List<NFAFragment<char>> { a }));
            g.SelectCharListAppendRule.Action = ((Expression<Func<List<NFAFragment<char>>, NFAFragment<char>, List<NFAFragment<char>>>>)
                ((a, b) => AppendToList(a, b)));
            g.SelectCharRule.Action = ((Expression<Func<List<NFAFragment<char>>, NFAFragment<char>>>)
                ((l) => Select(l)));
            g.SelectNotCharRule.Action = ((Expression<Func<List<NFAFragment<char>>, NFAFragment<char>>>)
                ((l) => SelectNot(l)));
            g.SelectRangeCharRule.Action = ((Expression<Func<NFAFragment<char>, NFAFragment<char>, NFAFragment<char>>>)
                ((a, b) => Range(a, b)));
            g.ZeroOrMoreRule.Action = ((Expression<Func<NFAFragment<char>, NFAFragment<char>>>)
                ((x) => ZeroOrMore(x)));

            return g;
        }

        public Expression<Func<string, FiniteAutomatonState<char>>> CreateRegexParser(string prefix)
        {
            var g = CreateRegexConversionGrammar(prefix);
            var parseTableBuilder = new LRParseTableBuilder();
            var parseTable = parseTableBuilder.BuildParseTable(g);

            var classifier = _classifierGen.Classifier<NFAConversionState, NFAFragment<char>>()
                .HasCurrentCharExprIs(ps => ps.HasCurrentChar())
                .CurrentCharExprIs(ps => ps.CurrentChar())
                .MoveNextCharExprIs(ps => ps.MoveNextChar())
                .MarkPosExprIs(ps => ps.MarkPos())
                .UnmarkPosExprIs(ps => ps.UnmarkPos())
                .GetFromMarkExprIs(ps => ps.GetFromMarkedPos());

            var parser = _parserGen.NewSession<NFAConversionState>()
                .TerminalIs(ps => ps.CurrentTerminal)
                .NonTerminalIs(ps => ps.CurrentNonTerminal)
                .TerminalValueExprIs<NFAFragment<char>>(ps => ps.CurrentTerminalValue)
                .NonTerminalValueExprIs<NFAFragment<char>>(ps => ps.CurrentNonTerminalValue)
                .NonTerminalValueExprIs<List<NFAFragment<char>>>(ps => ps.CurrentNonTerminalListValue)
                .NonTerminalValueExprIs<FiniteAutomatonState<char>>(ps => ps.CurrentRegexBeginState)
                .IncludeSymbols(true)
                .Generate("ParseExpr", parseTable, classifier);

            var expString = Expression.Parameter(typeof(string), "regexString");
            var expState = Expression.Parameter(typeof(NFAConversionState), "state");
            var expFragment = Expression.Parameter(typeof(NFAFragment<char>), "fragment");

            Expression<Func<string, FiniteAutomatonState<char>>> converter =
                Expression.Lambda<Func<string, FiniteAutomatonState<char>>>(
                    Expression.Block(new [] {expState, expFragment},
                        Expression.Assign(expState, Expression.New(typeof(NFAConversionState).GetConstructor(new[] { typeof(string) }), expString)),
                        Expression.Invoke(parser, expState),
                        Expression.Assign(expFragment, Expression.Property(expState, "CurrentNonTerminalValue")),
                        Expression.Assign(
                            Expression.Property(Expression.PropertyOrField(expFragment, "End"), "IsAccepting"),
                            Expression.Constant(true)),
                        Expression.PropertyOrField(expFragment, "Begin")), expString);
            return converter;
        }

    }
}
