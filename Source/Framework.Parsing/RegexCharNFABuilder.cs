using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using Framework.CodeGen;
using Framework.CodeGen.Expressions;


namespace Framework.Parsing
{
    public class RegexCharNFABuilder : RegexNFABuilder<char>
    {
        static Func<string, FiniteAutomatonState<char>> _regexCompiler;

        public static Func<string, FiniteAutomatonState<char>> RegexCompiler
        {
            get
            {
                var instance = Interlocked.CompareExchange(ref _regexCompiler, null, null);
                if (instance != null)
                    return instance;
                var expressionHelper = new ExpressionHelper();
                var regexNFABuilder = new RegexCharNFABuilder(expressionHelper);
                var expr = regexNFABuilder.CreateRegexParser("Framework.Parsing.RegexCharNFABuilder.Compiler");
                
                var regexCompiler = expr.Compile();
                Interlocked.CompareExchange(ref _regexCompiler, regexCompiler, null);
                return _regexCompiler;
            }
        }

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

}
