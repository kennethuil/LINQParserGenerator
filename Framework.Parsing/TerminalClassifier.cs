using System;
using System.Collections.Generic;
using System.Linq;

namespace Framework.Parsing
{
    public class TerminalClassifier
    {
        public TerminalClassifier()
        {
        }

        // A set of states that will be turned into a single DFA state.
        class StateSet<TChar> where TChar : IComparable<TChar>, IEquatable<TChar>
        {
            HashSet<FiniteAutomatonState<TChar>> _states = new HashableSet<FiniteAutomatonState<TChar>>();
            HashSet<Terminal<TChar>> _rejected = new HashableSet<Terminal<TChar>>();
            HashSet<Terminal<TChar>> _possible = new HashableSet<Terminal<TChar>>();
            public HashSet<FiniteAutomatonState<TChar>> States { get { return _states; } }
            public HashSet<Terminal<TChar>> RejectedTerminals { get { return _rejected; } }
            public HashSet<Terminal<TChar>> PossibleTerminals { get { return _possible; } }

            // Two sets are equal if they have the same set of states and rejected terminals.
            public override bool  Equals(object obj)
            {
                StateSet<TChar> other = obj as StateSet<TChar>;
                if (other == null)
                    return false;

                return States.Equals(other.States) &&
                    RejectedTerminals.Equals(other.RejectedTerminals);
            }

            public override int GetHashCode()
            {
                return States.GetHashCode() + RejectedTerminals.GetHashCode();
            }
        }

        static IEnumerable<T> EmptyIfNull<T>(IEnumerable<T> e)
        {
            if (e == null)
                return new T[0];
            return e;
        }

        static ICollection<T> EmptyIfNull<T>(ICollection<T> e)
        {
            if (e == null)
                return new T[0];
            return e;
        }

        static IList<T> EmptyIfNull<T>(IList<T> e)
        {
            if (e == null)
                return new T[0];
            return e;
        }

        static List<T> EmptyIfNull<T>(List<T> e)
        {
            if (e == null)
                return new List<T>();
            return e;
        }


        // Epsilon closure: Given a set of states, add in all states reachable from any member on an epsilon transition.
        static void EpsilonClosure<TChar>(StateSet<TChar> ss) where TChar : IComparable<TChar>, IEquatable<TChar>
        {
            // Initialize the worklist with the passed-in set of states.
            HashSet<FiniteAutomatonState<TChar>> worklist = new HashableSet<FiniteAutomatonState<TChar>>();
            worklist.UnionWith(ss.States);

            while (worklist.Count > 0)
            {
                var newStates = new HashSet<FiniteAutomatonState<TChar>>();
                foreach (var state in worklist)
                {
                    foreach (var transition in EmptyIfNull(state.Transitions))
                    {
                        if (transition.MatchEpsilon)
                        {
                            if (!ss.States.Contains(transition.Target))
                            {
                                // Found a new state that can be reached through an epsilon transition.
                                newStates.Add(transition.Target);
                                ss.States.Add(transition.Target);
                            }
                        }       
                    }
                    // If any state rejects a terminal, the whole set does.
                    ss.RejectedTerminals.UnionWith(EmptyIfNull(state.RejectTerminals));

                    // The set of possible terminals is the union of all states' possible terminals
                    ss.PossibleTerminals.UnionWith(EmptyIfNull(state.PossibleTerminals));
                }
                // Replace the worklist with the most recent set of new states discovered.
                worklist = newStates;
            }
        }

        // Create a new DFA state to represent a state set.
        static FiniteAutomatonState<TChar> CreateDFAState<TChar>(StateSet<TChar> set) where TChar : IComparable<TChar>, IEquatable<TChar>
        {
            FiniteAutomatonState<TChar> result;

            HashSet<Terminal<TChar>> accepted = new HashSet<Terminal<TChar>>();
            foreach (var state in set.States)
            {
                accepted.UnionWith(EmptyIfNull(state.AcceptTerminals));
            }
            accepted.ExceptWith(set.RejectedTerminals);
            result = new FiniteAutomatonState<TChar>
            {
                AcceptTerminals = accepted,
                IsAccepting = accepted.Count > 0,
                IsRejecting = false,
                RejectTerminals = set.RejectedTerminals,
                PossibleTerminals = set.PossibleTerminals,
                Transitions = new List<FiniteAutomatonStateTransition<TChar>>()
            };


            return result;
        }

        // Convert a state set and all state sets reachable from it to a DFA subgraph.
        static FiniteAutomatonState<TChar> ConvertToDFA<TChar>(IDictionary<StateSet<TChar>, FiniteAutomatonState<TChar>> mapping, StateSet<TChar> set)
             where TChar : IComparable<TChar>, IEquatable<TChar>
        {
            // If there's already a DFA state in the mapping, return it and bail out.
            FiniteAutomatonState<TChar> result;
            if (mapping.TryGetValue(set, out result))
                return result;

            // Otherwise, create a new DFA state and add it to the mapping.
            result = CreateDFAState(set);
            mapping.Add(set, result);

            // NOTE: Expression<T> does not override Object.Equals or Object.getHashCode, so this table relies on reference equality.
            //var charMovesBuild = new Dictionary<Expression<Func<TChar, bool>>, StateSet>();
            
            var eofMoveBuild = new StateSet<TChar>();
            // All rejected terminals get propagated from the set that they're first seen.
            // NFA state sets with different sets of rejected terminals are considered distinct.
            eofMoveBuild.RejectedTerminals.UnionWith(EmptyIfNull(set.RejectedTerminals));
            eofMoveBuild.PossibleTerminals.UnionWith(EmptyIfNull(set.PossibleTerminals));

            // Build all of the target state sets.  Keep track of which characters map to which sets, and which
            // sets are mapped to by an EOF transition.
            var movesByChar = new Dictionary<TChar, StateSet<TChar>>();
            StateSet<TChar> exceptTarget = CreateTargetSet(set);

            foreach (var state in set.States)
            {
                foreach (var transition in EmptyIfNull(state.Transitions))
                {
                    if (!transition.MatchAllExcept)
                    {
                        // Characters already being tracked
                        var existingChars = EmptyIfNull(transition.Characters).Intersect(movesByChar.Keys);
                        foreach (var ch in existingChars)
                        {
                            StateSet<TChar> targetSet = movesByChar[ch];
                            MergeInTransitionTarget(transition, targetSet);
                        }

                        // New characters introduced by this transition
                        var newChars = EmptyIfNull(transition.Characters).Except(movesByChar.Keys);
                        foreach (var newChar in newChars)
                        {
                            StateSet<TChar> targetSet = CreateTargetSet(set);

                            // Add to the charMovesBuild set.
                            movesByChar.Add(newChar, targetSet);
                            // Merge the transition's target into the set
                            MergeInTransitionTarget(transition, targetSet);
                        }
                    }
                    else
                    {
                        // New characters introduced by this transition.
                        var newChars = EmptyIfNull(transition.Characters).Except(movesByChar.Keys);
                        foreach (var newChar in newChars)
                        {
                            StateSet<TChar> targetSet = CreateTargetSet(set);
                            // Add to the charMovesBuild set
                            movesByChar.Add(newChar, targetSet);
                            // Do NOT merge the transition's target into the set.  The transition is telling us what
                            // characters do NOT match.  We'll merge later on when we go through all the except transitions
                            // to find out which tracked characters are not mentioned in each transition.
                        }
                    }
                    if (transition.MatchEof)
                    {
                        // Merge the transition's target into the set
                        eofMoveBuild.States.Add(transition.Target);
                        eofMoveBuild.RejectedTerminals.UnionWith(EmptyIfNull(transition.Target.RejectTerminals));
                        eofMoveBuild.PossibleTerminals.UnionWith(EmptyIfNull(transition.Target.PossibleTerminals));
                    }
                }
            }
            foreach (var state in set.States)
            {
                foreach (var transition in EmptyIfNull(state.Transitions))
                {
                    if (transition.MatchAllExcept)
                    {
                        // Tracked characters that aren't in the transition's except set.  They match this transition.
                        var includedChars = movesByChar.Keys.Except(EmptyIfNull(transition.Characters));
                        foreach (var ch in includedChars)
                        {
                            StateSet<TChar> targetSet = movesByChar[ch];
                            MergeInTransitionTarget(transition, targetSet);
                        }
                        // The exceptTarget is the set of all states pointed to by an except transition.
                        // The resulting DFA will have one except transition pointing to it, indicating that all
                        // characters not mentioned in any transition will match.
                        MergeInState(transition.Target, exceptTarget);
                    }
                }
            }

            // Now that all target state sets are complete, it's time to group sets of characters together by target state set.
            var movesByTarget = new Dictionary<StateSet<TChar>, ISet<TChar>>();
            foreach (var entry in movesByChar)
            {
                var ch = entry.Key;
                var target = entry.Value;
                ISet<TChar> charSet;
                if (!movesByTarget.TryGetValue(target, out charSet))
                {
                    charSet = new HashSet<TChar>();
                    movesByTarget.Add(target, charSet);
                }
                charSet.Add(ch);
            }

            foreach (var entry in movesByTarget)
            {
                var charSet = entry.Value;
                var targetSet = entry.Key;
                if (targetSet.States.Count == 0)
                    continue;

                EpsilonClosure(targetSet);
                var targetState = ConvertToDFA(mapping, targetSet);
                if (targetState == null)
                    continue;

                ((List<FiniteAutomatonStateTransition<TChar>>)result.Transitions).Add(new FiniteAutomatonStateTransition<TChar>
                {
                    Characters = charSet,
                    Target = targetState
                });
            }

            // Add an except transition to the except state if applicable.
            if (exceptTarget.States.Count != 0)
            {
                EpsilonClosure(exceptTarget);
                var targetState = ConvertToDFA(mapping, exceptTarget);
                if (targetState != null)
                {
                    ((List<FiniteAutomatonStateTransition<TChar>>)result.Transitions).Add(new FiniteAutomatonStateTransition<TChar>
                    {
                        Characters = new HashSet<TChar>(movesByChar.Keys),
                        MatchAllExcept = true,
                        Target = targetState
                    });
                }
            }

            
            if (eofMoveBuild.States.Count != 0)
            {
                EpsilonClosure(eofMoveBuild);
                var targetState = ConvertToDFA(mapping, eofMoveBuild);
                ((List<FiniteAutomatonStateTransition<TChar>>)result.Transitions).Add(new FiniteAutomatonStateTransition<TChar>
                {
                    MatchEof = true,
                    Target = targetState
                });
            }

            // The result DFA state and everything recursively reachable from it is now built.
            return result;
        }

        private static StateSet<TChar> CreateTargetSet<TChar>(StateSet<TChar> set) where TChar : IComparable<TChar>, IEquatable<TChar>
        {
            StateSet<TChar> targetSet = new StateSet<TChar>();
            // All rejected terminals get propagated from the set that they're first seen.
            // NFA state sets with different sets of rejected terminals are considered distinct.
            targetSet.RejectedTerminals.UnionWith(EmptyIfNull(set.RejectedTerminals));
            targetSet.PossibleTerminals.UnionWith(EmptyIfNull(set.PossibleTerminals));
            return targetSet;
        }

        private static void MergeInState<TChar>(FiniteAutomatonState<TChar> state, StateSet<TChar> targetSet) where TChar : IComparable<TChar>, IEquatable<TChar>
        {
            targetSet.States.Add(state);
            targetSet.RejectedTerminals.UnionWith(EmptyIfNull(state.RejectTerminals));
            targetSet.PossibleTerminals.UnionWith(EmptyIfNull(state.PossibleTerminals));
        }

        private static void MergeInTransitionTarget<TChar>(FiniteAutomatonStateTransition<TChar> transition, StateSet<TChar> targetSet)
             where TChar : IComparable<TChar>, IEquatable<TChar>
        {
            targetSet.States.Add(transition.Target);
            targetSet.RejectedTerminals.UnionWith(EmptyIfNull(transition.Target.RejectTerminals));
            targetSet.PossibleTerminals.UnionWith(EmptyIfNull(transition.Target.PossibleTerminals));
        }


        public static FiniteAutomatonState<TChar> ConvertToDFA<TChar>(FiniteAutomatonState<TChar> nsaBeginState)
             where TChar : IComparable<TChar>, IEquatable<TChar>
        {
            // Start with the closure of the begin state.
            StateSet<TChar> initial = new StateSet<TChar>();
            initial.States.Add(nsaBeginState);
            EpsilonClosure(initial);

            var setToDfaState = new Dictionary<StateSet<TChar>, FiniteAutomatonState<TChar>>();
            FiniteAutomatonState<TChar> newStart = ConvertToDFA(setToDfaState, initial);

            // TODO: Prune that puppy.

            return newStart;
        }

        static void MarkStateFromTerminal<TChar>(Terminal<TChar> terminal, FiniteAutomatonState<TChar> state, ISet<FiniteAutomatonState<TChar>> visited)
             where TChar : IComparable<TChar>, IEquatable<TChar>
        {
            if (visited.Contains(state))
                return;
            visited.Add(state);

            state.PossibleTerminals = new[] { terminal };
            if (state.IsAccepting && (state.AcceptTerminals == null || state.AcceptTerminals.Count() == 0))
            {
                state.AcceptTerminals = new[] { terminal };
            }
            if (state.IsRejecting && (state.RejectTerminals == null || state.RejectTerminals.Count() == 0))
                state.RejectTerminals = new[] { terminal };

            foreach (var transition in EmptyIfNull(state.Transitions))
            {
                MarkStateFromTerminal(terminal, transition.Target, visited);
            }
        }

        static FiniteAutomatonState<TChar> MarkStatesFromTerminal<TChar>(Terminal<TChar> terminal)
             where TChar : IComparable<TChar>, IEquatable<TChar>
        {
            MarkStateFromTerminal(terminal, terminal.InitialState, new HashSet<FiniteAutomatonState<TChar>>());
            return terminal.InitialState;
        }

        static public FiniteAutomatonState<TChar> CombineRecognizers<TChar>(ICollection<Terminal<TChar>> possibleTerminals)
             where TChar : IComparable<TChar>, IEquatable<TChar>
        {
            
            FiniteAutomatonState<TChar> newStartState = new FiniteAutomatonState<TChar>
            {
                AcceptTerminals = new Terminal<TChar>[0],
                RejectTerminals = new Terminal<TChar>[0],
                Transitions = from x in possibleTerminals
                              select new FiniteAutomatonStateTransition<TChar>
                              {
                                  MatchEpsilon = true,
                                  Target = MarkStatesFromTerminal(x)
                              }
            };
            newStartState = ConvertToDFA(newStartState);
            return newStartState;
        }

        public static FiniteAutomatonState<TChar> GetSequenceMatcher<TChar>(IEnumerable<ISet<TChar>> seq)
             where TChar : IComparable<TChar>, IEquatable<TChar>
        {
            if (seq.Any())
            {
                var first = seq.First();
                var rest = seq.Skip(1);
                return new FiniteAutomatonState<TChar>
                {
                    Transitions = new[] {
                        new FiniteAutomatonStateTransition<TChar> {
                            Characters = first,
                            Target = TerminalClassifier.GetSequenceMatcher(rest)
                        }
                    }
                };
            }
            return new FiniteAutomatonState<TChar>
            {
                IsAccepting = true
            };
        }

        public static FiniteAutomatonState<TChar> GetSequenceMatcher<TChar>(params ISet<TChar>[] seq)
             where TChar : IComparable<TChar>, IEquatable<TChar>
        {
            return GetSequenceMatcher((IEnumerable<ISet<TChar>>)seq);
        }

        public static FiniteAutomatonState<TChar> GetLiteralMatcher<TChar>(IEnumerable<TChar> seq)
             where TChar : IComparable<TChar>, IEquatable<TChar>
        {
            if (seq.Any())
            {
                var first = seq.First();
                var rest = seq.Skip(1);
                return new FiniteAutomatonState<TChar>
                {
                    Transitions = new[] {
                        new FiniteAutomatonStateTransition<TChar> {
                            Characters = new HashSet<TChar> {first},
                            Target = GetLiteralMatcher(rest)
                        }
                    }
                };
                
            }
            return new FiniteAutomatonState<TChar>
            {
                IsAccepting = true
            };
        }

        public static Terminal<TChar> GetLiteralToken<TChar>(String name, IEnumerable<TChar> seq)
             where TChar : IComparable<TChar>, IEquatable<TChar>
        {
            return new Terminal<TChar> { Name = name, InitialState = GetLiteralMatcher(seq) };
        }

        /*
        public static FiniteAutomatonState<TChar> GetLiteralMatcher<TChar>(params TChar[] seq)
             where TChar : IComparable<TChar>, IEquatable<TChar>
        {
            return GetLiteralMatcher((IEnumerable<TChar>)seq);
        }

        
        public TerminalClassifierSession<TChar, TParseState, THandlerResult> Classifier<TParseState, THandlerResult>()
        {
            return new TerminalClassifierSession<TChar, TParseState, THandlerResult>();
        }
        */
    }
}
