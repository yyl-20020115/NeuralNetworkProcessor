using System;

namespace CLanguage.Syntax;

[Flags]
public enum FunctionSpecifier : int
{
    None = 0,
    Inline = 1
}
