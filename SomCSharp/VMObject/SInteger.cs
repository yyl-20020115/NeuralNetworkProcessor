/**
 * Copyright (c) 2016 Michael Haupt, github@haupz.de
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

public class SInteger : SNumber
{
    /**
     * Language convention requires integers up to this value to be identical.
     */

    /**
     * Cache to store integers up to {@link #MAX_IDENTICAL_INT}.
     */
    private static readonly Dictionary<long, SInteger> CACHE = new();

    // Private variable holding the embedded integer
    private readonly long embeddedInteger;

    private SInteger(long value) => embeddedInteger = value;

    public static SInteger GetInteger(long value) 
        => value > int.MaxValue ? new SInteger(value) : !CACHE.ContainsKey(value) ? (CACHE[value] = new SInteger(value)) : CACHE[value];

    public long EmbeddedInteger => embeddedInteger;
    // Get the embedded integer

    public override string ToString() => base.ToString() + "(" + embeddedInteger + ")";

    public override SClass GetSOMClass(Universe universe) => universe.integerClass;

    public override SString PrimAsString(Universe universe) => universe.NewString(embeddedInteger.ToString());

    public override SNumber PrimAsDouble(Universe universe) => universe.NewDouble(embeddedInteger);

    public override SNumber PrimSqrt(Universe universe)
    {
        var result = Math.Sqrt(embeddedInteger);

        return result == Math.Round(result) ? IntOrBigInt(result, universe) : universe.NewDouble(result);
    }

    public override SNumber PrimAdd(SNumber right, Universe universe)
    {
        if (right is SBigInteger s)
        {
            return universe.NewBigInteger(new BigInteger(embeddedInteger) + (
                s.EmbeddedBiginteger));
        }
        else if (right is SDouble d)
        {
            return universe.NewDouble(embeddedInteger + d.EmbeddedDouble);
        }
        else if(right is SInteger r)
        {
            if(MathUtils.AddExact(embeddedInteger,r.embeddedInteger,out var result))
            {
                return universe.NewInteger(result);
            }
            else
            {
                return universe.NewBigInteger(new BigInteger(embeddedInteger) + (
                    new BigInteger(r.EmbeddedInteger)));
            }
        }
        else
            throw new InvalidOperationException();
    }

    public override SNumber PrimSubtract(SNumber right, Universe universe)
    {
        if (right is SBigInteger s)
        {
            return universe.NewBigInteger(new BigInteger(embeddedInteger) - (
                s.EmbeddedBiginteger));
        }
        else if (right is SDouble d)
        {
            return universe.NewDouble(embeddedInteger - d.EmbeddedDouble);
        }
        else if (right is SInteger r)
        {
            if(MathUtils.SubtractExact(embeddedInteger,r.EmbeddedInteger, out var result))
            {
                return universe.NewInteger(result);
            }
            else
            {
                return universe.NewBigInteger(new BigInteger(embeddedInteger) - (
                    new BigInteger(r.EmbeddedInteger)));
            }
        }
        else
            throw new InvalidOperationException();
    }

    public override SNumber PrimMultiply(SNumber right, Universe universe)
    {
        if (right is SBigInteger s)
        {
            return universe.NewBigInteger(new BigInteger(embeddedInteger) * (
                s.EmbeddedBiginteger));
        }
        else if (right is SDouble d)
        {
            return universe.NewDouble(embeddedInteger * d.EmbeddedDouble);
        }
        else if (right is SInteger r)
        {
            if(MathUtils.MultiplyExact(embeddedInteger,r.EmbeddedInteger, out var result))
            {
                return universe.NewInteger(result);
            }
            else
            {
                return universe.NewBigInteger(new BigInteger(embeddedInteger) * (
                    new BigInteger(r.EmbeddedInteger)));
            }
        }
        else
            throw new InvalidOperationException();
    }


    public override SNumber PrimDoubleDivide(SNumber right, Universe universe)
    {
        var result = 0.0;

        if (right is SBigInteger s) {
            result = embeddedInteger / ((double)s.EmbeddedBiginteger);
        } else if (right is SDouble d) {
            result = embeddedInteger / d.EmbeddedDouble;
        } 
        else if(right is SInteger i)
        {
            result = (double)embeddedInteger / i.EmbeddedInteger;
        }

        return universe.NewDouble(result);
    }

    public override SNumber PrimIntegerDivide(SNumber right, Universe universe) =>
        right is SBigInteger s
            ? (global::Som.VMObject.SNumber)universe.NewBigInteger(new BigInteger(embeddedInteger) / (
                s.EmbeddedBiginteger))
            : (global::Som.VMObject.SNumber)(right is SDouble d
                ? universe.NewInteger((long)(embeddedInteger / d.EmbeddedDouble))
                : universe.NewInteger(embeddedInteger / ((SInteger)right).EmbeddedInteger));

    public override SNumber PrimModulo(SNumber right, Universe universe)
    // Note: modulo semantics of SOM differ from Java, with respect to
    // negative operands, but BigInteger doesn't support a negative
    // second operand, so, we should get an exception, which we can
    // properly handle once an application actually needs it.
    {
        SNumber ret = universe.NewInteger(0);
        if (right is SBigInteger s)
        {
            var vr = embeddedInteger;
            var vl = s.EmbeddedBiginteger;
            var sr = Math.Sign(vr);
            var sl = s.EmbeddedBiginteger.Sign;
            if (sr != sl)
            {
                var rm = new BigInteger(vr) % vl;
                ret = universe.NewBigInteger(BigInteger.Abs(vl + rm) * sl);
            }
            else
            {
                ret = universe.NewBigInteger(new BigInteger(vr) % vl);
            }
        }
        else if(right is SDouble d)
        {
            var vr = embeddedInteger;
            var vl = d.EmbeddedDouble;
            var sr = Math.Sign(vr);
            var sl = Math.Sign(vl);
            if (sr != sl)
            {
                var rm = sr % vl;
                ret = universe.NewDouble(Math.Abs(vl + rm) * sl);
            }
            else
            {
                ret = universe.NewDouble(vr % vl);
            }
        }
        else if(right is SInteger l)
        {
            var vr = embeddedInteger;
            var vl = l.EmbeddedInteger;
            var sr = Math.Sign(vr);
            var sl = Math.Sign(vl);
            
            if (sr != sl)
            {
                //10 % -3 == -2
                //-10 % 3 == 2
                var rm = (long)Math.Floor((double)(vr % vl));
                ret = universe.NewInteger(Math.Abs(vl+rm)*sl);
            }
            else
            {
                ret = universe.NewInteger((long)Math.Floor((double)(vr % vl)));
            }
        }
        return ret;
    }

    public SInteger PrimRemainder(SNumber right, Universe universe) => right is SInteger s ? universe.NewInteger(embeddedInteger % s.embeddedInteger) : throw new InvalidOperationException();
    public override SNumber PrimBitAnd(SNumber right, Universe universe) => right is SBigInteger s
            ? universe.NewBigInteger(new BigInteger(embeddedInteger) & (
                s.EmbeddedBiginteger))
            : (SNumber)universe.NewInteger(embeddedInteger & ((SInteger)right).embeddedInteger);

    public override SNumber PrimBitXor(SNumber right, Universe universe) => right is SBigInteger s
            ? universe.NewBigInteger(new BigInteger(embeddedInteger) ^ (
                s.EmbeddedBiginteger))
            : (SNumber)universe.NewInteger(embeddedInteger ^ ((SInteger)right).embeddedInteger);
    public static int LeadingZeros(long value)
    {
        // Shift right unsigned to work with both positive and negative values
        var uValue = (ulong)value;
        int leadingZeros = 0;
        while (uValue != 0)
        {
            uValue >>= 1;
            leadingZeros++;
        }

        return (64 - leadingZeros);
    }
    public static int LeadingZeros(int value)
    {
        // Shift right unsigned to work with both positive and negative values
        var uValue = (uint)value;
        int leadingZeros = 0;
        while (uValue != 0)
        {
            uValue >>= 1;
            leadingZeros++;
        }

        return (32 - leadingZeros);
    }

    public override SNumber PrimLeftShift(SNumber right, Universe universe)
    {
        var r = ((SInteger)right).embeddedInteger;
        //assert r > 0;
        return sizeof(long) * 8 - LeadingZeros(embeddedInteger) + r > sizeof(long) * 8 - 1
            ? universe.NewBigInteger(new BigInteger(embeddedInteger) << ((int)r))
            : universe.NewInteger((embeddedInteger << (int)r));
    }

    public override SObject PrimEqual(SAbstractObject right, Universe universe)
    {
        var result = right is SBigInteger s
            ? new BigInteger(embeddedInteger).Equals(
                s.EmbeddedBiginteger)
            : right is SDouble d
                ? embeddedInteger == d.EmbeddedDouble
                : right is SInteger i && embeddedInteger == i.EmbeddedInteger;
        return AsSbool(result, universe);
    }

    public override SObject PrimLessThan(SNumber right, Universe universe)
    {
        var result = right is SBigInteger s
            ? new BigInteger(embeddedInteger).CompareTo(
                s.EmbeddedBiginteger) < 0
            : right is SDouble d ? embeddedInteger < d.EmbeddedDouble : embeddedInteger < ((SInteger)right).EmbeddedInteger;
        return AsSbool(result, universe);
    }
}
