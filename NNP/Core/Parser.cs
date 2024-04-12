﻿using NNP.ZRF;
using System.Security.Principal;
using Utilities;

namespace NNP.Core;

public class Parser
{
    public readonly List<Trend> Trends;
    public readonly List<Phase> Phases;
    public readonly List<TerminalPhase> Terminals;

    protected readonly TrendHashSet Pool = [];
    public Parser(Concept concept)
    {
        (this.Trends, this.Phases, this.Terminals) = Builder.Build(concept);
    }

    public virtual List<Trend> Parse(string Text)
    => this.Parse(InputProvider.CreateInput(Text));
    public virtual List<Trend> Parse(TextReader Reader)
        => this.Parse(InputProvider.CreateInput(Reader));

    public List<Trend> Parse(Input input)
    {
        var position = 0;

        foreach (var (utf32, last) in input())
        {
            var text = char.ConvertFromUtf32(utf32);
            //terminal and other target phases as bullets
            var bullets = this.Terminals
                .Where(terminal => terminal.Accept(utf32))
                .Distinct().Cast<Phase>().ToHashSet(PhaseComparer.Default);
            //all hits here are lexes
            var hits = bullets
                .Where(bullet => bullet.Parent.IsInitiator(bullet))
                .SelectMany(bullet => bullet.Parents);
            
            //this.Pool.RemoveWhere(p => p.IsComplete);

            var previous = new HashSet<Trend>(TrendComparer.Default);
            while (true)
            {
                //copy hits
                hits = hits.Select(hit => hit with
                {
                    Identity = Trend.IdentityBase++,
                    StartPosition = position,
                    EndPosition = position + text.Length
                }) ;

                
                foreach(var hit in hits.Where(h=>!h.IsLex))
                {
                    //reconnect the source
                    foreach(var bit in hit.Line)
                    {
                        //use new source collection
                        bit.Sources = [.. this.Pool.Where(
                            trend => trend.Name == bit.Name)];
                        //reconnect to the copy
                        foreach(var source in bit.Sources)
                        {
                            source.Targets.Add(bit);
                        }
                    }
                }

                this.Pool.UnionWith(hits);

                var dones = this.Pool.Where(hit => hit.Advance(bullets, position)).ToHashSet(TrendComparer.Default);

                if (dones.Count == 0 || dones.SetEquals(previous)) break;
                //this.Pool.ExceptWith(dones);
                //var dup = new HashSet<Trend>(this.Pool.ToArray().Where(p => !p.IsComplete));
                //this.Pool.Clear();
                //this.Pool.UnionWith(dup);
                var targets = dones
                    .SelectMany(done => done.Targets).ToHashSet(PhaseComparer.Default);
                //initiator only
                hits = targets.SelectMany(target => target.Parents)
                    .Where(hit => hit.IsAnyInitiator(targets));

                previous = dones;
            }
            position += text.Length;
            if (last)
            {
                previous = Compact(Trim(previous));
                return [.. previous.OrderByDescending(p => p.Identity)];
            }
        }

        return [.. this.Pool.OrderByDescending(p => p.Identity)];
    }

    public static HashSet<Trend> Compact(HashSet<Trend> trends)
    {
        var results = new HashSet<Trend>(trends);
        var enumerator = trends.OrderByDescending(t => t.Identity).GetEnumerator();
        var last = 0;
        while(enumerator.MoveNext())
        {
            results.ExceptWith(enumerator.Current.GetBranches(results));
            if (results.Count <= 1 || results.Count==last) break;
            last = results.Count;
            enumerator = trends.OrderByDescending(t => t.Identity).GetEnumerator();
        }
        return results;
    }
    public static HashSet<Trend> Trim(HashSet<Trend> trends)
    {
        foreach(var trend in trends) Trim(trend);
        return trends;
    }
    public static Trend Trim(Trend trend, HashSet<Trend>? visited=null)
    {
        if ((visited ??= []).Add(trend))
        {
            foreach (var phase in trend.Line)
            {
                phase.Sources.RemoveWhere(s => !s.IsComplete);
                foreach (var source in phase.Sources) Trim(source, visited);
            }
        }
        return trend;
    }
}
