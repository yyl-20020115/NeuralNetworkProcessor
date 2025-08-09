using System.Collections.Generic;

namespace NeuralNetworkProcessor.Util;

public class ConcurrentLinkedList<T> : LinkedList<T>, IRangeCollection<T>
{
    public IRangeCollection<T> AddRange(IEnumerable<T> items)
    {
        foreach(var value in items)
            this.AddLast(value);
        return this;
    }
}
