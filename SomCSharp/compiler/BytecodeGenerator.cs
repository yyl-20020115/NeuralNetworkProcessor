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
using Som.VMObject;
using static Som.Interpreter.Bytecodes;

public class BytecodeGenerator
{
    public void EmitPOP(MethodGenerationContext mgenc) => Emit1(mgenc, POP);
    public void EmitPUSHARGUMENT(MethodGenerationContext mgenc, byte idx, byte ctx) => Emit3(mgenc, PUSH_ARGUMENT, idx, ctx);
    public void EmitRETURNLOCAL(MethodGenerationContext mgenc) => Emit1(mgenc, RETURN_LOCAL);
    public void EmitRETURNNONLOCAL(MethodGenerationContext mgenc) => Emit1(mgenc, RETURN_NON_LOCAL);
    public void EmitDUP(MethodGenerationContext mgenc) => Emit1(mgenc, DUP);
    public void EmitPUSHBLOCK(MethodGenerationContext mgenc, SMethod blockMethod) => Emit2(mgenc, PUSH_BLOCK, mgenc.FindLiteralIndex(blockMethod));
    public void EmitPUSHLOCAL(MethodGenerationContext mgenc, byte idx, byte ctx) => Emit3(mgenc, PUSH_LOCAL, idx, ctx);
    public void EmitPUSHFIELD(MethodGenerationContext mgenc, SSymbol fieldName) => Emit2(mgenc, PUSH_FIELD, mgenc.GetFieldIndex(fieldName));
    public void EmitPUSHGLOBAL(MethodGenerationContext mgenc, SSymbol global) => Emit2(mgenc, PUSH_GLOBAL, mgenc.FindLiteralIndex(global));
    public void EmitPOPARGUMENT(MethodGenerationContext mgenc, byte idx, byte ctx) => Emit3(mgenc, POP_ARGUMENT, idx, ctx);
    public void EmitPOPLOCAL(MethodGenerationContext mgenc, byte idx, byte ctx) => Emit3(mgenc, POP_LOCAL, idx, ctx);
    public void EmitPOPFIELD(MethodGenerationContext mgenc, SSymbol fieldName) => Emit2(mgenc, POP_FIELD, mgenc.GetFieldIndex(fieldName));
    public void EmitSUPERSEND(MethodGenerationContext mgenc, SSymbol msg) => Emit2(mgenc, SUPER_SEND, mgenc.FindLiteralIndex(msg));
    public void EmitSEND(MethodGenerationContext mgenc, SSymbol msg) => Emit2(mgenc, SEND, mgenc.FindLiteralIndex(msg));
    public void EmitPUSHCONSTANT(MethodGenerationContext mgenc, SAbstractObject lit) => Emit2(mgenc, PUSH_CONSTANT, mgenc.FindLiteralIndex(lit));
    public void EmitPUSHCONSTANT(MethodGenerationContext mgenc, byte literalIndex) => Emit2(mgenc, PUSH_CONSTANT, literalIndex);
    protected static void Emit1(MethodGenerationContext mgenc, byte code) => mgenc.AddBytecode(code);
    protected static void Emit2(MethodGenerationContext mgenc, byte code, byte idx) => mgenc.AddBytecode(code).AddBytecode(idx);
    protected static void Emit3(MethodGenerationContext mgenc, byte code, byte idx, byte ctx) => mgenc.AddBytecode(code).AddBytecode(idx).AddBytecode(ctx);
}
