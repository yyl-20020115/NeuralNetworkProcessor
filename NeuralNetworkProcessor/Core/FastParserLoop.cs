using Utilities;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;
using NeuralNetworkProcessor.ZRF;
using System.IO;

namespace NeuralNetworkProcessor.Core;

public partial class FastParser
{
    public static readonly int VectorLength = Vector<int>.Count;
    public virtual List<Results> Parse(Input Input)
    {
        this.Reset();
        var Position = new RefPosition { Position = 0 };
        foreach (var _ in this.ParseStep(Position, Input)) ;
        foreach (var _ in this.LastResults) ;
        return this.LastResults;
    }
    protected virtual List<MatrixRow> RebuildMatrix(List<MatrixRow> matrix, HashSet<Trend> trends)
    {
        var additions = trends.Where(
            t => !matrix.Any(m => m.Trend == t)).ToList();
        foreach (var trend in additions)
            matrix.Add(new(
                trend,
                [.. trend.Cells.Select(c => c.Text)],
                trend.Cells.Select(c => this.GetSymbolValue(c.Text)).ToArray())
            { IsPreset = true });
        return matrix;
    }
    protected virtual List<Results> MatchMatrix(bool init,bool final, int position, List<Results> inputs, out int lexical_hits, out int syntax_hits)
    {
        lexical_hits = 0;
        syntax_hits = 0;
        var results = new List<Results>();
        var inputs_symbols = inputs.Select(n => n.Symbol.Text).ToList();

        if (Debuger.Enabled) Debuger.Debug(
            $"INPUT[{position}]({inputs.Count}): [{init}]{inputs.Aggregate("", (a, b) => a + (!string.IsNullOrEmpty(a) ? ", " : "") + b)}");
        var relations = new ListLookups<(int, MatrixRow), Results>();
        var partitions = new HashLookups<Definition, Pattern>();
        var additions = new HashLookups<Trend, MatrixRow>();
        var removings = new HashLookups<Trend, MatrixRow>();
        var enclosings = new HashLookups<MatrixRow, MatrixRow>();

        if (init){}
        var inputs_vectors = inputs_symbols.Select(
            n => new Vector<int>(this.GetSymbolValue(n))).ToList();
        var target_symbols = this.Matrix.Select(
            m => m.CurrentSymbol).ToList();
        var target_vectors = target_symbols.ToVectors();
        var hitsList = new List<int>[inputs_vectors.Count];
        var rowsList = new List<int>[target_vectors.Count * VectorLength];
        for (int i = 0; i < inputs_vectors.Count; i++)
        {
            for (int j = 0; j < target_vectors.Count; j++)
            {
                var result = Vector.Equals(inputs_vectors[i], target_vectors[j]);
                if (Vector.EqualsAny(result, new(-1)))
                {
                    for (int k = 0; k < VectorLength; k++)
                    {
                        if (result[k] == -1)
                        {
                            var p = j * VectorLength + k;
                            (hitsList[i] ??= new(target_symbols.Count)).Add(p);
                            (rowsList[p] ??= new(inputs_vectors.Count)).Add(i);
                            if (p < this.Matrix.Count)
                            {
                                if (this.AboveAtoms.Contains(this.Matrix[p].Trend.OwnerCluster))
                                {
                                    lexical_hits++;
                                }
                                else if (this.NonAtoms.Contains(this.Matrix[p].Trend.OwnerCluster))
                                {
                                    syntax_hits++;
                                }
                            }
                        }
                    }
                }
            }
        }
        for (int i = 0; i < rowsList.Length; i++)
        {
            var rows = rowsList[i];
            if (rows == null || i >= this.Matrix.Count) continue;
            //inputs are already grouped by Definitions
            relations[(i, this.Matrix[i])] 
                = inputs.Where((_, idx) => rows.Contains(idx)).ToList();
        }
        foreach (var ((i, row), rs) in relations)
        {
            //var impossibles = 0;
            //clone for inputs
            foreach (var inp in rs)
            {
                var (state, p) = row.TryAccept(
                    position,
                    inp, 
                    this.Matrix, 
                    additions, 
                    removings, 
                    enclosings);

                if (Debuger.Enabled) Debuger.Debug($"PROCESS:   [{state}][IsPrefix={row.IsPrefix}]{row} RESULT: \"{p?.Extract()}\", FROM: {row.ParentRow}");
                if (state >= AcceptState.Accepted && p != null)
                    partitions[row.Definition].Add(p);
            }
        }

        foreach (var key in partitions.Keys)
        {
            var patterns = partitions[key].ToList();
            var s = patterns.Min(p => p.Position);
            var e = patterns.Max(p => p.EndPosition);
            //use longest pattern as single result
            patterns = patterns.OrderByDescending(p => p.Length)
                .Take(1)
                .ToList()
                ;

            results.Add(new(
                [.. patterns],
                s, e - s, key))
                ;
        }
        if (enclosings.Count > 0)
            foreach (var (m, l) in enclosings)
                foreach (var i in l)
                    if (i != m)
                        //NOTICE: there should always be EXACT enters
                        //according to levels, otherwise, it will fail
                        //on some conditions
                        m.Closure.Add((i, i.Enter()));
        //NOTICE: remove everything not preset at the end of parsing
        if (final && results.Count==0)
            foreach(var row in this.Matrix)
                if(!row.IsPreset)
                    removings[row.Trend].Add(row);
        if (additions.Count > 0) this.Matrix.AddRange(additions.SelectMany(a => a.Value));
        if (removings.Count > 0)
        {
            var rms = removings.SelectMany(r => r.Value).ToHashSet();
            this.Matrix = this.Matrix.Where(m => !rms.Contains(m)).ToList();
        }
        if (Debuger.Enabled)
            Debuger.Debug($"====>Matrix:{this.Matrix.Count}");
        return results;
    }
    protected bool Emit(bool init,bool final, int position, out int lexical_hits, out int syntax_hits)
    {
        var rs = this.InputResults.Where(
            i => this.TopNames.Contains(i.Symbol.Text)).ToList();
        
        var ir = this.InputResults;

        this.InputResults = this.MatchMatrix(
            init,final, position, this.InputResults,
            out lexical_hits, out syntax_hits);

        if (this.InputResults.Count == 0 && rs.Count > 0) 
            this.LastResults = rs;
        if (final)
        {

        }
        return this.InputResults.Count > 0;
    }
}

public class Debuger
{
    public static bool Enabled = false;
    public static int Index = 0;
    public static void Debug(string text)
    {
        if (Enabled)
        {
            using var writer = new StringWriter();
            text ??= string.Empty;
            writer.Write($"[{Index:X8}]");
            writer.WriteLine(text);
            System.Diagnostics.Debug.Write(writer.ToString());
            Index++;
        }
    }

}