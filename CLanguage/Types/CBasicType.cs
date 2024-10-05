using CLanguage.Compiler;

namespace CLanguage.Types;

public abstract class CBasicType (string name, Signedness signedness, string size) : CType
{
    public string Name { get; private set; } = name;
    public Signedness Signedness { get; private set; } = signedness;
    public string Size { get; private set; } = size;

    public override bool Equals (object? obj) 
        => obj is CBasicType o && (Name == o.Name) && (Signedness == o.Signedness) && (Size == o.Size);

    public override int GetHashCode()
    {
        var hash = 17;
        hash = hash * 37 + Name.GetHashCode ();
        hash = hash * 37 + Size.GetHashCode ();
        hash = hash * 37 + Signedness.GetHashCode ();
        return hash;
    }

    public static readonly CIntType ConstChar = new ("char", Signedness.Signed, "") { TypeQualifiers = CLanguage.Syntax.TypeQualifiers.Const };
    public static readonly CIntType UnsignedChar = new ("char", Signedness.Unsigned, "");
    public static readonly CIntType SignedChar = new ("char", Signedness.Signed, "");
    public static readonly CIntType UnsignedShortInt = new ("int", Signedness.Unsigned, "short");
    public static readonly CIntType SignedShortInt = new ("int", Signedness.Signed, "short");
    public static readonly CIntType UnsignedInt = new ("int", Signedness.Unsigned, "");
    public static readonly CIntType SignedInt = new ("int", Signedness.Signed, "");
    public static readonly CIntType UnsignedLongInt = new ("int", Signedness.Unsigned, "long");
    public static readonly CIntType SignedLongInt = new ("int", Signedness.Signed, "long");
    public static readonly CIntType UnsignedLongLongInt = new ("int", Signedness.Unsigned, "long long");
    public static readonly CIntType SignedLongLongInt = new ("int", Signedness.Signed, "long long");
    public static readonly CFloatType Float = new ("float", 32);
    public static readonly CFloatType Double = new ("double", 64);
    public static readonly CBoolType Bool = new ();

    /// <summary>
    /// Section 6.3.1.1 (page 51) of N1570
    /// </summary>
    /// <returns>
    /// The promototed integer.
    /// </returns>
    /// <param name='context'>
    /// Context.
    /// </param>
    public CBasicType IntegerPromote(EmitContext context)
    {
        if (IsIntegral)
        {
            var size = GetByteSize(context);
            var intSize = context.MachineInfo.IntSize;
            return size < intSize ? SignedInt : size == intSize ? Signedness == Signedness.Unsigned ? UnsignedInt : (CBasicType)SignedInt : this;
        }
        else
        {
            return this;
        }
    }

    /// <summary>
    /// Section 6.3.1.8 (page 53) of N1570
    /// </summary>
    /// <returns>
    /// The converted type.
    /// </returns>
    /// <param name='otherType'>
    /// The other type participating in the arithmetic.
    /// </param>
    /// <param name='context'>
    /// Context.
    /// </param>
    public CBasicType ArithmeticConvert(CType otherType, EmitContext context)
    {
        if (otherType is not CBasicType otherBasicType) {
            context.Report.Error (19, "Cannot perform arithmetic with " + otherType);
            return CBasicType.SignedInt;
        }
        if (Name == "double" || otherBasicType.Name == "double")
        {
            return Double;
        }
        else if (Name == "single" || otherBasicType.Name == "single")
        {
            return Float;
        }
        else
        {

            var p1 = IntegerPromote(context);
            var size1 = p1.GetByteSize(context);

            var p2 = otherBasicType.IntegerPromote(context);
            var size2 = p2.GetByteSize(context);

            return p1.Signedness == p2.Signedness
                ? size1 >= size2 ? p1 : p2
                : p1.Signedness == Signedness.Unsigned
                    ? size1 > size2 ? p1 : size2 > size1 ? p2 : new CIntType(p2.Name, Signedness.Unsigned, p2.Size)
                    : size2 > size1 ? p2 : size1 > size2 ? p1 : new CIntType(p1.Name, Signedness.Unsigned, p1.Size);
        }
    }

    bool HasRankGreaterThan (CBasicType otherBasicType, EmitContext context) => false;

    //public override int ScoreCastTo (CType otherType)
    //{
    //    return 0;
    //}

    public override string ToString()
    {
        if (IsIntegral)
        {
            var sign = Signedness == Signedness.Signed ? "signed" : "unsigned";

            return string.IsNullOrEmpty(Size) ? sign + " " + Name : sign + " " + Size + " " + Name;

        }
        else
        {
            return string.IsNullOrEmpty(Size) ? Name : Size + " " + Name;
        }
    }
}

public enum Signedness : int
{
    Unsigned = 0,
    Signed = 1,
}
