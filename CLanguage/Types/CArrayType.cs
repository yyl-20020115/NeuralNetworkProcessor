using CLanguage.Compiler;

namespace CLanguage.Types;

public class CArrayType (CType elementType, int? length) : CType
{
    public CType ElementType { get; } = elementType;
    public int? Length { get; } = length;

    public override int NumValues {
        get {
            if (Length == null)
                return 1;
            var innerSize = ElementType.NumValues;
            return Length.Value * innerSize;
        }
    }

    protected override CPointerType CreatePointerType () => ElementType.Pointer;

    public override int GetByteSize (EmitContext c)
    {
        if (Length == null)
            return c.MachineInfo.PointerSize;
        var innerSize = ElementType.GetByteSize (c);
        return Length.Value * innerSize;
    }

    public override int ScoreCastTo (CType otherType) => Equals (otherType)
            ? 1000
            : otherType is CPointerType pt ? ElementType.Equals (pt.InnerType) ? 900 : ElementType.ScoreCastTo (pt.InnerType) / 2 : 0;

    public override bool Equals (object? obj) =>obj is CArrayType a && Length == a.Length && ElementType.Equals (a.ElementType);

    public override int GetHashCode ()
    {
        var hash = 17;
        hash = hash * 37 + ElementType.GetHashCode ();
        hash = hash * 37 + Length.GetHashCode ();
        return hash;
    }

    public override string ToString () => $"{ElementType}[{Length}]";
}
