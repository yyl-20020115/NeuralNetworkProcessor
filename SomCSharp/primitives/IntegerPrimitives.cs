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

namespace Som.Primitives;
using Som.Interpreter;
using Som.VM;
using Som.VMObject;
using System.Numerics;

public class IntegerPrimitives : Primitives
{
    public IntegerPrimitives(Universe universe) : base(universe) {}

    public class AsStringPrimitive : SPrimitive
    {
        public AsStringPrimitive(Universe universe)
            : base("asString", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var self = (SNumber)frame.Pop();
            frame.Push(self.PrimAsString(universe));
        }
    }
    public class AsDoublePrimitive : SPrimitive
    {
        public AsDoublePrimitive(Universe universe)
            : base("asDouble", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var self = (SNumber)frame.Pop();
            frame.Push(self.PrimAsDouble(universe));
        }
    }
    public class AtRandomPrimitive : SPrimitive
    {
        public AtRandomPrimitive(Universe universe)
            : base("atRandom", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var self = (SInteger)frame.Pop();
            frame.Push(universe.NewInteger(
                (long)(self.EmbeddedInteger * Random.Shared.NextDouble())));
        }
    }
    public class SqrtPrimitive : SPrimitive
    {
        public SqrtPrimitive(Universe universe)
            : base("sqrt", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var self = (SInteger)frame.Pop();
            frame.Push(self.PrimSqrt(universe));
        }
    }
    public class AddPrimitive : SPrimitive
    {
        public AddPrimitive(Universe universe)
            : base("+", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var right = (SNumber)frame.Pop();
            var left = (SNumber)frame.Pop();
            frame.Push(left.PrimAdd(right, universe));
        }
    }
    public class SubPrimitive : SPrimitive
    {
        public SubPrimitive(Universe universe)
            : base("-", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var right = (SNumber)frame.Pop();
            var left = (SNumber)frame.Pop();
            frame.Push(left.PrimSubtract(right, universe));
        }
    }
    public class MulPrimitive : SPrimitive
    {
        public MulPrimitive(Universe universe)
            : base("*", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var right = (SNumber)frame.Pop();
            var left = (SNumber)frame.Pop();
            frame.Push(left.PrimMultiply(right, universe));
        }
    }
    public class DoubleDivPrimitive : SPrimitive
    {
        public DoubleDivPrimitive(Universe universe)
            : base("//", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var right = (SNumber)frame.Pop();
            var left = (SNumber)frame.Pop();
            frame.Push(left.PrimDoubleDivide(right, universe));
        }
    }
    public class FromStringPrimitive : SPrimitive
    {
        public FromStringPrimitive(Universe universe)
            : base("fromString:", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var param = (SString)frame.Pop();
            frame.Pop();

            if (long.TryParse(param.EmbeddedString, out var result))
            {
                frame.Push(universe.NewInteger(result));
            }
            else
            {
                frame.Push(new SBigInteger(BigInteger.Parse(param.EmbeddedString)));
            }
        }
    }
    public class IntegerDivPrimitive : SPrimitive
    {
        public IntegerDivPrimitive(Universe universe)
            : base("/", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var right = (SNumber)frame.Pop();
            var left = (SNumber)frame.Pop();
            frame.Push(left.PrimIntegerDivide(right, universe));
        }
    }
    public class ModuloPrimitive : SPrimitive
    {
        public ModuloPrimitive(Universe universe)
            : base("%", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var right = (SNumber)frame.Pop();
            var left = (SNumber)frame.Pop();
            frame.Push(left.PrimModulo(right, universe));
        }
    }
    public class RemainderPrimitive : SPrimitive
    {
        public RemainderPrimitive(Universe universe)
            : base("rem:", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var right = (SNumber)frame.Pop();
            var left = (SInteger)frame.Pop();
            frame.Push(left.PrimRemainder(right, universe));
        }
    }
    public class PrimBitAndPrimitive : SPrimitive
    {
        public PrimBitAndPrimitive(Universe universe)
            : base("&", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var right = (SNumber)frame.Pop();
            var left = (SNumber)frame.Pop();
            frame.Push(left.PrimBitAnd(right, universe));
        }
    }
    public class PrimEqualPrimitive : SPrimitive
    {
        public PrimEqualPrimitive(Universe universe)
            : base("=", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var right = frame.Pop();
            var left = (SNumber)frame.Pop();
            frame.Push(left.PrimEqual(right, universe));
        }
    }
    public class PrimBitXorPrimitive : SPrimitive
    {
        public PrimBitXorPrimitive(Universe universe)
            : base("bitXor:", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var right = (SNumber)frame.Pop();
            var left = (SNumber)frame.Pop();
            frame.Push(left.PrimBitXor(right, universe));
        }
    }

    public class PrimLessThanPrimitive : SPrimitive
    {
        public PrimLessThanPrimitive(Universe universe)
            : base("<", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var right = (SNumber)frame.Pop();
            var left = (SNumber)frame.Pop();
            frame.Push(left.PrimLessThan(right, universe));
        }
    }
    public class PrimLeftShiftPrimitive : SPrimitive
    {
        public PrimLeftShiftPrimitive(Universe universe)
            : base("<<", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var right = (SNumber)frame.Pop();
            var left = (SNumber)frame.Pop();
            frame.Push(left.PrimLeftShift(right, universe));
        }
    }
    public class PrimRightShiftPrimitive : SPrimitive
    {
        public PrimRightShiftPrimitive(Universe universe)
            : base(">>>", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var right = (SInteger)frame.Pop();
            var rcvr = (SInteger)frame.Pop();
            frame.Push(
                universe.NewInteger(rcvr.EmbeddedInteger >> (int)right.EmbeddedInteger));
        }
    }
    public class As32BitSignedValuePrimitive : SPrimitive
    {
        public As32BitSignedValuePrimitive(Universe universe)
            : base("as32BitSignedValue", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var rcvr = (SNumber)frame.Pop();

            frame.Push(universe.NewInteger(
                rcvr is SInteger si ? (int)(si.EmbeddedInteger&0xFFFFFFFFL) : 
                (int)(((SBigInteger)rcvr).EmbeddedBiginteger&0xFFFFFFFFL))
                );
        }
    }
    public class As32BitUnsignedValuePrimitive : SPrimitive
    {
        public As32BitUnsignedValuePrimitive(Universe universe)
            : base("as32BitUnsignedValue", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var rcvr = (SInteger)frame.Pop();
            frame.Push(universe.NewInteger( 
                (uint)(((ulong)rcvr.EmbeddedInteger)&0xffffffffUL)));
        }
    }

    public override void InstallPrimitives()
    {
        this.InstallInstancePrimitive(new AsStringPrimitive(universe));
        this.InstallInstancePrimitive(new AsDoublePrimitive(universe));
        this.InstallInstancePrimitive(new AtRandomPrimitive(universe));
        this.InstallInstancePrimitive(new SqrtPrimitive(universe));
        this.InstallInstancePrimitive(new AddPrimitive(universe));
        this.InstallInstancePrimitive(new SubPrimitive(universe));
        this.InstallInstancePrimitive(new MulPrimitive(universe));
        this.InstallInstancePrimitive(new DoubleDivPrimitive(universe));
        this.InstallInstancePrimitive(new IntegerDivPrimitive(universe));
        this.InstallInstancePrimitive(new ModuloPrimitive(universe));
        this.InstallInstancePrimitive(new RemainderPrimitive(universe));
        this.InstallInstancePrimitive(new PrimBitAndPrimitive(universe));
        this.InstallInstancePrimitive(new PrimEqualPrimitive(universe));
        this.InstallInstancePrimitive(new PrimBitXorPrimitive(universe));
        this.InstallInstancePrimitive(new PrimLessThanPrimitive(universe));
        this.InstallInstancePrimitive(new PrimLeftShiftPrimitive(universe));
        this.InstallInstancePrimitive(new PrimRightShiftPrimitive(universe));
        this.InstallInstancePrimitive(new As32BitSignedValuePrimitive(universe));
        this.InstallInstancePrimitive(new As32BitUnsignedValuePrimitive(universe));
        this.InstallClassPrimitive(new FromStringPrimitive(universe));
    }
}
