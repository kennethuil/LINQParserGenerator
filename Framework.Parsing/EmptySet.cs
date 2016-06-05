using System;
using System.Collections.Generic;

namespace Framework.Parsing
{
    public class EmptySetEnumerator<T> : IEnumerator<T>
    {
        public static readonly EmptySetEnumerator<T> Instance = new EmptySetEnumerator<T>();
        private EmptySetEnumerator()
        {
        }

        public T Current
        {
            get { throw new NotImplementedException(); }
        }

        public void Dispose()
        {
        }

        object System.Collections.IEnumerator.Current
        {
            get { throw new NotImplementedException(); }
        }

        public bool MoveNext()
        {
            return false;
        }

        public void Reset()
        {
        }
    }

    public class EmptySet<T> : ISet<T>
    {
        public static readonly EmptySet<T> Instance = new EmptySet<T>();
        private EmptySet()
        {
        }

        public bool Add(T item)
        {
            throw new NotImplementedException();
        }

        public void ExceptWith(IEnumerable<T> other)
        {
        }

        public void IntersectWith(IEnumerable<T> other)
        {
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            return !other.GetEnumerator().MoveNext();
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            return false;
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            return true;
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            return !other.GetEnumerator().MoveNext();
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            return false;
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            return !other.GetEnumerator().MoveNext();
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public void UnionWith(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        void ICollection<T>.Add(T item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(T item)
        {
            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
        }

        public int Count
        {
            get { return 0; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return EmptySetEnumerator<T>.Instance;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return EmptySetEnumerator<T>.Instance;
        }
    }
}
