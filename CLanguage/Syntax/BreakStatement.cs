using CLanguage.Compiler;

namespace CLanguage.Syntax;

public class BreakStatement : Statement
{
    public BreakStatement ()
    {
    }

    public override bool AlwaysReturns => false;

    public override void AddDeclarationToBlock (BlockContext context)
    {
    }

    protected override void DoEmit (EmitContext ec)
    {
        switch (ec.BreakLabel) {
            case not null:
                ec.Emit (Interpreter.OpCode.Jump, ec.BreakLabel);
                break;
            default:
                ec.Report.Error (139, "No enclosing statement out of which to break");
                break;
        }
    }
}
