using NeuralNetworkProcessor.ZRF;
using System.Collections.Generic;
using System.Linq;
using Utilities;

namespace NeuralNetworkProcessor.Core;

public sealed record MatrixRow
{
    public static int MatrixRowSerialNumber = 0;
    public int SerialNumber = MatrixRowSerialNumber++;
    public bool IsPreset = false;
    public bool IsPrefix = false;
    public int StartingPosition = 0;
    public Trend Trend = null;
    public MatrixRow ParentRow = null;
    public bool IsFree => this.Pivot == 0 && this.Position == -1 && this.EndPosition == -1;
    public bool IsCompleted => this.Top.IsCompleted;
    public bool HasNonTailDeepRecurse
    {
        get
        {
            for (int i = 0; i < this.Trend.CellsCount - 1; i++)
                if (this.Trend.Cells[i].HasAnyDeepSource) return true;
            return false;
        }
    }
    public List<(MatrixRow, MatrixLine)> Closure => this.Top.Closure;
    public ListStack<MatrixLine> Stack { get; init; } = new();
    public MatrixLine Top => this.Stack.Top;
    public int Pivot => this.Top.Pivot;
    public bool IsBeforeLast => this.Pivot + 1 < this.SequenceLength;
    public int SequenceLength => this.Trend.CellsCount;
    public int Position => this.Top.Position;
    public int EndPosition => this.Top.EndPosition;
    public int CurrentSymbol => this.Top.CurrentSymbol;
    public Description Description => this.Trend.Description;
    public Definition Definition => this.Description.Definition;
    public Cell CurrentCell 
        => this.Pivot < this.Trend.CellsCount
        ? this.Trend.Cells[this.Pivot]
        : null
        ;
    public bool IsBeforeNonRecurse
        => this.Pivot + 1 < this.SequenceLength
        && this.Trend.Cells[this.Pivot] is Cell cell
        && !cell.HasLowerRecurse
        ;
    public bool IsAtDeepRecurse
        => this.Trend.Cells[this.Pivot] is Cell cell
        && cell.HasAnyDeepSource
        ;
    public bool IsPostDeepRecurse
        => this.Pivot > 0 && this.Trend.Cells[this.Pivot - 1] is Cell cell
        && cell.HasAnyDeepSource
        ;
    public MatrixRow(Trend Trend, string[] Pattern_, int[] Sequence, MatrixRow ParentRow = null)
    {
        this.Stack.Push(new(this.Trend = Trend, Pattern_, Sequence, this));
        this.ParentRow = ParentRow;
    }
    public MatrixRow ApplySerialNumber()
    {
        this.SerialNumber = MatrixRowSerialNumber++;
        return this;
    }
    public MatrixRow SeparatorDuplicate(int position)
    {
        var dup = this.Stack.Top.Duplicate(
            shared: true,
            doAdvance: true);
        var row = dup.ContainerRow = new MatrixRow(this)
        {
            Stack = new() { dup },
            ParentRow = this,
            IsPreset = false,
            IsPrefix = true,
            StartingPosition = position                
        }.ApplySerialNumber();
        return row;
    }
    public MatrixRow PusherDuplicate(int position)
    {
        var row = new MatrixRow(this)
        {
            Stack = new(this.Stack),
            ParentRow = this,
            IsPreset = false,
            IsPrefix = false,
            StartingPosition = position
        }.ApplySerialNumber().WithEnter();
        return row;
    }
    public MatrixRow Reset()
    {
        this.Top.Reset();
        return this;
    }
    public MatrixRow OnInit(MatrixLine line) => this;
    public MatrixLine CreateLine() =>
        new MatrixLine(
            this.Trend,
            this.Top.Pattern_,
            this.Top.Sequence,
            this
        ).ApplySerialNumber();
    public MatrixLine Enter(MatrixLine line = null)
        => this.Stack.Push(line ?? this.CreateLine());
    public MatrixRow WithEnter()
    {
        this.Enter();
        return this;
    }
    public (bool,int, MatrixLine) Leave(MatrixLine line = null)
    {
        var done = false;
        line ??= this.Top;
        if (this.Stack.Count > 1)
        {
            if (this.Top == line)
            {
                this.Stack.Pop();
                done = true;
            }
        }
        else
        {
            if (this.Top == line)
            {
                this.Top.Reset();
                done = true;
            }
        }
        if (!done)
        {
            //bad pop
        }
        return (done, this.Stack.Count, line);
    }
    public (AcceptState, Pattern) TryAccept(
        int position,
        Results results,
        List<MatrixRow> matrix,
        HashLookups<Trend, MatrixRow> additions,
        HashLookups<Trend, MatrixRow> removings,
        HashLookups<MatrixRow, MatrixRow> enclosings)
    {
        if (this.IsPrefix && this.Top.SymbolExtractions != this.ParentRow.Top.SymbolExtractions)
            this.Top.SymbolExtractions = this.ParentRow.Top.SymbolExtractions;
        var (state, pattern) = this.Top.TryAccept(results, matrix);
        if (state > AcceptState.Unaccepted)
            this.TryPrefixComplete(
                position, 
                results, 
                matrix, 
                additions, 
                removings, 
                enclosings);
        return (state, pattern);
    }
    public MatrixRow TryPrefixComplete(
        int position,
        Results results,
        List<MatrixRow> matrix,
        HashLookups<Trend, MatrixRow> additions,
        HashLookups<Trend, MatrixRow> removings,
        HashLookups<MatrixRow, MatrixRow> enclosings)
    {
        if (this.IsPostDeepRecurse && this.Closure.Count > 0)
        {
            foreach (var (c, l) in this.Closure)
            {
                //NOTICE: this kills (..)*(..)*(..)
                var (b, t, s) = c.Leave(l);
            }

            this.Closure.Clear();
            //"("   Exp   ")" 
            //       ^
            //   "("...")"
            //       -> move to next
            if (this.ParentRow != null && !this.ParentRow.IsFree)
            {
                this.ParentRow.Top.Advance(true);
                if (this.ParentRow.IsCompleted)
                    this.ParentRow.Reset();
            }
            removings[this.Trend].Add(this);
        }
        else if (this.HasNonTailDeepRecurse && this.IsAtDeepRecurse)
        {
            //this means "(" is shifted to next stage, we should dup for another "("
            if (!matrix.Any(r => r.ParentRow == this))
            {
                MatrixRow row = null;
                if (additions.TryGetValue(this.Trend, out var col) && col.Count >= 1)
                    row = col.FirstOrDefault(c => !c.IsPrefix);
                if (row == null)
                {
                    additions[this.Trend].Add(
                        row = this.PusherDuplicate(position));

                    if (ProgramEntry.Enabled)
                        ProgramEntry.Debug($"PUSH-DUP:  {row} FROM {this}");
                }
                if (!this.IsPrefix || (this.IsPrefix && this.IsBeforeNonRecurse))
                {
                    var sep = this.SeparatorDuplicate(position);
                    additions[this.Trend].Add(sep);

                    if (ProgramEntry.Enabled)
                        ProgramEntry.Debug($"SEPT-DUP:  {sep} FROM {this}");

                    var closure = this.CurrentCell.DeepClosureTrends;
                    if (closure.Count > 0)
                    {
                        enclosings[sep].Add(row);
                        enclosings[sep].Append(matrix.Where(
                            m =>
                            m != this &&
                            closure.Contains(m.Trend)));
                    }
                }
            }
        }
        return this;
    }
    public override string ToString() 
        => $"<({this.SerialNumber})Start:{this.StartingPosition},Depth:{this.Stack.Count},Preset:{(this.IsPreset?'T':'F')},Prefix:{(this.IsPrefix?'T':'F')}> "+this.Top;
}
