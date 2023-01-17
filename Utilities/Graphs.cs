using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Utilities
{
    public interface Edge
    {
        public object Origin { get; }
        public object Target { get; }
        public Edge Duplicate();
        /// <summary>
        /// This method is used to generate undirectional graphs
        /// </summary>
        /// <returns></returns>
        public Edge Reverse();
    }
    public abstract record EdgeObject(object O, object T) : Edge
    {
        public virtual object Origin { get; } = O;
        public virtual object Target { get; } = T;
        public abstract Edge Duplicate();
        public abstract Edge Reverse();
    }
    public class LongEdge
    {
        public bool AsLoop { get; protected set; }
        public List<object> Nodes { get; init; } = new List<object>();
        public List<object> LoopableNodes { get; init; } = new List<object>();
        public LongEdge Reverse()
        {
            var le = new LongEdge()
            {
                AsLoop = this.AsLoop,
                Nodes = new List<object>(this.Nodes),
                LoopableNodes = new List<object>(this.LoopableNodes)
            };
            le.Nodes.Reverse();
            le.LoopableNodes.Reverse();
            return le;
        }
        public LongEdge() { }
        public LongEdge(IEnumerable<object> Nodes, bool AsLoop = false)
        {
            if (this.AsLoop = AsLoop) this.LoopableNodes.AddRange(Nodes.Distinct());
            this.Nodes.AddRange(Nodes);
        }
        public override bool Equals(object? obj)
            => obj is LongEdge that && GraphTools.ShiftEquals(this.LoopableNodes, that.LoopableNodes);
        public override int GetHashCode()
            => (this.AsLoop ? this.LoopableNodes : this.Nodes).Aggregate(0, (a, b) => a ^ b.GetHashCode());
        public override string ToString()
            => this.Nodes.Aggregate("", (a, b) => a + (string.IsNullOrEmpty(a) ? "" : ",") + b.ToString());
    }
    public static class GraphTools
    {
        public static bool ShiftEquals<T>(IList<T> list1, IList<T> list2)
        {
            if (list1 == null || list2 == null || list1.Count != list2.Count)
                return false;
            if (object.ReferenceEquals(list1, list2) || list1.Count == 0 && list2.Count == 0)
                return true;
            for (int i = 0, count = list1.Count; i < count; i++)
                if (list2.SequenceEqual(ShiftSquence(list1, i))) return true;
            return false;
        }
        public static IEnumerable<T> ShiftSquence<T>(IList<T> list, int i)
        {
            for (int j = 0, count = list.Count; j < count; j++) yield return list[(i + j) % count];
        }
        public static HashLookups<object, Edge> BuildLookups(IEnumerable<Edge> edges)
        {
            var hl = new HashLookups<object, Edge>();
            foreach (var edge in edges) hl.AddRange(edge.Origin, edges.Where(e => e.Origin == edge.Origin));
            return hl;
        }
        public static HashLookups<object, Edge> MakeUndirectionalGraph(HashLookups<object, Edge> lookups)
        {
            foreach(var item in lookups) foreach(var e in item.Value.ToArray()) item.Value.Add(e.Reverse());
            return lookups;
        }
        public static bool HasAnyLoop(IEnumerable<Edge> edges)
            => HasAnyLoop(BuildLookups(edges));
        public static bool HasAnyLoop(HashLookups<object, Edge> dict)
        {
            foreach (var o in dict.Keys) if (HasLoop(o, dict)) return true;
            return false;
        }
        public static int CountLoops(IEnumerable<Edge> edges) => CountLoops(BuildLookups(edges));
        public static int CountLoops(HashLookups<object, Edge> dict)
        {
            var count = 0;
            foreach (var o in dict.Keys)
                if (HasLoop(o, dict)) count++;
            return count;
        }
        public static bool HasLoop(object o, HashLookups<object, Edge> dict)
            => FastRouteDetect(o, o, dict);
        public static bool FastRouteDetect(object o, object d, HashLookups<object, Edge> dict)
        {
            if (o != null && d != null)
            {
                var visited = new HashSet<object>() { o };
                var edges = new HashSet<Edge>(dict[o]);
                var nodes = default(HashSet<object>);
                do
                {
                    if ((nodes = new HashSet<object>(
                        edges.Select(e => e.Target))).Contains(d))
                        //found 
                        return true;
                    edges = new HashSet<Edge>(nodes.SelectMany(n => dict[n]));
                } while (visited.Append_ReturnFalseIfAllKnown(nodes));
            }
            return false;
        }
        public static bool GetRoughLoops(object o, ICollection<Edge> edges,
          out List<(HashSet<object>, HashSet<Edge>)> pairs)
            => GetRoughRoutes(o, o, edges, out pairs);
        public static bool GetRoughRoutes(object o, object d,
          ICollection<Edge> edges,
          out List<(HashSet<object>, HashSet<Edge>)> pairs)
            => GetRoughRoutes(o, d, BuildLookups(edges), out pairs);
        public static bool GetRoughRoutes(object o, object d,
            HashLookups<object, Edge> dict,
            out List<(HashSet<object> Nodes, HashSet<Edge> Edges)> pairs)
        {
            pairs = new();
            if (o != null && d != null)
            {
                var visited = new HashSet<object>() { o };
                var edges = new HashSet<Edge>(dict[o]);
                var nodes = default(HashSet<object>);
                do
                {
                    nodes = new HashSet<object>(edges.Select(e => e.Target));
                    pairs.Add((nodes, edges));
                    //found the destination(t)
                    if (nodes.Contains(d)) return true;
                    edges = new HashSet<Edge>(nodes.SelectMany(n => dict[n]));
                } while (visited.Append_ReturnFalseIfAllKnown(nodes));
            }
            return false;
        }
        public static LongEdge ToLongEdge(this List<Edge> edges, bool asloop = false)
            => edges.Count > 0 && IsValidPath(edges)
            ? new LongEdge(edges.Select(e => e.Origin).Append(edges.Last().Target), asloop)
            : new LongEdge()
            ;
        public static bool IsValidPath(this List<Edge> edges)
        {
            for (int i = 0; i < edges.Count - 1; i++)
                if (edges[i].Target != edges[i + 1].Origin) return false;
            return true;
        }
        public static bool GetAllLoops(IEnumerable<Edge> edges, out HashSet<LongEdge> loops)
        {
            var full = new HashSet<LongEdge>();
            var lookup = BuildLookups(edges);
            var fulls = new List<LongEdge>();
            foreach (var o in lookup.Keys)
            {
                if (GetLoopRoutes(o, lookup, out var routes))
                {
                    var ls = routes.Select(r => r.ToLongEdge(true)).ToList();
                    ls.ForEach(loop => full.Add(loop));
                }
            }
            loops = full;
            return full.Count > 0;
        }
        public static bool GetLoopRoutes(object o,
          ICollection<Edge> edges,
          out List<LongEdge> routes,
          int max_routes = -1) {
            routes = new List<LongEdge>();
            if (GetRoutes(o, o, BuildLookups(edges),
                out List<List<Edge>> routesList, max_routes)) {
                routes.AddRange(routesList.Select(r => r.ToLongEdge(true)));
                return true;
            }
            return false;
        }
        public static bool GetLoopRoutes(object o,
          ICollection<Edge> edges,
          out List<List<Edge>> routes,
          int max_routes = -1)
          => GetRoutes(o, o, BuildLookups(edges), out routes, max_routes);
        public static bool GetLoopRoutes(object o,
            HashLookups<object, Edge> dict,
            out List<List<Edge>> routes,
            int max_routes = 1)
            => GetRoutes(o, o, dict, out routes, max_routes);
        public static bool GetRoutes(object o, object d,
            HashLookups<object, Edge> dict,
            out List<LongEdge> routes,
            int max_routes = -1) {
            routes = new List<LongEdge>();
            if (GetRoutes(o, d, dict, out List<List<Edge>> routeList, max_routes))
            {
                routes.AddRange(routeList.Select(r => r.ToLongEdge(false)));
                return true;
            }
            return false;
        }
        public static bool GetRoutes(object o, object d,
            HashLookups<object, Edge> dict,
            out List<List<Edge>> routes,
            int max_routes = -1) {
            routes = new();
            if (GetRoughRoutes(o, d, dict, out var pairs)) {
                var edges = pairs.Select(p => p.Item2).ToList();
                var copy = DuplicateEdges(edges);
                do
                {
                    edges[^1].RemoveWhere(eb0 => eb0.Target != d);
                    for (int i = edges.Count - 2; i >= 0; i--)
                        edges[i].RemoveWhere(es0 =>
                            !edges[i + 1].Select(eb1 => eb1.Origin).ToHashSet().Contains(es0.Target));
                    
                    edges[0].RemoveWhere(eb2 => eb2.Origin != o);
                    for (int i = 1; i < edges.Count; i++)
                        edges[i].RemoveWhere(es1 =>
                            !edges[i - 1].Select(eb3 => eb3.Target).ToHashSet().Contains(es1.Origin));
                    if (CheckEquals(copy, edges)) break;
                    else copy = DuplicateEdges(edges);
                } while (pairs.Any(p => p.Item1.Count > 0 && p.Item2.Count > 0));
                if (copy.Count > 1) {
                    var slices = copy.Select(c => c.ToList()).ToList();
                    var limits = slices.Select(s => s.Count).ToArray();
                    var paths = new List<int[]>();
                    if (limits != null)
                    {
                        var results = new int[limits.Length];
                        var any = DoCount(results, limits, (values) =>
                        {
                            var failed = false;
                            for (int i = 0; i < values.Length - 1; i++)
                            {
                                var back = slices[i + 0][results[i + 0]];
                                var fore = slices[i + 1][results[i + 1]];
                                if (back.Target != fore.Origin) { failed = true; break; }
                            }
                            //found one path
                            if (!failed && results.Clone() is int[] rs)
                                paths.Add(rs);
                            //if less than max_routes, let it continue;
                            return max_routes == -1 || paths.Count < max_routes;
                        });
                    }
                    if (paths.Count > 0)
                        foreach (var path in paths)
                        {
                            var route = new List<Edge>();
                            for (int p = 0; p < path.Length; p++)
                                route.Add(slices[p][path[p]]);
                            routes.Add(route);
                        }
                }
            }
            return routes.Count > 0;
        }
        public delegate bool OnResultChanges(int[] result);
        static bool DoCount(int[] results, int[] limits, OnResultChanges onNumberChanged)
        {
            if (results.Length > 0 && results.Length == limits.Length)
            {
                int p;
                do
                {
                    for (int i = 0; i < limits[0]; i++)
                    {
                        results[0] = i;
                        if (!onNumberChanged(results)) return false;
                    }
                    p = 0;
                    results[p++] = 0;
                    while (true)
                    {
                        if (p == limits.Length) break;
                        else if (++results[p] < limits[p]) break;
                        else results[p++] = 0;
                    }
                } while (p < limits.Length);
            }
            return true;
        }
        public static List<(HashSet<object> Nodes, HashSet<Edge> Edges)> DuplicatePairs(List<(HashSet<object> Nodes, HashSet<Edge> Edges)> routes)
            => routes.Select(pair => (new HashSet<object>(pair.Nodes), new HashSet<Edge>(pair.Edges.Select(e => e.Duplicate())))).ToList();
        public static List<HashSet<Edge>> DuplicateEdges(List<HashSet<Edge>> Edges)
            => Edges.Select(e => new HashSet<Edge>(e)).ToList();
        public class EdgesComparer : IEqualityComparer<HashSet<Edge>>
        {
            public bool Equals(HashSet<Edge>? x, HashSet<Edge>? y)
                => x != null && y != null && x.SetEquals(y);
            public int GetHashCode([DisallowNull] HashSet<Edge> obj) => obj.GetHashCode();
        }
        public static bool CheckEquals(List<HashSet<Edge>> route1, List<HashSet<Edge>> route2)
            => route1.Count == route2.Count && route1.SequenceEqual(route2, new EdgesComparer());
        public static bool GetFullCoverage(HashLookups<object, Edge> lookups, out HashSet<object> coverage)
        {
            coverage = new();
            //only use copy of the input lookups
            lookups = new(lookups.Data);
            if (lookups.Count > 0)
                foreach (var pair in lookups)
                {
                    object? u = pair.Key;
                    object? v = null;
                    var values = pair.Value;
                    if (values.Count > 0)
                    {
                        foreach (var edge in values.ToArray())
                            if (edge.Target == u)
                            {
                                if (v == null)
                                {
                                    v = edge.Target;
                                    coverage.Add(u);
                                    coverage.Add(v);
                                }
                                pair.Value.Remove(edge);
                            }
                        values.Clear();
                    }
                    if (v != null)
                    {
                        values = lookups[v];
                        if (values.Count > 0)
                        {
                            foreach (var edge in values.ToArray())
                                if (edge.Target == v)
                                    values.Remove(edge);
                            values.Clear();
                        }
                    }
                }

            return coverage.Count > 0;
        }
    }
}
