using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuralNetworkProcessor.Util
{
    public class LinkedListQueue<T> : LinkedList<T>, IRangeCollection<T>
    {
        public LinkedListQueue()
            :base()
        {

        }
        public LinkedListQueue(IEnumerable<T> items) : base(items) { }
        public IRangeCollection<T> AddRange(IEnumerable<T> items)
        {
            foreach(var item in items)
                this.AddLast(item);
            return this;
        }
        public IRangeCollection<T> Enqueue(T item)
        {
            this.AddLast(item);
            return this;
        }
        public T Dequeue()
        {
            if (this.Count == 0) throw new InvalidOperationException(nameof(Dequeue));
            var top = this.First;
            this.RemoveFirst();
            return top.Value;
        }
    }
}
