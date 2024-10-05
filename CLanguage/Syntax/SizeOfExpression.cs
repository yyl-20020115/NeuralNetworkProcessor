﻿using CLanguage.Interpreter;
using CLanguage.Types;
using CLanguage.Compiler;

namespace CLanguage.Syntax;

public class SizeOfExpression : Expression
{
    public Expression Query { get; private set; }

    public SizeOfExpression (Expression query) => Query = query;

    public override CType GetEvaluatedCType (EmitContext ec) => CBasicType.UnsignedLongInt;

    protected override void DoEmit (EmitContext ec)
    {
        var type = Query.GetEvaluatedCType (ec);
        Value cval = type.NumValues;
        ec.Emit (OpCode.LoadConstant, cval);
    }
}
