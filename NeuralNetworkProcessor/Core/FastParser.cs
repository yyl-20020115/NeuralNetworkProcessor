using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Utilities;

namespace NeuralNetworkProcessor.Core;

public delegate void SendInputFunction(int UTF32);
public delegate IEnumerable<string> NextInputFunction();
public delegate bool OnReportErrorFunction(ErrorType Type , int Position, int UTF32 = -1, IList<MatrixLine> Expectations = null);
public partial class FastParser
{
    public virtual OnReportErrorFunction OnReportErrorCallBack { get; set; } = null;
    public virtual List<TerminalCluster> InputAccepters { get; protected set; }
    public virtual Aggregation Aggregation { get; protected set; } = null;
    public virtual HashSet<Cluster> Tops { get; } = [];
    public virtual HashSet<string> TopNames { get; } = [];
    public virtual HashSet<Cluster> Atoms { get; } = [];
    public virtual HashSet<Cluster> AboveAtoms { get; } = [];
    public virtual HashSet<Trend> AboveAtomTrends { get; protected set; } = [];
    public virtual HashSet<Cluster> NonAtoms { get; } = [];
    public virtual HashSet<Trend> NonAtomTrends { get; protected set; } = [];
    public virtual List<Results> LastResults { get; protected set; } = [];
    public virtual IEnumerator<int> Enumerator { get; protected set; } = null;

    protected const int SymbolOffset = UnicodeClassTools.UNICODE_PLANE16_END + 1;
    protected List<Results> InputResults = [];
    protected readonly DualDictionary<int, string> Symbols = [];
    protected List<MatrixRow> Matrix = null;
    protected int GetSymbolValue(string symbol)
    {
        if (this.Symbols.Count == 0) this.Symbols.Add(0, string.Empty);
        if (!this.Symbols.TryGetValue(symbol, out var i))
            this.Symbols.Add(i = SymbolOffset + this.Symbols.Count, symbol);
        return i;
    }

    public FastParser() { }
    public virtual FastParser AddCluster(Cluster cluster)
    {
        this.Aggregation.Clusters.Add(cluster);
        return this.Bind(this.Aggregation);
    }
    public virtual FastParser RemoveCluster(Cluster cluster)
    {
        this.Aggregation.Clusters.Remove(cluster);
        return this.Bind(this.Aggregation);
    }

    public virtual FastParser Bind(Aggregation Aggregation)
    {
        if ((this.Aggregation = Aggregation) != null)
        {
            this.InputAccepters = this.Aggregation.Clusters
                .Where(c => c is TerminalCluster)
                .Cast<TerminalCluster>().ToList();
            this.Tops.UnionWith(Algorithms.GetTops(this.Aggregation));
            this.TopNames.UnionWith(Tops.Select(t => t.Name));
            this.Atoms.UnionWith(Algorithms.GetAtoms(this.Aggregation));
            this.AboveAtoms.UnionWith(Algorithms.GetAbove(this.Aggregation, this.Atoms));
            this.AboveAtomTrends.UnionWith(this.AboveAtoms.SelectMany(c => c.Trends));
            this.NonAtoms.UnionWith(this.Aggregation.Clusters);
            this.NonAtoms.ExceptWith(this.Atoms);
            this.NonAtomTrends.UnionWith(this.NonAtoms.SelectMany(c => c.Trends));
        }
        return this;
    }

    public virtual List<Results> Parse(string Text)
        => this.Parse(InputProvider.CreateInput(Text));
    public virtual List<Results> Parse(TextReader Reader)
        => this.Parse(InputProvider.CreateInput(Reader));
    public virtual void Reset()
    {
        //this.Aggregation?.Reset();
        this.LastResults.Clear();
        //this.Symbols.Clear();
        this.Matrix = this.RebuildMatrix([], this.NonAtomTrends);
    }
    public virtual int ParseStep(int Position, Input NextInput)
    {
        if ((this.Enumerator ??= this.ParseStep(
            new RefPosition(Position), NextInput)
                .GetEnumerator()).MoveNext())
            return this.Enumerator.Current;

        this.Enumerator = null;
        return -1;
    }
    protected virtual IEnumerable<int> ParseStep(RefPosition RefPosition, Input NextInput)
    {
        foreach (var (UTF32,final) in NextInput()) 
        {
            var Text = char.ConvertFromUtf32(UTF32);
            var activateds = this.ParseCharStep(
                    UTF32,
                    Text.Length,
                    RefPosition.Position);
            if (activateds == 0)
            {
                //nothing is active means no reponse to the input UTF32
                if (!this.OnReportError(
                    ErrorType.Character,
                    RefPosition.Position, UTF32))
                    yield break;
            }

            var init = true;
            bool repeat;
            do
            {
                repeat = Emit(
                    init,
                    final,
                    RefPosition.Position,
                    out var lexical_hits,
                    out var syntax_hits);
                if (repeat && syntax_hits == 0)
                {
                    if (lexical_hits == 0)
                    {
                        //if (!this.OnReportError(
                        //    ErrorType.Lexcial,
                        //    RefPosition.Position,
                        //    Expectations: this.Matrix.Where(row => this.AboveAtomTrends.Contains(row.Trend)
                        //        && row.LastEnd == RefPosition.Position).OrderBy(row => row.Position).ToList()
                        //    )) yield break;
                    }
                    else
                    {
                        //if (!this.OnReportError(
                        //    ErrorType.Syntax,
                        //    RefPosition.Position,
                        //    Expectations: this.Matrix.Where(row => this.NonAtomTrends.Contains(row.Trend)
                        //    && row.ExpectingPosition == RefPosition.Position).OrderBy(row => row.Position).ToList()
                        //    )) yield break;
                    }
                }
                init = false;
            } while (repeat);
            RefPosition.Position += Text.Length;
            yield return RefPosition.Position;
        }
    }

    protected virtual int ParseCharStep(int UTF32, int Length, int Position)
    {
        var span = new TextSpan(
            char.ConvertFromUtf32(UTF32),
            Position, Length, UTF32: UTF32);
        var trends = this.InputAccepters
            .Where(i => i.Accept(UTF32)).SelectMany(c => c.Targets)
            .Where(s => s.OwnerTrend != null).Select(s => s.OwnerTrend).Distinct().ToList();
        foreach (var trend in trends)
        {
            var pattern = new Pattern(
                new SymbolExtraction[]
                { span.Duplicate() }
                .ToImmutableArray(),
                Position, 
                span.Length,
                trend.Description,
                trend) 
            { Definition = trend.OwnerCluster.Definition };
            var result = new Results(
                new[] { pattern }
                .ToImmutableArray(),
                Position, 
                pattern.Length, 
                trend.OwnerCluster.Definition.IsDynamicBuilt 
                ? new ("_'"+span.Text+"'", [])
                : trend.OwnerCluster.Definition);
            this.InputResults.Add(result);
        }
        return trends.Count;
    }
    protected virtual bool OnReportError(ErrorType Type, int Position, int UTF32 = -1, IList<MatrixLine> Expectations = null)
        //here we can understand where is wrong (needs input)
        //at very high level (not terminal level)
        => (this.OnReportErrorCallBack != null) && this.OnReportErrorCallBack(Type, Position, UTF32, Expectations);
}
