using System;
using System.Collections.Generic;
using System.Linq;

namespace NeuralNetworkProcessor.Util;

public class ConcurrentCollection<T> : ConcurrentBag<T>, IRangeCollection<T>
{
    public bool IsReadOnly => false;

    public bool Contains(T item)
        => Array.IndexOf([.. this], item) >= 0;

    public bool Remove(T item)
    {
        var array = this.ToArray();
        var index = Array.IndexOf(array, item);
        if(index >= 0)
            this.ClearAndAddRange(array.Where((a, i) => i != index));
        return index >= 0;
    }

    public IRangeCollection<T> AddRange(IEnumerable<T> items)
    {
        foreach(var item in items) this.Add(item);
        return this;
    }
}
