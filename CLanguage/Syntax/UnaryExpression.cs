using System;

using CLanguage.Types;
using CLanguage.Interpreter;
using CLanguage.Compiler;

namespace CLanguage.Syntax;

public enum Unop : int
{
    None,
    Not,
    Negate,
    BinaryComplement,
    PreIncrement,
    PreDecrement,
    PostIncrement,
    PostDecrement,
}

public class UnaryExpression (Unop op, Expression right) : Expression
{
    public Unop Op { get; private set; } = op;
    public Expression Right { get; private set; } = right;

    public override CType GetEvaluatedCType (EmitContext ec) => (Op == Unop.Not) ? CBasicType.SignedInt : GetPromotedType (Right, Op.ToString (), ec);

    protected override void DoEmit (EmitContext ec)
    {
        switch (Op) {
            case Unop.PreIncrement: {
                    var ie = new AssignExpression (Right, new BinaryExpression (Right, Binop.Add, ConstantExpression.One));
                    ie.Emit (ec);
                }
                break;
            case Unop.PreDecrement: {
                    var ie = new AssignExpression (Right, new BinaryExpression (Right, Binop.Add, ConstantExpression.NegativeOne));
                    ie.Emit (ec);
                }
                break;
            case Unop.PostIncrement: {
                    Right.Emit (ec);
                    var ie = new AssignExpression (Right, new BinaryExpression (Right, Binop.Add, ConstantExpression.One));
                    ie.Emit (ec);
                    ec.Emit (OpCode.Pop);
                }
                break;
            case Unop.PostDecrement: {
                    Right.Emit (ec);
                    var ie = new AssignExpression (Right, new BinaryExpression (Right, Binop.Add, ConstantExpression.NegativeOne));
                    ie.Emit (ec);
                    ec.Emit (OpCode.Pop);
                }
                break;
            default: {
                    var aType = (CBasicType)GetEvaluatedCType (ec);

                    Right.Emit (ec);
                    ec.EmitCast (Right.GetEvaluatedCType (ec), aType);

                    var ioff = ec.GetInstructionOffset (aType);

                    switch (Op) {
                        case Unop.None:
                            break;
                        case Unop.Negate:
                            ec.Emit ((OpCode)(OpCode.NegateInt8 + ioff));
                            break;
                        case Unop.Not:
                            ec.Emit ((OpCode)(OpCode.NotInt8 + ioff));
                            break;
                        case Unop.BinaryComplement:
                            ec.Emit ((OpCode)(OpCode.BinaryNotInt8 + ioff));
                            break;
                        default:
                            throw new NotSupportedException ("Unsupported unary operator '" + Op + "'");
                    }
                }
                break;
        }
    }

    public override string ToString () => $"({Op} {Right})";

    public override Value EvalConstant (EmitContext ec)
    {
        var rightType = Right.GetEvaluatedCType (ec);

        if (rightType.IsIntegral) {
            var right = (int)Right.EvalConstant (ec);

            return Op switch {
                Unop.None => (Value)right,
                Unop.Not => (Value)((right == 0) ? 1 : 0),
                Unop.Negate => (Value)(-right),
                Unop.BinaryComplement => (Value)~right,
                Unop.PreIncrement => (Value)(right + 1),
                Unop.PreDecrement => (Value)(right - 1),
                Unop.PostIncrement => (Value)right,
                Unop.PostDecrement => (Value)right,
                _ => throw new NotSupportedException ("Unsupported unary operator '" + Op + "'"),
            };
        }

        return base.EvalConstant (ec);
    }
}
