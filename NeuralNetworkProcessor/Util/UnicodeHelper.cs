using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Utilities;

namespace NeuralNetworkProcessor.Util;
public record struct CharRange(int First = -1, int Last = -1)
{
    public readonly bool InRange(int ch) => ch >= this.First && ch <= this.Last;
    public override readonly string ToString() => $"({this.First:X8}-{this.Last:X8})";
}

public static class UnicodeHelper
{
    public static string TryEnclose(string text) =>
        text.Length >= 2 && text[0] == '"' && text[^1] == '"'
    ?   text : "\"" + text + "\"";
    public static string TryDeclose(string text) =>
        text.Length >= 2 && ((text[0] == '"' && text[^1] == '"')
        || (text[0] == '\'' && text[^1] == '\'')) ? text[1..^1] : text;
    public static bool TryDecloseText(string text, out string result)
    {
        if(text.Length >= 2 && ((text[0] == '"' && text[^1] == '"')
        || (text[0] == '\'' && text[^1] == '\'')))
        {
            result = text[1..^1];
            return true;
        }
        else
        {
            result = text;
            return false;
        }
    }
    public static bool TryParseUnicode(string text,out int u)
    {
        u = -1;
        return text.StartsWith("\\u", System.StringComparison.CurrentCultureIgnoreCase) && int.TryParse(text[2..], NumberStyles.HexNumber, null, out u);
    }
    /// <summary>
    /// \uXXXXXXXX-\uXXXXXXXX
    /// </summary>
    /// <param name="text"></param>
    /// <param name="range"></param>
    /// <returns></returns>
    public static bool TryDecloseRange(string text, out CharRange? range)
    {
        if (text.Length >= 2 && ((text[0] == '"' && text[^1] == '"')
        || (text[0] == '\'' && text[^1] == '\'')))
        {
            text = text[1..^1];
            var i = text.IndexOf('-');
            if (i >= 0)
            {
                if(TryParseUnicode(text[..i],out var first) && TryParseUnicode(text[(i + 1)..],out var last))
                {
                    range= new (first, last);
                    return true;
                }                
            }
        }
        range = new();
        return false;
    }
    /// <summary>
    /// \uXXXXXXXX
    /// </summary>
    /// <param name="text"></param>
    /// <param name="u"></param>
    /// <returns></returns>
    public static bool TryDecloseUnicode(string text, out int _char)
    {
        _char = -1;
        return text.Length >= 2 && ((text[0] == '"' && text[^1] == '"')
        || (text[0] == '\'' && text[^1] == '\'')) && TryParseUnicode(text[1..^1], out _char);
    }

    public static IEnumerable<(UnicodeClass u, int c, int len)> NextPoint(string Text)
    {
        var text = string.Empty;
        var builder = new StringBuilder();
        for (int i = 0, len = Text.Length; i < len; i++)
        {
            if (Text[i] == '&' && (i + 2) < Text.Length
                && Text[i + 1] == '#')
            {
                for (i += 2; i < len; i++)
                    if (Text[i] == ';')
                    { i++; break; }
                    else
                    { builder.Append(Text[i]); }

                if ((text = builder.ToString().Trim()).Length > 0)
                {
                    if ((text[0] is 'x' or 'X')
                        ? int.TryParse(text[1..], NumberStyles.AllowHexSpecifier, null, out var val)
                        : int.TryParse(text, out val))
                        yield return (UnicodeClass.Unknown, val, text.Length + 3);
                    else if (text.Length >= 2 && text[0] == '+')
                    {
                        var uc = UnicodeClassTools.GetClassByShortName(text[1..^0]);
                        if (uc != UnicodeClass.Unknown)
                            yield return (uc, 0, text.Length + 3);
                        else
                            //NOTICE: should not be here
                            foreach (var c in text)
                                yield return (UnicodeClass.Unknown, c, 1);
                    }
                }
                builder.Clear();
            }
            else
            {
                yield return (UnicodeClass.Unknown, Text[i], 1);
            }
        }
    }
}
