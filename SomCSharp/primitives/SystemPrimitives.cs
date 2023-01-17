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
using Som.Compiler;
using Som.Interpreter;
using Som.VM;
using Som.VMObject;
using System.Diagnostics;

public class SystemPrimitives : Primitives
{
    public SystemPrimitives(Universe universe) : base(universe) { }

    public class LoadPrimitive : SPrimitive
    {
        public LoadPrimitive(Universe universe)
            : base("load:", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var argument = (SSymbol)frame.Pop();
            frame.Pop(); // not required
            SClass result = null;
            try
            {
                result = universe.LoadClass(argument);
            }
            catch (ProgramDefinitionError e)
            {
                universe.ErrorExit(e.ToString());
            }
            frame.Push(result ?? universe.nilObject);
        }
    }
    public class ExitPrimitive : SPrimitive
    {
        public ExitPrimitive(Universe universe)
            : base("exit:", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var error = (SInteger)frame.Pop();
            universe.Exit((int)error.EmbeddedInteger);
        }
    }
    public class GlobalPrimitive : SPrimitive
    {
        public GlobalPrimitive(Universe universe)
            : base("global:", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var argument = (SSymbol)frame.Pop();
            frame.Pop(); // not required
            var result = universe.GetGlobal(argument);
            frame.Push(result ?? universe.nilObject);
        }
    }
    public class GlobalPutPrimitive : SPrimitive
    {
        public GlobalPutPrimitive(Universe universe)
            : base("global:put:", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var value = frame.Pop();
            var argument = (SSymbol)frame.Pop();
            universe.SetGlobal(argument, value);
        }
    }
    public class PrintStringPrimitive : SPrimitive
    {
        public PrintStringPrimitive(Universe universe)
            : base("printString:", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var argument = (SString)frame.Pop();
            Universe.Print(argument.EmbeddedString);
        }
    }
    public class PrintNewlinePrimitive : SPrimitive
    {
        public PrintNewlinePrimitive(Universe universe)
            : base("printNewline", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            Universe.Println("");
        }
    }
    public class ErrorPrintPrimitive : SPrimitive
    {
        public ErrorPrintPrimitive(Universe universe)
            : base("errorPrint:", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var argument = (SString)frame.Pop();
            Universe.ErrorPrint(argument.EmbeddedString);
        }
    }
    public class ErrorPrintlnPrimitive : SPrimitive
    {
        public ErrorPrintlnPrimitive(Universe universe)
            : base("errorPrintln:", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var argument = (SString)frame.Pop();
            Universe.ErrorPrintln(argument.EmbeddedString);
        }
    }
    public class PrintStackTracePrimitive : SPrimitive
    {
        public PrintStackTracePrimitive(Universe universe)
            : base("printStackTrace", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            frame.Pop();
            frame.PrintStackTrace();
            frame.Push(universe.trueObject);
        }
    }
    public class LoadFilePrimitive : SPrimitive
    {
        public LoadFilePrimitive(Universe universe)
            : base("loadFile:", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var fileName = (SString)frame.Pop();
            frame.Pop();

            var p = fileName.EmbeddedString;
            try
            {
                frame.Push(universe.NewString(File.ReadAllText(p)));
            }
            catch (IOException)
            {
                frame.Push(universe.nilObject);
            }
        }
    }
    public class FullGCPrimitive : SPrimitive
    {
        public FullGCPrimitive(Universe universe)
            : base("fullGC", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            frame.Pop();
            GC.Collect();
            frame.Push(universe.trueObject);
        }
    }
    public class TicksPrimitive : SPrimitive
    {
        protected SystemPrimitives sp;
        public TicksPrimitive(Universe universe, SystemPrimitives sp)
            : base("ticks", universe) { this.sp = sp; }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            frame.Pop(); // ignore
            int time = (int)(new Stopwatch().ElapsedMilliseconds - sp.startMicroTime);
            frame.Push(universe.NewInteger(time));
        }
    }
    public class TimePrimitive : SPrimitive
    {
        protected SystemPrimitives sp;
        public TimePrimitive(Universe universe,SystemPrimitives sp)
            : base("time", universe) { this.sp = sp; }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            frame.Pop(); // ignore
            int time = (int)(DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - sp.startTime);
            frame.Push(universe.NewInteger(time));
        }
    }
    public override void InstallPrimitives()
    {
        this.InstallInstancePrimitive(new LoadPrimitive(universe));
        this.InstallInstancePrimitive(new ExitPrimitive(universe));
        this.InstallInstancePrimitive(new GlobalPrimitive(universe));
        this.InstallInstancePrimitive(new GlobalPutPrimitive(universe));
        this.InstallInstancePrimitive(new PrintStringPrimitive(universe));
        this.InstallInstancePrimitive(new PrintNewlinePrimitive(universe));
        this.InstallInstancePrimitive(new ErrorPrintPrimitive(universe));
        this.InstallInstancePrimitive(new ErrorPrintlnPrimitive(universe));
        this.InstallInstancePrimitive(new PrintStackTracePrimitive(universe));
        this.InstallInstancePrimitive(new LoadFilePrimitive(universe));
        this.InstallInstancePrimitive(new FullGCPrimitive(universe));
        this.InstallInstancePrimitive(new TicksPrimitive(universe,this));
        this.InstallInstancePrimitive(new TimePrimitive(universe,this));

        this.startMicroTime = new Stopwatch().ElapsedMilliseconds;
        this.startTime = new Stopwatch().ElapsedMilliseconds;        
    }

    protected long startTime;
    protected long startMicroTime;
}
