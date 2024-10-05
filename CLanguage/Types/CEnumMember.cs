using System;

namespace CLanguage.Types;

public class CEnumMember (string name, int value)
{
    public string Name { get; } = name ?? throw new ArgumentNullException (nameof (name));
    public int Value { get; } = value;

    public override string ToString () => $"{Name} = {Value}";
}
