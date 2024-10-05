using NNP.ZRF;
using Utilities;

namespace NNP.Core;

public class Parser
{
    public readonly List<Trend> Trends;
    public readonly List<Phase> Phases;
    public readonly List<TerminalPhase> Terminals;

    protected readonly TrendHashSet Pool = [];
    public Parser(Concept concept) 
        => (this.Trends,
            this.Phases, 
            this.Terminals) = Builder.Build(concept);

    public virtual List<Trend> Parse(string Text)
        => this.Parse(InputProvider.CreateInput(Text));
    public virtual List<Trend> Parse(TextReader Reader)
        => this.Parse(InputProvider.CreateInput(Reader));

    public static int Step = 0;
    public static void DebugPrint<T>(string title, params T[] objects)
    {
        using var log = File.AppendText("log.txt");
        log.WriteLine($"{Step++} {title}:");
        if (objects.Length > 0)
        {
            log.WriteLine("\t" + string.Join(Environment.NewLine + "\t", objects));
        }
        log.WriteLine($"================================");
    }
    public List<Trend> Parse(Input input)
    {
        var position = 0;

        var completeds = new HashSet<Trend>(TrendComparer.Default);

        foreach (var (utf32, last) in input())
        {
            this.Parse(utf32, last, position, completeds);
            position += char.ConvertFromUtf32(utf32).Length;

        }

        return [.. this.Pool.OrderBy(p => p.Identity)];
    }
    public List<Trend> Parse(int utf32,bool last,int position, HashSet<Trend> completeds)
    {
        //terminal and other target phases as bullets
        var bullet_phases = this.Terminals
            .Where(terminal => terminal.Accept(utf32))
            .Distinct()
            .Select(bullet_phase => bullet_phase with { Position = position, Parents = [bullet_phase.Parent] })
            .Cast<Phase>()
            .ToHashSet(PhaseComparer.Default)
            ;
        
        //all hits here are lexes
        var hit_trends = bullet_phases
            .Where(bullet => bullet.Parent.IsInitiator(bullet))
            .SelectMany(bullet => bullet.Parents).ToHashSet(TrendComparer.Default);

        //try to find advanced trends with bullets
        var advanced_trends = hit_trends.Where(
            hit_trend => hit_trend.Advance(bullet_phases, position))
            .ToHashSet(TrendComparer.Default);
 
        //if nothing advanced, exit 
        if (advanced_trends.Count == 0)
            return [];

        //get advanced trends' targets
        bullet_phases = advanced_trends
            .SelectMany(advanceds => advanceds.Targets).ToHashSet(PhaseComparer.Default);

        //get target trends initiator only
        hit_trends = bullet_phases.SelectMany(target => target.Parents)
            .Where(trend => trend.IsAnyInitiator(bullet_phases))
            .Select(trend => trend with
            {
                Identity = Trend.IdentityBase++,
                StartPosition = position,
                EndPosition = position
            })
            .ToHashSet(TrendComparer.Default);

        var previous_trends = new HashSet<Trend>(TrendComparer.Default);

        while (true)
        {
            //build pool
            this.Pool.UnionWith(hit_trends);

            advanced_trends = this.Pool.Where(
                hit_trend => hit_trend.Advance(bullet_phases, position))
                .ToHashSet(TrendComparer.Default);

            //test exit condition
            if (advanced_trends.Count == 0
                || advanced_trends.Select(t=>t.ToString())
                    .ToHashSet().SetEquals(previous_trends.Select(t => t.ToString()))) 
                break;

            bullet_phases = advanced_trends
                .SelectMany(trend => trend.Targets).ToHashSet(PhaseComparer.Default);
            
            hit_trends = bullet_phases.SelectMany(target => target.Parents)
                   .Where(trend => trend.IsAnyInitiator(bullet_phases))
                   .Distinct()
                   .Select(trend => trend with
                   {
                       Identity = Trend.IdentityBase++,
                       StartPosition = position,
                       EndPosition = position
                   })
                   .ToHashSet(TrendComparer.Default);
            hit_trends.UnionWith(advanced_trends);

            this.Pool.ExceptWith(advanced_trends.Where(done => !done.IsTop));
            previous_trends = advanced_trends;
        }
        completeds.RemoveWhere(t => !t.IsComplete || t.IsLex);
        if (last)
        {
            previous_trends = Compact(Trim(previous_trends));
            return [.. previous_trends.OrderBy(p => p.Identity)];
        }
        else
        {
            return [];
        }

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
