using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Utilities;

namespace YamlDotNet.Serialization.AntiRecursion
{


    public abstract record MemberEdge(object O, object T, MemberInfo? M = null) : EdgeObject(O, T)
    {
        public virtual MemberInfo? M { get; set; } = M;
    }
    public record ItemEdge(IEnumerable E, object T, MemberInfo? M = null) : MemberEdge(E, T, M)
    {
        public override object Origin { get; } = E;
        public override object Target { get; } = T;
        public override Edge Duplicate() => new ItemEdge(this);
        public override Edge Reverse() => new ItemEdge((IEnumerable)T, E);
    }
    public record FieldEdge(object O, object T, FieldInfo F) : MemberEdge(O, T)
    {
        public override Edge Duplicate() => new FieldEdge(this);

        public override Edge Reverse() => new FieldEdge(T, O, F);
    }
    public record PropertyEdge(object O, object T, PropertyInfo P) : MemberEdge(O, T)
    {
        public override Edge Duplicate() => new PropertyEdge(this);
        public override Edge Reverse() => new PropertyEdge(T, O, P);
    }
    public static class AntiRecursionStrategyGenerator
    {
        public static HashSet<Edge>? CollectEdges(IEnumerable<object> os, HashSet<Edge>? edges = null, HashSet<object>? visited = null, bool deep = true)
        {
            edges ??= new HashSet<Edge>();
            if (os != null && (visited ??= new HashSet<object>()).Add(os))
                foreach (var o in os)
                    if (o != null && edges.Add(new ItemEdge(os, o)) && deep)
                        CollectEdges(o, edges, visited, deep);
            return edges;
        }
        public static HashSet<Edge>? CollectEdges(object? o, HashSet<Edge>? edges = null, HashSet<object>? visited = null, bool deep = true)
        {
            edges ??= new HashSet<Edge>();
            if ((visited ??= new HashSet<object>()).Add(o))
                if (o != null)
                {
                    var type = o.GetType();
                    foreach (var field in type.GetPublicFields())
                    {
                        var t = field.GetValue(o);
                        if (edges.Add(new FieldEdge(o, t, field)) && t != null && deep)
                            CollectEdges(t, edges, visited, deep);
                    }
                    foreach (var property in type.GetPublicProperties())
                    {
                        var p = property.GetValue(o);
                        if (edges.Add(new PropertyEdge(o, p, property)) && p != null && deep)
                            CollectEdges(p, edges, visited, deep);
                    }
                }
            return edges;
        }
        /// <summary>
        /// NOTICE: we can use this algorithm to break loops and determine 
        /// where to put YamlIsLink or YamlHasLinks Attributes (on Fields or Properties)
        /// </summary>
        /// <param name="root"></param>
        /// <param name="infos"></param>
        /// <returns></returns>
        public static bool GetBreakingPosition(object root, out List<MemberInfo> infos)
        {
            infos = new List<MemberInfo>();
            if(GetBreakingEdges(root,out var edges))
            {
                foreach(var edge in edges)
                {
                    switch (edge)
                    {
                        case PropertyEdge p:
                            infos.Add(p.P);
                            break;
                        case FieldEdge f:
                            infos.Add(f.F);
                            break;
                        case ItemEdge i:
                            infos.Add(i.M);
                            break;
                    }
                }
            }
            return infos.Count>0;
        }
        public static bool GetBreakingEdges(object root, out List<Edge> edges)
        {
            edges = new List<Edge>();
            if(GetBreakingRoutes(root,out var routes))
            {
                foreach(var route in routes)
                {
                    if (route!=null && route.Count > 1)
                    {
                        //break edge at the middle of route
                        edges.Add(route[route.Count / 2]);
                    }
                }

                return edges.Count>0;
            }
            return false;
        }
        public static bool GetBreakingRoutes(object root,out List<List<Edge>> all_routes)
        {
            all_routes = new List<List<Edge>>();
            var edges = CollectEdges(root, deep: false);
            foreach(var s in edges.Select(e => e.Target))
            {
                //fields or properties or collection items
                if(GraphTools.GetLoopRoutes(s, edges, out List<List<Edge>>  routes,1))
                {
                    all_routes.AddRange(routes);
                }
            }
            return all_routes.Count > 0;
        }
    }
}
