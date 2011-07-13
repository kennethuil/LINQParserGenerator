using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Framework.CodeGen;

namespace Framework.Parsing
{
    public class RegexNFABuilderGen
    {
        protected IExpressionHelper _expressionHelper;
        protected TerminalClassifier<char> _classifierGen;
        protected ParserGenerator<char> _parserGen;

        public RegexNFABuilderGen(IExpressionHelper expressionHelper)
        {
            _expressionHelper = expressionHelper;
            _classifierGen = new TerminalClassifier<char>(expressionHelper);
            _parserGen = new ParserGenerator<char>(expressionHelper);
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

        public class ParserState<TChar> : StringInput where TChar : IComparable<TChar>, IEquatable<TChar>
        {
            public ParserState(string str)
                : base(str)
            {
            }

            public int Terminal
            {
                get;
                set;
            }

            public int NonTerminal
            {
                get;
                set;
            }

            public NFAFragment<TChar> TerminalValue
            {
                get;
                set;
            }

            public NFAFragment<TChar> NonTerminalValue
            {
                get;
                set;
            }

            public List<NFAFragment<TChar>> NonTerminalListValue
            {
                get;
                set;
            }
        }

        public RegexGrammar CreateRegexGrammar()
        {
            return new RegexGrammar();
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

        public static NFAFragment<TChar> Range<TChar>(NFAFragment<TChar> lower, NFAFragment<TChar> upper)
            where TChar : IComparable<TChar>, IEquatable<TChar>
        {
            // NOTE: assuming lower and upper each have their Begin and End joined directly by a single-character transition.
            // TODO: That assumption doesn't hold in the case of multi-byte characters parsed as a byte stream.
            ParameterExpression ch = Expression.Parameter(typeof(TChar), "ch");
            var newEnd = new FiniteAutomatonState<TChar> { };
            Expression newExprBody = GetRangeExprBody(lower, upper, ch);
            var newExpr = Expression.Lambda<Func<TChar, bool>>(newExprBody, true, ch);
            return new NFAFragment<TChar>
            {
                Begin = new FiniteAutomatonState<TChar>
                {
                    Transitions = new[] {
                        new FiniteAutomatonStateTransition<TChar> {
                            CharacterMatchExpression = newExpr,
                            Target = newEnd
                        }
                    }
                },
                End = newEnd
            };
        }

        public static NFAFragment<TChar> RangeNot<TChar>(NFAFragment<TChar> lower, NFAFragment<TChar> upper)
            where TChar : IComparable<TChar>, IEquatable<TChar>
        {
            // NOTE: assuming lower and upper each have their Begin and End joined directly by a single-character transition.
            // TODO: That assumption doesn't hold in the case of multi-byte characters parsed as a byte stream.
            ParameterExpression ch = Expression.Parameter(typeof(TChar), "ch");
            var newEnd = new FiniteAutomatonState<TChar> { };
            Expression newExprBody = GetRangeExprBody(lower, upper, ch);
            var newExpr = Expression.Lambda<Func<TChar, bool>>(Expression.Not(newExprBody), true, ch);
            return new NFAFragment<TChar>
            {
                Begin = new FiniteAutomatonState<TChar>
                {
                    Transitions = new[] {
                        new FiniteAutomatonStateTransition<TChar> {
                            CharacterMatchExpression = newExpr,
                            Target = newEnd
                        }
                    }
                },
                End = newEnd
            };
        }

        private static Expression GetRangeExprBody<TChar>(NFAFragment<TChar> lower, NFAFragment<TChar> upper, ParameterExpression ch)
            where TChar : IComparable<TChar>, IEquatable<TChar>
        {
            Expression lowerCharExpr = GetSingleCharExpr(lower);
            Expression upperCharExpr = GetSingleCharExpr(upper);
            return Expression.AndAlso
                (Expression.GreaterThanOrEqual(ch, lowerCharExpr),
                 Expression.LessThanOrEqual(ch, upperCharExpr));
        }

        private static Expression GetSingleCharExpr<TChar>(NFAFragment<TChar> lower)
            where TChar : IComparable<TChar>, IEquatable<TChar>
        {
            var lowerTransition = lower.Begin.Transitions.First();
            var lowerExpr = lowerTransition.CharacterMatchExpression;
            return ((BinaryExpression)lowerExpr.Body).Right;
        }

        public static NFAFragment<TChar> SelectNot<TChar>(IList<NFAFragment<TChar>> items)
            where TChar : IComparable<TChar>, IEquatable<TChar>
        {
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
                                       new FiniteAutomatonStateTransition<TChar> {
                                           MatchEpsilon = true,
                                           Target = newEnd
                                       }
                                   })
                },
                End = newEnd
            };

        }


        public static List<T> AppendToList<T>(List<T> l, T item)
        {
            l.Add(item);
            return l;
        }


    }
}
