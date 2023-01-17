namespace NeuralNetworkProcessor.NT;

public record class GEdge(GNode Source, GNode Destination,GNetwork Network)
{
    public GNode Source { init; get; } = Source;
    public GNode Destination { init; get; } = Destination;
    public GNetwork Network { init; get; } = Network;
    public GEdge GetReversed()
        => new(this.Destination, this.Source, Network);
}
