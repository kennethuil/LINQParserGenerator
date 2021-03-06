﻿using System;
using System.Collections.Generic;

namespace Framework.Parsing
{
    [Serializable]
    public class LRParseState<TChar> where TChar : IComparable<TChar>, IEquatable<TChar>
    {
        public IDictionary<Terminal<TChar>, ICollection<LRParseAction<TChar>>> Actions { get; set; }
        public IDictionary<NonTerminal, LRParseState<TChar>> Goto { get; set; }
    }
}
