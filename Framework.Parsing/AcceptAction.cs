using System;


namespace Framework.Parsing
{
    public class AcceptAction<TChar> : LRParseAction<TChar> where TChar : IComparable<TChar>, IEquatable<TChar>
    {
    }
}
