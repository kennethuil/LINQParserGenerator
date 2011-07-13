using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Framework.Parsing
{
    public class AcceptAction<TChar> : LRParseAction<TChar> where TChar : IComparable<TChar>, IEquatable<TChar>
    {
    }
}
