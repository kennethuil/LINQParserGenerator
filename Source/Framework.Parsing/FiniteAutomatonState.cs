using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Framework.Parsing
{
    
    public class FiniteAutomatonState<TChar>
        where TChar : IComparable<TChar>, IEquatable<TChar>
    {
        /// <summary>
        /// All of the possible transitions out of this state.
        /// </summary>
        public IEnumerable<FiniteAutomatonStateTransition<TChar>> Transitions { get; set; }

        /// <summary>
        /// True if this state is an accepting state; i.e., reaching this state means that the text matches.
        /// </summary>
        public bool IsAccepting { get; set; }

        /// <summary>
        /// True if this state is a rejecting state; i.e., reaching this state means that the text does not match.
        /// </summary>
        public bool IsRejecting { get; set; }

        /// <summary>
        /// Reaching this state means that the given terminals have been matched.
        /// </summary>
        public IEnumerable<Terminal<TChar>> AcceptTerminals { get; set; }

        /// <summary>
        /// Reaching this state means that each of these given terminals can possibly match given one or more sequences of additional characters.
        /// </summary>
        public IEnumerable<Terminal<TChar>> PossibleTerminals { get; set; }

        /// <summary>
        /// Reaching this state means that the given terminals are definitely not in the process of being matched.
        /// </summary>
        public IEnumerable<Terminal<TChar>> RejectTerminals { get; set; }
    }
}
