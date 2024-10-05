using System;
using CLanguage.Compiler;

namespace CLanguage.Types;

public class CIntType (string name, Signedness signedness, string size) : CBasicType(name, signedness, size)
{
    public override bool IsIntegral => true;

    public override int NumValues => 1;

    public int GetByteSize (MachineInfo c) => Name == "char"
            ? c.CharSize
            : Name == "int"
            ? Size == "short" ? c.ShortIntSize : Size == "long" ? c.LongIntSize : Size == "long long" ? c.LongLongIntSize : c.IntSize
            : throw new NotSupportedException (this.ToString ());

    public override int GetByteSize (EmitContext c) => GetByteSize (c.MachineInfo);

    public override int ScoreCastTo (CType otherType) => Equals (otherType)
            ? 1000
            : otherType is CIntType it
            ? Size == it.Size ? 900 : 800
            : otherType is CFloatType ft ? ft.Bits == 64 ? 400 : 300 : otherType is CBoolType bt ? 200 : 0;

    public override object GetClrValue (Value[] values, MachineInfo machineInfo)
    {
        var byteSize = GetByteSize (machineInfo);
        return Signedness switch {
            Signedness.Signed => byteSize switch {
                1 => values[0].Int8Value,
                2 => values[0].Int16Value,
                4 => values[0].Int32Value,
                _ => (object)values[0].Int64Value,
            },
            _ => byteSize switch {
                1 => values[0].UInt8Value,
                2 => values[0].UInt16Value,
                4 => values[0].UInt32Value,
                _ => (object)values[0].UInt64Value,
            },
        };
    }
}
