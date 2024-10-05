using CLanguage.Types;
using CLanguage.Interpreter;
using CLanguage.Compiler;

namespace CLanguage.Syntax;

public class ConditionalExpression (Expression condition, Expression trueValue, Expression falseValue) : Expression
{
    public Expression Condition { get; set; } = condition;
    public Expression TrueValue { get; set; } = trueValue;
    public Expression FalseValue { get; set; } = falseValue;

    public override CType GetEvaluatedCType (EmitContext ec) => TrueValue.GetEvaluatedCType (ec);

    protected override void DoEmit (EmitContext ec)
    {
        var falseLabel = ec.DefineLabel ();
        var endLabel = ec.DefineLabel ();

        Condition.Emit (ec);
        ec.EmitCastToBoolean (Condition.GetEvaluatedCType (ec));
        ec.Emit (OpCode.BranchIfFalse, falseLabel);

        TrueValue.Emit (ec);
        ec.Emit (OpCode.Jump, endLabel);

        ec.EmitLabel (falseLabel);
        FalseValue.Emit (ec);

        ec.EmitLabel (endLabel);
    }
}

