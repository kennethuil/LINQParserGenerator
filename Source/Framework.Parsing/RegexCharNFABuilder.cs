using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Framework.CodeGen;


namespace Framework.Parsing
{
    /*
    public class RegexCharNFABuilder : RegexNFABuilderGen
    {
        static ISet<char> _any = AllCharacters();
        static ISet<char> _digit = new HashSet<char> { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
        static ISet<char> _notDigit = Except(_any, _digit);
        static ISet<char> _letter = AllLetters();
        static ISet<char> _word = Union(_digit, _letter);
        static ISet<char> _notWord = Except(_any, _word);
        static ISet<char> _whitespace = new HashSet<char> { '\t', '\r', '\n', ' ' };
        static ISet<char> _notWhitespace = Except(_any, _whitespace);


        private static HashSet<char> AllLetters()
        {
            char ch;
            HashSet<char> result = new HashSet<char>();
            for (ch = 'A'; ch <= 'Z'; ++ch)
            {
                result.Add(ch);
            }
            for (ch = 'a'; ch <= 'z'; ++ch)
            {
                result.Add(ch);
            }
            return result;
        }



        private static HashSet<char> AllCharacters()
        {
            char ch = '\x0000';
            HashSet<char> result = new HashSet<char>();
            do
            {
                result.Add(ch);
                ++ch;
            } while (ch != '\x0000');
            return result;
        }

         private static ISet<char> Except(ISet<char> first, ISet<char> second)
        {
            HashSet<char> result = new HashSet<char>(first);
            result.ExceptWith(second);
            return result;
        }

        private static ISet<char> Union(ISet<char> first, ISet<char> second)
        {
            HashSet<char> result = new HashSet<char>(first);
            result.UnionWith(second);
            return result;
        }

        private static ISet<char> Intersect(ISet<char> first, ISet<char> second)
        {
            HashSet<char> result = new HashSet<char>(first);
            result.IntersectWith(second);
            return result;
        }


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
            public IList<NFAFragment<char>> CurrentNonTerminalListValue { get; set; }
            public FiniteAutomatonState<char> CurrentRegexBeginState { get; set; }
        }

        public RegexCharNFABuilder(IExpressionHelper expressionHelper) : base(expressionHelper)
        {
        }

        /*
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
                classifier.AddImplies(entry.Value, _any);
            }
        }
        
        public static FiniteAutomatonState<char> GetLiteralMatcher(string literal)
        {
            int i;
            var current = new FiniteAutomatonState<char>
            {
                IsAccepting = true
            };
            for (i = literal.Length - 1; i >= 0; --i)
            {
                ParameterExpression x = Expression.Parameter(typeof(char), "x");

                current = new FiniteAutomatonState<char>
                {
                    Transitions = new[] {
                        new FiniteAutomatonStateTransition<char> {
                            //CharacterMatchExpression = SpecificCharMatchExpr(literal[i]),
                            CharactersMatched = new HashableSet<char> {literal[i]},
                            Target = current
                        }
                    }
                };
            }

            return current;
        }

        public static ISet<char> SpecificCharMatchExpr(char ch)
        {
            /*
            Expression<Func<char, bool>> expr;
            if (_specificCharExpr.TryGetValue(ch, out expr))
                return expr;

            var expCh = Expression.Parameter(typeof(char),"ch");
            expr = Expression.Lambda<Func<char,bool>>(
                Expression.Equal(expCh, Expression.Constant(ch)), expCh);
            _specificCharExpr[ch] = expr;
            return expr;
            
            return new HashSet<char> { ch };
        }

        public static NFAFragment<char> OtherChar(string ch)
        {
            ISet<char> charSet;
            if (ch[0] == '.')
                charSet = _any;
            //expr = SpecificCharMatchExpr(ch[0]);

            return SingleCharExpressionFragment(new HashSet<char> {ch[0]});
        }

        public static NFAFragment<char> SingleCharEscape(string esc)
        {
            var escapeChar = esc[1];
            ISet<char> expr;
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
            ISet<char> expr = null;

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

        private static NFAFragment<char> SingleCharExpressionFragment(ISet<char> expr)
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
                                                                                                                CharactersMatched = expr,
                                                                                                                Target = newEnd
                                                                                                            }
                                                               }
                                       },
                                       End = newEnd

                       };
        }

        public RegexGrammar<NFAFragment<char>> CreateRegexConversionGrammar(string prefix)
        {
            RegexGrammar<NFAFragment<char>> g = new RegexGrammar<NFAFragment<char>>();
            // Value types for symbols.


            g.OtherChar.StringAction = ((s) => OtherChar(s));
            g.SingleCharEscape.StringAction = ((s) => SingleCharEscape(s));
            g.CharClassEscape.StringAction = ((s) => CharClassEscape(s));

            g.AlternateRule.Action = 
                ((a, b) => Alternate(a, b));
            g.ConcatRule.Action = 
                ((a, b) => Concat(a, b));
            g.OneOrMoreRule.Action = 
                ((a) => OneOrMore(a));
            g.SelectCharListSingleonRule.Action = 
                ((a) => new List<NFAFragment<char>> { a });
            g.SelectCharListAppendRule.Action = 
                ((a, b) => AppendToList(a, b));
            g.SelectCharRule.Action = 
                ((l) => Select(l));
            g.SelectNotCharRule.Action = 
                ((l) => SelectNot(l));
            g.SelectRangeCharRule.Action = 
                ((a, b) => Range(a, b));
            g.ZeroOrMoreRule.Action = 
                ((x) => ZeroOrMore(x));
            g.ZeroOrOneRule.Action = 
                ((x => ZeroOrOne(x)));

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
                .NonTerminalValueExprIs<IList<NFAFragment<char>>>(ps => ps.CurrentNonTerminalListValue)
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
     */
}
