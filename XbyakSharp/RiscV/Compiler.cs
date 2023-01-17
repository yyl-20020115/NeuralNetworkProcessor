namespace XbyakSharp.RiscV;
public class Compiler
{
    public Parser Parser { get; } = new ();
    public CodeGenerator CodeGenerator { get; } = new();
    public CodeGenerator Compile(TextReader reader) 
        => this.CodeGenerator.Generate(this.Parser.Parse(reader));
}
