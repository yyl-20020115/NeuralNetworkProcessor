using System;
namespace CLanguage.Syntax;

public class TypeName (DeclarationSpecifiers specifiers, Declarator? declarator)
{
    public DeclarationSpecifiers Specifiers { get; } = specifiers;
    public Declarator? Declarator { get; } = declarator;

    public override string ToString () => string.Join (", ", Specifiers);
}
