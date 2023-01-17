using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Utilities;

namespace NeuralNetworkProcessor.Core
{
    public class TextSpanQueue : LinkedTailQueue<TextSpan>
    {
        public int CurrentIndex = -1;
        public bool Disposing = false;
        public Cell Cell { get; set; }
        public Trend Trend { get; set; }
        public TextSpanQueue Upper { get; set; }
        public bool TryGetStartPosition(out long Position)
        {
            Position = -1L;
            if (this.Head != null)
            {
                Position = this.Head.Position;
                return true;
            }
            return false;
        }

    }
    public class TextSpanQueueStacks : LinkedStack<TextSpanQueue>
    {
        public TextSpanQueueStacks(){}
        public TextSpanQueueStacks(IEnumerable<TextSpanQueue> textSpans):base(textSpans) { }
        public bool TryGetMinimalStartPosition(out long Position)
        {
            Position = -1L;
            var first = this.Data.First;
            while (first != this.Data.Last)
            {
                if(first.Value.TryGetStartPosition(out var P))
                    Position = Math.Min(Position < 0L ? 0L : Position, P);
                first = first.Next;
            }
            return Position >= 0L;
        }
    }
    public record TrendStacks
    {
        public bool IsExpectingDeepTail => this.Trend.IsDeepRecurse
            && this.Trend.IsFinal(this.ExpectingCell,true)
            && this.ExpectingCell.HasAnyDeepRecurseSource;

        public bool IsCurrentHole => this.Trend.IsDeepRecurse 
            && this.Trend.IsSurrounded(this.CurrentCell) 
            && this.CurrentCell.DeepClosureTrends.Count > 0;

        public bool IsExpectingHole => this.Trend.IsDeepRecurse
            && this.Trend.IsSurrounded(this.ExpectingCell)
            && this.ExpectingCell.DeepClosureTrends.Count > 0;

        public bool IsPreviousHole => this.Trend.IsDeepRecurse
            && this.Trend.IsSurrounded(this.PreviousCell)
            && this.PreviousCell.DeepClosureTrends.Count > 0;

        public Cluster Cluster { get; protected set; } = null;
        public Aggregation Aggregation => this.Cluster.Owner;
        public Trend Trend { get; set; } = Trend.Default;
        public Cell StarterCell => this.Trend.StarterCell;
        public Cell PreviousCell => this.CurrentIndex > 0 && this.CurrentIndex < this.Trend.Cells.Count
            ? this.Trend.Cells[this.CurrentIndex - 1] : null;
        public Cell CurrentCell => this.CurrentIndex >= 0 && this.CurrentIndex < this.Trend.Cells.Count
            ? this.Trend.Cells[this.CurrentIndex] : null;
        public Cell ExpectingCell => this.CurrentIndex + 1 >= 0 && this.CurrentIndex + 1 < this.Trend.Cells.Count
            ? this.Trend.Cells[this.CurrentIndex + 1] : null;
        public bool IsCompleted => this.ExpectingCell == null;
        public TextSpanQueueStacks SpanQueueStacks { get; } = new();
  
        public int CurrentIndex {
            get => this.SpanQueueStacks.Top.CurrentIndex;
            set => this.SpanQueueStacks.Top.CurrentIndex = value;
        }
        public bool Disposing {
            get => this.SpanQueueStacks.Count > 0 
                && this.SpanQueueStacks.Top.Disposing
                && !this.Aggregation.FocalStack.Any(F=>F.C==SpanQueueStacks.Top.Cell)
                ;
            set { if (this.SpanQueueStacks.Count > 0) 
                    this.SpanQueueStacks.Top.Disposing = value; }
        }

        public bool IsFollowing(long EndPosition)
            => this.SpanQueueStacks.TryGetMinimalStartPosition(out var p) && EndPosition == p;

        public bool TryDisposing()
        {
            if (this.Disposing) this.Leave();
            return this.SpanQueueStacks.Count == 0;
        }
        public TrendStacks(Cluster Cluster, Trend Trend)
        {
            this.Cluster = Cluster;
            this.Enter(Trend: this.Trend = Trend);
        }
        public TextSpanQueue Enter(Cell Cell = null, Trend Trend = null, TextSpanQueue Upper = null)
        {
            var q = new TextSpanQueue()
            {
                Trend = Trend ?? this.Trend,
                Upper = Upper ?? (this.SpanQueueStacks.Top),
                Cell = Cell
            };
            this.SpanQueueStacks.Push(q);
            return q;
        }
        public TextSpanQueue Leave(Cell Cell = null) 
            => (this.SpanQueueStacks.Count > 0 && (Cell == null || Cluster.CellEquals(this.SpanQueueStacks.Top.Cell, Cell)))
            ? this.SpanQueueStacks.Pop() : null;
        public bool OnPulse(long Position, Cell Cell, TextSpan Span)
        {
            bool IsFollowing(TextSpan span, TextSpan next) => span != null && next != null && span.EndPosition == next.Position;
            bool IsAligning(TextSpan span, TextSpan next) => span != null && next != null && span.Position == next.Position;
            bool IsBreaking(TextSpan span, TextSpan next) => span != null && next != null && span.EndPosition < next.Position;
           
            //var top = SpanQueueStacks.Peek();
            if(!this.SpanQueueStacks.TryPeek(out var top))
            {
                top = this.Enter(Trend: this.Trend);
            }
            //try following first
            var qs = IsFollowing(top.Tail, Span);
            var tried = false;

            //is following
            if (qs)
            {
                if (Cell != this.CurrentCell)
                {
                    top.TailAppend(Span);
                    this.CurrentCell?.Monitor?.SetActive(true);
                    this.CurrentIndex++;
                    tried = true;
                }
                else if(this.ExpectingCell == null && this.Trend.IsLeftRecurse)
                {
                    //at the end of left recurse
                }
            }
            if(!tried) //not following, try aligning
            {
                qs = IsAligning(top.Tail, Span);
                if (qs)
                {
                    this.CurrentCell?.Monitor?.SetActive(true);
                    if (Span.Length >= top.Tail.Length) top.TailReplace(Span);
                }
                //not aligning, it can be a starter
                else if (this.CurrentIndex == -1)
                {
                    top.TailReplace(Span, true);
                    this.CurrentIndex++;
                    this.CurrentCell?.Monitor?.SetActive(true);
                }
                //if every possible queue is breaking, it is unable to procceed
                else if (IsBreaking(top.Tail, Span))
                {
                    if (this.Trend.IsFinal(Cell, true)
                        && Cell.HasAnyDeepRecurseSource)
                    {
                        //DO NOTHING
                    }
                    else
                    {
                        this.Disposing = true;
                        this.Trend.Owner.OnReportBreak(this.Trend, CurrentIndex);
                    }
                }
                else //some still not breaking, should not be here!
                {
                    //Span maybe bad!
                    //this.Disposing = true;
                }
            }

            var ret = true;
            if (this.IsCompleted)
            {
                this.Trend?.Monitor?.SetActive(true);
                ret = this.Trend.Owner.OnPulse(Position, this, this.CreatePattern(top));
            }
            if (this.IsPreviousHole)
            {
                this.Aggregation.LeaveWith(this.PreviousCell,this);
            }
            else if (this.IsExpectingHole)
            {
                this.Aggregation.EnterWith(this.ExpectingCell, this, true);
            }
            return ret;
        }
        protected List<Pattern> CreatePatterns(IEnumerable<LinkedTailQueue<TextSpan>> qs)
        {
            var Patterns = new List<Pattern>();
            foreach (var q in qs)
                if (q.Count > 0) Patterns.Add(this.CreatePattern(q));
            return Patterns;
        }
        protected Pattern CreatePattern(LinkedTailQueue<TextSpan> q) 
            => new (q.Cast<SymbolExtraction>()
                        .ToImmutableArray(),
                        q.Head.Position,
                        q.Tail.Position - q.Head.Position + q.Tail.Length,
                        this.Trend.Description, this.Trend);
        public override string ToString() 
            => this.CreatePatterns(this.SpanQueueStacks)
                .Aggregate(">>", (a, b) => a + "|" + b.ToString());
    }
}
