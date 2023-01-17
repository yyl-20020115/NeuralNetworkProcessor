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

namespace Som.Compiler;
using Som.VM;
using Som.VMObject;
using static Som.Compiler.Parser;
using static Som.Interpreter.Bytecodes;
using System.Runtime.Serialization;

public class MethodGenerationContext
{
    protected ClassGenerationContext holderGenc;
    protected MethodGenerationContext outerGenc;

    protected SSymbol signature;
    protected List<string> arguments = new ();
    protected List<string> locals = new ();
    protected List<SAbstractObject> literals = new ();
    protected List<byte> bytecode = new();
    protected bool finished;
    protected bool primitive;
    protected bool blockMethod;

    /**
     * Constructor used for block methods.
     */
    public MethodGenerationContext(ClassGenerationContext holderGenc, MethodGenerationContext outerGenc)
    {
        this.holderGenc = holderGenc;
        this.outerGenc = outerGenc;
        this.blockMethod = outerGenc != null;
    }

    /**
     * Constructor used for normal methods.
     */
    public MethodGenerationContext(ClassGenerationContext holderGenc)
        : this(holderGenc, null) { }
    public void AddArgument(string arg) => arguments.Add(arg);
    public bool IsPrimitive => primitive;
    public ISInvokable Assemble(Universe universe)
        => primitive ? SPrimitive.GetEmptyPrimitive(signature.EmbeddedString, universe) : AssembleMethod(universe);

    public SMethod AssembleMethod(Universe universe)
    {
        // create a method instance with the given number of bytecodes
        var numLocals = locals.Count;
        var meth = universe.NewMethod(signature, bytecode.Count,
            numLocals, ComputeStackDepth(),
            literals);

        // copy bytecodes into method
        var i = 0;
        foreach (var bc in bytecode) meth.SetBytecode(i++, bc);
        // return the method - the holder field is to be set later on!
        return meth;
    }

    private int ComputeStackDepth()
    {
        int depth = 0;
        int maxDepth = 0;
        int i = 0;

        while (i < bytecode.Count)
        {
            switch (bytecode[(i)])
            {
                case HALT:
                    i++;
                    break;
                case DUP:
                    depth++;
                    i++;
                    break;
                case PUSH_LOCAL:
                case PUSH_ARGUMENT:
                    depth++;
                    i += 3;
                    break;
                case PUSH_FIELD:
                case PUSH_BLOCK:
                case PUSH_CONSTANT:
                case PUSH_GLOBAL:
                    depth++;
                    i += 2;
                    break;
                case POP:
                    depth--;
                    i++;
                    break;
                case POP_LOCAL:
                case POP_ARGUMENT:
                    depth--;
                    i += 3;
                    break;
                case POP_FIELD:
                    depth--;
                    i += 2;
                    break;
                case SEND:
                case SUPER_SEND:
                    {
                        // these are special: they need to look at the number of
                        // arguments (extractable from the signature)
                        var sig = (SSymbol)literals[(bytecode[(i + 1)])];
                        depth -= sig.NumberOfSignatureArguments;
                        depth++; // return value
                        i += 2;
                        break;
                    }
                case RETURN_LOCAL:
                case RETURN_NON_LOCAL:
                    i++;
                    break;
                default:
                    throw new IllegalStateException("Illegal bytecode "
                        + bytecode[(i)]);
            }

            if (depth > maxDepth) maxDepth = depth;
        }

        return maxDepth;
    }

    public void MarkAsPrimitive() => primitive = true;

    public void SetSignature(SSymbol sig) => signature = sig;

    public bool AddArgumentIfAbsent(string arg)
    {
        if (arguments.Contains(arg)) return false;
        arguments.Add(arg);
        return true;
    }

    public bool IsFinished => finished;

    public void MarkAsFinished() => this.finished = false;

    public bool AddLocalIfAbsent(string local)
    {
        if (locals.Contains(local)) return false;
        locals.Add(local);
        return true;
    }

    public void AddLocal(string local) => locals.Add(local);

    public bool HasBytecodes => bytecode.Count > 0;

    public void RemoveLastBytecode() => bytecode.RemoveAt(bytecode.Count - 1);

    public bool IsBlockMethod => blockMethod;

    public void SetFinished() => finished = true;

    public bool AddLiteralIfAbsent(SAbstractObject lit, Parser parser)
    {
        if (literals.Contains(lit)) return false;
        AddLiteral(lit, parser);
        return true;
    }

    public ClassGenerationContext Holder => holderGenc;

    public byte AddLiteral(SAbstractObject lit, Parser parser)
    {
        int i = literals.Count;
        if (i > Byte.MaxValue) {
            var methodSignature = holderGenc.Name.EmbeddedString + ">>" + signature;
            throw new ParseError(
                "The method " + methodSignature + " has more than the supported " +
                    Byte.MaxValue
                    + " literal values. Please split the method. The literal to be added is: " + lit,
                Symbol.NONE, parser);
        }
        literals.Add(lit);
        return (byte)i;
    }

    public void UpdateLiteral(SAbstractObject oldVal, byte index, SAbstractObject newVal) =>
        //assert literals.get(index) == oldVal;
        literals[index] = newVal;

    public bool FindVariable(string var, Triplet<byte, byte, bool> tri)
    {
        // triplet: index, context, isArgument
        tri.X = (byte)locals.IndexOf(var);
        if (tri.X == 0xff)
        {
            tri.X = (byte)arguments.IndexOf(var);
            if (tri.X == 0xff)
            {
                if (outerGenc == null)
                {
                    return false;
                }
                else
                {
                    tri.Y = (byte)(tri.Y + 1);
                    return outerGenc.FindVariable(var, tri);
                }
            }
            else
            {
                tri.Z = true;
            }
        }

        return true;
    }

    public bool HasField(SSymbol field) => holderGenc.HasField(field);

    public byte GetFieldIndex(SSymbol field) => holderGenc.GetFieldIndex(field);

    public int NumberOfArguments => arguments.Count;

    public MethodGenerationContext AddBytecode(byte code)
    {
        bytecode.Add(code);
        return this;
    }

    public byte FindLiteralIndex(SAbstractObject lit) => (byte)literals.IndexOf(lit);

    public MethodGenerationContext Outer => outerGenc;
}

[Serializable]
internal class IllegalStateException : Exception
{
    public IllegalStateException()
    {
    }

    public IllegalStateException(string? message) : base(message)
    {
    }

    public IllegalStateException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    protected IllegalStateException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}