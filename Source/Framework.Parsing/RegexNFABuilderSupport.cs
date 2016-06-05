using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Framework.CodeGen;

namespace Framework.Parsing
{

    public class RegexNFABuilderSupport
    {
        protected IExpressionHelper _expressionHelper;
        protected TerminalClassifier<char> _classifierGen;
        protected ParserGenerator<char> _parserGen;

        public RegexNFABuilderSupport(IExpressionHelper expressionHelper)
        {
            _expressionHelper = expressionHelper;
            _classifierGen = new TerminalClassifier<char>();
            _parserGen = new ParserGenerator<char>(expressionHelper);
        }

        public class ParserState<TChar> : StringInput where TChar : IComparable<TChar>, IEquatable<TChar>
        {
            public ParserState(string str)
                : base(str)
            {
            }

            int _terminal;

            public int Terminal
            {
                get { return _terminal; }
                set { _terminal = value; }
            }

            int _nonTerminal;
            public int NonTerminal
            {
                get { return _nonTerminal; }

                set { _nonTerminal = value; }
            }

            NFAFragment<TChar> _terminalValue;

            public NFAFragment<TChar> TerminalValue
            {
                get { return _terminalValue; }
                set { _terminalValue = value; }
            }

            char _terminalCharValue;
            public char TerminalCharValue
            {
                get { return _terminalCharValue; }
                set { _terminalCharValue = value; }
            }

            object _nonTerminalValue;

            public object NonTerminalValue
            {
                get { return _nonTerminalValue; }
                set { _nonTerminalValue = value; }
            }

            IList<NFAFragment<TChar>> _nonTerminalListValue;

            public IList<NFAFragment<TChar>> NonTerminalListValue
            {
                get { return _nonTerminalListValue; }
                set { _nonTerminalListValue = value; }
            }

            char _nonTerminalCharValue;

            public char NonTerminalCharValue
            {
                get { return _nonTerminalCharValue; }
                set { _nonTerminalCharValue = value; }
            }
        }

        public static NFAFragment<TChar> Alternate<TChar>(NFAFragment<TChar> a, NFAFragment<TChar> b)
            where TChar : IComparable<TChar>, IEquatable<TChar>
        {
            var newBegin = new FiniteAutomatonState<TChar>
            {
                Transitions = new[] {
                    new FiniteAutomatonStateTransition<TChar> {
                        MatchEpsilon = true,
                        Target = a.Begin
                    },
                    new FiniteAutomatonStateTransition<TChar> {
                        MatchEpsilon = true,
                        Target = b.Begin
                    }
                }
            };
            var newEnd = new FiniteAutomatonState<TChar>
            {

            };
            a.End.Transitions = new[]
            {
                new FiniteAutomatonStateTransition<TChar> {
                    MatchEpsilon = true,
                    Target = newEnd
                }
            };
            b.End.Transitions = new[]
            {
                new FiniteAutomatonStateTransition<TChar> {
                    MatchEpsilon = true,
                    Target = newEnd
                }
            };
            return new NFAFragment<TChar>
            {
                Begin = newBegin,
                End = newEnd
            };
        }

        public static NFAFragment<TChar> Concat<TChar>(NFAFragment<TChar> a, NFAFragment<TChar> b)
            where TChar : IComparable<TChar>, IEquatable<TChar>
        {
            a.End.Transitions = new[] {
                new FiniteAutomatonStateTransition<TChar> {
                    MatchEpsilon = true,
                    Target = b.Begin
                }
            };
            return new NFAFragment<TChar>
            {
                Begin = a.Begin,
                End = b.End
            };
        }

        public static NFAFragment<TChar> ZeroOrOne<TChar>(NFAFragment<TChar> x)
            where TChar : IComparable<TChar>, IEquatable<TChar>
        {
            var newEnd = new FiniteAutomatonState<TChar>
            {
            };

            x.End.Transitions = new[]
            {
                new FiniteAutomatonStateTransition<TChar> {
                    MatchEpsilon = true,
                    Target = newEnd
                }
            };
            return new NFAFragment<TChar>
            {
                Begin = new FiniteAutomatonState<TChar>
                {
                    Transitions = new[] {
                        new FiniteAutomatonStateTransition<TChar> {
                            MatchEpsilon = true,
                            Target = newEnd
                        },
                        new FiniteAutomatonStateTransition<TChar> {
                            MatchEpsilon = true,
                            Target = x.Begin
                        }
                    }
                },
                End = newEnd
            };
        }

        public static NFAFragment<TChar> ZeroOrMore<TChar>(NFAFragment<TChar> x)
            where TChar : IComparable<TChar>, IEquatable<TChar>
        {
            var newEnd = new FiniteAutomatonState<TChar>
            {
            };

            x.End.Transitions = new[]
            {
                new FiniteAutomatonStateTransition<TChar> {
                    MatchEpsilon = true,
                    Target = newEnd
                },
                new FiniteAutomatonStateTransition<TChar> {
                    MatchEpsilon = true,
                    Target = x.Begin
                }
            };
            return new NFAFragment<TChar>
            {
                Begin = new FiniteAutomatonState<TChar>
                {
                    Transitions = new[] {
                        new FiniteAutomatonStateTransition<TChar> {
                            MatchEpsilon = true,
                            Target = newEnd
                        },
                        new FiniteAutomatonStateTransition<TChar> {
                            MatchEpsilon = true,
                            Target = x.Begin
                        }
                    }
                },
                End = newEnd
            };
        }

        public static NFAFragment<TChar> OneOrMore<TChar>(NFAFragment<TChar> x)
    where TChar : IComparable<TChar>, IEquatable<TChar>
        {
            var newEnd = new FiniteAutomatonState<TChar>
            {
            };

            x.End.Transitions = new[]
            {
                new FiniteAutomatonStateTransition<TChar> {
                    MatchEpsilon = true,
                    Target = newEnd
                },
                new FiniteAutomatonStateTransition<TChar> {
                    MatchEpsilon = true,
                    Target = x.Begin
                }
            };
            return new NFAFragment<TChar>
            {
                Begin = new FiniteAutomatonState<TChar>
                {
                    Transitions = new[] {
                        new FiniteAutomatonStateTransition<TChar> {
                            MatchEpsilon = true,
                            Target = x.Begin
                        }
                    }
                },
                End = newEnd
            };
        }

        public static NFAFragment<TChar> Select<TChar>(IList<NFAFragment<TChar>> items)
            where TChar : IComparable<TChar>, IEquatable<TChar>
        {
            if (items.Count == 1)
                return items[0];

            var newEnd = new FiniteAutomatonState<TChar>
            {
            };
            foreach (var item in items)
            {
                item.End.Transitions = new[] {
                    new FiniteAutomatonStateTransition<TChar> {
                        MatchEpsilon = true,
                        Target = newEnd
                    }
                };
            }
            return new NFAFragment<TChar>
            {
                Begin = new FiniteAutomatonState<TChar>
                {
                    Transitions = from item in items
                                  select new FiniteAutomatonStateTransition<TChar>
                                  {
                                      MatchEpsilon = true,
                                      Target = item.Begin
                                  }
                },
                End = newEnd
            };
        }

        public static NFAFragment<TChar> SelectNot<TChar>(IList<NFAFragment<TChar>> items)
            where TChar : IComparable<TChar>, IEquatable<TChar>
        {
            // If the next character matches any of the items, we want the terminal to be rejected.
            // So we hook the FSA's for each item to a reject state with an epsilon transitions, meaning that
            // matching any of the item FSA's leads to immediate rejection.
            // In parallel, we have a transition that matches any character leading out of the entire FSA fragment,
            // so iff a character matches that transition and does not match any of the FSA's for the items,
            // parsing will continue.  (In the NFA abstraction, if any possible path leads to a reject state, the
            // string is rejected).
            var newEnd = new FiniteAutomatonState<TChar> { };
            var reject = new FiniteAutomatonState<TChar>
            {
                IsRejecting = true
            };
            foreach(var item in items)
            {
                item.End.Transitions = new[] {
                    new FiniteAutomatonStateTransition<TChar> {
                        MatchEpsilon = true,
                        Target = reject
                    }

                };
            }
            return new NFAFragment<TChar>
            {
                Begin = new FiniteAutomatonState<TChar>
                {
                    Transitions = (from item in items
                                   select new FiniteAutomatonStateTransition<TChar>
                                   {
                                       MatchEpsilon = true,
                                       Target = item.Begin
                                   }).Concat(new[] {
                                       // TODO: This assumes that consuming one instance of TChar is the same as consuming one character!  This is NOT always the case!
                                       new FiniteAutomatonStateTransition<TChar> {
                                           Characters = new HashSet<TChar>(),
                                           MatchAllExcept = true,
                                           Target = newEnd
                                       }
                                   })
                },
                End = newEnd
            };

        }


        public static IList<T> AppendToList<T>(IList<T> l, T item)
        {
            l.Add(item);
            return l;
        }

    
    }

    public class NFAFragment<TChar> where TChar : IComparable<TChar>, IEquatable<TChar>
    {
        FiniteAutomatonState<TChar> _begin;
        FiniteAutomatonState<TChar> _end;

        public FiniteAutomatonState<TChar> Begin
        {
            get { return _begin; }
            set { _begin = value; }
        }

        public FiniteAutomatonState<TChar> End
        {
            get { return _end; }
            set { _end = value; }
        }
    }
}
