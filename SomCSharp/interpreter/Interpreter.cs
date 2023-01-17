/**
 * Copyright (c) 2017 Michael Haupt, github@haupz.de
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

namespace Som.Interpreter;
using Som.VM;
using Som.VMObject;
using static Som.Interpreter.Bytecodes;

public class Interpreter
{

    protected Universe universe;

    public Interpreter(Universe universe)
    {
        this.universe = universe;
    }

    private void DoDup() =>
        // Handle the DUP bytecode
        Frame.Push(Frame.GetStackElement(0));

    private void DoPushLocal(int bytecodeIndex) =>
        // Handle the PUSH LOCAL bytecode
        Frame.Push(
            Frame.GetLocal(Method.GetBytecode(bytecodeIndex + 1),
                Method.GetBytecode(bytecodeIndex + 2)));

    private void DoPushArgument(int bytecodeIndex) =>
        // Handle the PUSH ARGUMENT bytecode
        Frame.Push(
            Frame.GetArgument(Method.GetBytecode(bytecodeIndex + 1),
                Method.GetBytecode(bytecodeIndex + 2)));

    private void DoPushField(int bytecodeIndex)
    {
        // Handle the PUSH FIELD bytecode
        int fieldIndex = Method.GetBytecode(bytecodeIndex + 1);

        // Push the field with the computed index onto the stack
        Frame.Push(((SObject)Self).GetField(fieldIndex));
    }

    private void DoPushBlock(int bytecodeIndex)
    {
        // Handle the PUSH BLOCK bytecode
        var blockMethod = (SMethod)Method.GetConstant(bytecodeIndex);

        // Push a new block with the current getFrame() as context onto the
        // stack
        Frame.Push(
            universe.NewBlock(blockMethod, Frame,
                blockMethod.NumberOfArguments));
    }

    private void DoPushConstant(int bytecodeIndex) =>
        // Handle the PUSH CONSTANT bytecode
        Frame.Push(Method.GetConstant(bytecodeIndex));

    private void DoPushGlobal(int bytecodeIndex)
    {
        // Handle the PUSH GLOBAL bytecode
        var globalName = (SSymbol)Method.GetConstant(bytecodeIndex);

        // Get the global from the universe
        var global = universe.GetGlobal(globalName);

        if (global != null)
        {
            // Push the global onto the stack
            Frame.Push(global);
        }
        else
        {
            // Send 'unknownGlobal:' to self
            Self.SendUnknownGlobal(globalName, universe, this);
        }
    }

    private void DoPop() =>
        // Handle the POP bytecode
        Frame.Pop();

    private void DoPopLocal(int bytecodeIndex) =>
        // Handle the POP LOCAL bytecode
        Frame.SetLocal(Method.GetBytecode(bytecodeIndex + 1),
            Method.GetBytecode(bytecodeIndex + 2), Frame.Pop());

    private void DoPopArgument(int bytecodeIndex) =>
        // Handle the POP ARGUMENT bytecode
        Frame.SetArgument(Method.GetBytecode(bytecodeIndex + 1),
            Method.GetBytecode(bytecodeIndex + 2), Frame.Pop());

    private void DoPopField(int bytecodeIndex)
    {
        // Handle the POP FIELD bytecode
        int fieldIndex = Method.GetBytecode(bytecodeIndex + 1);

        // Set the field with the computed index to the value popped from the stack
        ((SObject)Self).SetField(fieldIndex, Frame.Pop());
    }

    private void DoSuperSend(int bytecodeIndex)
    {
        // Handle the SUPER SEND bytecode
        var signature = (SSymbol)Method.GetConstant(bytecodeIndex);

        // Send the message
        // Lookup the invokable with the given signature
        var holderSuper = (SClass)Method.Holder.SuperClass;
        var invokable = holderSuper.LookupInvokable(signature);

        ActivateOrDnu(signature, invokable);
    }

    private void DoReturnLocal()
    {
        // Handle the RETURN LOCAL bytecode
        var result = Frame.Pop();
        // Pop the top frame and push the result
        PopFrameAndPushResult(result);
    }

    private void DoReturnNonLocal()
    {
        // Handle the RETURN NON LOCAL bytecode
        var result = Frame.Pop();
        // Compute the context for the non-local return
        var context = Frame.GetOuterContext();
        // Make sure the block context is still on the stack
        if (!context.HasPreviousFrame)
        {
            // Try to recover by sending 'escapedBlock:' to the sending object
            // this can get a bit nasty when using nested blocks. In this case
            // the "sender" will be the surrounding block and not the object
            // that actually sent the 'value' message.
            var block = (SBlock)Frame.GetArgument(0, 0);
            var sender =
                Frame.PreviousFrame.GetOuterContext().GetArgument(0, 0);

            // pop the frame of the currently executing block...
            PopFrame();

            // pop old arguments from stack
            SMethod method = Frame.Method;
            int numArgs = method.NumberOfArguments;
            for (int i = 0; i < numArgs; i += 1)
            {
                Frame.Pop();
            }

            // ... and execute the escapedBlock message instead
            sender.SendEscapedBlock(block, universe, this);
            return;
        }

        // Unwind the frames
        while (Frame != context)
        {
            PopFrame();
        }

        // Pop the top frame and push the result
        PopFrameAndPushResult(result);
    }

    private void DoSend(int bytecodeIndex)
    {
        // Handle the SEND bytecode
        var signature = (SSymbol)Method.GetConstant(bytecodeIndex);

        // Get the number of arguments from the signature
        int numberOfArguments = signature.NumberOfSignatureArguments;

        // Get the receiver from the stack
        var receiver = Frame.GetStackElement(numberOfArguments - 1);
        if(receiver != null)
        {
            // Send the message
            Send(signature, receiver.GetSOMClass(universe), bytecodeIndex);
        }
        else
        {

        }
    }

    public SAbstractObject Start()
    {
        // Iterate through the bytecodes
        while (true)
        {

            // Get the current bytecode index
            int bytecodeIndex = Frame.BytecodeIndex;

            // Get the current bytecode
            byte bytecode = Method.GetBytecode(bytecodeIndex);

            // Get the length of the current bytecode
            int bytecodeLength = GetBytecodeLength(bytecode);

            // Compute the next bytecode index
            int nextBytecodeIndex = bytecodeIndex + bytecodeLength;

            // Update the bytecode index of the frame
            Frame.SetBytecodeIndex(nextBytecodeIndex);

            // Handle the current bytecode
            switch (bytecode)
            {

                case HALT:
                    {
                        // Handle the HALT bytecode
                        return Frame.GetStackElement(0);
                    }

                case DUP:
                    {
                        DoDup();
                        break;
                    }

                case PUSH_LOCAL:
                    {
                        DoPushLocal(bytecodeIndex);
                        break;
                    }

                case PUSH_ARGUMENT:
                    {
                        DoPushArgument(bytecodeIndex);
                        break;
                    }

                case PUSH_FIELD:
                    {
                        DoPushField(bytecodeIndex);
                        break;
                    }

                case PUSH_BLOCK:
                    {
                        DoPushBlock(bytecodeIndex);
                        break;
                    }

                case PUSH_CONSTANT:
                    {
                        DoPushConstant(bytecodeIndex);
                        break;
                    }

                case PUSH_GLOBAL:
                    {
                        DoPushGlobal(bytecodeIndex);
                        break;
                    }

                case POP:
                    {
                        DoPop();
                        break;
                    }

                case POP_LOCAL:
                    {
                        DoPopLocal(bytecodeIndex);
                        break;
                    }

                case POP_ARGUMENT:
                    {
                        DoPopArgument(bytecodeIndex);
                        break;
                    }

                case POP_FIELD:
                    {
                        DoPopField(bytecodeIndex);
                        break;
                    }

                case SEND:
                    {
                        DoSend(bytecodeIndex);
                        break;
                    }

                case SUPER_SEND:
                    {
                        DoSuperSend(bytecodeIndex);
                        break;
                    }

                case RETURN_LOCAL:
                    {
                        DoReturnLocal();
                        break;
                    }

                case RETURN_NON_LOCAL:
                    {
                        DoReturnNonLocal();
                        break;
                    }

                default:
                    Universe.ErrorPrintln("Nasty bug in interpreter");
                    break;
            }
        }
    }

    public Frame PushNewFrame(SMethod method, Frame contextFrame)
    {
        // Allocate a new frame and make it the current one
        frame = universe.NewFrame(frame, method, contextFrame);

        // Return the freshly allocated and pushed frame
        return frame;
    }

    public Frame PushNewFrame(SMethod method) => PushNewFrame(method, null);

    public Frame Frame =>
        // Get the frame from the interpreter
        frame;

    public SMethod Method =>
        // Get the method from the interpreter
        Frame.Method;

    public SAbstractObject Self =>
        // Get the self object from the interpreter
        Frame.GetOuterContext().GetArgument(0, 0);

    private void Send(SSymbol selector, SClass receiverClass,
         int bytecodeIndex)
    {
        // First try the inline cache
        ISInvokable invokable;

        var m = Method;
        var cachedClass = m.GetInlineCacheClass(bytecodeIndex);
        if (cachedClass == receiverClass)
        {
            invokable = m.GetInlineCacheInvokable(bytecodeIndex);
        }
        else
        {
            if (cachedClass == null)
            {
                // Lookup the invokable with the given signature
                invokable = receiverClass.LookupInvokable(selector);
                m.SetInlineCache(bytecodeIndex, receiverClass, invokable);
            }
            else
            {
                // the bytecode index after the send is used by the selector constant, and can be used
                // safely as another cache item
                cachedClass = m.GetInlineCacheClass(bytecodeIndex + 1);
                if (cachedClass == receiverClass)
                {
                    invokable = m.GetInlineCacheInvokable(bytecodeIndex + 1);
                }
                else
                {
                    invokable = receiverClass.LookupInvokable(selector);
                    if (cachedClass == null)
                    {
                        m.SetInlineCache(bytecodeIndex + 1, receiverClass, invokable);
                    }
                }
            }
        }

        ActivateOrDnu(selector, invokable);
    }

    public void ActivateOrDnu(SSymbol selector, ISInvokable invokable)
    {
        if (invokable != null)
        {
            // Invoke the invokable in the current frame
            invokable.Invoke(Frame, this);
        }
        else
        {
            int numberOfArguments = selector.NumberOfSignatureArguments;

            // Compute the receiver
            var receiver = frame.GetStackElement(numberOfArguments - 1);

            receiver.SendDoesNotUnderstand(selector, universe, this);
        }
    }

    private Frame PopFrame()
    {
        // Save a reference to the top frame
        var result = frame;

        // Pop the top frame from the frame stack
        frame = frame.PreviousFrame;

        // Destroy the previous pointer on the old top frame
        result.ClearPreviousFrame();

        // Return the popped frame
        return result;
    }

    private void PopFrameAndPushResult(SAbstractObject result)
    {
        // Pop the top frame from the interpreter frame stack and compute the
        // number of arguments
        int numberOfArguments = PopFrame().Method.NumberOfArguments;

        // Pop the arguments
        for (int i = 0; i < numberOfArguments; i++)
        {
            Frame.Pop();
        }

        // Push the result
        Frame.Push(result);
    }

    protected Frame frame;
}
