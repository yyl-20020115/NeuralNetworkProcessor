using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Utilities;

public class DualList<T> :
    IProducerConsumerCollection<T>, IEnumerable<T>, IEnumerable, ICollection, IReadOnlyCollection<T>,
     ICollection<T>,  IList<T>,  IReadOnlyList<T>, IList
{
    protected List<T> list = new();
    protected ConcurrentBag<T> bag= new();

    public T this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    object? IList.this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public int Count => ((ICollection)bag).Count;

    public bool IsSynchronized => ((ICollection)bag).IsSynchronized;

    public object SyncRoot => ((ICollection)bag).SyncRoot;

    public bool IsReadOnly => throw new NotImplementedException();

    public bool IsFixedSize => throw new NotImplementedException();

    public void Add(T item)
    {
        throw new NotImplementedException();
    }

    public int Add(object? value)
    {
        throw new NotImplementedException();
    }

    public void Clear()
    {
        throw new NotImplementedException();
    }

    public bool Contains(T item)
    {
        throw new NotImplementedException();
    }

    public bool Contains(object? value)
    {
        throw new NotImplementedException();
    }

    public void CopyTo(T[] array, int index)
    {
        ((IProducerConsumerCollection<T>)bag).CopyTo(array, index);
    }

    public void CopyTo(Array array, int index)
    {
        ((ICollection)bag).CopyTo(array, index);
    }

    public IEnumerator<T> GetEnumerator()
    {
        return ((IEnumerable<T>)bag).GetEnumerator();
    }

    public int IndexOf(T item)
    {
        throw new NotImplementedException();
    }

    public int IndexOf(object? value)
    {
        throw new NotImplementedException();
    }

    public void Insert(int index, T item)
    {
        throw new NotImplementedException();
    }

    public void Insert(int index, object? value)
    {
        throw new NotImplementedException();
    }

    public bool Remove(T item)
    {
        throw new NotImplementedException();
    }

    public void Remove(object? value)
    {
        throw new NotImplementedException();
    }

    public void RemoveAt(int index)
    {
        throw new NotImplementedException();
    }

    public T[] ToArray()
    {
        return ((IProducerConsumerCollection<T>)bag).ToArray();
    }

    public bool TryAdd(T item)
    {
        return ((IProducerConsumerCollection<T>)bag).TryAdd(item);
    }

    public bool TryTake([MaybeNullWhen(false)] out T item)
    {
        return ((IProducerConsumerCollection<T>)bag).TryTake(out item);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)bag).GetEnumerator();
    }
}
