using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Framework.Parsing
{
    public class ParseError<TChar> where TChar : IComparable<TChar>, IEquatable<TChar>
    {
        public ParseLocation Location { get; set; }

        public ICollection<string> ExpectedTerminalNames { get; set; }
    }
}
