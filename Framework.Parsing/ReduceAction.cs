using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Framework.Parsing
{
    [Serializable]
    public sealed class ReduceAction<TChar> : LRParseAction<TChar> where TChar : IComparable<TChar>, IEquatable<TChar>
    {
        public GrammarRule ReductionRule { get; set; }
    }
}
