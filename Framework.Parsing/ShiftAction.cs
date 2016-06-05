using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Framework.Parsing
{
    [Serializable]
    public sealed class ShiftAction<TChar> : LRParseAction<TChar> where TChar : IComparable<TChar>, IEquatable<TChar>
    {
        public LRParseState<TChar> TargetState { get; set; }
    }
}
