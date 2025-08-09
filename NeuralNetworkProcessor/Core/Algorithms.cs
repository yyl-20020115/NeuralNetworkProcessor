using System.Collections.Generic;
using System.Linq;

namespace NeuralNetworkProcessor.Core;

public static class Algorithms
{
    public static bool CanReach(this Cell from, Trend to, HashSet<Cell> visited = null)
    {
        if ((visited ??= []).Add(from))
        {
            if (from.Sources.Any(s => s.Trends.Contains(to)))
                return true;
            else
                foreach (var c in from.Sources.SelectMany(s => s.Trends).Select(t => t.Cells[0]))
                    if (c.CanReach(to,visited)) return true;
        }
        return false;
    }
    public static bool HasAnyTailRecurse(Cell cx, HashSet<Cell> visited = null)
    {
        if ((visited ??= []).Add(cx))
            foreach (var s in cx.Sources)
                if (s.HasRecurse) return true;
                //only check the last cell
                else foreach (var cz in s.Trends
                    .Where(t => t.CellsCount > 0)
                    .Select(t => t.Cells[^1]))
                    if (HasAnyTailRecurse(cx, visited)) return true;
        return false;
    }
    public static void MarkDeepRecurseClosure(Cell cx, HashSet<Trend> candidates, HashSet<Trend> selected, int mindepth = 3 )
    {
        var current = new HashSet<Trend> { cx.OwnerTrend };
        var visited = new HashSet<Trend> { cx.OwnerTrend };
        var depth = 0;
        bool anynew;
        do
        {
            depth++;
            anynew = false;
            var comming = new HashSet<Trend>();
            foreach (var item in current)
            {
                if (depth > 1 && candidates.Contains(item)) selected.Add(item);
                foreach (var c in item.Cells
                   .SelectMany(c => c.Sources)
                   .SelectMany(s => s.Trends))
                {
                    anynew |= visited.Add(c);
                    comming.Add(c);
                }
            }
            if (comming.Contains(cx.OwnerTrend) && depth >= mindepth) break;
            current = comming;
        } while (anynew);
        selected.Add(cx.OwnerTrend);
    }

    public static bool DetectDeepRecurse(Trend t,int mindepth = 3)
    {
        if (t.IsSimpleLeftRecurse) return false;
        var current = new HashSet<Trend> { t };
        var visited = new HashSet<Trend> { t };
        var depth = 0;
        bool anynew;
        do
        {
            depth++;
            anynew = false;
            var comming = new HashSet<Trend>();
            foreach (var item in current)
                foreach (var c in item.Cells
                    .SelectMany(m => m.Sources)
                    .SelectMany(y => y.Trends)){
                    anynew |= visited.Add(c);
                    comming.Add(c);
                }
            if (comming.Contains(t))
                if (depth >= mindepth) return true;
            current = comming;
        } while (anynew);
        return false;
    }

    /// <summary>
    /// Tops are roots of definitions, such as CompilationUnit etc
    /// </summary>
    /// <param name="aggregation"></param>
    /// <returns></returns>
    public static HashSet<Cluster> GetTops(Aggregation aggregation) 
        => GetTops(aggregation.Clusters)
        ;
    public static HashSet<Cluster> GetTops(List<Cluster> all)
    {
        var cells = all.SelectMany(a => a.Trends.SelectMany(t => t.Cells)).ToList();
        var tops = all.Where(a=>a.HasTrends && !cells.Any(c=>c.Sources.Contains(a))).ToHashSet();
        if (tops.Count == 0)
            tops.UnionWith(all.Where(a => a.HasTrends && a.Trends.SelectMany(t => t.Cells).ToHashSet().SetEquals(cells.Where(c => c.Sources.Contains(a)))));
        return tops;
    }
    /// <summary>
    /// Atoms are actually terminals/literals such as "if","while" or digits 
    /// </summary>
    /// <param name="aggregation"></param>
    /// <returns></returns>
    public static HashSet<Cluster> GetAtoms(Aggregation aggregation)
        => GetAtoms(aggregation.Clusters)
        ;
    public static HashSet<Cluster> GetAtoms(IEnumerable<Cluster> clusters) 
        => [.. clusters.Where(c => c is TerminalCluster)]
        ;
    public static HashSet<Cluster> GetAbove(Aggregation aggregation,HashSet<Cluster> atoms)
        => GetAboveAtoms(aggregation.Clusters,atoms)
        ;
    public static HashSet<Cluster> GetAboveAtoms(IEnumerable<Cluster> clusters, HashSet<Cluster> atoms)
        => [.. clusters.Where(
            c => c.Trends.SelectMany(t => t.Cells).Any(s => s.Sources.Any(u =>
            atoms.Contains(u))))]
        ;
}