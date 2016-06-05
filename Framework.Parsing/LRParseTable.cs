using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Framework.Parsing
{
    [Serializable]
    public class LRParseTable<TChar> where TChar : IComparable<TChar>, IEquatable<TChar>
    {
        public IList<GrammarRule> Rules { get; set; }
        public IList<LRParseState<TChar>> States { get; set; }

        // TODO: Dependency-inject and use integrated logging.
        public void Dump()
        {
            // Number the states
            int i = 0;
            var numbering = new Dictionary<LRParseState<TChar>, int>();
            foreach (var state in States)
            {
                numbering[state] = i;
                ++i;
            }

            // Now loop through the states and log them out.
            foreach (var state in States)
            {
                foreach (var entry in state.Actions)
                {
                    var symbol = entry.Key;

                    var actions = entry.Value;
                    foreach (var action in actions)
                    {
                        Debug.Write("Action(" + numbering[state] + ", " + symbol + ") = ");
                        if (action is ShiftAction<TChar>)
                        {
                            Debug.WriteLine("shift " + numbering[((ShiftAction<TChar>)action).TargetState]);
                        }
                        else if (action is ReduceAction<TChar>)
                        {
                            Debug.WriteLine("reduce " + ((ReduceAction<TChar>)action).ReductionRule);
                        }
                        else if (action is AcceptAction<TChar>)
                        {
                            Debug.WriteLine("accept");
                        }
                        else
                        {
                            Debug.WriteLine("???");
                        }
                    }
                }
                foreach (var entry in state.Goto)
                {
                    var symbol = entry.Key;
                    var targetState = entry.Value;
                    Debug.WriteLine("GOTO(" + numbering[state] + ", " + symbol + ") = " + numbering[targetState]);
                }
            }
        }
    }
}
