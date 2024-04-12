using System.Collections;
using System.Collections.Generic;

namespace Utilities;

public class TailQueue<T> : ICollection<T>
{
    public TailQueue() { }
    public TailQueue(IEnumerable<T> ts) => this.Data.AddRange(ts);
    public T? Head => this.Data.Count > 0 ? this.Data[0] : default;
    public T? Tail => this.Data.Count > 0 ? this.Data[^1] : default;
    public bool TailReplace(T item, bool orappend = false)
    {
        if (this.Data.Count > 0)
        {
            this.Data[^1] = item;
            return true;
        }else if (orappend)
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
    public T this[int index] { get => this.Data[index]; set => this.Data[index] = value; }
    public List<T> Data { get; } = new();
    public int Count => this.Data.Count;
    public bool IsReadOnly => (this.Data as ICollection<T>).IsReadOnly;
    public void Add(T item) => this.Data.Add(item);
    public void Clear() => this.Data.Clear();
    public bool Contains(T item) => this.Data.Contains(item);
    public void CopyTo(T[] array, int arrayIndex) => this.Data.CopyTo(array, arrayIndex);
    public IEnumerator<T> GetEnumerator() => this.Data.GetEnumerator();
    public int IndexOf(T item) => this.Data.IndexOf(item);
    public void Insert(int index, T item) => this.Data.Insert(index, item);
    public bool Remove(T item) => this.Data.Remove(item);
    public void RemoveAt(int index) => this.Data.RemoveAt(index);
    IEnumerator IEnumerable.GetEnumerator() => this.Data.GetEnumerator();
}
