using NeuralNetworkProcessor.ZRF;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace NeuralNetworkProcessor.Core;

public enum AcceptState : int
{
    Impossible = -4,
    Blocked = -3,
    Disconnected = -2,
    Overlapped = -1,
    Unaccepted = 0,
    Repeated = 1,
    Advanced = 2,
    Accepted = 3,
    Reaccepted = 4,
}
public sealed record MatrixLine(
    Trend Trend,string[] Pattern_,int[] Sequence,
    MatrixRow ContainerRow = null)
{
    public static int MatrixLineSerialNumber = 0;
    public int SerialNumber = MatrixLineSerialNumber++;
    public int Pivot = 0;
    public bool Shared = false;
    public bool Shifted = false;
    public bool Init = true;

    public MatrixRow ContainerRow = ContainerRow;
    public List<(MatrixRow, MatrixLine)> Closure { get; } = [];
    public bool HasContent 
        => this.SymbolExtractions.Count > 0
        ;
    public bool IsProcessing 
        => 0 <= this.Pivot && this.Pivot <= this.SequenceLength - 1
        ;
    public bool IsEnding 
        => this.Pivot >= this.SequenceLength - 1
        ;
    public int SequenceLength 
        => this.Sequence.Length
        ;
    public bool IsCompleted 
        => this.IsEnding
        && this.SymbolExtractions.Count
        >= this.SequenceLength
        ;
    public bool IsNull 
        => this.SequenceLength == 0
        ;
    public bool IsPoint 
        => this.SequenceLength == 1
        ;
    public int Position 
        => this.HasContent
        ? this.SymbolExtractions[0].Position
        : -1
        ;
    public int EndPosition 
        => this.HasContent
        ? this.SymbolExtractions[^1].EndPosition
        : -1
        ;
    public int FullLength 
        => this.HasContent
        ? this.SymbolExtractions.Sum(s => s.Length)
        : 0
        ;
    public int CurrentSymbol 
        => this.Pivot < this.SequenceLength
        ? this.Sequence[this.Pivot]
        : -1
        ;
    public List<SymbolExtraction> SymbolExtractions { get;  set; } = [];
    public int LastStartPos 
        => this.SymbolExtractions.Count > 0 
        ? this.SymbolExtractions[^1].Position 
        : -1
        ;
    public int LastEndPosPlus 
        => this.SymbolExtractions.Count > 0 
        ? this.SymbolExtractions[^1].EndPosition 
        : -1
        ;
    public bool IsAboveRecurse
        => this.Trend.Cells[this.Pivot] is Cell cell
        && cell.HasLowerRecurse
        && this.Trend.CellsCount >= 3 //indicating not left recurse
        ;
    public bool IsAboveDeepRecurse
        => this.Trend.Cells[this.Pivot] is Cell cell
        && cell.HasAnyDeepSource
        && this.Trend.CellsCount >= 3 //indicating not left recurse
        ;
    public Description Description 
        => this.Trend.Description
        ;
    public Definition Definition 
        => this.Description.Definition
        ;
    public MatrixLine Duplicate(bool shared = false, bool doAdvance = false)
    {
        shared |= this.Shared;
        var line = new MatrixLine(this)
        {
            SymbolExtractions
                = shared 
                ? this.SymbolExtractions 
                : new (this.SymbolExtractions),
            Shared = shared,
        }.ApplySerialNumber();
        if (doAdvance) line.Advance(true);
        return line;
    }
    public MatrixLine ApplySerialNumber()
    {
        SerialNumber = MatrixLineSerialNumber++;
        return this;
    }
    public int Advance(bool force = false)
    {
        var init = this.Init;
        if (init) this.ContainerRow.OnInit(this);
        this.Init = false;
        if ((force || !this.IsAboveRecurse) && this.Pivot < this.SequenceLength - 1)
        {
            this.Shifted = true;
            return this.Pivot++;
        }
        else
        {
            return this.Pivot;
        }
    }
    public int Stall()
    {
        this.Init = false;
        this.Shifted = false;
        return this.Pivot;
    }
    public MatrixLine Reset()
    {
        this.Init = true;
        this.Shifted = false;
        this.Pivot = 0;
        //NOTICE:if isshared, the shared connection is broken
        this.SymbolExtractions = [];
        this.Shared = false;
        return this;
    }
    public (AcceptState, Pattern) TryAccept(Results results,List<MatrixRow> matrix)
    {
        retry:
        //This is called surely after Duplicate
        if (this.IsNull) //no symbol at all
        {
            return (AcceptState.Impossible, null);
        }
        else if (this.IsPoint) //just one symbol
        {
            this.SymbolExtractions.Add(
                results.ToSpan(
                    this.Trend.Cells[this.Pivot]));
            this.Advance();
            Pattern p = null;
            if (this.IsCompleted)
            {
                p = new(this.SymbolExtractions.ToImmutableArray(),
                         this.Position,
                        this.FullLength,
                        this.Trend.Description,
                        this.Trend);
                this.Reset();
            }
            return (AcceptState.Accepted, p);
        }
        else if (this.IsProcessing) // including completed
        {
            var actioned = false;
            var repeated = false;
            if ((this.Pivot == 0 && SymbolExtractions.Count == 0) ||
                (this.Pivot > 0 && results.Position == this.LastEndPosPlus &&!this.IsCompleted))
            {
                this.SymbolExtractions.Add(results.ToSpan(this.Trend.Cells[this.Pivot]));
                this.Advance();
                actioned = true;
            }
            else if (repeated = results.Position == this.LastStartPos)
            {
                var last = this.SymbolExtractions[^1] as TextSpan;
                var cell = last.Cell;
                //this means repeated, then no increase pivot
                this.SymbolExtractions[^1] = results.ToSpan(cell);
                this.Stall();
                actioned = true;
            }
            else if (results.Position < this.LastStartPos)
            {
                return (AcceptState.Overlapped, null);
            }
            else if (results.Position > this.LastEndPosPlus)
            {
                //all disconnected should be removed
                if (this.Trend.IsSimpleLeftRecurse)
                {
                    actioned = true;
                    this.Reset();
                }
                else if (this.Trend.IsLeftRecurse) //this means deep left recurse
                {
                    actioned = true;
                    this.Reset();
                    if (results.Symbol.Text == this.Definition.Text)
                        goto retry;
                }
                return (AcceptState.Disconnected, null);
            }
            if (!actioned) return (AcceptState.Unaccepted, null);
            if (!repeated && !this.Shifted) this.Shifted = true;
            Pattern p = null;
            var state = AcceptState.Impossible;
            if (this.IsCompleted)
            {
                state = repeated 
                    ? AcceptState.Reaccepted 
                    : AcceptState.Accepted
                    ;
                p = new(this.SymbolExtractions.ToImmutableArray(),
                        this.Position,
                        this.FullLength,
                        this.Trend.Description,
                        this.Trend);
                if (this.Trend.IsSimpleLeftRecurse)
                {
                    this.Reset();
                    return (state, p);
                }
            }
            else
            {
                state = repeated 
                    ? AcceptState.Repeated 
                    : AcceptState.Advanced
                    ;
            }
            return (state, p);
        }
        return (AcceptState.Unaccepted, null);
    }
    public override string ToString()
        => $"({this.SerialNumber}):{this.Trend}({this.Pivot}),({this.Position},{this.EndPosition})"
        + "::\"" + (SymbolExtractions.Count > 0 ? ( this.SymbolExtractions.Aggregate(
            "", (a, b) => a + b.Extract())):string.Empty)+"\"";
}
