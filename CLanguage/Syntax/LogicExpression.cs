using System;

using CLanguage.Types;
using CLanguage.Interpreter;
using CLanguage.Compiler;

namespace CLanguage.Syntax;

public enum LogicOp : int
{
    And,
    Or,
}

public class LogicExpression (Expression left, LogicOp op, Expression right) : Expression
{
    public Expression Left { get; private set; } = left;
    public LogicOp Op { get; private set; } = op;
    public Expression Right { get; private set; } = right;

    protected override void DoEmit (EmitContext ec)
    {
        var label = new Label ();
        Left.Emit (ec);
        ec.EmitCastToBoolean (Left.GetEvaluatedCType (ec));

        switch (Op) {
            case LogicOp.And:
                ec.Emit (OpCode.BranchIfFalseNoSPChange, label);  //or Dup instruction. 
                break;
            case LogicOp.Or:
                ec.Emit (OpCode.BranchIfTrueNoSPChange, label);
                break;
        }

        Right.Emit (ec); // (true)||(1/0) <- second part not executed in C 
        ec.EmitCastToBoolean (Right.GetEvaluatedCType (ec));

        switch (Op) {
            case LogicOp.And:
                ec.Emit (OpCode.LogicalAnd);
                break;
            case LogicOp.Or:
                ec.Emit (OpCode.LogicalOr);
                break;
            default:
                throw new NotSupportedException ("Unsupported logical operator '" + Op + "'");
        }
        ec.EmitLabel (label);
    }



    public override CType GetEvaluatedCType (EmitContext ec) => CBasicType.Bool;

    public override string ToString ()
    => $"({Left} {Op} {Right})";
}
