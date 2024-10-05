﻿using System;
using System.Linq;
using CLanguage.Interpreter;
using CLanguage.Types;
using CLanguage.Compiler;

namespace CLanguage.Syntax;

public class MemberFromPointerExpression (Expression left, string memberName) : Expression
{
    public Expression Left { get; private set; } = left;
    public string MemberName { get; private set; } = memberName;

    public override bool CanEmitPointer => true;

    public override CType GetEvaluatedCType (EmitContext ec)
    {
        var targetType = Left.GetEvaluatedCType (ec);

        var pType = targetType as CPointerType;

        if (pType != null && pType.InnerType is CStructType structType) {

            var member = structType.Members.FirstOrDefault (x => x.Name == MemberName);
            if (member == null) {
                ec.Report.Error (1061, "'{1}' not found in '{0}'", structType.Name, MemberName);
                return CBasicType.SignedInt;
            }

            return member.MemberType;
        }

        if (pType != null) {
            ec.Report.Error (1061, "'{1}' not found in '{0}'", pType, MemberName);
            return CBasicType.SignedInt;
        }
        else {
            ec.Report.Error (1061, "-> cannot be used with '{0}'", targetType);
            return CBasicType.SignedInt;
        }
    }

    protected override void DoEmit (EmitContext ec)
    {
        var targetType = Left.GetEvaluatedCType (ec);

        if (targetType is CPointerType pType && pType.InnerType is CStructType structType) {

            var member = structType.Members.FirstOrDefault (x => x.Name == MemberName);

            if (member == null) {
                ec.Report.Error (1061, "'{1}' not found in '{0}'", structType.Name, MemberName);
            }
            else {
                if (member is CStructMethod method && member.MemberType is CFunctionType functionType) {
                    var res = ec.ResolveMethodFunction (structType, method);
                    if (res != null) {
                        Left.Emit (ec);
                        ec.Emit (OpCode.LoadConstant, Value.Pointer (res.Address));
                    }
                }
                else {
                    Left.Emit (ec);
                    ec.Emit (OpCode.LoadConstant, Value.Pointer (structType.GetFieldValueOffset (member, ec)));
                    ec.Emit (OpCode.OffsetPointer);
                    ec.Emit (OpCode.LoadPointer);
                }
            }
        }
        else {
            throw new NotSupportedException ($"Cannot read '{MemberName}' on " + targetType?.GetType ().Name);
        }
    }

    protected override void DoEmitPointer (EmitContext ec)
    {
        var targetType = Left.GetEvaluatedCType (ec);

        if (targetType is CPointerType pType && pType.InnerType is CStructType structType) {

            var member = structType.Members.FirstOrDefault (x => x.Name == MemberName);

            if (member == null) {
                ec.Report.Error (1061, "'{1}' not found in '{0}'", structType.Name, MemberName);
            }
            else {
                if (member is CStructMethod method && member.MemberType is CFunctionType functionType) {
                    ec.Report.Error (1656, "Cannot assign to '{0}'", MemberName);
                }
                else {
                    Left.Emit (ec);
                    ec.Emit (OpCode.LoadConstant, Value.Pointer (structType.GetFieldValueOffset (member, ec)));
                    ec.Emit (OpCode.OffsetPointer);
                }
            }
        }
        else {
            throw new NotSupportedException ($"Cannot write '{MemberName}' on " + targetType?.GetType ().Name);
        }
    }

    public override string ToString () => $"{Left}->{MemberName}";
}
