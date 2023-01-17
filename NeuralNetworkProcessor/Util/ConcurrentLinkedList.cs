using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuralNetworkProcessor.Util;

public class ConcurrentLinkedList<T> : LinkedList<T>, IRangeCollection<T>
{
    public IRangeCollection<T> AddRange(IEnumerable<T> items)
    {
        foreach(var value in items)
        {
            this.AddLast(value);
        }
        return this;
    }
}
