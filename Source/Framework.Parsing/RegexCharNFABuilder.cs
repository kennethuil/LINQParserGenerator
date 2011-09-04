using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Framework.CodeGen;


namespace Framework.Parsing
{
    public class RegexCharNFABuilder : RegexNFABuilder<char>
    {
        public RegexCharNFABuilder(IExpressionHelper expressionHelper) : base(expressionHelper)
        {
        }

        public static NFAFragment<char> SpecificChar(char ch)
        {
            var end = new FiniteAutomatonState<char>
            {
            };
            return new NFAFragment<char>
            {
                Begin = new FiniteAutomatonState<char>
                {
                    Transitions = new[] {
                        new FiniteAutomatonStateTransition<char> {
                            Target = end,
                            Characters = new HashSet<char> {ch}
                        }
                    }
                },
                End = end
            };
        }

        public static NFAFragment<char> WordChar()
        {
            var end = new FiniteAutomatonState<char>
            {
            };
            return new NFAFragment<char>
            {
                Begin = new FiniteAutomatonState<char>
                {
                    Transitions = new[] {
                        new FiniteAutomatonStateTransition<char> {
                            Target = end,
                            Characters = Utilities.Union(Utilities.AllLetters(), Utilities.AllDigits())
                        }
                    }
                },
                End = end
            };  
        }

        public static NFAFragment<char> NonWordChar()
        {
            var frag = WordChar();
            frag.Begin.Transitions.First().MatchAllExcept = true;
            return frag;
        }

        public static NFAFragment<char> Digit()
        {
            var end = new FiniteAutomatonState<char>
            {
            };
            return new NFAFragment<char>
            {
                Begin = new FiniteAutomatonState<char>
                {
                    Transitions = new[] {
                        new FiniteAutomatonStateTransition<char> {
                            Target = end,
                            Characters = Utilities.AllDigits()
                        }
                    }
                },
                End = end
            };  
        }

        public static NFAFragment<char> NonDigit()
        {
            var frag = Digit();
            frag.Begin.Transitions.First().MatchAllExcept = true;
            return frag;
        }

        public static NFAFragment<char> Whitespace()
        {
            var end = new FiniteAutomatonState<char>
            {
            };
            return new NFAFragment<char>
            {
                Begin = new FiniteAutomatonState<char>
                {
                    Transitions = new[] {
                        new FiniteAutomatonStateTransition<char> {
                            Target = end,
                            Characters = Utilities.AllWhitespace()
                        }
                    }
                },
                End = end
            };  
        }

        public static NFAFragment<char> NonWhitespace()
        {
            var frag = Whitespace();
            frag.Begin.Transitions.First().MatchAllExcept = true;
            return frag;
        }

        public static NFAFragment<char> Range(char lower, char upper)
        {
            var end = new FiniteAutomatonState<char>
            {
            };
            HashSet<char> chars = new HashSet<char>();
            char ch;

            // Loop to fill all but upper, then add upper separately.
            // That way, having upper == char.MaxValue won't cause an infinite loop.
            for (ch = lower; ch < upper; ++ch)
                chars.Add(ch);
            chars.Add(upper);
            
            return new NFAFragment<char>
            {
                Begin = new FiniteAutomatonState<char>
                {
                    Transitions = new[] {
                        new FiniteAutomatonStateTransition<char> {
                            Target = end,
                            Characters = chars
                        }
                    }
                },
                End = end
            }; 
        }

        public override Expression<Func<char, NFAFragment<char>>> GetSpecificCharFragmentExpression()
        {
            return (c) => SpecificChar(c);
        }

        public override Expression<Func<NFAFragment<char>>> GetWordCharFragmentExpression()
        {
            return () => WordChar();
        }

        public override Expression<Func<NFAFragment<char>>> GetNonWordCharFragmentExpression()
        {
            return () => NonWordChar();
        }

        public override Expression<Func<NFAFragment<char>>> GetWhitespaceFragmentExpression()
        {
            return () => Whitespace();
        }

        public override Expression<Func<NFAFragment<char>>> GetNonWhitespaceFragmentExpression()
        {
            return () => NonWhitespace();
        }

        public override Expression<Func<NFAFragment<char>>> GetDigitFragmentExpression()
        {
            return () => Digit();
        }

        public override Expression<Func<NFAFragment<char>>> GetNonDigitFragmentExpression()
        {
            return () => NonDigit();
        }

        public override Expression<Func<char, char, NFAFragment<char>>> GetRangeFragmentExpression()
        {
            return (lower, upper) => Range(lower, upper);
        }
    }

    /*
    public class RegexCharNFABuilder : RegexNFABuilderGen
    {



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
