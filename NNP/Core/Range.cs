using Utilities;

namespace NNP.Core;

public enum CharRangeType : uint
{
    Unknown = 0,
    UnicodeChar = 1,
    UnicodeRange = 2,
    UnicodeClass = 3
}
public struct CharRangeFilter
{
    public CharRangeType Type;
    public UnicodeClass Class;
    public int StartChar;
    public int EndChar;
    public override readonly string ToString()
        => $"{nameof(CharRangeType)}:{Type},{nameof(UnicodeClass)}:{Class},{nameof(StartChar)}:{StartChar},{nameof(EndChar)}:{EndChar}";
    public readonly bool Hit(int InputChar)
       => UnicodeClassTools.IsValidUnicode(InputChar)
        && (this.IsCharHit(InputChar)
            || this.IsClassHit(InputChar)
            || this.IsRangeHit(InputChar)
            );
    private readonly bool IsCharHit(int InputChar)
        => this.Type == CharRangeType.UnicodeChar
        && InputChar == StartChar
        ;
    private readonly bool IsClassHit(int InputChar)
        => (this.Type == CharRangeType.UnicodeClass)
            && ((this.Class == UnicodeClass.Any)
            || (this.Class == (UnicodeClass)Char.GetUnicodeCategory(
                UnicodeClassTools.ToText(InputChar), 0)))
           ;
    private readonly bool IsRangeHit(int InputChar)
        => this.Type == CharRangeType.UnicodeRange
        && InputChar >= this.StartChar
        && InputChar <= this.EndChar
        ;
}
