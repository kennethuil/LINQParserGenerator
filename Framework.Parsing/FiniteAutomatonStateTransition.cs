﻿using System;
using System.Collections.Generic;

namespace Framework.Parsing
{
    public class FiniteAutomatonStateTransition<TChar>
        where TChar : IComparable<TChar>, IEquatable<TChar>
    {
        /// <summary>
        /// An expression used to determine whether this transition is allowed upon consuming the given character.
        /// </summary>
        //public Expression<Func<TChar, bool>> CharacterMatchExpression { get; set; }

        /// <summary>
        /// A set of characters that this transition matches
        /// </summary>
        public ISet<TChar> Characters { get; set; }

        /// <summary>
        ///  True iff the characters set should be interpreted as the set of characters *not* to match.
        /// </summary>
        public bool MatchAllExcept { get; set; }

        // TODO: Define a lambda that gets executed whenever this transition is taken, and have the Classifier generator create code to execute it.
        /// <summary>
        /// True if this transition is allowed at the end of the input.
        /// </summary>
        public bool MatchEof { get; set; }

        /// <summary>
        /// True if this transition may be taken without consuming a character from the input.
        /// </summary>
        public bool MatchEpsilon { get; set; }

        /// <summary>
        /// The target state of the transition.
        /// </summary>
        public FiniteAutomatonState<TChar> Target;

    }
}
