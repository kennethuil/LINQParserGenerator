using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Framework.Parsing
{
    /// <summary>
    /// A set that can itself be stored in a hash set or hash table.  Two instances are considered equal if every element in either set has an
    /// equal element in the other set.
    /// NOTE: Don't add or remove elements after the set is stored in a hash-based collection.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class HashableSet<T> : HashSet<T>
    {
        public override bool Equals(object obj)
        {
            if (!(obj is HashableSet<T>))
            {
                return false;
            }
            return this.SetEquals((IEnumerable<T>)obj);
        }

        public override int GetHashCode()
        {
            // Simple add of hashcodes isn't affected by ordering.
            int hashCode = 0;

            foreach (var el in this)
            {
                hashCode += el.GetHashCode();
            }
            return hashCode;
        }
    }
}
