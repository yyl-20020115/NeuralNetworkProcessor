using System;
using CLanguage.Types;

using CLanguage.Interpreter;
using CLanguage.Compiler;

namespace CLanguage.Syntax;

public enum Binop : int
{
    Add,
    Subtract,
    Multiply,
    Divide,
    Mod,
    ShiftLeft,
    ShiftRight,
    BinaryAnd,
    BinaryOr,
    BinaryXor,
}

public class BinaryExpression : Expression
{
    public Expression Left { get; private set; }
    public Binop Op { get; private set; }
    public Expression Right { get; private set; }

    public BinaryExpression (Expression left, Binop op, Expression right)
    {
        Left = left ?? throw new ArgumentNullException (nameof (left));
        Op = op;
        Right = right ?? throw new ArgumentNullException (nameof (right));
    }

    protected override void DoEmit (EmitContext ec)
    {
        var aType = GetArithmeticType (Left, Right, Op.ToString (), ec);

        Left.Emit (ec);
        ec.EmitCast (Left.GetEvaluatedCType (ec), aType);
        Right.Emit (ec);
        ec.EmitCast (Right.GetEvaluatedCType (ec), aType);

        var ioff = ec.GetInstructionOffset (aType);

        switch (Op) {
            case Binop.Add:
                ec.Emit ((OpCode)(OpCode.AddInt8 + ioff));
                break;
            case Binop.Subtract:
                ec.Emit ((OpCode)(OpCode.SubtractInt8 + ioff));
                break;
            case Binop.Multiply:
                ec.Emit ((OpCode)(OpCode.MultiplyInt8 + ioff));
                break;
            case Binop.Divide:
                ec.Emit ((OpCode)(OpCode.DivideInt8 + ioff));
                break;
            case Binop.Mod:
                ec.Emit ((OpCode)(OpCode.ModuloInt8 + ioff));
                break;
            case Binop.BinaryAnd:
                ec.Emit ((OpCode)(OpCode.BinaryAndInt8 + ioff));
                break;
            case Binop.BinaryOr:
                ec.Emit ((OpCode)(OpCode.BinaryOrInt8 + ioff));
                break;
            case Binop.BinaryXor:
                ec.Emit ((OpCode)(OpCode.BinaryXorInt8 + ioff));
                break;
            case Binop.ShiftLeft:
                ec.Emit ((OpCode)(OpCode.ShiftLeftInt8 + ioff));
                break;
            case Binop.ShiftRight:
                ec.Emit ((OpCode)(OpCode.ShiftRightInt8 + ioff));
                break;
            default:
                throw new NotSupportedException ("Unsupported binary operator '" + Op + "'");
        }
    }

    public override CType GetEvaluatedCType (EmitContext ec) => GetArithmeticType (Left, Right, Op.ToString (), ec);

    public override string ToString () => $"({Left} {Op} {Right})";

    public override Value EvalConstant (EmitContext ec)
    {
        var leftType = Left.GetEvaluatedCType (ec);
        var rightType = Right.GetEvaluatedCType (ec);

        if (leftType.IsIntegral && rightType.IsIntegral) {
            var left = (int)Left.EvalConstant (ec);
            var right = (int)Right.EvalConstant (ec);

            return Op switch {
                Binop.Add => (Value)(left + right),
                Binop.Subtract => (Value)(left - right),
                Binop.Multiply => (Value)(left * right),
                Binop.Divide => (Value)(left / right),
                Binop.Mod => (Value)(left % right),
                Binop.BinaryAnd => (Value)(left & right),
                Binop.BinaryOr => (Value)(left | right),
                Binop.BinaryXor => (Value)(left ^ right),
                Binop.ShiftLeft => (Value)(left << right),
                Binop.ShiftRight => (Value)(left >> right),
                _ => throw new NotSupportedException ("Unsupported binary operator '" + Op + "'"),
            };
        }

        return base.EvalConstant (ec);
    }
}
