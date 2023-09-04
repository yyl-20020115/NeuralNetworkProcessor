namespace NeuralNetworkProcessor.Core;

public class RefPosition
{
    public int Position = -1;
    public RefPosition(int Position = -1) => this.Position = Position;
    public override string ToString() => this.Position.ToString();
}
