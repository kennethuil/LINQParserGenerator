using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Framework.Parsing
{
    [Serializable]
    public abstract class LRParseAction<TChar> where TChar : IComparable<TChar>, IEquatable<TChar>
    {
    }
}
