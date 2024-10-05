using CLanguage.Types;

using CLanguage.Interpreter;
using CLanguage.Compiler;

namespace CLanguage.Syntax;

public class SequenceExpression (Expression first, Expression second) : Expression
{
    public Expression First { get; set; } = first;
    public Expression Second { get; set; } = second;

    public override CType GetEvaluatedCType (EmitContext ec) => Second.GetEvaluatedCType (ec);

    protected override void DoEmit (EmitContext ec)
    {
        First.Emit (ec);
        ec.Emit (OpCode.Pop);
        Second.Emit (ec);
    }

    public override string ToString () => "(" + First + ", " + Second + ")";
}

