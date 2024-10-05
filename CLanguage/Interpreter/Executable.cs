using System.Collections.Generic;
using System.Linq;
using CLanguage.Types;
using System.Text;

namespace CLanguage.Interpreter;

public class Executable
{
    public MachineInfo MachineInfo { get; private set; }

    public List<BaseFunction> Functions { get; private set; }

    readonly List<CompiledVariable> globals = [];
    public IReadOnlyList<CompiledVariable> Globals => globals;

    public Executable (MachineInfo machineInfo)
    {
        MachineInfo = machineInfo;
        Functions = [.. machineInfo.InternalFunctions.Cast<BaseFunction> ()];
    }

    public CompiledVariable AddGlobal (string name, CType type)
    {
        var last = Globals.LastOrDefault ();
        var offset = last == null ? 0 : last.StackOffset + last.VariableType.NumValues;
        var v = new CompiledVariable (name, offset, type);
        globals.Add (v);
        return v;
    }

    public Value GetConstantMemory (string stringConstant)
    {
        var index = Globals.Count;
        var bytes = Encoding.UTF8.GetBytes (stringConstant);
        var len = bytes.Length + 1;
        var type = new CArrayType (CBasicType.SignedChar, len);
        var v = AddGlobal ("__c" + Globals.Count, type);
        v.InitialValue = bytes.Concat (new byte[] { 0 }).Select (x => (Value)x).ToArray ();
        return Value.Pointer (v.StackOffset);
    }
}

