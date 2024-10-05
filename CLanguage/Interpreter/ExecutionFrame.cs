namespace CLanguage.Interpreter;

public class ExecutionFrame (BaseFunction function)
{
    public int FP { get; set; }
    public int IP { get; set; }
    public BaseFunction Function { get; set; } = function ?? throw new System.ArgumentNullException (nameof (function));

    public override string ToString () => $"{FP}: {Function.Name}";
}
