
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
namespace Som.Primitives;
using Som.Interpreter;
using Som.VM;
using Som.VMObject;

public class ObjectPrimitives : Primitives
{

    public ObjectPrimitives(Universe universe) : base(universe)
    {
    }
    public class EqualPrimitive : SPrimitive
    {
        public EqualPrimitive(Universe universe)
            : base("==", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var op1 = frame.Pop();
            var op2 = frame.Pop();
            frame.Push(op1 == op2 ? universe.trueObject : universe.falseObject);
        }
    }
    public class HashCodePrimitive : SPrimitive
    {
        public HashCodePrimitive(Universe universe)
            : base("hashcode", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var self = frame.Pop();
            frame.Push(universe.NewInteger(self.GetHashCode()));
        }
    }
    public class ObjectSizePrimitive : SPrimitive
    {
        public ObjectSizePrimitive(Universe universe)
            : base("objectSize", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var self = frame.Pop();

            // each object holds its class as an implicit member that contributes to its size
            int size = 1;
            if (self is SArray a)
            {
                size += a.NumberOfIndexableFields;
            }
            else if (self is SObject s)
            {
                size += s.NumberOfFields;
            }
            frame.Push(universe.NewInteger(size));
        }
    }

    public class PerformPrimitive : SPrimitive
    {
        public PerformPrimitive(Universe universe)
            : base("perform:", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var arg = frame.Pop();
            var self = frame.GetStackElement(0);
            var selector = (SSymbol)arg;

            var invokable = self.GetSOMClass(universe).LookupInvokable(selector);
            invokable.Invoke(frame, interpreter);
        }
    }
    public class ClassPrimitive : SPrimitive
    {
        public ClassPrimitive(Universe universe)
            : base("class", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var self = frame.Pop();
            frame.Push(self.GetSOMClass(universe));
        }
    }
    public class HaltPrimitive : SPrimitive
    {
        public HaltPrimitive(Universe universe)
            : base("halt", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            Universe.ErrorPrintln("BREAKPOINT");
        }
    }

    public class PerformInSuperClassPrimitive : SPrimitive
    {
        public PerformInSuperClassPrimitive(Universe universe)
            : base("perform:inSuperclass:", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var arg2 = frame.Pop();
            var arg = frame.Pop();
            // Object self = frame.getStackElement(0);

            var selector = (SSymbol)arg;
            var clazz = (SClass)arg2;

            var invokable = clazz.LookupInvokable(selector);
            invokable.Invoke(frame, interpreter);
        }
    }

    public class PerformWithArgumentsPrimitive : SPrimitive
    {
        public PerformWithArgumentsPrimitive(Universe universe)
            : base("perform:withArguments:", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var arg2 = frame.Pop();
            var arg = frame.Pop();
            var self = frame.GetStackElement(0);

            var selector = (SSymbol)arg;
            var args = (SArray)arg2;

            for (int i = 0; i < args.NumberOfIndexableFields; i++)
            {
                frame.Push(args.GetIndexableField(i));
            }

            var invokable = self.GetSOMClass(universe).LookupInvokable(selector);
            invokable.Invoke(frame, interpreter);
        }
    }

    public class InstVarAtPrimitive : SPrimitive
    {
        public InstVarAtPrimitive(Universe universe)
            : base("instVarAt:", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var arg = frame.Pop();
            var self = (SObject)frame.Pop();
            var idx = (SInteger)arg;

            frame.Push(self.GetField(idx.EmbeddedInteger - 1));
        }
    }

    public class InstVarAtPutPrimitive : SPrimitive
    {
        public InstVarAtPutPrimitive(Universe universe)
            : base("instVarAt:put:", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var val = frame.Pop();
            var arg = frame.Pop();
            var self = (SObject)frame.GetStackElement(0);

            var idx = (SInteger)arg;

            self.SetField(idx.EmbeddedInteger - 1, val);
        }
    }

    public override void InstallPrimitives()
    {
        this.InstallInstancePrimitive(new EqualPrimitive(universe));
        this.InstallInstancePrimitive(new HashCodePrimitive(universe));
        this.InstallInstancePrimitive(new ObjectSizePrimitive(universe));
        this.InstallInstancePrimitive(new PerformPrimitive(universe));
        this.InstallInstancePrimitive(new ClassPrimitive(universe));
        this.InstallInstancePrimitive(new HaltPrimitive(universe));
        this.InstallInstancePrimitive(new PerformInSuperClassPrimitive(universe));
        this.InstallInstancePrimitive(new PerformWithArgumentsPrimitive(universe));
        this.InstallInstancePrimitive(new InstVarAtPrimitive(universe));
        this.InstallInstancePrimitive(new InstVarAtPutPrimitive(universe));
    }
}
