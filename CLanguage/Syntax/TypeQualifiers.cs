using System;

namespace CLanguage.Syntax;

[Flags]
public enum TypeQualifiers : int
{
    None = 0,
    Const = 1,
    Restrict = 2,
    Volatile = 4,
}
