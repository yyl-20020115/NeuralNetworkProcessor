using System;
using System.Collections.Generic;
using CLanguage.Compiler;
using CLanguage.Interpreter;
using CLanguage.Types;

namespace CLanguage.Syntax;

public class FunctionDefinition (DeclarationSpecifiers specifiers, Declarator declarator, List<Declaration>? parameterDeclarations, Block body) : Statement
{
    public DeclarationSpecifiers Specifiers { get; set; } = specifiers ?? throw new ArgumentNullException (nameof (specifiers));
    public Declarator Declarator { get; set; } = declarator ?? throw new ArgumentNullException (nameof (declarator));
    public List<Declaration>? ParameterDeclarations { get; set; } = parameterDeclarations;
    public Block Body { get; set; } = body ?? throw new ArgumentNullException (nameof (body));

    public override bool AlwaysReturns => false;

    public override void AddDeclarationToBlock (BlockContext context)
    {
        var fdef = this;
        var block = context.Block;
        if (context.MakeCType (fdef.Specifiers, fdef.Declarator, null, block) is CFunctionType ftype) {
            var name = fdef.Declarator.DeclaredIdentifier;
            var nameContext = fdef.Declarator.InnerDeclarator is IdentifierDeclarator idents ? String.Join ("::", idents.Context) : "";
            var f = new CompiledFunction (name, nameContext, ftype, fdef.Body);
            block.Functions.Add (f);
        }
    }

    protected override void DoEmit (EmitContext ec)
    {
        // Emitted by the compiler
    }
}
