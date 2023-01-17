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

public class DoublePrimitives : Primitives
{
    public DoublePrimitives(Universe universe) : base(universe) { }
    public class AsStringPrimitive : SPrimitive
    {
        public AsStringPrimitive(Universe universe)
            : base("asString", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var self = (SDouble)frame.Pop();
            frame.Push(self.PrimAsString(universe));
        }
    }
    public class AsIntegerPrimitive : SPrimitive
    {
        public AsIntegerPrimitive(Universe universe)
            : base("asInteger", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var self = (SDouble)frame.Pop();
            frame.Push(self.PrimAsInteger(universe));
        }
    }
    public class SqrtPrimitive : SPrimitive
    {
        public SqrtPrimitive(Universe universe)
            : base("sqrt", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var self = (SDouble)frame.Pop();
            frame.Push(self.PrimSqrt(universe));
        }
    }
    public class AddPrimitive : SPrimitive
    {
        public AddPrimitive(Universe universe)
            : base("+", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var op1 = (SNumber)frame.Pop();
            var op2 = (SDouble)frame.Pop();
            frame.Push(op2.PrimAdd(op1, universe));
        }
    }
    public class SubPrimitive : SPrimitive
    {
        public SubPrimitive(Universe universe)
            : base("-", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var op1 = (SNumber)frame.Pop();
            var op2 = (SDouble)frame.Pop();
            frame.Push(op2.PrimSubtract(op1, universe));
        }
    }
    public class MulPrimitive : SPrimitive
    {
        public MulPrimitive(Universe universe)
            : base("*", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var op1 = (SNumber)frame.Pop();
            var op2 = (SDouble)frame.Pop();
            frame.Push(op2.PrimMultiply(op1, universe));
        }
    }
    public class DoubleDivPrimitive : SPrimitive
    {
        public DoubleDivPrimitive(Universe universe)
            : base("//", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var op1 = (SNumber)frame.Pop();
            var op2 = (SDouble)frame.Pop();
            frame.Push(op2.PrimDoubleDivide(op1, universe));
        }
    }
    public class FromStringPrimitive : SPrimitive
    {
        public FromStringPrimitive(Universe universe)
            : base("fromString:", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var arg = (SString)frame.Pop();
            frame.Pop();

            if (!double.TryParse(arg.EmbeddedString, out var d)) d = double.NaN;

            frame.Push(universe.NewDouble(d));
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
            var op1 = (SNumber)frame.Pop();
            var op2 = (SDouble)frame.Pop();
            frame.Push(op2.PrimModulo(op1, universe));
        }
    }
    public class RoundPrimitive : SPrimitive
    {
        public RoundPrimitive(Universe universe)
            : base("round", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var rcvr = (SDouble)frame.Pop();
            var result = (long)Math.Round(rcvr.EmbeddedDouble, MidpointRounding.AwayFromZero);
            frame.Push(universe.NewInteger(result));
        }
    }
    public class EqualPrimitive : SPrimitive
    {
        public EqualPrimitive(Universe universe)
            : base("=", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var op1 = frame.Pop();
            var op2 = (SDouble)frame.Pop();
            frame.Push(op2.PrimEqual(op1, universe));
        }
    }
    public class LessThanPrimitive : SPrimitive
    {
        public LessThanPrimitive(Universe universe)
            : base("<", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var op1 = (SNumber)frame.Pop();
            var op2 = (SDouble)frame.Pop();
            frame.Push(op2.PrimLessThan(op1, universe));
        }
    }
    public class SinPrimitive : SPrimitive
    {
        public SinPrimitive(Universe universe)
            : base("sin", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var rcvr = (SDouble)frame.Pop();
            var result = Math.Sin(rcvr.EmbeddedDouble);
            frame.Push(universe.NewDouble(result));
        }
    }
    public class CosPrimitive : SPrimitive
    {
        public CosPrimitive(Universe universe)
            : base("cos", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var rcvr = (SDouble)frame.Pop();
            var result = Math.Cos(rcvr.EmbeddedDouble);
            frame.Push(universe.NewDouble(result));
        }
    }
    public class PositiveInfinityPrimitive : SPrimitive
    {
        public PositiveInfinityPrimitive(Universe universe)
            : base("PositiveInfinity", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            frame.Pop();
            frame.Push(universe.NewDouble(double.PositiveInfinity));
        }
    }
    public override void InstallPrimitives()
    {
        //Console.WriteLine("install pri for double");
        this.InstallInstancePrimitive(new AsStringPrimitive(universe));
        this.InstallInstancePrimitive(new AsIntegerPrimitive(universe));
        this.InstallInstancePrimitive(new SqrtPrimitive(universe));
        this.InstallInstancePrimitive(new AddPrimitive(universe));
        this.InstallInstancePrimitive(new SubPrimitive(universe));
        this.InstallInstancePrimitive(new MulPrimitive(universe));
        this.InstallInstancePrimitive(new DoubleDivPrimitive(universe));
        this.InstallInstancePrimitive(new ModuloPrimitive(universe));
        this.InstallInstancePrimitive(new EqualPrimitive(universe));
        this.InstallInstancePrimitive(new LessThanPrimitive(universe));
        this.InstallInstancePrimitive(new RoundPrimitive(universe));
        this.InstallInstancePrimitive(new SinPrimitive(universe));
        this.InstallInstancePrimitive(new CosPrimitive(universe));
        this.InstallClassPrimitive(new PositiveInfinityPrimitive(universe));
        this.InstallClassPrimitive(new FromStringPrimitive(universe));
    }
}
