namespace Som.VMObject;
using Som.VM;
using System.Numerics;

public abstract class SNumber : SAbstractObject
{
    public abstract SString PrimAsString(Universe universe);

    public abstract SNumber PrimAsDouble(Universe universe);

    public abstract SNumber PrimSqrt(Universe universe);

    public abstract SNumber PrimAdd(SNumber right, Universe universe);

    public abstract SNumber PrimSubtract(SNumber right, Universe universe);

    public abstract SNumber PrimMultiply(SNumber right, Universe universe);

    public abstract SNumber PrimDoubleDivide(SNumber right, Universe universe);

    public abstract SNumber PrimIntegerDivide(SNumber right, Universe universe);

    public abstract SNumber PrimModulo(SNumber right, Universe universe);

    public abstract SNumber PrimBitAnd(SNumber right, Universe universe);

    public abstract SNumber PrimBitXor(SNumber right, Universe universe);

    public abstract SNumber PrimLeftShift(SNumber right, Universe universe);

    public abstract SObject PrimEqual(SAbstractObject right, Universe universe);

    public abstract SObject PrimLessThan(SNumber right, Universe universe);

    protected static SNumber IntOrBigInt(double value, Universe universe) => value > long.MaxValue || value < long.MinValue
            ? universe.NewBigInteger(new BigInteger(Math.Round(value)))
            : universe.NewInteger((long)Math.Round(value));

    protected static SObject AsSbool(bool result, Universe universe) => result ? universe.trueObject : universe.falseObject;

}
