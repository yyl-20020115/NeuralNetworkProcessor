namespace NeuralNetworkProcessor.Core;

public class RefPosition(int Position = -1)
{
    public int Position = Position;

    public override string ToString() => this.Position.ToString();
}
