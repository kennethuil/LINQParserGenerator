using System.Collections.Generic;

namespace Framework.Parsing
{
    public class SetComparer<Eltype> : IEqualityComparer<ISet<Eltype>>
    {

        public bool Equals(ISet<Eltype> x, ISet<Eltype> y)
        {
            if (x == null)
                return (y == null);
            if (y == null)
                return false;
            return x.SetEquals(y);
        }

        public int GetHashCode(ISet<Eltype> obj)
        {
            // Simple add of hashcodes isn't affected by ordering.
            int hashCode = 0;

            foreach (var el in obj)
            {
                hashCode += el.GetHashCode();
            }
            return hashCode;
        }
    }
}