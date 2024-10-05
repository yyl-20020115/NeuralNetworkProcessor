using CLanguage.Interpreter;
using CLanguage.Compiler;

namespace CLanguage.Syntax;

public class WhileStatement (bool isDo, Expression condition, Block loop) : Statement
{
    public bool IsDo { get; private set; } = isDo;
    public Expression Condition { get; private set; } = condition;
    public Block Loop { get; private set; } = loop;

    protected override void DoEmit(EmitContext parentContext)
    {
        var condLabel = parentContext.DefineLabel();
        var loopLabel = parentContext.DefineLabel();
        var endLabel = parentContext.DefineLabel();

        var ec = parentContext.PushLoop (breakLabel: endLabel, continueLabel: condLabel);

        if (IsDo) {
            ec.EmitLabel (loopLabel);
            Loop.Emit (ec);
            ec.EmitLabel (condLabel);
            Condition.Emit (ec);
            ec.EmitCastToBoolean (Condition.GetEvaluatedCType (ec));
            ec.Emit (OpCode.BranchIfFalse, endLabel);
            ec.Emit (OpCode.Jump, condLabel);
        }
        else {
            ec.EmitLabel (condLabel);
            Condition.Emit (ec);
            ec.EmitCastToBoolean (Condition.GetEvaluatedCType (ec));
            ec.Emit (OpCode.BranchIfFalse, endLabel);
            ec.EmitLabel (loopLabel);
            parentContext.BeginBlock (Loop);
            Loop.Emit (ec);
            ec.Emit (OpCode.Jump, condLabel);
        }
        ec.EmitLabel (endLabel);
    }

    public override bool AlwaysReturns => false;

    public override string ToString () => IsDo ? $"do {Loop} while({Condition});" : $"while ({Condition}) {Loop};";

    public override void AddDeclarationToBlock (BlockContext context)
    {
        Loop.AddDeclarationToBlock (context);
    }
}
