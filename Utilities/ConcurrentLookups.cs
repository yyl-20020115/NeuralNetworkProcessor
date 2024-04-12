using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Utilities;

public class ConcurrentLookups<TKey, TValue>: Lookups<TKey, TValue>
    where TKey : notnull {
    public ConcurrentLookups() { }
    public ConcurrentLookups(IEnumerable<KeyValuePair<TKey, ICollection<TValue>>> pairs)
    {
        foreach(var pair in pairs) this[pair.Key] = pair.Value;
    }
    public ICollection<TValue> this[TKey key]
    {
        get => this.Data.GetOrAdd(key, new ConcurrentList<TValue>());
        set
        {
            var list = this[key];
            foreach (var v in value) list.Add(v);
        }
    }
    public ConcurrentDictionary<TKey, ICollection<TValue>> Data { get; set; } = new();
    public int Count => ((ICollection<KeyValuePair<TKey, ICollection<TValue>>>)Data).Count;
    public bool IsReadOnly => ((ICollection<KeyValuePair<TKey, ICollection<TValue>>>)Data).IsReadOnly;
    public ICollection<TKey> Keys => ((IDictionary<TKey, ICollection<TValue>>)Data).Keys;
    public ICollection<ICollection<TValue>> Values => ((IDictionary<TKey, ICollection<TValue>>)Data).Values;
    public bool IsSynchronized => ((ICollection)Data).IsSynchronized;
    public object SyncRoot => ((ICollection)Data).SyncRoot;
    public void Add(KeyValuePair<TKey, ICollection<TValue>> item) => ((ICollection<KeyValuePair<TKey, ICollection<TValue>>>)Data).Add(item);
    public void Add(TKey key, ICollection<TValue> value) => ((IDictionary<TKey, ICollection<TValue>>)Data).Add(key, value);
    public void Clear() => ((ICollection<KeyValuePair<TKey, ICollection<TValue>>>)Data).Clear();
    public bool Contains(KeyValuePair<TKey, ICollection<TValue>> item) => ((ICollection<KeyValuePair<TKey, ICollection<TValue>>>)Data).Contains(item);
    public bool ContainsKey(TKey key) => ((IDictionary<TKey, ICollection<TValue>>)Data).ContainsKey(key);
    public void CopyTo(KeyValuePair<TKey, ICollection<TValue>>[] array, int arrayIndex) => ((ICollection<KeyValuePair<TKey, ICollection<TValue>>>)Data).CopyTo(array, arrayIndex);
    public void CopyTo(Array array, int index) => ((ICollection)Data).CopyTo(array, index);
    public IEnumerator<KeyValuePair<TKey, ICollection<TValue>>> GetEnumerator() => ((IEnumerable<KeyValuePair<TKey, ICollection<TValue>>>)Data).GetEnumerator();
    public bool Remove(KeyValuePair<TKey, ICollection<TValue>> item) => ((ICollection<KeyValuePair<TKey, ICollection<TValue>>>)Data).Remove(item);
    public bool Remove(TKey key) => ((IDictionary<TKey, ICollection<TValue>>)Data).Remove(key);
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out ICollection<TValue> value) => ((IDictionary<TKey, ICollection<TValue>>)Data).TryGetValue(key, out value);
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Data).GetEnumerator();
}
