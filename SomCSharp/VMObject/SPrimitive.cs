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
namespace Som.VMObject;
using Som.Interpreter;
using Som.VM;

public abstract class SPrimitive : SAbstractObject, ISInvokable
{
    public virtual bool IsPrimitive => true;

    public SPrimitive(string signatureString, Universe universe)
    {
        this.signature = universe.SymbolFor(signatureString);
        this.universe = universe;
    }

    public virtual SSymbol Signature => signature;

    public virtual SClass Holder { get => holder; set => holder = value; }

    public virtual bool IsEmpty => false;
    // By default a primitive is not empty

    public override SClass GetSOMClass(Universe universe) => universe.primitiveClass;

    protected class EmptyPrimitive : SPrimitive
    {
        public EmptyPrimitive(string signatureString, Universe universe)
            : base(string.Empty, universe) => signature = universe.SymbolFor(signatureString);
        //@Override
        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            // Write a warning to the screen
            Universe.Println("Warning: undefined primitive "
                + this.Signature.EmbeddedString + " called");
        }

        //@Override
        public override bool IsEmpty => true;
        // The empty primitives are empty
    }
    public virtual void Invoke(Frame frame, Interpreter interpreter) { }
    public static SPrimitive GetEmptyPrimitive(string signatureString, Universe universe) =>
        // Return an empty primitive with the given signature
        new EmptyPrimitive(signatureString, universe);

    protected SSymbol signature;
    protected SClass holder;
    protected Universe universe;
}
