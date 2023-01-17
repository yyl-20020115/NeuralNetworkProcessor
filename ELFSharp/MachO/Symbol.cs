using System;
using System.Diagnostics;

namespace ELFSharp.MachO;
[DebuggerDisplay("Symbol({Name,nq},{Value})")]
public struct Symbol
{
    public string Name { get; private set; }
    public Int64 Value { get; private set; }
    public Symbol(string name, long value) : this()
    {
        Name = name;
        Value = value;
    }
}
