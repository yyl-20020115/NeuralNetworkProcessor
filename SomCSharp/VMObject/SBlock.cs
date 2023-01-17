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
using Som.Interpreter;
using Som.VM;

public class SBlock : SAbstractObject
{
    public SBlock(SMethod method, Frame context, SClass blockClass)
    {
        this.method = method;
        this.context = context;
        this.blockClass = blockClass;
    }

    public SMethod Method => method;

    public Frame Context => context;

    public override SClass GetSOMClass(Universe universe) => blockClass;

    public static SPrimitive GetEvaluationPrimitive(int numberOfArguments, Universe universe) => new Evaluation(numberOfArguments, universe);

    public class Evaluation : SPrimitive
    {
        public Evaluation(int numberOfArguments, Universe universe)
           : base(ComputeSignatureString(numberOfArguments), universe) 
            => this.numberOfArguments = numberOfArguments;

        public override void Invoke(Frame frame, Interpreter interpreter)
        {
            // Get the block (the receiver) from the stack
            var self = (SBlock)frame.GetStackElement(numberOfArguments - 1);

            // Get the context of the block...
            var context = self.Context;

            // Push a new frame and set its context to be the one specified in
            // the block
            var newFrame = interpreter.PushNewFrame(self.Method, context);
            newFrame.CopyArgumentsFrom(frame);
        }

        private static string ComputeSignatureString(int numberOfArguments)
        {
            // Compute the signature string
            var signatureString = "value";
            if (numberOfArguments > 1) signatureString += ":";

            // Add extra value: selector elements if necessary
            for (int i = 2; i < numberOfArguments; i++)
                signatureString += "with:";

            // Return the signature string
            return signatureString;
        }

        protected int numberOfArguments;
    }

    protected SMethod method;
    protected Frame context;
    protected SClass blockClass;
}
