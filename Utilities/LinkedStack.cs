using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Utilities
{
    //
    // 摘要:
    //     Represents a variable size last-in-first-out (LIFO) collection of instances of
    //     the same specified type in the form of LinkedList<T>
    //
    // 类型参数:
    //   T:
    //     Specifies the type of elements in the stack.
    public class LinkedStack<T> 
        : IEnumerable<T>, IEnumerable, IReadOnlyCollection<T>, ICollection
        where T: notnull
    {
        public LinkedList<T> Data { get; init; } = new LinkedList<T>();
        public int Count => this.Data.Count;
        public bool IsSynchronized => ((ICollection)Data).IsSynchronized;
        public object SyncRoot => ((ICollection)Data).SyncRoot;
        public LinkedStack() { }
        public LinkedStack(IEnumerable<T> collection) 
            => this.Data = new LinkedList<T>(collection);
        public void Clear() => this.Data.Clear();
        public bool Contains(T item) => this.Data.Contains(item);
        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)Data).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Data).GetEnumerator();
        public void CopyTo(Array array, int index) => (Data as ICollection).CopyTo(array, index);
        public void CopyTo(T[] array, int index) => Data.CopyTo(array, index);
        public T Peek()
            => this.Count == 0 ? throw new InvalidOperationException() : this.Data.First!.Value;
        public T Pop()
        {
            if (this.Count == 0) throw new InvalidOperationException();
            var top = this.Data.First!.Value;
            this.Data.RemoveFirst();
            return top;
        }
        public T? Top => this.TryPeek(out var top) ? top : default;
        public void Push(T item) => this.Data.AddFirst(item);
        public void Add(T item) => this.Push(item);
        public T[] ToArray() => this.Data.ToArray();
        public bool TryPeek([MaybeNullWhen(false)] out T result)
        {
            var hasAny = this.Count > 0;
            result = hasAny ? this.Data.First!.Value : default;
            return hasAny;
        }
        public bool TryPop([MaybeNullWhen(false)] out T result)
        {
            if (this.TryPeek(out result))
            {
                this.Data.RemoveFirst();
                return true;
            }
            return false;
        }
    }
}
