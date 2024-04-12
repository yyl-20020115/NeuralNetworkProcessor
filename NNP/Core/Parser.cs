using NNP.ZRF;
using System.Reflection.Metadata.Ecma335;
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

    public static void DebugPrint<T>(string title, params T[] objects)
    {
        using var log = File.AppendText("log.txt");
        log.WriteLine($"{title}:");
        if (objects.Length > 0)
        {
            log.WriteLine("\t" + string.Join(Environment.NewLine + "\t", objects));
        }
        log.WriteLine($"================================");
    }
    public List<Trend> Parse(Input input)
    {
        var position = 0;

        foreach (var (utf32, last) in input())
        {
            var text = char.ConvertFromUtf32(utf32);
            DebugPrint("input", "char:"+text , "pos:"+position);
            //terminal and other target phases as bullets
            var bullets = this.Terminals
                .Where(terminal => terminal.Accept(utf32))
                .Distinct()
                .Select(b => b with { Position = position })
                .Cast<Phase>()
                .ToHashSet(PhaseComparer.Default);
            
            DebugPrint(nameof(bullets), bullets.ToArray());

            //all hits here are lexes
            var hits = bullets
                .Where(bullet => bullet.Parent.IsInitiator(bullet))
                .SelectMany(bullet => bullet.Parents).ToHashSet(TrendComparer.Default);

            DebugPrint(nameof(hits), hits.ToArray());

            var previous = new HashSet<Trend>(TrendComparer.Default);
            while (true)
            {
                this.Pool.UnionWith(hits);
                DebugPrint(nameof(Pool), this.Pool.ToArray());

                var dones = this.Pool.Where(hit => hit.Advance(bullets, position)).ToHashSet(TrendComparer.Default);

                DebugPrint(nameof(dones), dones.ToArray());

                if (dones.Count == 0 || dones.SetEquals(previous)) break;

                var targets = dones
                    .SelectMany(done => done.Targets).ToHashSet(PhaseComparer.Default);

                DebugPrint(nameof(targets), targets.ToArray());

                //initiator only
                hits = targets.SelectMany(target => target.Parents)
                    .Where(hit => hit.IsAnyInitiator(targets)).ToHashSet(TrendComparer.Default);


                //copy hits
                hits = hits.Select(hit => hit with
                {
                    Identity = Trend.IdentityBase++,
                    StartPosition = position,
                    EndPosition = position
                }).ToHashSet(TrendComparer.Default);

                DebugPrint(nameof(hits), hits.ToArray());

                var hash = bullets.SelectMany(bullet => bullet.Parents).ToHashSet(TrendComparer.Default);
                foreach (var hit in hits)
                {
                    //reconnect the source
                    foreach (var bit in hit.Line)
                    {
                        var bitset = new TrendHashSet();
                        foreach (var source in bit.Sources)
                        {
                            if (hash.Contains(source))
                                bitset.UnionWith(this.Pool.Where(
                                    trend => trend.Name == source.Name));
                        }
                        if (bitset.Count > 0)
                        {
                            bit.Sources = bitset;
                            foreach (var source in bitset)
                            {
                                source.Targets.Add(bit);
                            }
                        }
                    }
                }
                this.Pool.ExceptWith(dones.Where(done => !done.IsTop));
                previous = dones;
                bullets = dones
                    .SelectMany(done => done.Targets)
                    .Select(b => b with { Position = position })
                    .ToHashSet(PhaseComparer.Default);
            }
            position += text.Length;
            if (last)
            {
                previous = Compact(Trim(previous));
                return [.. previous.OrderBy(p => p.Identity)];
            }
        }

        return [.. this.Pool.OrderBy(p => p.Identity)];
    }

    public static HashSet<Trend> Compact(HashSet<Trend> trends)
    {
        var results = new HashSet<Trend>(trends);
        var enumerator = trends.OrderByDescending(t => t.Identity).GetEnumerator();
        var last = 0;
        while (enumerator.MoveNext())
        {
            results.ExceptWith(enumerator.Current.GetBranches(results));
            if (results.Count <= 1 || results.Count == last) break;
            last = results.Count;
            enumerator = trends.OrderByDescending(t => t.Identity).GetEnumerator();
        }
        return results;
    }
    public static HashSet<Trend> Trim(HashSet<Trend> trends)
    {
        foreach (var trend in trends) Trim(trend);
        return trends;
    }
    public static Trend Trim(Trend trend, HashSet<Trend>? visited = null)
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
