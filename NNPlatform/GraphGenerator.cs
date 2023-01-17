using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using GraphSharp.Controls;
using NeuralNetworkProcessor.Core;
using QuickGraph;

namespace NNPlatform
{
    public class NeuralNetworkVertex : DependencyObject, NeuralMonitor
    {
        public bool IsActive => (bool)GetValue(IsActiveProperty);
        public void SetActive(bool active)
            => SetValue(IsActiveProperty, active);
        
        // Using a DependencyProperty as the backing store for IsActiveDP.
        // This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register(nameof(IsActive), typeof(bool),
                typeof(NeuralNetworkVertex), new PropertyMetadata(false));

        public Neural Entity { get; protected set; }
        public long SerialNumber => this.Entity != null ? this.Entity.SerialNumber : 0;
        public long ContainerSerialNumber => this.Entity switch
        {
            Cell c=>c.Owner.SerialNumber,
            Trend t=>t.Owner.SerialNumber,
            Cluster c=>c.Owner.SerialNumber,
            _=>0
        };
        public string Text => this.Entity + $" ({this.SerialNumber })";
        public bool IsTrend => this.Entity is Trend;
        public bool IsCell => this.Entity is Cell;
        public bool IsCluster => this.Entity is Cluster;
        public NeuralNetworkVertex(Neural NeuralEntity)
        {
            this.Entity = NeuralEntity ?? throw new ArgumentNullException(nameof(NeuralEntity));
            //TODO: this changes the matching method
            //this.Entity.Monitor = this;
        }
        public override string ToString() => this.Text;
    }
    public class NeuralNetworkEdge : Edge<NeuralNetworkVertex>
    {
        public bool IsActive => this.Source != null && this.Target != null
            && this.Source.IsActive && this.Target.IsActive;
        public string Text => $"{this.Source.Text} -> {this.Target.Text}";
        public string ToolTip => Text;
        public NeuralNetworkEdge()
            : this(null, null) { }
        public NeuralNetworkEdge(NeuralNetworkVertex source, NeuralNetworkVertex target)
            : base(source, target) { }
        public override IEdge<NeuralNetworkVertex> Clone() => new NeuralNetworkEdge(this.Source, this.Target);
        public override string ToString() => this.Text;
    }
    public class NeuralNetworkGraph : BidirectionalGraph<NeuralNetworkVertex, NeuralNetworkEdge>
    {
        public NeuralNetworkGraph() { }

        public NeuralNetworkGraph(bool allowParallelEdges)
            : base(allowParallelEdges) { }

        public NeuralNetworkGraph(bool allowParallelEdges, int vertexCapacity)
            : base(allowParallelEdges, vertexCapacity) { }
    }
    public class NeuralNetworkGraphLayout : GraphLayout<NeuralNetworkVertex, NeuralNetworkEdge, NeuralNetworkGraph> { }
    public static class GraphGenerator
    {
        public static NeuralNetworkVertex GetVertex(Neural nc, Dictionary<Neural, NeuralNetworkVertex> dict)
            => dict.TryGetValue(nc, out var nv) ? nv :
            dict[nc] = new NeuralNetworkVertex(nc);

        public static NeuralNetworkEdge CreateEdge(NeuralNetworkVertex Source, NeuralNetworkVertex Target) 
            => new(Source, Target);
        public static NeuralNetworkGraph GenerateNetwork(Aggregation a, Dictionary<Neural, NeuralNetworkVertex> dict = null)
        {
            dict ??= new();
            var graph = new NeuralNetworkGraph(true);
            
            a.Clusters.SelectMany(c => c.Trends).SelectMany(t => t.Cells).ToList()
                .ForEach(cell =>GetVertex(cell,dict));

            foreach (var c in a.Clusters)
            {
                var vc = GetVertex(c, dict);

                graph.AddVertex(vc);

                foreach (var t in c.Trends)
                {
                    var vp = GetVertex(t, dict);
                    graph.AddVertex(vp);
                    graph.AddEdge(CreateEdge(vp, vc));
                    //add node vertex as sub vertex (not child vertex)
                    foreach (var n in t.Cells)
                    {
                        var vs = GetVertex(n, dict);
                        //cells are sub vertex inside trends
                        graph.AddSubVertex(vp,vs);
                    }
                }
            }
            //search and connect in second pass
            foreach(var c in a.Clusters)
            {
                var vc = GetVertex(c, dict);
                
                foreach (var t in c.Targets)
                {
                    var vt = GetVertex(t, dict);
                    if (!graph.ContainsVertex(vt))
                        graph.AddVertex(vt);
                    graph.AddEdge(CreateEdge(vc, vt));
                }
            }

            var ic = a.Clusters.Where(c => c.Name == "").FirstOrDefault();

            var vi = GetVertex(ic ?? Cluster.InputSourceCluster, dict);
            graph.AddVertex(vi);
            foreach (var c in a.Clusters.Where(C => C is TerminalCluster))
                graph.AddEdge(CreateEdge(vi, GetVertex(c, dict)));

            return graph;
        }
        public static string ToDot(NeuralNetworkGraph Graph, string GraphName = "G", string RankDir = "LR")
        {
            using var writer = new StringWriter();

            writer.Write("digraph " + GraphName);
            writer.Write('{');
            writer.WriteLine();
            writer.WriteLine("rankdir={0};", RankDir);
            foreach (var v in Graph.Vertices)
            {
                if (v.IsTrend)
                {
                    var subs = Graph.GetSubVertices(v).ToList();

                    writer.Write($"{v.SerialNumber} [shape=record label=\"\'{v.Text}\'|");
                    writer.Write('{');
                    writer.Write('{');
                    for (int i = 0; i < subs.Count; i++)
                    {
                        if (i > 0)
                        {
                            writer.Write('|');
                        }
                        var child = subs[i];

                        writer.Write($"<child.SerialNumber>'{child.Text}'");
                    }
                    writer.Write('}');
                    writer.Write("|<uplink>");
                    writer.Write('}');
                    writer.WriteLine("\"];");
                }
                else if (!v.IsCell)
                {
                    writer.WriteLine($"{v.SerialNumber} [label=\"'{v.Text}'\"];");
                }
            }
            foreach (var e in Graph.Edges)
            {
                if (e.Target.IsCell)
                {
                    writer.WriteLine("{0}->{1}:{2};", e.Source.SerialNumber, e.Target.ContainerSerialNumber, e.Target.SerialNumber);
                }
                else //edges for top level nodes
                {
                    writer.WriteLine("{0}->{1};", e.Source.SerialNumber, e.Target.SerialNumber);
                }
            }
            writer.Write('}');
            writer.WriteLine();

            return writer.ToString();
        }
    }
}
