namespace NeuralNetworkProcessorSample.Calculator;
public partial record Context
{

}
public partial record InterpretrContext : Context
{
    public Interpreter Interpreter { get; set; } = new();
}
