namespace CLanguage.Syntax;

public class TypeSpecifier (TypeSpecifierKind kind, string name, Block? body = null)
{
    public TypeSpecifierKind Kind { get; private set; } = kind;
    public string Name { get; private set; } = name;
    public Block? Body { get; private set; } = body;

    public override string ToString () => Name;
}

public enum TypeSpecifierKind : int
{
    Builtin,
    Typename,
    Struct,
    Class,
    Union,
    Enum
}
