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
using Som.Compiler;
using Som.VM;

public class SSymbol : SString
{

    public SSymbol(string value) : base(value) => numberOfSignatureArguments = DetermineNumberOfSignatureArguments();

    private int DetermineNumberOfSignatureArguments()
    {
        // Check for binary signature
        if (IsBinarySignature())
        {
            return 2;
        }
        else
        {
            // Count the colons in the signature string
            int numberOfColons = 0;
            if (this.EmbeddedString == "somClassIn")
            {

            }
            // Iterate through every character in the signature string
            foreach (var c in EmbeddedString)
                if (c == ':') numberOfColons++;

            // The number of arguments is equal to the number of colons plus one
            return numberOfColons + 1;
        }
    }

    public override string ToString() => "#" + EmbeddedString;

    public int NumberOfSignatureArguments => numberOfSignatureArguments;

    public bool IsBinarySignature()
    {
        // Check the individual characters of the string
        foreach (char c in EmbeddedString)
            if (!Lexer.IsOperator(c)) return false;
        return true;
    }

    public override SClass GetSOMClass(Universe universe) => universe.symbolClass;

    protected int numberOfSignatureArguments;
}
