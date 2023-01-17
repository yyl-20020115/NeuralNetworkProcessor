using System.Linq;
using System.Collections.Generic;

namespace NeuralNetworkProcessor.NT;

public class GProcessor
{
    public readonly GNetwork Network;
    public readonly List<GNode> Terminals = new();
    public readonly GExecutor<GPath> Executor;
    public GProcessor(GNetwork newtork)
    {
        this.Network = newtork;
        this.Executor = new (path => this.Process(path));
    }
    
    public ICollection<GPath> Process(GLocationReader reader)
    {
        this.Terminals.Clear();
        this.Terminals.AddRange(
            this.Network.Nodes.Where(node => node.Type == GNodeType.Terminal));
        
        var @char = reader.Read();
        var position = 0;
        if (@char != -1)
        {
            this.Executor.Consume(
                this.Terminals.Where(terminal => terminal.Match(@char)).Select(
                    n => new GPath(new GPoint(@char, position, reader.Line, reader.Column, n))
                    { Reader = reader.Clone(), LastPosition = position }).ToList()
                    ,true);

            this.Executor.Start();
            this.Executor.WaitComplete();
        }
        return this.Executor.Collection;
    }
    public void Process(GPath path)
    {
        var @char = path.Reader.Read();
        var position = path.Reader.Position;
        var line = path.Reader.Line;
        var column = path.Reader.Column;
        var last = path.FlatLastPoint;
        var node = last.Node;
        var edges = Network.Edges.Where(e => e.Source == node).ToList();

        var locals = new List<GPath>(); 
        foreach(var edge in edges)
        {
            var dest = edge.Destination;
            var point = new GPoint(@char, position, line, column, dest);
            if (dest.Type == GNodeType.Terminal)
            {
                if (dest.Match(@char))
                {
                    if (dest.Network == node.Network)
                    {
                        path.AddPoints(point);
                    }
                    else
                    {
                        var split = path with
                        {
                            Points = path.Points.ToList(),
                            SubPaths = path.SubPaths.ToList(),
                            LastPosition = position,
                        };
                        locals.Add(split.AddPoints(point));
                    }
                }
            }
            else if (dest.Type == GNodeType.EndJoint)
            {
                var split = path with
                {
                    Points = path.Points.ToList(),
                    SubPaths = path.SubPaths.ToList(),
                    LastPosition = position,
                };
                locals.Add(split.FoldPoints(point));
            }
        }

        this.Executor.Consume(locals);
    }
}
