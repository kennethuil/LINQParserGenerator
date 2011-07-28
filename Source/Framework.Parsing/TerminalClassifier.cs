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
            var charMovesBuild = new Dictionary<Expression<Func<TChar, bool>>, StateSet>();
            var eofMoveBuild = new StateSet();
            // All rejected terminals get propagated from the set that they're first seen.
            // NFA state sets with different sets of rejected terminals are considered distinct.
            eofMoveBuild.RejectedTerminals.UnionWith(EmptyIfNull(set.RejectedTerminals));
            eofMoveBuild.PossibleTerminals.UnionWith(EmptyIfNull(set.PossibleTerminals));

            // Build a mapping of distinct character tests to the set of NFA states reachable through one transition on that test.
            // Also, find the set of NFA states reachable through one transition on EOF.
            // NOTE: No recursion happens while these sets are being built - we can only put sets in the mapping or associate them with
            // DFA states after they're completely populated.
            HashSet<Expression<Func<TChar, bool>>> allMatchTests = new HashSet<Expression<Func<TChar, bool>>>();
            foreach (var state in set.States)
            {
                

                foreach (var transition in EmptyIfNull(state.Transitions))
                {
                    if (transition.CharacterMatchExpression != null)
                        allMatchTests.Add(_canonicalizer.GetInstance(transition.CharacterMatchExpression));
                }
            }
            foreach (var state in set.States)
            {
                foreach (var transition in EmptyIfNull(state.Transitions))
                {
                    var matchExpression = _canonicalizer.GetInstance(transition.CharacterMatchExpression);
                    ISet<Expression<Func<TChar, bool>>> overlaps;
                    if (matchExpression != null && _overlaps.TryGetValue(matchExpression, out overlaps))
                    {
                        Expression<Func<TChar, bool>> anyOverlap = null;

                        // For every transition condition from this state that this transition condition overlaps, we modify the match
                        // expression on this transition to exclude all the overlapped transition conditions.
                        foreach (var overlap in overlaps.Intersect(allMatchTests))
                        {
                            anyOverlap = _expressionHelper.Or(anyOverlap, overlap);

                            // If A implies B, then B overlaps A.  Therefore we can check for implications here and add an extra transition
                            // on the more restrictive condition pointing to the target of the less restrictive condition, while we're working on
                            // the less restrictive condition and have its target.  This saves us from having to build an extra mapping.
                            if (Implies(overlap, matchExpression))
                                AddCharacterMove(set, transition, overlap, charMovesBuild);
                        }
                        
                        matchExpression = _expressionHelper.AndNot(matchExpression, anyOverlap);
                    }
                    if (matchExpression != null)
                    {
                        AddCharacterMove(set, transition, matchExpression, charMovesBuild);
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

            // Now that all sets are fully populated, we can recursively turn them into DFA states and attach transitions to them
            // to our result DFA state.
            foreach (var entry in charMovesBuild)
            {
                var charTest = entry.Key;
                var targetSet = entry.Value;
                EpsilonClosure(targetSet);
                var targetState = ConvertToDFA(mapping, targetSet);
                if (targetState == null)
                    continue;

                ((List<FiniteAutomatonStateTransition<TChar>>)result.Transitions).Add(new FiniteAutomatonStateTransition<TChar>
                {
                    CharacterMatchExpression = charTest,
                    Target = targetState
                });
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

        private void AddCharacterMove(StateSet set, FiniteAutomatonStateTransition<TChar> transition, Expression<Func<TChar, bool>> matchExpression, Dictionary<Expression<Func<TChar, bool>>, StateSet> charMovesBuild)
        {
            matchExpression = _canonicalizer.GetInstance(matchExpression);
            StateSet targetSet;
            if (!charMovesBuild.TryGetValue(matchExpression, out targetSet))
            {
                targetSet = new StateSet();
                // All rejected terminals get propagated from the set that they're first seen.
                // NFA state sets with different sets of rejected terminals are considered distinct.
                targetSet.RejectedTerminals.UnionWith(EmptyIfNull(set.RejectedTerminals));
                targetSet.PossibleTerminals.UnionWith(EmptyIfNull(set.PossibleTerminals));

                // Add to the charMovesBuild set.
                charMovesBuild.Add(matchExpression, targetSet);
            }
            // Merge the transition's target into the set
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

        public static FiniteAutomatonState<TChar> GetExpressionSequenceMatcher(IEnumerable<Expression<Func<TChar, bool>>> seq)
        {
            if (seq.Any())
            {
                var first = seq.First();
                var rest = seq.Skip(1);
                return new FiniteAutomatonState<TChar>
                {
                    Transitions = new[] {
                        new FiniteAutomatonStateTransition<TChar> {
                            CharacterMatchExpression = first,
                            Target = TerminalClassifier<TChar>.GetExpressionSequenceMatcher(rest)
                        }
                    }
                };
            }
            return new FiniteAutomatonState<TChar>
            {
                IsAccepting = true
            };
        }

        public static FiniteAutomatonState<TChar> GetExpressionSequenceMatcher(params Expression<Func<TChar, bool>>[] seq)
        {
            return GetExpressionSequenceMatcher((IEnumerable<Expression<Func<TChar, bool>>>)seq);
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
