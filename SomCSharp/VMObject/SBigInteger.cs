/**
 * Copyright (c) 2009 Michael Haupt, michael.haupt@hpi.uni-potsdam.de
 * Software Architecture Group, Hasso Plattner Institute, Potsdam, Germany
 * http://www.hpi.uni-potsdam.de/swa/
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

namespace Som.VMObject;
using Som.VM;
using System.Numerics;

public class SBigInteger : SNumber
{
    // Private variable holding the embedded big integer
    protected BigInteger embeddedBiginteger;

    public SBigInteger(BigInteger value) => embeddedBiginteger = value;

    public BigInteger EmbeddedBiginteger => embeddedBiginteger;
    // Get the embedded big integer

    public override string ToString() => base.ToString() + "(" + embeddedBiginteger + ")";

    public override SClass GetSOMClass(Universe universe) => universe.integerClass;

    public override SString PrimAsString(Universe universe) => universe.NewString(embeddedBiginteger.ToString());

    public override SNumber PrimAsDouble(Universe universe) => universe.NewDouble(((double)embeddedBiginteger));

    private static SNumber AsNumber(BigInteger result, Universe universe) => result.GetBitLength() >= sizeof(long) ? universe.NewBigInteger(result) : universe.NewInteger(((long)result));

    private static BigInteger AsBigInteger(SNumber right) => right is SInteger si ? new BigInteger(si.EmbeddedInteger) : ((SBigInteger)right).embeddedBiginteger;

    public override SNumber PrimAdd(SNumber right, Universe universe) => right is SDouble d
            ? universe.NewDouble(
                ((double)embeddedBiginteger) + d.EmbeddedDouble)
            : AsNumber(embeddedBiginteger + AsBigInteger(right), universe);


    public override SNumber PrimSubtract(SNumber right, Universe universe) => right is SDouble d
            ? universe.NewDouble(
                ((double)embeddedBiginteger) - d.EmbeddedDouble)
            : AsNumber(embeddedBiginteger - AsBigInteger(right), universe);

    public override SNumber PrimMultiply(SNumber right, Universe universe) => right is SDouble @double
            ? universe.NewDouble(
                ((double)embeddedBiginteger) * @double.EmbeddedDouble)
            : AsNumber(embeddedBiginteger * AsBigInteger(right), universe);

    public override SNumber PrimDoubleDivide(SNumber right, Universe universe)
    {
        var r = right is SInteger integer ? integer.EmbeddedInteger : (double)((SBigInteger)right).embeddedBiginteger;
        return universe.NewDouble(((double)embeddedBiginteger) / r);
    }

    public override SNumber PrimIntegerDivide(SNumber right, Universe universe) => AsNumber(embeddedBiginteger / AsBigInteger(right), universe);

    public override SNumber PrimModulo(SNumber right, Universe universe) => AsNumber(embeddedBiginteger % AsBigInteger(right), universe);

    public override SNumber PrimSqrt(Universe universe)
    {
        var result = Math.Sqrt((double)embeddedBiginteger);
        return result == Math.Round(result) ? IntOrBigInt(result, universe) : universe.NewDouble(result);
    }
    public override SNumber PrimBitAnd(SNumber right, Universe universe) => AsNumber(embeddedBiginteger & AsBigInteger(right), universe);

    public override SObject PrimEqual(SAbstractObject right, Universe universe) => right is not SNumber
            ? universe.falseObject
            : embeddedBiginteger.CompareTo(AsBigInteger((SNumber)right)) == 0 ? universe.trueObject : universe.falseObject;

    public override SObject PrimLessThan(SNumber right, Universe universe) => embeddedBiginteger.CompareTo(AsBigInteger(right)) < 0 ? universe.trueObject : universe.falseObject;

    public override SNumber PrimLeftShift(SNumber right, Universe universe) => universe.NewBigInteger(embeddedBiginteger >> ((int)AsBigInteger(right)));

    public override SNumber PrimBitXor(SNumber right, Universe universe) => AsNumber(embeddedBiginteger ^ (AsBigInteger(right)), universe);
}
