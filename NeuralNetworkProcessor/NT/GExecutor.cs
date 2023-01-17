using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NeuralNetworkProcessor.Util;

namespace NeuralNetworkProcessor.NT;

public class GExecutor<T>
{
    protected volatile int _TotalCount = 0;
    public int TotalCount => this._TotalCount;
    public bool IsWorking => this._TotalCount > 0;
    public int WaitTimeOut = 50;
    public readonly Task[] Tasks;
    public readonly CancellationTokenSource TokenSource;
    public readonly AutoResetEvent ProcessEvent = new(false);
    public readonly ConcurrentCollection<T> Collection = new();
    public readonly Action<T> Action;
    public GExecutor(Action<T> action, int count = -1, CancellationTokenSource TokenSource = null)
    {
        this.Action = action;
        TokenSource ??= new ();
        count = count < 0 ? Environment.ProcessorCount : count;
        this.Tasks = new Task[count];
        for (int i = 0; i < count; i++)
            this.Tasks[i] = new (
                this.Execute, this.TokenSource.Token, TaskCreationOptions.LongRunning);
    }

    public GExecutor<T> Consume(IList<T> collection, bool update = true)
    {
        this.Collection.AddRange(collection);
        if (update)
            this.UpdateTotalCount(collection.Count);
        this.ProcessEvent.Set();
        return this;
    }
    public void Start()
    {
        for (int i = 0; i < this.Tasks.Length; i++)
            this.Tasks[i].Start();
    }
    public void Stop() 
        => this.TokenSource.Cancel();
    public void WaitComplete() 
        => Task.WaitAll(this.Tasks);
    protected int UpdateTotalCount(int delta)
        => Interlocked.Add(ref this._TotalCount, delta);
    public virtual void Execute()
    {
        var handles = new WaitHandle[] { this.TokenSource.Token.WaitHandle, this.ProcessEvent };
        var result = 1;
        do
        {
            if (result == 1 && this.Collection.TryTake(out var item))
            {
                this.Action(item);
                this.UpdateTotalCount(-1);
                if (this.IsWorking) this.ProcessEvent.Set();
            }
        }
        while (this.IsWorking && (result = WaitHandle.WaitAny(handles, this.WaitTimeOut)) != 0);
    }
}

