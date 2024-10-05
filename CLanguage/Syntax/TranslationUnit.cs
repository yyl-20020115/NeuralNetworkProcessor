using System;

namespace CLanguage.Syntax;

public class TranslationUnit : Block
{
    public string Name { get; }

    public TranslationUnit (string name)
        : base (Compiler.VariableScope.Global)
    {
        if (string.IsNullOrWhiteSpace (Name = name)) {
            throw new ArgumentException ("Translation unit name must be specified", nameof (name));
        }
    }

    public override string ToString () => Name;
}
