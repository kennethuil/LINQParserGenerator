using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Framework.Parsing
{
    public class Utilities
    {
        public static ISet<char> AllCharacters()
        {
            HashSet<char> result = new HashSet<char>();
            char ch = (char)0;
            do {
                result.Add(ch);
                ++ch;
            } while (ch < (char)0xffff);
            return result;
        }

        public static ISet<char> AllLetters()
        {
            HashSet<char> result = new HashSet<char>();
            char ch;
            for (ch = 'A'; ch <= 'Z'; ++ch)
                result.Add(ch);

            for (ch = 'a'; ch <= 'z'; ++ch)
                result.Add(ch);
            return result;
        }

        public static ISet<char> AllDigits()
        {
            HashSet<char> result = new HashSet<char>();
            char ch;
            for (ch = '0'; ch <= '9'; ++ch)
                result.Add(ch);
            return result;
        }

        public static ISet<char> AllWhitespace()
        {
            return new HashSet<char> { ' ', '\t', '\r', '\n' };
        }

        public static ISet<T> Union<T>(ISet<T> first, ISet<T> second)
        {
            var result = new HashableSet<T>(first);
            result.UnionWith(second);
            return result;
        }

        public static ISet<T> Intersect<T>(ISet<T> first, ISet<T> second)
        {
            var result = new HashableSet<T>(first);
            result.IntersectWith(second);
            return result;
        }

        public static ISet<T> Except<T>(ISet<T> first, ISet<T> second)
        {
            var result = new HashableSet<T>(first);
            result.ExceptWith(second);
            return result;
        }

        public static FiniteAutomatonState<char> MatchLiteralCaseInsensitive(IEnumerable<char> seq)
        {
            if (seq.Any())
            {
                var first = seq.First();
                var rest = seq.Skip(1);
                return new FiniteAutomatonState<char>
                {
                    Transitions = new[] {
                        new FiniteAutomatonStateTransition<char> {
                            Characters = new HashSet<char> {char.ToLower(first), char.ToUpper(first)},
                            Target = MatchLiteralCaseInsensitive(rest)
                        }
                    }
                };

            }
            return new FiniteAutomatonState<char>
            {
                IsAccepting = true
            };
        }
    }
}
