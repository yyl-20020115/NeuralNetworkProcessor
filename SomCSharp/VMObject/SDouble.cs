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

public class SDouble : SNumber
{
    protected double embeddedDouble;

    public SDouble(double value) => embeddedDouble = value;

    public double EmbeddedDouble =>
        // Get the embedded double
        embeddedDouble;
    public override string ToString() => base.ToString() + "(" + embeddedDouble + ")";

    public override SClass GetSOMClass(Universe universe) => universe.doubleClass;

    private static double CoerceToDouble(SNumber o, Universe universe) => o is SDouble d
            ? d.embeddedDouble
            : o is SInteger i
                ? i.EmbeddedInteger
                : o is SBigInteger s ? (double)s.EmbeddedBiginteger 
                : throw new Exception("Cannot coerce to Double!");

    public override SString PrimAsString(Universe universe) => universe.NewString(embeddedDouble.ToString());

    public override SDouble PrimAsDouble(Universe universe) => this;

    public SInteger PrimAsInteger(Universe universe) => universe.NewInteger((long)embeddedDouble);

    public override SNumber PrimSqrt(Universe universe) => universe.NewDouble(Math.Sqrt(embeddedDouble));

    public override SNumber PrimAdd(SNumber right, Universe universe) => universe.NewDouble(embeddedDouble + CoerceToDouble(right, universe));

    public override SNumber PrimSubtract(SNumber right, Universe universe) => universe.NewDouble(embeddedDouble - CoerceToDouble(right, universe));

    public override SNumber PrimMultiply(SNumber right, Universe universe) => universe.NewDouble(embeddedDouble * CoerceToDouble(right, universe));

    public override SNumber PrimDoubleDivide(SNumber right, Universe universe) => universe.NewDouble(embeddedDouble / CoerceToDouble(right, universe));

    public override SNumber PrimIntegerDivide(SNumber right, Universe universe) => throw new RuntimeException("not yet implemented, SOM doesn't offer it");

    public override SNumber PrimModulo(SNumber right, Universe universe) => universe.NewDouble(embeddedDouble % CoerceToDouble(right, universe));

    public override SNumber PrimBitAnd(SNumber right, Universe universe) => throw new RuntimeException("Not supported for doubles");

    public override SNumber PrimBitXor(SNumber right, Universe universe) => throw new RuntimeException("Not supported for doubles");

    public override SNumber PrimLeftShift(SNumber right, Universe universe) => throw new RuntimeException("Not supported for doubles");

    public override SObject PrimEqual(SAbstractObject right, Universe universe) => right is not SNumber ? universe.falseObject : AsSbool(embeddedDouble == CoerceToDouble((SNumber)right, universe), universe);

    public override SObject PrimLessThan(SNumber right, Universe universe) => AsSbool(embeddedDouble < CoerceToDouble(right, universe), universe);
}
