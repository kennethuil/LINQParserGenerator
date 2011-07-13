using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Framework.Parsing;

namespace Framework.Parsing
{
    [Serializable]
    public class Eof<TChar> : Terminal<TChar>
        where TChar : IComparable<TChar>, IEquatable<TChar>
    {
        private Eof()
        {
            var accept = new FiniteAutomatonState<TChar>
            {
                IsAccepting = true
            };
            this.InitialState = new FiniteAutomatonState<TChar>
            {
                Transitions = new FiniteAutomatonStateTransition<TChar>[]
                {
                    new FiniteAutomatonStateTransition<TChar>
                    {
                        MatchEof = true,
                        Target = accept
                    }
                }
            };
            this.Name = "<EOF>";
        }

        public static readonly Eof<TChar> Instance = new Eof<TChar>();
    }
}
