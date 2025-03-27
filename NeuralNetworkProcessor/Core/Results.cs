using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using YamlDotNet.Serialization;
using NeuralNetworkProcessor.ZRF;

namespace NeuralNetworkProcessor.Core;

public interface Extraction
{
    string Extract();
}
public interface SymbolExtraction : Extraction 
{
    string Text { get; }
    Symbol Symbol { get; }
    int Position { get; }
    int EndPosition { get; }
    int Length { get; }
}
public interface PatternExtraction : Extraction 
{ 
    IList<SymbolExtraction> SymbolExtractions { get; }
}
public record TextSpan(string Text, int Position, int Length, Symbol Symbol = null, int UTF32 = -1,Results AccompanyResults = null) : SymbolExtraction
{
    public static readonly TextSpan Defualt = new ("", 0, 0, Phrase.Default);
    public Results Buddy { get; set; } = AccompanyResults;
    [YamlIgnore]
    public Symbol Symbol { get; set; } = Symbol ?? Phrase.Default;
    [YamlIgnore]
    public Cell Cell { get; set; } = Cell.Default;
    public string Text { get; set; } = Text;
    public int Position { get; set; } = Position;
    public int Length { get; set; } = Length;
    [YamlIgnore]
    public int EndPosition => this.Position + Length;
    public TextSpan BindWith(Cell Cell)
    {
        if((this.Cell = Cell) != null) this.Text = this.Cell.Text;
        return this;
    }
    public TextSpan Bind(Symbol Symbol)
    {
        this.Symbol = Symbol;
        return this;
    }
    public TextSpan Bind(int Position, int Length = 0)
    {
        this.Position = Position;
        this.Length = Length;
        return this;
    }
    public TextSpan Duplicate()
        => new(this);
    public override string ToString() => 
          this.Buddy != null && this.Buddy != Results.Default
        ? this.Buddy.ToString() 
        : this.Text ?? string.Empty
        ;

    public string Extract() => this.Buddy != null && this.Buddy != Results.Default
        ? this.Buddy.Extract()
        : this.Text ?? string.Empty
        ;
}
public record Pattern(ImmutableArray<SymbolExtraction> SymbolExtractions, int Position, int Length, Description Description = null,Trend Trend=null) : PatternExtraction
{
    public static readonly Pattern Default = new ([], 0, 0, Description.Default,Trend.Default);
    public static long CurrentSerial { get; protected set; } = 0L;
    public long Serial { get; init; } = CurrentSerial++;
    /// <summary>
    /// DumpText is actually generated once and never gets changed
    /// </summary>
    [YamlIgnore]
    public int PhraseCount => this.Description.Phrases.Count;
    [YamlIgnore]
    public int DescriptionIndex => this.Description.Index;
    [YamlIgnore]
    public Description Description { get; init; } = Description ?? Description.Default;
    [YamlIgnore]
    public Definition Definition { get; set; } = Description?.Definition ?? Definition.Default;
    [YamlIgnore]
    public Trend Trend { get; set; } = Trend;
    public ImmutableArray<SymbolExtraction> SymbolExtractions { get; set; } = SymbolExtractions;
    [YamlIgnore]
    IList<SymbolExtraction> PatternExtraction.SymbolExtractions => SymbolExtractions;
    public int Position { get; set; } = Position;
    public int Length { get; set; } = Length;
    [YamlIgnore]
    public int EndPosition => this.Position + Length;
    public Pattern Bind(int Position, int Length = 0)
    {
        this.Position = Position;
        this.Length = Length;
        return this;
    }
    public override string ToString()
        => $"["+this.SymbolExtractions.Aggregate("", (a, b) => a +(string.IsNullOrEmpty(a)?"":",")+ b)+"]";
    public virtual string Extract()
        => this.SymbolExtractions.Aggregate("", (a, b) => a + b.Extract());
}
public record Results(ImmutableArray<Pattern> Patterns, int Position, int Length, Symbol Symbol) : SymbolExtraction
{
    public static readonly Results Default = new ([], 0, 0, Definition.Default);
    [YamlIgnore]
    public Symbol Symbol { get; init; } = Symbol;
    [YamlIgnore]
    public Cluster Cluster { get; set; } = null;
    public ImmutableArray<Pattern> Patterns { get; set; } = Patterns;
    [YamlIgnore]
    public string Text => this.Symbol?.Text ?? string.Empty;
    public int Position { get; protected set; } = Position;
    public int Length { get; protected set; } = Length;
    [YamlIgnore]
    public int EndPosition 
        => this.Position + Length;
    public Results Bind(int Position, int Length = 0)
    {
        this.Position = Position;
        this.Length = Length;
        return this;
    }
    public override string ToString()
        => $"{this.Symbol.Text}({this.Position},{this.EndPosition})=\"{this.Extract()}\"{this.Patterns.Aggregate("",
            (a,b)=>a+ (a!="" ? ",":"") +b.ToString())}";
    public virtual string Extract() => this.Patterns.Aggregate("",
            (a, b) => a + (a != "" ? "," : "") + b.Extract());
    public Results Include(Results other)
    {
        if(this.Symbol == other.Symbol 
            && this.Position == other.Position)
        {
            this.Patterns = this.Patterns.AddRange(other.Patterns);
            this.Length = this.Patterns.Max(p => p.Length);
        }
        return this;
    }

    public Results Includes(Results[] others)
    {
        foreach(var other in others) this.Include(other);
        return this;
    }
    public TextSpan ToSpan(Cell cell) 
        => new(this.Text, this.Position, this.Length, this.Symbol, AccompanyResults: this) { Cell = cell };
}
