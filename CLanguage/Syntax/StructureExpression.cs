using System;
using System.Collections.Generic;
using System.Linq;
using CLanguage.Types;
using CLanguage.Compiler;

namespace CLanguage.Syntax;

public class StructureExpression : Expression
{
    public class Item (string? field, Expression expression)
    {
        public int Index;
        public string? Field = field;
        public Expression Expression = expression;
    }

    public List<Item> Items { get; private set; }

    public StructureExpression ()
    {
        Items = [];
    }

    public override string ToString () => $"{{ {string.Join (", ", Items.Select (x => x.Expression.ToString ()))} }}";

    public override CType GetEvaluatedCType (EmitContext ec) => CType.Void;

    protected override void DoEmit (EmitContext ec) => throw new NotImplementedException (GetType ().Name + ": Emit");
}
