using NeuralNetworkProcessor.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Utilities;

namespace NeuralNetworkProcessor.MT;

public class MultiParser
{



    public readonly List<Results> InputResults = [];
    public readonly List<Results> LastResults = [];

    public virtual List<TerminalCluster> InputAccepters { get; protected set; }
    public virtual Aggregation Aggregation { get; protected set; } = null;

    public virtual MultiParser Bind(Aggregation Aggregation)
    {
        if ((this.Aggregation = Aggregation) != null)
        {
            this.InputAccepters = this.Aggregation.Clusters
                .Where(c => c is TerminalCluster)
                .Cast<TerminalCluster>().ToList();
        }
        return this;
    }

    public virtual List<Results> Parse(string Text)
    => this.Parse(InputProvider.CreateInput(Text));


    public virtual List<Results> Parse(TextReader Reader)
        => this.Parse(InputProvider.CreateInput(Reader));


    public virtual List<Results> Parse(Input Input)
    {
        var Position = new RefPosition { Position = 0 };
        foreach (var _ in this.ParseStep(Position, Input)) ;
        foreach (var _ in this.LastResults) ;
        return this.LastResults;
    }
    private HashSet<Trend> TrendPool = [];

    protected virtual IEnumerable<int> ParseStep(RefPosition RefPosition, Input NextInput)
    {
        foreach (var (UTF32, final) in NextInput())
        {
            var results = new List<Results>();
            var text = char.ConvertFromUtf32(UTF32);
            var trends = this.ParseCharStep(
                    UTF32,
                    text.Length,
                    RefPosition.Position,
                    results);
            if (trends.Count == 0)
            {
                //nothing is active means no reponse to the input UTF32
                yield break;
            }

            TrendPool.UnionWith(trends);

            var init = true;
            bool repeat;
            do
            {
                foreach(var trend in TrendPool)
                {
                    //如果向前一步就能完成
                    if (trend.Advance())
                    {
                        //加入其它
                        foreach(var cell in trend.OwnerCluster.Targets)
                        {
                            if (cell.Index == 0) //开启新的trend,需要复制
                            {
                                TrendPool.Add(cell.OwnerTrend.InitClone());
                            }
                            else if(TrendPool.Contains(cell.OwnerTrend)) //已经在池中
                            {
                                //already advanced
                            }
                        }
                        TrendPool.Remove(trend);
                    }
                    else
                    {
                        //如果向前一步不能完成

                    }
                }


                var lexical_hits = 0;
                var syntax_hits = 0;
                repeat = Emit(
                    init,
                    final,
                    RefPosition.Position,
                    ref lexical_hits,
                    ref syntax_hits);
                if (repeat && syntax_hits == 0)
                {

                }
                init = false;
            } while (repeat);
            RefPosition.Position += text.Length;
            yield return RefPosition.Position;
        }
    }

    protected virtual List<Trend> ParseCharStep(int UTF32, int Length, int Position,List<Results> results)
    {
        var span = new TextSpan(
            char.ConvertFromUtf32(UTF32),
            Position, Length, UTF32: UTF32);

        var trends = this.InputAccepters
            .Where(i => i.Accept(UTF32)).SelectMany(c => c.Targets)
            .Where(s => s.OwnerTrend != null).Distinct().Select(s => s.OwnerTrend.InitClone()).ToList();
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
            results.Add(new (
                new[] { pattern }
                .ToImmutableArray(),
                Position,
                pattern.Length,
                trend.OwnerCluster.Definition.IsDynamicBuilt
                ? new("_'" + span.Text + "'", [])
                : trend.OwnerCluster.Definition));
        }
        return trends;
    }

    protected bool Emit(bool init, bool final, int position, ref int lexical_hits, ref int syntax_hits)
    {
        return true;
    }

}
