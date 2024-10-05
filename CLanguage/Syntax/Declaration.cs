namespace CLanguage.Syntax;

public abstract class Declaration (DeclarationSpecifiers specs, Declarator decl, Initializer init) : Statement
{
    public DeclarationSpecifiers Specifiers { get; set; } = specs;
    public Declarator Declarator { get; set; } = decl;
    public Initializer Initializer { get; set; } = init;

    public override bool AlwaysReturns => false;
}
