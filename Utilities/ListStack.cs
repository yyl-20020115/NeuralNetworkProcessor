using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Utilities;

//
// 摘要:
//     Represents a variable size last-in-first-out (LIFO) collection of instances of
//     the same specified type in the form of LinkedList<T>
//
// 类型参数:
//   T:
//     Specifies the type of elements in the stack.
public class ListStack<T>
    : IEnumerable<T>, IEnumerable, IReadOnlyCollection<T>, ICollection
    where T : notnull
{
    public List<T> Data { get; init; } = new List<T>();
    public int Count => this.Data.Count;
    public bool IsSynchronized => ((ICollection)Data).IsSynchronized;
    public object SyncRoot => ((ICollection)Data).SyncRoot;
    public ListStack() { }
    public ListStack(IEnumerable<T> collection)
        => this.Data = new List<T>(collection);
    public void Clear() => this.Data.Clear();
    public bool Contains(T item) => this.Data.Contains(item);
    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)Data).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Data).GetEnumerator();
    public void CopyTo(Array array, int index) => (Data as ICollection).CopyTo(array, index);
    public void CopyTo(T[] array, int index) => Data.CopyTo(array, index);
    public bool Remove(T item) => this.Data.Remove(item);
    public T Peek()
        => this.Count == 0
        ? throw new InvalidOperationException()
        : this.Data[^1]
        ;
    public T Pop()
    {
        if (this.Count == 0) throw new InvalidOperationException();
        var top = this.Data[^1];
        this.Data.RemoveAt(this.Data.Count - 1);
        return top;
    }
    public T? Top => this.TryPeek(out var top) ? top : default;
    public T Push(T item) { this.Data.Add(item); return item; }
    public T Add(T item) { this.Push(item); return item; }
    public void Append(IEnumerable<T> items) => this.Data.AddRange(items);
    public T[] ToArray() => this.Data.ToArray();
    public bool TryPeek([MaybeNullWhen(false)] out T result)
    {
        var hasAny = this.Count > 0;
        result = hasAny 
            ? this.Data[^1] 
            : default
            ;
        return hasAny;
    }
    public bool TryPop([MaybeNullWhen(false)] out T result)
    {
        if (this.TryPeek(out result))
        {
            this.Data.RemoveAt(this.Data.Count-1);
            return true;
        }
        return false;
    }
}