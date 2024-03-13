using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utilities;

namespace NeuralNetworkProcessor.NT;

public record class GPoint(int Char, int Position, int Line, int Column, GNode Node)
{
    public override string ToString()
        => $"{Node.Name}=\'{UnicodeClassTools.ToText(this.Char, "")}\'({this.Position},{this.Line},{this.Column})";

    public StringBuilder Flattern(StringBuilder builder = null)
        => (builder ?? new()).Append(UnicodeClassTools.ToText(this.Char));
}
public partial record class GPath
{
    public GLocationReader Reader;
    public readonly GNetwork Network;
    public List<GPoint> Points = [];
    public List<GPath> SubPaths = [];
    public GPoint FlatLastPoint => this.Points.LastOrDefault();
    public int LastPosition = -1;

    public GPath(params GPoint[] Points)
    {
        this.AddPoints(Points);
        if (Points.Length > 0)
            this.Network = Points[^1].Node.Network;
    }
    public GPath(params GPath[] Paths)
    {
        this.AddSubPaths(Paths);
        if (Paths.Length > 0) 
            this.Network = Paths[^1].Network;
    }
    public GPath AddPoints(params GPoint[] Points)
    {
        this.Points.AddRange(Points);
        return this;
    }
    public GPath AddSubPaths(params GPath[] Paths)
    {
        this.SubPaths.AddRange(Paths);
        return this;
    }
    public GPath FoldPoints(GPoint Tail)
    {
        var last = this.Points.Count - 1;
        for (int i = last; i >= 0; i--)
        {
            if (this.Points[i].Node.Network == Tail.Node.Network)
                continue;
            else
            {
                var Tails = this.Points.TakeLast(last - i).ToList();
                if (Tails.Count >= 0)
                {
                    Tails.Add(Tail);
                    this.Points.RemoveRange(i, last - i);
                    var Folded = new GPath(Tails.ToArray());
                    return new(this, Folded) { Reader = this.Reader.Clone() };
                }
                break;
            }
        }
        return new(this, new(Tail));
    }
    public StringBuilder Flattern(StringBuilder builder = null)
    {
        builder ??= new StringBuilder();
        if (this.SubPaths.Count > 0)
            this.SubPaths.ForEach(path => path.Flattern(builder));
        if (this.Points.Count > 0)
            this.Points.ForEach(point => point.Flattern(builder));
        return builder;
    }
}
