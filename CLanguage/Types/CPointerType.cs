using CLanguage.Compiler;

namespace CLanguage.Types;

public class CPointerType : CType
{
    public CType InnerType { get; private set; }

    public CPointerType (CType innerType) => InnerType = innerType;

    public static readonly CPointerType PointerToConstChar = new(CBasicType.ConstChar);
    public static readonly CPointerType PointerToVoid = new(CType.Void);

    public override int NumValues => 1;

    public override int GetByteSize (EmitContext c) => c.MachineInfo.PointerSize;

    public override string ToString () => $"{InnerType}*";

    public override bool Equals (object? obj) => obj is CPointerType o && InnerType.Equals (o.InnerType);

    public override int GetHashCode ()
    {
        int hash = 17;
        hash = hash * 37 + InnerType.GetHashCode ();
        hash = hash * 37 + 1;
        return hash;
    }
}
