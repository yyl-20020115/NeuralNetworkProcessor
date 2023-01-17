namespace NeuralNetworkProcessor.NT;
public interface GLocationReader
{
    int Position { get; }
    int Line { get; }
    int Column { get; }
    int Length { get; }
    int Peek();
    int Read();
    string ReadLine();
    string ReadToEnd();
    int Read(char[] buffer, int index, int count);
    GLocationReader Clone();
}
