using System;
using System.Collections.Generic;

namespace Framework.Parsing
{
    public static class ParsingExtensions
    {
        /// <summary>
        /// Get all of the terminals that can possibly be valid when shifting to the LR(1) state nextState
        /// </summary>
        /// <param name="nextState">The state being shifted to after consuming the next terminal</param>
        /// <returns>The set of all terminals that can possibly be valid when shifting to nextState</returns>
        public static ISet<Terminal<TChar>> GetPossibleTerminals<TChar>(this LRParseState<TChar> nextState)
            where TChar : IComparable<TChar>, IEquatable<TChar>
        {
            var result = new HashableSet<Terminal<TChar>>();
            foreach (var entry in nextState.Actions)
            {
                var term = entry.Key;
                if (term != Eof<TChar>.Instance)
                    result.Add(term);
            }
            return result;
        }
    }
}
