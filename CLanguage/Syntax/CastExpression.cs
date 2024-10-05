using CLanguage.Compiler;
using CLanguage.Types;

namespace CLanguage.Syntax;

public class CastExpression (TypeName typeName, Expression innerExpression) : Expression
{
    public TypeName TypeName { get; } = typeName;
    public Expression InnerExpression { get; } = innerExpression;

    public override CType GetEvaluatedCType (EmitContext ec) => ec.ResolveTypeName (TypeName) ?? CBasicType.SignedInt;

    protected override void DoEmit (EmitContext ec)
    {
        var rtype = GetEvaluatedCType (ec);
        var itype = InnerExpression.GetEvaluatedCType (ec);
        InnerExpression.Emit (ec);
        ec.EmitCast (itype, rtype);
    }
}
