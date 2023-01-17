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

public class SymbolPrimitives : Primitives
{
    public SymbolPrimitives(Universe universe) : base(universe) { }
    public class AsStringPrimitive : SPrimitive
    {
        public AsStringPrimitive(Universe universe)
            : base("asString", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var self = (SSymbol)frame.Pop();
            frame.Push(universe.NewString(self.EmbeddedString));
        }
    }
    public class EqualPrimitive : SPrimitive
    {
        public EqualPrimitive(Universe universe)
            : base("=", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var op1 = frame.Pop();
            var op2 = (SSymbol)frame.Pop(); // self
            frame.Push(op1 == op2 ? universe.trueObject : universe.falseObject);
        }
    }

    public override void InstallPrimitives()
    {
        this.InstallInstancePrimitive(new AsStringPrimitive(universe));
        this.InstallInstancePrimitive(new EqualPrimitive(universe));
    }
}
