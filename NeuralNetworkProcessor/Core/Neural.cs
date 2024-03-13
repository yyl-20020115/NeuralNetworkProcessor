namespace NeuralNetworkProcessor.Core;

public interface Neural 
{
    long SerialNumber { get; }
}
public interface NeuralMonitor
{
    bool IsActive { get; }
    void SetActive(bool active);
}

public abstract record NeuralEntity : Neural
{
    protected static long CurrentSerialNumber = 0L;
    public static long GenerateSerialNumber() 
        => CurrentSerialNumber++;
    public long SerialNumber { get; protected set; } = GenerateSerialNumber();
}
