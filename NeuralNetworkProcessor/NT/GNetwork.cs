using NeuralNetworkProcessor.ZRF;
using System.Collections.Generic;
using System.Text;

namespace NeuralNetworkProcessor.NT;

public record class GNetwork(string Name = "",GNetwork Parent = null, Group Group = null, object Tag = null)
{
    public GNode StartJointNode { init; get; }
    public GNode EndJointNode { init; get; }
    public readonly List<GNode> Nodes = [];
    public readonly List<GEdge> Edges = [];
    public readonly List<GNetwork> SubNetworks = [];
    public GNetwork Init() 
        => this.AddNodes(this.StartJointNode, this.EndJointNode);
    public GNetwork AddNodes(params GNode[] nodes) 
        => this.AddNodes(nodes as IEnumerable<GNode>);
    public GNetwork AddEdges(params GEdge[] edges) 
        => this.AddEdges(edges as IEnumerable<GEdge>);
    public GNetwork AddNodes(IEnumerable<GNode> nodes)
    {
        this.Nodes.AddRange(nodes);
        return this;
    }
    public GNetwork AddEdges(IEnumerable<GEdge> edges)
    {
        this.Edges.AddRange(edges);
        return this;
    }

    public GNetwork Merge(GNetwork sub)
    {
        this.SubNetworks.Add(sub);
        this.Nodes.AddRange(sub.Nodes);
        this.Edges.AddRange(sub.Edges);

        return this;
    }
    public StringBuilder FormatDot(StringBuilder builder)
    {
        builder.AppendLine("digraph G{");
        foreach(var node in this.Nodes)
            builder.AppendLine($"\t{node.Name};");
        foreach(var edge in this.Edges)
            builder.AppendLine($"{edge.Source}->{edge.Destination};");
        builder.AppendLine("}");
        return builder;
    }
    public override string ToString() 
        => this.FormatDot(new ()).ToString();
}
