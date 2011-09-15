using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Framework.Parsing
{
    public class StateBuilder<TChar> where TChar : IComparable<TChar>, IEquatable<TChar>
    {
        FiniteAutomatonState<TChar> _initialState;
        FiniteAutomatonState<TChar> _currentState;

        public StateBuilder()
        {
            _initialState = new FiniteAutomatonState<TChar> { };
            _currentState = _initialState;
        }

        public StateBuilder(FiniteAutomatonState<TChar> initial)
        {
            _initialState = initial;
            _currentState = initial;
        }

        public StateBuilder(FiniteAutomatonState<TChar> initial, FiniteAutomatonState<TChar> current)
        {
            _initialState = initial;
            _currentState = current;
        }

        public FiniteAutomatonState<TChar> InitialState
        {
            get
            {
                return _initialState;
            }
        }

        public TransitionBuilder<TChar> OnAnything()
        {
            return OnAnyExcept(new TChar[0]);
        }

        public TransitionBuilder<TChar> OnAnyOf(params TChar[] chars)
        {
            var set = new HashableSet<TChar>(chars);

            return OnAnyOf(set);
        }

        private TransitionBuilder<TChar> OnAnyOf(HashableSet<TChar> set)
        {
            List<FiniteAutomatonStateTransition<TChar>> transitions;

            if (_currentState.Transitions == null)
            {
                transitions = new List<FiniteAutomatonStateTransition<TChar>>();
                _currentState.Transitions = transitions;

            }
            else
                transitions = _currentState.Transitions as List<FiniteAutomatonStateTransition<TChar>>;

            var transition = new FiniteAutomatonStateTransition<TChar>
                                 {
                                     Characters = set
                                 };
            transitions.Add(transition);
            return new TransitionBuilder<TChar>(_initialState, transition);
        }

        public StateBuilder<TChar> From(FiniteAutomatonState<TChar> from)
        {
            return new StateBuilder<TChar>(_initialState, from);
        }

        public TransitionBuilder<TChar> OnAnyOf(params ISet<TChar>[] sets)
        {
            HashableSet<TChar> result = new HashableSet<TChar>();
            foreach (var set in sets)
            {
                result.UnionWith(set);
            }
            return OnAnyOf(result);
        }

        public TransitionBuilder<TChar> OnAnyExcept(params ISet<TChar>[] sets)
        {
            HashableSet<TChar> result = new HashableSet<TChar>();
            foreach (var set in sets)
            {
                result.UnionWith(set);
            }
            return OnAnyExcept(result);
        }

        public TransitionBuilder<TChar> OnAnyExcept(params TChar[] chars)
        {
            var set = new HashableSet<TChar>(chars);

            return OnAnyExcept(set);
        }

        private TransitionBuilder<TChar> OnAnyExcept(HashableSet<TChar> set)
        {
            List<FiniteAutomatonStateTransition<TChar>> transitions;

            if (_currentState.Transitions == null)
            {
                transitions = new List<FiniteAutomatonStateTransition<TChar>>();
                _currentState.Transitions = transitions;

            }
            else
                transitions = _currentState.Transitions as List<FiniteAutomatonStateTransition<TChar>>;

            var transition = new FiniteAutomatonStateTransition<TChar>
                                 {
                                     Characters = set,
                                     MatchAllExcept = true
                                 };
            transitions.Add(transition);
            return new TransitionBuilder<TChar>(_initialState, transition);
        }

        public StateBuilder<TChar> Accept()
        {
            _currentState.IsAccepting = true;
            return this;
        }

        public StateBuilder<TChar> Reject()
        {
            _currentState.IsRejecting = true;
            return this;
        }
    }
}
