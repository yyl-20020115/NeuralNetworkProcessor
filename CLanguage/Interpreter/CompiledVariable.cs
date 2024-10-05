using System;
using CLanguage.Types;

namespace CLanguage.Interpreter;

public class CompiledVariable (string name, int offset, CType type)
{
    public string Name { get; } = name ?? throw new ArgumentNullException (nameof (name));
    public CType VariableType { get; } = type ?? throw new ArgumentNullException (nameof (type));

    public int StackOffset { get; set; } = offset;
    public Value[]? InitialValue { get; set; }

    public override string ToString () => $"{VariableType} {Name}";
}
