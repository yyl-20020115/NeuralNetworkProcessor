using System;
using CLanguage.Compiler;

namespace CLanguage.Syntax;

public class EnumeratorStatement (string left, Expression? right = null) : Statement
{
    public string Name { get; } = left ?? throw new ArgumentNullException (nameof (left));
    public Expression? LiteralValue { get; } = right;

    public override bool AlwaysReturns => false;

    public override string ToString () => $"{Name} = {LiteralValue}";

    protected override void DoEmit (EmitContext ec)
    {
    }

    public override void AddDeclarationToBlock (BlockContext context)
    {
    }
}
