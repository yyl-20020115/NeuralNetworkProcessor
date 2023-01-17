namespace Som.Compiler;

public class ProgramDefinitionError : Exception
{
    public ProgramDefinitionError(string message)
        :base(message){}

    public override string ToString() => "ProgramDefinitionError: " + this.Message;
}