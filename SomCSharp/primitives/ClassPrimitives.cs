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

public class ClassPrimitives : Primitives
{
    public ClassPrimitives(Universe universe) : base(universe) { }
    public class NewPrimitive : SPrimitive
    {
        public NewPrimitive(Universe universe)
            : base("new", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var self = (SClass)frame.Pop();
            frame.Push(universe.NewInstance(self));
        }
    }
    public class NamePrimitive : SPrimitive
    {
        public NamePrimitive(Universe universe)
            : base("name", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var self = (SClass)frame.Pop();
            frame.Push(self.Name);
        }
    }
    public class SuperClassPrimitive : SPrimitive
    {
        public SuperClassPrimitive(Universe universe)
            : base("superclass", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var self = (SClass)frame.Pop();
            frame.Push(self.SuperClass);
        }
    }
    public class FieldsPrimitive : SPrimitive
    {
        public FieldsPrimitive(Universe universe)
            : base("fields", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var self = (SClass)frame.Pop();
            frame.Push(self.InstanceFields);
        }
    }
    public class MethodsPrimitive : SPrimitive
    {
        public MethodsPrimitive(Universe universe)
            : base("methods", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var self = (SClass)frame.Pop();
            frame.Push(self.InstanceInvokables);
        }
    }

    public override void InstallPrimitives()
    {
        this.InstallInstancePrimitive(new NewPrimitive(universe));
        this.InstallInstancePrimitive(new NamePrimitive(universe));
        this.InstallInstancePrimitive(new SuperClassPrimitive(universe));
        this.InstallInstancePrimitive(new FieldsPrimitive(universe));
        this.InstallInstancePrimitive(new MethodsPrimitive(universe));
    }
}
