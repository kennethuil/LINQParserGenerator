using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Framework.Parsing
{
    public class Canonicalizer<T> : IEnumerable<T> where T : class
    {
        Dictionary<T, T> _table;

        public Canonicalizer()
        {
            _table = new Dictionary<T, T>();
        }

        public Canonicalizer(IEqualityComparer<T> comparer)
        {
            _table = new Dictionary<T, T>(comparer);
        }

        public Canonicalizer(IEnumerable<T> initialSet, IEqualityComparer<T> comparer)
        {
            _table = new Dictionary<T, T>(comparer);
            foreach (var x in initialSet)
            {
                GetInstance(x);
            }
        }

        /// <summary>
        /// Returns the canonical instance from this set.  If no equivalent instance has been previously canonicalized, "proposed"
        /// will become the canonical instance.  Therefore, a reference equality check between "proposed" and the return value will tell you
        /// whether the proposed instance is newly added.
        /// </summary>
        /// <param name="proposed"></param>
        /// <returns></returns>
        public T GetInstance(T proposed)
        {
            if (proposed == null)
                return null;
            T result;
            if (_table.TryGetValue(proposed, out result))
                return result;

            _table.Add(proposed, proposed);
            return proposed;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _table.Keys.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _table.Keys.GetEnumerator();
        }
    }
}
