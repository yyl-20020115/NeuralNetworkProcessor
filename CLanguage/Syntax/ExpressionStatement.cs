using CLanguage.Interpreter;
using CLanguage.Compiler;

namespace CLanguage.Syntax;

public class ExpressionStatement : Statement
{
    public Expression Expression { get; set; }

    public ExpressionStatement (Expression expr) => Expression = expr;

    protected override void DoEmit (EmitContext ec)
    {
        if (Expression != null) {
            Expression.Emit (ec);

            ec.Emit (OpCode.Pop);
        }
    }

    public override string ToString () => $"{Expression};";

    public override void AddDeclarationToBlock (BlockContext context)
    {
    }

    public override bool AlwaysReturns => false;
}
