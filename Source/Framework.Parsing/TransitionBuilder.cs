using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Framework.Parsing
{
    public class TransitionBuilder<TChar> where TChar : IComparable<TChar>, IEquatable<TChar>
    {
        FiniteAutomatonState<TChar> _initialState;
        FiniteAutomatonStateTransition<TChar> _currentTransition;

        public TransitionBuilder(FiniteAutomatonState<TChar> initialState, FiniteAutomatonStateTransition<TChar> current)
        {
            _initialState = initialState;
            _currentTransition = current;
        }

        public StateBuilder<TChar> Goto(FiniteAutomatonState<TChar> state)
        {
            _currentTransition.Target = state;
            return new StateBuilder<TChar>(_initialState, state);
        }

        public StateBuilder<TChar> GotoNew(out FiniteAutomatonState<TChar> state)
        {
            state = new FiniteAutomatonState<TChar> { };
            _currentTransition.Target = state;
            return new StateBuilder<TChar>(_initialState, state);
        }

        public StateBuilder<TChar> GotoNew()
        {
            var state = new FiniteAutomatonState<TChar> { };
            _currentTransition.Target = state;
            return new StateBuilder<TChar>(_initialState, state);
        }
    }
}
