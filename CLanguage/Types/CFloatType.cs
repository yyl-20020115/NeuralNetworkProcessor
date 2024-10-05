using CLanguage.Compiler;

namespace CLanguage.Types;

public class CFloatType (string name, int bits) : CBasicType(name, Signedness.Signed, "")
{
    public int Bits { get; } = bits;

    public override int NumValues => 1;

    public override int GetByteSize (EmitContext c) => Bits / 8;

    public override int ScoreCastTo (CType otherType) => Equals (otherType)
            ? 1000
            : otherType switch {
                CFloatType => 900,
                _ => 0
            };

}
