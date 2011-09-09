using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using Framework.CodeGen;
using Framework.Parsing;

namespace Framework.Parsing
{
    public class TerminalClassifier<TChar> where TChar : IComparable<TChar>, IEquatable<TChar>
    {
        Canonicalizer<Expression<Func<TChar, bool>>> _canonicalizer = new Canonicalizer<Expression<Func<TChar, bool>>>
            (new ExpressionEqualityComparer<Expression<Func<TChar, bool>>>());

        // For each entry, if entry.Key is true, everything in entry.Value might be true.  Also, everything in entry.Value might be false.
        IDictionary<Expression<Func<TChar, bool>>, ISet<Expression<Func<TChar, bool>>>> _overlaps = new Dictionary<Expression<Func<TChar, bool>>, ISet<Expression<Func<TChar, bool>>>>();

        // For each entry, if entry.Key is true, everything in entry.Value is definitely true.
        IDictionary<Expression<Func<TChar, bool>>, ISet<Expression<Func<TChar, bool>>>> _implies = new Dictionary<Expression<Func<TChar, bool>>, ISet<Expression<Func<TChar, bool>>>>();

        IExpressionHelper _expressionHelper;

        public TerminalClassifier(IExpressionHelper helper)
        {
            _expressionHelper = helper;
        }

        void AddRelationship(IDictionary<Expression<Func<TChar, bool>>, ISet<Expression<Func<TChar, bool>>>> relations,
            Expression<Func<TChar, bool>> a,
            Expression<Func<TChar, bool>> b)
        {
            ISet<Expression<Func<TChar, bool>>> targets;
            if (!relations.TryGetValue(a, out targets))
            {
                targets = new HashSet<Expression<Func<TChar,bool>>>();
                relations.Add(a, targets);
            }
            targets.Add(b);
        }
        
        public void AddImplies(Expression<Func<TChar, bool>> a, Expression<Func<TChar, bool>> b)
        {
            AddRelationship(_implies, _canonicalizer.GetInstance(a), 
                _canonicalizer.GetInstance(b));
            AddOverlaps(_canonicalizer.GetInstance(b), _canonicalizer.GetInstance(a));
        }

        public void AddOverlaps(Expression<Func<TChar, bool>> a, Expression<Func<TChar, bool>> b)
        {
            AddRelationship(_overlaps, _canonicalizer.GetInstance(a), _canonicalizer.GetInstance(b));
        }

        // A set of states that will be turned into a single DFA state.
        class StateSet
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
                StateSet other = obj as StateSet;
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

        bool Implies(Expression<Func<TChar, bool>> a, Expression<Func<TChar, bool>> b)
        {
            ISet<Expression<Func<TChar, bool>>> implied;
            if (!_implies.TryGetValue(a, out implied))
                return false;
            return implied.Contains(b);
        }

        IEnumerable<T> EmptyIfNull<T>(IEnumerable<T> e)
        {
            if (e == null)
                return new T[0];
            return e;
        }

        ICollection<T> EmptyIfNull<T>(ICollection<T> e)
        {
            if (e == null)
                return new T[0];
            return e;
        }

        IList<T> EmptyIfNull<T>(IList<T> e)
        {
            if (e == null)
                return new T[0];
            return e;
        }

        List<T> EmptyIfNull<T>(List<T> e)
        {
            if (e == null)
                return new List<T>();
            return e;
        }


        // Epsilon closure: Given a set of states, add in all states reachable from any member on an epsilon transition.
        void EpsilonClosure(StateSet ss)
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
        FiniteAutomatonState<TChar> CreateDFAState(StateSet set)
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
        FiniteAutomatonState<TChar> ConvertToDFA(IDictionary<StateSet, FiniteAutomatonState<TChar>> mapping, StateSet set)
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
            
            var eofMoveBuild = new StateSet();
            // All rejected terminals get propagated from the set that they're first seen.
            // NFA state sets with different sets of rejected terminals are considered distinct.
            eofMoveBuild.RejectedTerminals.UnionWith(EmptyIfNull(set.RejectedTerminals));
            eofMoveBuild.PossibleTerminals.UnionWith(EmptyIfNull(set.PossibleTerminals));

            // Build all of the target state sets.  Keep track of which characters map to which sets, and which
            // sets are mapped to by an EOF transition.
            var movesByChar = new Dictionary<TChar, StateSet>();
            StateSet exceptTarget = CreateTargetSet(set);

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
                            StateSet targetSet = movesByChar[ch];
                            MergeInTransitionTarget(transition, targetSet);
                        }

                        // New characters introduced by this transition
                        var newChars = EmptyIfNull(transition.Characters).Except(movesByChar.Keys);
                        foreach (var newChar in newChars)
                        {
                            StateSet targetSet = CreateTargetSet(set);

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
                            StateSet targetSet = CreateTargetSet(set);
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
                            StateSet targetSet = movesByChar[ch];
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
            var movesByTarget = new Dictionary<StateSet, ISet<TChar>>();
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

        private StateSet CreateTargetSet(StateSet set)
        {
            StateSet targetSet = new StateSet();
            // All rejected terminals get propagated from the set that they're first seen.
            // NFA state sets with different sets of rejected terminals are considered distinct.
            targetSet.RejectedTerminals.UnionWith(EmptyIfNull(set.RejectedTerminals));
            targetSet.PossibleTerminals.UnionWith(EmptyIfNull(set.PossibleTerminals));
            return targetSet;
        }

        private void MergeInState(FiniteAutomatonState<TChar> state, StateSet targetSet)
        {
            targetSet.States.Add(state);
            targetSet.RejectedTerminals.UnionWith(EmptyIfNull(state.RejectTerminals));
            targetSet.PossibleTerminals.UnionWith(EmptyIfNull(state.PossibleTerminals));
        }

        private void MergeInTransitionTarget(FiniteAutomatonStateTransition<TChar> transition, StateSet targetSet)
        {
            targetSet.States.Add(transition.Target);
            targetSet.RejectedTerminals.UnionWith(EmptyIfNull(transition.Target.RejectTerminals));
            targetSet.PossibleTerminals.UnionWith(EmptyIfNull(transition.Target.PossibleTerminals));
        }


        public FiniteAutomatonState<TChar> ConvertToDFA(FiniteAutomatonState<TChar> nsaBeginState)
        {
            // Start with the closure of the begin state.
            StateSet initial = new StateSet();
            initial.States.Add(nsaBeginState);
            EpsilonClosure(initial);

            var setToDfaState = new Dictionary<StateSet, FiniteAutomatonState<TChar>>();
            FiniteAutomatonState<TChar> newStart = ConvertToDFA(setToDfaState, initial);

            // TODO: Prune that puppy.

            return newStart;
        }

        void MarkStateFromTerminal(Terminal<TChar> terminal, FiniteAutomatonState<TChar> state, ISet<FiniteAutomatonState<TChar>> visited)
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

        FiniteAutomatonState<TChar> MarkStatesFromTerminal(Terminal<TChar> terminal)
        {
            MarkStateFromTerminal(terminal, terminal.InitialState, new HashSet<FiniteAutomatonState<TChar>>());
            return terminal.InitialState;
        }

        public FiniteAutomatonState<TChar> CombineRecognizers(ICollection<Terminal<TChar>> possibleTerminals)
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

        public static FiniteAutomatonState<TChar> GetSequenceMatcher(IEnumerable<ISet<TChar>> seq)
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
                            Target = TerminalClassifier<TChar>.GetSequenceMatcher(rest)
                        }
                    }
                };
            }
            return new FiniteAutomatonState<TChar>
            {
                IsAccepting = true
            };
        }

        public static FiniteAutomatonState<TChar> GetSequenceMatcher(params ISet<TChar>[] seq)
        {
            return GetSequenceMatcher((IEnumerable<ISet<TChar>>)seq);
        }

        public static FiniteAutomatonState<TChar> GetLiteralMatcher(IEnumerable<TChar> seq)
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

        public static Terminal<TChar> GetLiteralToken(String name, IEnumerable<TChar> seq)
        {
            return new Terminal<TChar> { Name = name, InitialState = GetLiteralMatcher(seq) };
        }

        public static FiniteAutomatonState<TChar> GetLiteralMatcher(params TChar[] seq)
        {
            return GetLiteralMatcher((IEnumerable<TChar>)seq);
        }

        public TerminalClassifierSession<TChar> Classifier(Type parseStateType, Type resultType)
        {
            return new TerminalClassifierSession<TChar>(this, parseStateType, resultType);
        }

        public TerminalClassifierSession<TChar, TParseState, THandlerResult> Classifier<TParseState, THandlerResult>()
        {
            return new TerminalClassifierSession<TChar, TParseState, THandlerResult>(this);
        }
    }
}
