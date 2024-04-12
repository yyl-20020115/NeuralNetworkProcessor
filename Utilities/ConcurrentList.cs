using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Utilities;

public class ConcurrentList<T> 
    : ConcurrentBag<T>
    , IList<T>
    , ICollection<T>
    , IEnumerable<T>
    , IEnumerable
    , IReadOnlyCollection<T>
    , IReadOnlyList<T>
    , ICollection
{
    public T this[int index] 
    {
        get => base.ToArray()[index];
        set { }
    }
    public bool IsReadOnly => false;
    public bool Contains(T item) => base.ToArray().Contains(item);
    public int IndexOf(T item) => Array.IndexOf(this.ToArray(), item);
    public void Insert(int index, T item) { }
    public bool Remove(T item) => false;
    public void RemoveAt(int index) { }
}
