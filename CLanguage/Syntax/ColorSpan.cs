namespace CLanguage.Syntax;

public struct ColorSpan
{
    public int Index;
    public int Length;
    public SyntaxColor Color;

    public override readonly string ToString () => Color.ToString ();
}

public enum SyntaxColor : int
{
    Comment,
    Identifier,
    Number,
    String,
    Keyword,
    Operator,
    Function,
    Type,
}
