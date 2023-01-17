using System.Collections;
using System.Collections.Generic;

namespace Utilities
{
    public class LinkedTailQueue<T> :ICollection<T>
    {
        public LinkedList<T> Data { get; } = new();
        public LinkedTailQueue() { }
        public LinkedTailQueue(IEnumerable<T> ts) => this.Data = new LinkedList<T>(ts);
        public T? Head => this.Data.Count > 0 ? this.Data.First!.Value : default;
        public T? Tail => this.Data.Count > 0 ? this.Data.Last!.Value : default;
        public bool TailReplace(T item, bool orappend = false)
        {
            if (this.Data.Count > 0)
            {
                this.Data.Last!.ValueRef = item;
                return true;
            }
            else if (orappend)
            {
                return this.TailAppend(item);
            }
            return false;
        }
        public bool TailAppend(T item)
        {
            this.Add(item);
            return true;
        }
        public int Count => this.Data.Count;
        public bool IsReadOnly => (this.Data as ICollection<T>).IsReadOnly;
        public void Add(T item) => this.Data.AddLast(item);
        public void Clear() => this.Data.Clear();
        public bool Contains(T item) => this.Data.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => this.Data.CopyTo(array, arrayIndex);
        public IEnumerator<T> GetEnumerator() => this.Data.GetEnumerator();
        public bool Remove(T item) => this.Data.Remove(item);
        IEnumerator IEnumerable.GetEnumerator() => this.Data.GetEnumerator();
    }
}
