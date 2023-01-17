/**
 * Copyright (c) 2013 Stefan Marr,   stefan.marr@vub.ac.be
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

public class StringPrimitives : Primitives
{

    public StringPrimitives(Universe universe) : base(universe) { }
    public class ConcatenatePrimitive : SPrimitive
    {
        public ConcatenatePrimitive(Universe universe)
            : base("concatenate:", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var argument = (SString)frame.Pop();
            var self = (SString)frame.Pop();
            frame.Push(universe.NewString(self.EmbeddedString
                + argument.EmbeddedString));
        }
    }
    public class AsSymbolPrimitive : SPrimitive
    {
        public AsSymbolPrimitive(Universe universe)
            : base("asSymbol", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var self = (SString)frame.Pop();
            frame.Push(universe.SymbolFor(self.EmbeddedString));
        }
    }
    public class LengthPrimitive : SPrimitive
    {
        public LengthPrimitive(Universe universe)
            : base("length", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var self = (SString)frame.Pop();
            frame.Push(universe.NewInteger(self.EmbeddedString.Length));
        }
    }
    public class EqualPrimitive : SPrimitive
    {
        public EqualPrimitive(Universe universe)
            : base("=", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var op2 = frame.Pop();
            var op1 = (SString)frame.Pop(); // self
            if (op2 is SString s)
            {
                if (s.EmbeddedString == (op1.EmbeddedString))
                {
                    frame.Push(universe.trueObject);
                    return;
                }
            }

            frame.Push(universe.falseObject);
        }
    }

    public class SubstringFromToPrimitive : SPrimitive
    {
        public SubstringFromToPrimitive(Universe universe)
            : base("primSubstringFrom:to:", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var end = (SInteger)frame.Pop();
            var start = (SInteger)frame.Pop();
            var self = (SString)frame.Pop();

            try
            {
                var s = start.EmbeddedInteger;
                var e = end.EmbeddedInteger;

                frame.Push(universe.NewString(
                    self.EmbeddedString
                    .Substring(
                    (int)s - 1,
                    (int)(e-(s-1)))));
            }
            catch (IndexOutOfRangeException)
            {
                frame.Push(universe.NewString(
                    "Error - index out of bounds"));
            }
        }
    }
    public class HashCodePrimitive : SPrimitive
    {
        public HashCodePrimitive(Universe universe)
            : base("hashcode", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var self = (SString)frame.Pop();
            frame.Push(universe.NewInteger(self.EmbeddedString.GetHashCode()));
        }
    }
    public class IsWhiteSpacePrimitive : SPrimitive
    {
        public IsWhiteSpacePrimitive(Universe universe)
            : base("isWhiteSpace", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var self = (SString)frame.Pop();
            var embedded = self.EmbeddedString;

            for (int i = 0; i < embedded.Length; i++)
            {
                if (!char.IsWhiteSpace(embedded[i]))
                {
                    frame.Push(universe.falseObject);
                    return;
                }
            }

            frame.Push(embedded.Length > 0 ? universe.trueObject : universe.falseObject);
        }
    }
    public class IsLettersPrimitives : SPrimitive
    {
        public IsLettersPrimitives(Universe universe)
            : base("isLetters", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var self = (SString)frame.Pop();
            var embedded = self.EmbeddedString;

            for (int i = 0; i < embedded.Length; i++)
            {
                if (!char.IsLetter(embedded[i]))
                {
                    frame.Push(universe.falseObject);
                    return;
                }
            }

            frame.Push(embedded.Length > 0 ? universe.trueObject : universe.falseObject);
        }
    }

    public class IsDigitsPrimitive : SPrimitive
    {
        public IsDigitsPrimitive(Universe universe)
            : base("isDigits", universe) { }
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            var self = (SString)frame.Pop();
            var embedded = self.EmbeddedString;

            for (int i = 0; i < embedded.Length; i++)
            {
                if (!char.IsDigit(embedded[i]))
                {
                    frame.Push(universe.falseObject);
                    return;
                }
            }
            frame.Push(embedded.Length > 0 ? universe.trueObject : universe.falseObject);
        }

    }
    public override void InstallPrimitives()
    {
        this.InstallInstancePrimitive(new ConcatenatePrimitive(universe));
        this.InstallInstancePrimitive(new AsSymbolPrimitive(universe));
        this.InstallInstancePrimitive(new LengthPrimitive(universe));
        this.InstallInstancePrimitive(new EqualPrimitive(universe));
        this.InstallInstancePrimitive(new SubstringFromToPrimitive(universe));
        this.InstallInstancePrimitive(new HashCodePrimitive(universe));
        this.InstallInstancePrimitive(new IsWhiteSpacePrimitive(universe));
        this.InstallInstancePrimitive(new IsLettersPrimitives(universe));
        this.InstallInstancePrimitive(new IsDigitsPrimitive(universe));
    }
}
