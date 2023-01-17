/**
 * Copyright (c) 2017 Michael Haupt, github@haupz.de
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
namespace Som.Compiler;
using Som.VM;
using Som.VMObject;
using static Som.Interpreter.Bytecodes;

public class Disassembler
{
    public static void Dump(SClass cl, Universe universe)
    {
        for (int i = 0; i < cl.NumberOfInstanceInvokables; i++)
        {
            var inv = cl.GetInstanceInvokable(i);

            // output header and skip if the Invokable is a Primitive
            Universe.ErrorPrint(cl.Name.ToString() + ">>" + inv.Signature.ToString() + " = ");

            if (inv.IsPrimitive)
            {
                Universe.ErrorPrintln("<primitive>");
                continue;
            }
            // output actual method
            DumpMethod((SMethod)inv, "\t", universe);
        }
    }

    public static void DumpMethod(SMethod m, string indent, Universe universe)
    {
        Universe.ErrorPrintln("(");
        // output stack information
        Universe.ErrorPrintln(indent + "<" + m.NumberOfLocals + " locals, "
            + m.MaximumNumberOfStackElements + " stack, "
            + m.NumberOfBytecodes + " bc_count>");
        // output bytecodes
        for (var b = 0; b < m.NumberOfBytecodes; b += GetBytecodeLength(m.GetBytecode(b)))
        {
            Universe.ErrorPrint(indent);
            // bytecode index
            if (b < 10) Universe.ErrorPrint(" ");
            if (b < 100) Universe.ErrorPrint(" ");
            Universe.ErrorPrint(" " + b + ":");
            // mnemonic
            var bytecode = m.GetBytecode(b);
            Universe.ErrorPrint(GetPaddedBytecodeName(bytecode) + "  ");
            // parameters (if any)
            if (GetBytecodeLength(bytecode) == 1)
            {
                Universe.ErrorPrintln();
                continue;
            }
            switch (bytecode)
            {
                case PUSH_LOCAL:
                    Universe.ErrorPrintln("local: " + m.GetBytecode(b + 1) + ", context: " + m.GetBytecode(b + 2));
                    break;
                case PUSH_ARGUMENT:
                    Universe.ErrorPrintln("argument: " + m.GetBytecode(b + 1) + ", context " + m.GetBytecode(b + 2));
                    break;
                case PUSH_FIELD:
                    {
                        var idx = m.GetBytecode(b + 1);
                        var fieldName = ((SSymbol)m.Holder.InstanceFields.GetIndexableField(idx)).EmbeddedString;
                        Universe.ErrorPrintln("(index: " + idx + ") field: " + fieldName);
                        break;
                    }
                case PUSH_BLOCK:
                    Universe.ErrorPrint("block: (index: " + m.GetBytecode(b + 1) + ") ");
                    DumpMethod((SMethod)m.GetConstant(b), indent + "\t", universe);
                    break;
                case PUSH_CONSTANT:
                    var constant = m.GetConstant(b);
                    Universe.ErrorPrintln("(index: " + m.GetBytecode(b + 1) + ") value: " + "(" + constant.GetSOMClass(universe).Name.ToString() + ") " + constant.ToString());
                    break;
                case PUSH_GLOBAL:
                    Universe.ErrorPrintln("(index: " + m.GetBytecode(b + 1) + ") value: " + ((SSymbol)m.GetConstant(b)).ToString());
                    break;
                case POP_LOCAL:
                    Universe.ErrorPrintln("local: " + m.GetBytecode(b + 1) + ", context: " + m.GetBytecode(b + 2));
                    break;
                case POP_ARGUMENT:
                    Universe.ErrorPrintln("argument: " + m.GetBytecode(b + 1) + ", context: " + m.GetBytecode(b + 2));
                    break;
                case POP_FIELD:
                    {
                        var idx = m.GetBytecode(b + 1);
                        var fieldName = ((SSymbol)m.Holder.InstanceFields.GetIndexableField(idx)).EmbeddedString;
                        Universe.ErrorPrintln("(index: " + idx + ") field: " + fieldName);
                        break;
                    }
                case SEND:
                    Universe.ErrorPrintln("(index: " + m.GetBytecode(b + 1) + ") signature: " + ((SSymbol)m.GetConstant(b)).ToString());
                    break;
                case SUPER_SEND:
                    Universe.ErrorPrintln("(index: " + m.GetBytecode(b + 1) + ") signature: " + ((SSymbol)m.GetConstant(b)).ToString());
                    break;
                default:
                    Universe.ErrorPrintln("<incorrect bytecode>");
                    break;
            }
        }
        Universe.ErrorPrintln(indent + ")");
    }
}
