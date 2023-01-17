using NeuralNetworkProcessor.Util;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NeuralNetworkProcessor.NT;

public class ConcurrentTaskCollection : ConcurrentCollection<Task>
{
    public int MaxRetries = 2;
    public int LocalCount => this._locals.Value.DangerousCount;
    public bool KeepCompleted = false;
    public readonly List<Task> CompletedTasks = new();
    public override void Add(Task item)
    {
        var queue = this._locals.Value;
        var dc = queue.DangerousCount;
        var any = false;
        if (dc > 0)
        {
            var p = 0;
            var tasks = new Task[dc + 1];

            for (int i = queue._headIndex; i < queue._tailIndex; i++)
            {
                if (queue._array[i].Status < TaskStatus.RanToCompletion)
                {
                    any = true;
                    tasks[p] = queue._array[i];
                }
                else
                {
                    if (KeepCompleted)
                    {
                        CompletedTasks.Add(queue._array[i]);
                    }
                }
            }
            if (any)
            {
                tasks[p] = item;
                if (p < dc - 1)
                {
                    var nt = new Task[p + 1];
                    Array.Copy(tasks, nt, nt.Length);
                    tasks = nt;
                }
                lock (queue) // synchronize with steals
                {
                    // If the queue isn't empty, reset the state to clear out all items.
                    queue._headIndex = WorkStealingQueue.StartIndex;
                    queue._tailIndex = p;
                    queue._addTakeCount = queue._stealCount = 0;
                    queue._array = tasks;
                }
            }
        }
        if (!any)
        {
            base.Add(item);
        }
    }
}
