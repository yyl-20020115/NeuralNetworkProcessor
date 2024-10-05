using CLanguage.Syntax;
using System.Collections.Generic;

namespace CLanguage.Parser;

public class ParserInput (Token[] tokens) : yyParser.yyInput
{
    public readonly Token[] Tokens = tokens;
    int index = -1;
    readonly HashSet<string> typedefs = [];

    public bool advance ()
    {
        if (index + 1 < Tokens.Length) {
            index++;
            return true;
        }
        return false;
    }

    public int token () => CurrentToken.Kind;

    public object value () => CurrentToken.Value ?? "";

    public Token CurrentToken => Tokens[index].Kind == TokenKind.IDENTIFIER && typedefs.Contains(Tokens[index].StringValue!) ?
        Tokens[index].AsKind (TokenKind.TYPE_NAME) :
        Tokens[index];

    public void AddTypedef (string declaredIdentifier)
    {
        typedefs.Add (declaredIdentifier);
    }
}
