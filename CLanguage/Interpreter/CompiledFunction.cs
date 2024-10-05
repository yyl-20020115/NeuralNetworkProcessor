using System;
using System.Collections.Generic;
using System.IO;
using CLanguage.Syntax;
using CLanguage.Types;

namespace CLanguage.Interpreter;

public class CompiledFunction : BaseFunction
{
    public Block? Body { get; }

    public List<CompiledVariable> LocalVariables { get; }
    public List<Instruction> Instructions { get; }

    public CompiledFunction (string name, string nameContext, CFunctionType functionType, Block? body)
    {
        Name = name;
        NameContext = nameContext;
        FunctionType = functionType;
        Body = body;
        LocalVariables = [];
        Instructions = [];
    }

    public override string ToString () => Name;

    public string Assembler {
        get {
            var w = new StringWriter ();
            for (var i = 0; i < Instructions.Count; i++) {
                w.WriteLine ("{0}: {1}", i, Instructions[i]);
            }
            return w.ToString ();
        }
    }

    public override void Init (CInterpreter state)
    {
        var last = LocalVariables.Count == 0 ? null : LocalVariables[LocalVariables.Count - 1];
        if (last != null) {
            state.SP += last.StackOffset + last.VariableType.NumValues;
        }
    }

    public override void Step (CInterpreter state, ExecutionFrame frame)
    {
        var ip = frame.IP;

        var done = false;

        Value a = new Value ();
        Value b = new Value ();

        while (!done && ip < Instructions.Count && state.RemainingTime > 0) {

            var i = Instructions[ip];

            //Debug.WriteLine (new string(' ', 4*state.CallStackDepth) + i + " ;" + state.ActiveFrame?.Function.Name);

            if (state.SP < frame.FP)
                throw new Exception ($"{(ip - 1 >= 0 ? Instructions[ip - 1] : null)} {this.Name}@{ip - 1} stack underflow");

            switch (i.Op) {
                case OpCode.Dup:
                    state.Stack[state.SP] = state.Stack[state.SP - 1];
                    state.SP++;
                    ip++;
                    break;
                case OpCode.Pop:
                    state.SP--;
                    ip++;
                    break;
                case OpCode.Jump:
                    if (i.Label != null)
                        ip = i.Label.Index;
                    else
                        throw new InvalidOperationException ($"Jump label not set");
                    break;
                case OpCode.BranchIfFalse:
                    a = state.Stack[state.SP - 1];
                    state.SP--;
                    if (a.UInt8Value == 0) {
                        if (i.Label != null)
                            ip = i.Label.Index;
                        else
                            throw new InvalidOperationException ($"BranchIfFalse label not set");
                    }
                    else {
                        ip++;
                    }
                    break;
                case OpCode.BranchIfTrue:
                    a = state.Stack[state.SP - 1];
                    state.SP--;
                    if (a.UInt8Value != 0) {
                        if (i.Label != null)
                            ip = i.Label.Index;
                        else
                            throw new InvalidOperationException ($"BranchIfTrue label not set");
                    }
                    else {
                        ip++;
                    }
                    break;
                case OpCode.BranchIfFalseNoSPChange:
                    a = state.Stack[state.SP - 1];
                    if (a.UInt8Value == 0) {
                        if (i.Label != null)
                            ip = i.Label.Index;
                        else
                            throw new InvalidOperationException ($"BranchIfFalse label not set");
                    }
                    else {
                        ip++;
                    }
                    break;
                case OpCode.BranchIfTrueNoSPChange:
                    a = state.Stack[state.SP - 1];
                    if (a.UInt8Value != 0) {
                        if (i.Label != null)
                            ip = i.Label.Index;
                        else
                            throw new InvalidOperationException ($"BranchIfTrue label not set");
                    }
                    else {
                        ip++;
                    }
                    break;


                case OpCode.Call:
                    a = state.Stack[state.SP - 1];
                    state.SP--;
                    ip++;
                    state.Call (a);
                    done = true;
                    break;
                case OpCode.Return:
                    state.Return ();
                    done = true;
                    break;
                case OpCode.LoadConstant:
                    state.Stack[state.SP] = i.X;
                    state.SP++;
                    ip++;
                    break;
                case OpCode.LoadFramePointer:
                    state.Stack[state.SP] = frame.FP;
                    state.SP++;
                    ip++;
                    break;
                case OpCode.LoadPointer:
                    a = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 1] = state.Stack[a.PointerValue];
                    ip++;
                    break;
                case OpCode.StorePointer:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[b.PointerValue] = a;
                    state.SP -= 2;
                    ip++;
                    break;
                case OpCode.OffsetPointer:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2].PointerValue = a.PointerValue + b.Int32Value;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.LoadGlobal:
                    state.Stack[state.SP] = state.Stack[i.X.Int32Value];
                    state.SP++;
                    ip++;
                    break;
                case OpCode.StoreGlobal:
                    state.Stack[i.X.Int32Value] = state.Stack[state.SP - 1];
                    state.SP--;
                    ip++;
                    break;
                case OpCode.LoadArg:
                    state.Stack[state.SP] = state.Stack[frame.FP + i.X.Int32Value];
                    state.SP++;
                    ip++;
                    break;
                case OpCode.StoreArg:
                    state.Stack[frame.FP + i.X.Int32Value] = state.Stack[state.SP - 1];
                    state.SP--;
                    ip++;
                    break;
                case OpCode.LoadLocal:
                    state.Stack[state.SP] = state.Stack[frame.FP + i.X.Int32Value];
                    state.SP++;
                    ip++;
                    break;
                case OpCode.StoreLocal:
                    state.Stack[frame.FP + i.X.Int32Value] = state.Stack[state.SP - 1];
                    state.SP--;
                    ip++;
                    break;

                case OpCode.AddInt8:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (sbyte)((sbyte)a + (sbyte)b);
                    state.SP--;
                    ip++;
                    break;
                case OpCode.AddUInt8:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (byte)((byte)a + (byte)b);
                    state.SP--;
                    ip++;
                    break;
                case OpCode.AddInt16:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (short)((short)a + (short)b);
                    state.SP--;
                    ip++;
                    break;
                case OpCode.AddUInt16:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (ushort)((ushort)a + (ushort)b);
                    state.SP--;
                    ip++;
                    break;
                case OpCode.AddInt32:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (int)a + (int)b;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.AddUInt32:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (uint)a + (uint)b;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.AddInt64:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (long)a + (long)b;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.AddUInt64:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (ulong)a + (ulong)b;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.AddFloat32:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (float)a + (float)b;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.AddFloat64:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (double)a + (double)b;
                    state.SP--;
                    ip++;
                    break;

                case OpCode.SubtractInt8:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (sbyte)((sbyte)a - (sbyte)b);
                    state.SP--;
                    ip++;
                    break;
                case OpCode.SubtractUInt8:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (byte)((byte)a - (byte)b);
                    state.SP--;
                    ip++;
                    break;
                case OpCode.SubtractInt16:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (short)((short)a - (short)b);
                    state.SP--;
                    ip++;
                    break;
                case OpCode.SubtractUInt16:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (ushort)((ushort)a - (ushort)b);
                    state.SP--;
                    ip++;
                    break;
                case OpCode.SubtractInt32:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = ((int)a - (int)b);
                    state.SP--;
                    ip++;
                    break;
                case OpCode.SubtractUInt32:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (uint)((uint)a - (uint)b);
                    state.SP--;
                    ip++;
                    break;
                case OpCode.SubtractInt64:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (long)a - (long)b;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.SubtractUInt64:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (ulong)a - (ulong)b;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.SubtractFloat32:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (float)a - (float)b;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.SubtractFloat64:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (double)a - (double)b;
                    state.SP--;
                    ip++;
                    break;

                case OpCode.MultiplyInt8:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (sbyte)((sbyte)a * (sbyte)b);
                    state.SP--;
                    ip++;
                    break;
                case OpCode.MultiplyUInt8:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (byte)((byte)a * (byte)b);
                    state.SP--;
                    ip++;
                    break;
                case OpCode.MultiplyInt16:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (short)((short)a * (short)b);
                    state.SP--;
                    ip++;
                    break;
                case OpCode.MultiplyUInt16:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (ushort)((ushort)a * (ushort)b);
                    state.SP--;
                    ip++;
                    break;
                case OpCode.MultiplyInt32:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = ((int)a * (int)b);
                    state.SP--;
                    ip++;
                    break;
                case OpCode.MultiplyUInt32:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (uint)((uint)a * (uint)b);
                    state.SP--;
                    ip++;
                    break;
                case OpCode.MultiplyInt64:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (long)((long)a * (long)b);
                    state.SP--;
                    ip++;
                    break;
                case OpCode.MultiplyUInt64:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (ulong)((ulong)a * (ulong)b);
                    state.SP--;
                    ip++;
                    break;
                case OpCode.MultiplyFloat32:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (float)a * (float)b;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.MultiplyFloat64:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (double)a * (double)b;
                    state.SP--;
                    ip++;
                    break;

                case OpCode.DivideInt8:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (sbyte)((sbyte)a / (sbyte)b);
                    state.SP--;
                    ip++;
                    break;
                case OpCode.DivideUInt8:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (byte)((byte)a / (byte)b);
                    state.SP--;
                    ip++;
                    break;
                case OpCode.DivideInt16:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (short)((short)a / (short)b);
                    state.SP--;
                    ip++;
                    break;
                case OpCode.DivideUInt16:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (ushort)((ushort)a / (ushort)b);
                    state.SP--;
                    ip++;
                    break;
                case OpCode.DivideInt32:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (int)a / (int)b;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.DivideUInt32:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (uint)((uint)a / (uint)b);
                    state.SP--;
                    ip++;
                    break;
                case OpCode.DivideInt64:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (long)((long)a / (long)b);
                    state.SP--;
                    ip++;
                    break;
                case OpCode.DivideUInt64:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (ulong)((ulong)a / (ulong)b);
                    state.SP--;
                    ip++;
                    break;
                case OpCode.DivideFloat32:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (float)a / (float)b;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.DivideFloat64:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (double)a / (double)b;
                    state.SP--;
                    ip++;
                    break;

                case OpCode.ShiftLeftInt8:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (sbyte)((sbyte)a << (sbyte)b);
                    state.SP--;
                    ip++;
                    break;
                case OpCode.ShiftLeftUInt8:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (byte)((byte)a << (byte)b);
                    state.SP--;
                    ip++;
                    break;
                case OpCode.ShiftLeftInt16:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (short)((short)a << (short)b);
                    state.SP--;
                    ip++;
                    break;
                case OpCode.ShiftLeftUInt16:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (ushort)((ushort)a << (ushort)b);
                    state.SP--;
                    ip++;
                    break;
                case OpCode.ShiftLeftInt32:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (int)a << (int)b;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.ShiftLeftUInt32:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (uint)((uint)a << (int)b);
                    state.SP--;
                    ip++;
                    break;
                case OpCode.ShiftLeftInt64:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (long)((long)a << (int)(long)b);
                    state.SP--;
                    ip++;
                    break;
                case OpCode.ShiftLeftUInt64:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (ulong)((ulong)a << (int)(ulong)b);
                    state.SP--;
                    ip++;
                    break;
                    // invalid instruction in C
                case OpCode.ShiftLeftFloat32:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = ((int)(float)a << (int)(float)b);
                    state.SP--;
                    ip++;
                    break;
                // invalid instruction in C
                case OpCode.ShiftLeftFloat64:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = ((long)(double)a << (int)(double)b);
                    state.SP--;
                    ip++;
                    break;

                case OpCode.ShiftRightInt8:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (sbyte)a >> (sbyte)b;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.ShiftRightUInt8:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (byte)a >> (byte)b;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.ShiftRightInt16:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = ((short)a >> (short)b);
                    state.SP--;
                    ip++;
                    break;
                case OpCode.ShiftRightUInt16:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = ((ushort)a >> (ushort)b);
                    state.SP--;
                    ip++;
                    break;
                case OpCode.ShiftRightInt32:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = ((int)a >> (int)b);
                    state.SP--;
                    ip++;
                    break;
                case OpCode.ShiftRightUInt32:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (Value)((uint)a >> (int)b);
                    state.SP--;
                    ip++;
                    break;
                case OpCode.ShiftRightInt64:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (long)((long)a >> (int)(long)b);
                    state.SP--;
                    ip++;
                    break;
                case OpCode.ShiftRightUInt64:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (ulong)((ulong)a >> (int)(ulong)b);
                    state.SP--;
                    ip++;
                    break;
                // invalid instruction in C
                case OpCode.ShiftRightFloat32:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = ((int)(float)a >> (int)(float)b);
                    state.SP--;
                    ip++;
                    break;
                // invalid instruction in C
                case OpCode.ShiftRightFloat64:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = ((long)(double)a >> (int)(double)b);
                    state.SP--;
                    ip++;
                    break;

                case OpCode.ModuloInt16:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = ((short)a % (short)b);
                    state.SP--;
                    ip++;
                    break;
                case OpCode.ModuloUInt16:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = ((ushort)a % (ushort)b);
                    state.SP--;
                    ip++;
                    break;
                case OpCode.ModuloInt32:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = ((int)a % (int)b);
                    state.SP--;
                    ip++;
                    break;
                case OpCode.ModuloUInt32:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (Value)((uint)a % (uint)b);
                    state.SP--;
                    ip++;
                    break;
                case OpCode.ModuloInt64:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (long)((long)a % (long)b);
                    state.SP--;
                    ip++;
                    break;
                case OpCode.ModuloUInt64:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (ulong)((ulong)a % (ulong)b);
                    state.SP--;
                    ip++;
                    break;
                // invalid instruction in C
                case OpCode.ModuloFloat32:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = ((float)a % (float)b);
                    state.SP--;
                    ip++;
                    break;
                // invalid instruction in C
                case OpCode.ModuloFloat64:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = ((double)a % (double)b);
                    state.SP--;
                    ip++;
                    break;

                case OpCode.BinaryAndInt8:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (sbyte)a & (sbyte)b;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.BinaryAndUInt8:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (byte)a & (byte)b;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.BinaryAndInt16:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (short)a & (short)b;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.BinaryAndUInt16:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (ushort)a & (ushort)b;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.BinaryAndInt32:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (int)a & (int)b;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.BinaryAndUInt32:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (uint)a & (uint)b;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.BinaryAndInt64:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (long)a & (long)b;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.BinaryAndUInt64:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (ulong)a & (ulong)b;
                    state.SP--;
                    ip++;
                    break;
                // invalid instruction in C
                case OpCode.BinaryAndFloat32:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (long)(float)a & (long)(float)b;
                    state.SP--;
                    ip++;
                    break;
                // invalid instruction in C
                case OpCode.BinaryAndFloat64:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (long)(double)a & (long)(double)b;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.BinaryOrInt8:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (sbyte)a | (sbyte)b;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.BinaryOrUInt8:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (byte)a | (byte)b;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.BinaryOrInt16:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (short)a | (short)b;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.BinaryOrUInt16:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (ushort)a | (ushort)b;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.BinaryOrInt32:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (int)a | (int)b;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.BinaryOrUInt32:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (uint)a | (uint)b;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.BinaryOrInt64:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (long)a | (long)b;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.BinaryOrUInt64:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (ulong)a | (ulong)b;
                    state.SP--;
                    ip++;
                    break;
                // invalid instruction in C
                case OpCode.BinaryOrFloat32:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (long)(float)a | (long)(float)b;
                    state.SP--;
                    ip++;
                    break;
                // invalid instruction in C
                case OpCode.BinaryOrFloat64:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (long)(double)a | (long)(double)b;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.BinaryXorInt8:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (sbyte)a ^ (sbyte)b;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.BinaryXorUInt8:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (byte)a ^ (byte)b;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.BinaryXorInt16:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (short)a ^ (short)b;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.BinaryXorUInt16:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (ushort)a ^ (ushort)b;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.BinaryXorInt32:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (int)a ^ (int)b;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.BinaryXorUInt32:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (uint)a ^ (uint)b;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.BinaryXorInt64:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (long)a ^ (long)b;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.BinaryXorUInt64:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (ulong)a ^ (ulong)b;
                    state.SP--;
                    ip++;
                    break;
                // invalid instruction in C
                case OpCode.BinaryXorFloat32:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (long)(float)a ^ (long)(float)b;
                    state.SP--;
                    ip++;
                    break;
                // invalid instruction in C
                case OpCode.BinaryXorFloat64:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = (long)(double)a ^ (long)(double)b;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.EqualToInt16:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = ((short)a == (short)b) ? 1 : 0;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.EqualToUInt16:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = ((ushort)a == (ushort)b) ? 1 : 0;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.EqualToInt32:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = ((int)a == (int)b) ? 1 : 0;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.EqualToUInt32:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = ((uint)a == (uint)b) ? 1 : 0;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.EqualToFloat32:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = ((float)a == (float)b) ? 1 : 0;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.EqualToFloat64:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = ((double)a == (double)b) ? 1 : 0;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.LessThanInt16:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = ((short)a < (short)b) ? 1 : 0;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.LessThanUInt16:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = ((ushort)a < (ushort)b) ? 1 : 0;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.LessThanInt32:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = ((int)a < (int)b) ? 1 : 0;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.LessThanUInt32:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = ((uint)a < (uint)b) ? 1 : 0;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.LessThanFloat32:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = ((float)a < (float)b) ? 1 : 0;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.LessThanFloat64:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = ((double)a < (double)b) ? 1 : 0;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.GreaterThanInt16:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = ((short)a > (short)b) ? 1 : 0;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.GreaterThanUInt16:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = ((ushort)a > (ushort)b) ? 1 : 0;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.GreaterThanInt32:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = ((int)a > (int)b) ? 1 : 0;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.GreaterThanUInt32:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = ((uint)a > (uint)b) ? 1 : 0;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.GreaterThanFloat32:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = ((float)a > (float)b) ? 1 : 0;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.GreaterThanFloat64:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = ((double)a > (double)b) ? 1 : 0;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.NotInt8:
                    a = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 1] = (sbyte)a == 0 ? 1 : 0;
                    ip++;
                    break;
                case OpCode.NotUInt8:
                    a = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 1] = (byte)a == 0 ? 1 : 0;
                    ip++;
                    break;
                case OpCode.NotInt16:
                    a = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 1] = (short)a == 0 ? 1 : 0;
                    ip++;
                    break;
                case OpCode.NotUInt16:
                    a = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 1] = (ushort)a == 0 ? 1 : 0;
                    ip++;
                    break;
                case OpCode.NotInt32:
                    a = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 1] = (int)a == 0 ? 1 : 0;
                    ip++;
                    break;
                case OpCode.NotUInt32:
                    a = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 1] = (uint)a == 0 ? 1 : 0;
                    ip++;
                    break;
                case OpCode.NotInt64:
                    a = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 1] = (long)a == 0 ? 1 : 0;
                    ip++;
                    break;
                case OpCode.NotUInt64:
                    a = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 1] = (ulong)a == 0 ? 1 : 0;
                    ip++;
                    break;
                case OpCode.NotFloat32:
                    a = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 1] = (float)a == 0.0f ? 1 : 0;
                    ip++;
                    break;
                case OpCode.NotFloat64:
                    a = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 1] = (double)a == 0.0 ? 1 : 0;
                    ip++;
                    break;
                case OpCode.BinaryNotInt8:
                    a = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 1] = ~(sbyte)a;
                    ip++;
                    break;
                case OpCode.BinaryNotUInt8:
                    a = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 1] = ~(byte)a;
                    ip++;
                    break;
                case OpCode.BinaryNotInt16:
                    a = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 1] = ~(short)a;
                    ip++;
                    break;
                case OpCode.BinaryNotUInt16:
                    a = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 1] = ~(ushort)a;
                    ip++;
                    break;
                case OpCode.BinaryNotInt32:
                    a = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 1] = ~(int)a;
                    ip++;
                    break;
                case OpCode.BinaryNotUInt32:
                    a = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 1] = ~(uint)a;
                    ip++;
                    break;
                case OpCode.BinaryNotInt64:
                    a = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 1] = ~(long)a;
                    ip++;
                    break;
                case OpCode.BinaryNotUInt64:
                    a = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 1] = ~(ulong)a;
                    ip++;
                    break;
                case OpCode.BinaryNotFloat32:
                    a = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 1] = ~(int)(float)a;
                    ip++;
                    break;
                case OpCode.BinaryNotFloat64:
                    a = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 1] = ~(int)(double)a;
                    ip++;
                    break;
                case OpCode.NegateInt8:
                    a = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 1] = -(sbyte)a;
                    ip++;
                    break;
                case OpCode.NegateUInt8:
                    a = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 1] = -(byte)a;
                    ip++;
                    break;
                case OpCode.NegateInt16:
                    a = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 1] = -(short)a;
                    ip++;
                    break;
                case OpCode.NegateUInt16:
                    a = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 1] = -(ushort)a;
                    ip++;
                    break;
                case OpCode.NegateInt32:
                    a = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 1] = -(int)a;
                    ip++;
                    break;
                case OpCode.NegateUInt32:
                    a = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 1] = (Value)(-(uint)a);
                    ip++;
                    break;
                case OpCode.NegateInt64:
                    a = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 1] = -(long)a;
                    ip++;
                    break;
                case OpCode.NegateUInt64:
                    a = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 1] = (Value)((ulong)-(long)a);
                    ip++;
                    break;
                case OpCode.NegateFloat32:
                    a = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 1] = (Value)(-(float)a);
                    ip++;
                    break;
                case OpCode.NegateFloat64:
                    a = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 1] = (Value)(-(double)a);
                    ip++;
                    break;
                case OpCode.LogicalAnd:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = ((a.Int32Value != 0) && (b.Int32Value != 0)) ? 1 : 0;
                    state.SP--;
                    ip++;
                    break;
                case OpCode.LogicalOr:
                    a = state.Stack[state.SP - 2];
                    b = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 2] = ((a.Int32Value != 0) || (b.Int32Value != 0)) ? 1 : 0;
                    state.SP--;
                    ip++;
                    break;
                default:
                    a = state.Stack[state.SP - 1];
                    state.Stack[state.SP - 1] = Convert (a, i.Op);
                    ip++;
                    break;
            }

            state.RemainingTime -= state.CpuSpeed;
        }

        frame.IP = ip;

        if (ip >= Instructions.Count) {
            throw new ExecutionException ("Function '" + Name + "' never returned.");
        }
    }

    Value Convert (Value x, OpCode op) => op switch {
        OpCode.ConvertInt8Int8 => (Value)(sbyte)(sbyte)x,
        OpCode.ConvertInt8UInt8 => (Value)(byte)(sbyte)x,
        OpCode.ConvertInt8Int16 => (Value)(short)(sbyte)x,
        OpCode.ConvertInt8UInt16 => (Value)(ushort)(sbyte)x,
        OpCode.ConvertInt8Int32 => (Value)(int)(sbyte)x,
        OpCode.ConvertInt8UInt32 => (Value)(uint)(sbyte)x,
        OpCode.ConvertInt8Int64 => (Value)(long)(sbyte)x,
        OpCode.ConvertInt8UInt64 => (Value)(ulong)(sbyte)x,
        OpCode.ConvertInt8Float32 => (Value)(float)(sbyte)x,
        OpCode.ConvertInt8Float64 => (Value)(double)(sbyte)x,
        OpCode.ConvertUInt8Int8 => (Value)(sbyte)(byte)x,
        OpCode.ConvertUInt8UInt8 => (Value)(byte)(byte)x,
        OpCode.ConvertUInt8Int16 => (Value)(short)(byte)x,
        OpCode.ConvertUInt8UInt16 => (Value)(ushort)(byte)x,
        OpCode.ConvertUInt8Int32 => (Value)(int)(byte)x,
        OpCode.ConvertUInt8UInt32 => (Value)(uint)(byte)x,
        OpCode.ConvertUInt8Int64 => (Value)(long)(byte)x,
        OpCode.ConvertUInt8UInt64 => (Value)(ulong)(byte)x,
        OpCode.ConvertUInt8Float32 => (Value)(float)(byte)x,
        OpCode.ConvertUInt8Float64 => (Value)(double)(byte)x,
        OpCode.ConvertInt16Int8 => (Value)(sbyte)(short)x,
        OpCode.ConvertInt16UInt8 => (Value)(byte)(short)x,
        OpCode.ConvertInt16Int16 => (Value)(short)(short)x,
        OpCode.ConvertInt16UInt16 => (Value)(ushort)(short)x,
        OpCode.ConvertInt16Int32 => (Value)(int)(short)x,
        OpCode.ConvertInt16UInt32 => (Value)(uint)(short)x,
        OpCode.ConvertInt16Int64 => (Value)(long)(short)x,
        OpCode.ConvertInt16UInt64 => (Value)(ulong)(short)x,
        OpCode.ConvertInt16Float32 => (Value)(float)(short)x,
        OpCode.ConvertInt16Float64 => (Value)(double)(short)x,
        OpCode.ConvertUInt16Int8 => (Value)(sbyte)(ushort)x,
        OpCode.ConvertUInt16UInt8 => (Value)(byte)(ushort)x,
        OpCode.ConvertUInt16Int16 => (Value)(short)(ushort)x,
        OpCode.ConvertUInt16UInt16 => (Value)(ushort)(ushort)x,
        OpCode.ConvertUInt16Int32 => (Value)(int)(ushort)x,
        OpCode.ConvertUInt16UInt32 => (Value)(uint)(ushort)x,
        OpCode.ConvertUInt16Int64 => (Value)(long)(ushort)x,
        OpCode.ConvertUInt16UInt64 => (Value)(ulong)(ushort)x,
        OpCode.ConvertUInt16Float32 => (Value)(float)(ushort)x,
        OpCode.ConvertUInt16Float64 => (Value)(double)(ushort)x,
        OpCode.ConvertInt32Int8 => (Value)(sbyte)(int)x,
        OpCode.ConvertInt32UInt8 => (Value)(byte)(int)x,
        OpCode.ConvertInt32Int16 => (Value)(short)(int)x,
        OpCode.ConvertInt32UInt16 => (Value)(ushort)(int)x,
        OpCode.ConvertInt32Int32 => (Value)(int)(int)x,
        OpCode.ConvertInt32UInt32 => (Value)(uint)(int)x,
        OpCode.ConvertInt32Int64 => (Value)(long)(int)x,
        OpCode.ConvertInt32UInt64 => (Value)(ulong)(int)x,
        OpCode.ConvertInt32Float32 => (Value)(float)(int)x,
        OpCode.ConvertInt32Float64 => (Value)(double)(int)x,
        OpCode.ConvertUInt32Int8 => (Value)(sbyte)(uint)x,
        OpCode.ConvertUInt32UInt8 => (Value)(byte)(uint)x,
        OpCode.ConvertUInt32Int16 => (Value)(short)(uint)x,
        OpCode.ConvertUInt32UInt16 => (Value)(ushort)(uint)x,
        OpCode.ConvertUInt32Int32 => (Value)(int)(uint)x,
        OpCode.ConvertUInt32UInt32 => (Value)(uint)(uint)x,
        OpCode.ConvertUInt32Int64 => (Value)(long)(uint)x,
        OpCode.ConvertUInt32UInt64 => (Value)(ulong)(uint)x,
        OpCode.ConvertUInt32Float32 => (Value)(float)(uint)x,
        OpCode.ConvertUInt32Float64 => (Value)(double)(uint)x,
        OpCode.ConvertInt64Int8 => (Value)(sbyte)(long)x,
        OpCode.ConvertInt64UInt8 => (Value)(byte)(long)x,
        OpCode.ConvertInt64Int16 => (Value)(short)(long)x,
        OpCode.ConvertInt64UInt16 => (Value)(ushort)(long)x,
        OpCode.ConvertInt64Int32 => (Value)(int)(long)x,
        OpCode.ConvertInt64UInt32 => (Value)(uint)(long)x,
        OpCode.ConvertInt64Int64 => (Value)(long)(long)x,
        OpCode.ConvertInt64UInt64 => (Value)(ulong)(long)x,
        OpCode.ConvertInt64Float32 => (Value)(float)(long)x,
        OpCode.ConvertInt64Float64 => (Value)(double)(long)x,
        OpCode.ConvertUInt64Int8 => (Value)(sbyte)(ulong)x,
        OpCode.ConvertUInt64UInt8 => (Value)(byte)(ulong)x,
        OpCode.ConvertUInt64Int16 => (Value)(short)(ulong)x,
        OpCode.ConvertUInt64UInt16 => (Value)(ushort)(ulong)x,
        OpCode.ConvertUInt64Int32 => (Value)(int)(ulong)x,
        OpCode.ConvertUInt64UInt32 => (Value)(uint)(ulong)x,
        OpCode.ConvertUInt64Int64 => (Value)(long)(ulong)x,
        OpCode.ConvertUInt64UInt64 => (Value)(ulong)(ulong)x,
        OpCode.ConvertUInt64Float32 => (Value)(float)(ulong)x,
        OpCode.ConvertUInt64Float64 => (Value)(double)(ulong)x,
        OpCode.ConvertFloat32Int8 => (Value)(sbyte)(float)x,
        OpCode.ConvertFloat32UInt8 => (Value)(byte)(float)x,
        OpCode.ConvertFloat32Int16 => (Value)(short)(float)x,
        OpCode.ConvertFloat32UInt16 => (Value)(ushort)(float)x,
        OpCode.ConvertFloat32Int32 => (Value)(int)(float)x,
        OpCode.ConvertFloat32UInt32 => (Value)(uint)(float)x,
        OpCode.ConvertFloat32Int64 => (Value)(long)(float)x,
        OpCode.ConvertFloat32UInt64 => (Value)(ulong)(float)x,
        OpCode.ConvertFloat32Float32 => (Value)(float)(float)x,
        OpCode.ConvertFloat32Float64 => (Value)(double)(float)x,
        OpCode.ConvertFloat64Int8 => (Value)(sbyte)(double)x,
        OpCode.ConvertFloat64UInt8 => (Value)(byte)(double)x,
        OpCode.ConvertFloat64Int16 => (Value)(short)(double)x,
        OpCode.ConvertFloat64UInt16 => (Value)(ushort)(double)x,
        OpCode.ConvertFloat64Int32 => (Value)(int)(double)x,
        OpCode.ConvertFloat64UInt32 => (Value)(uint)(double)x,
        OpCode.ConvertFloat64Int64 => (Value)(long)(double)x,
        OpCode.ConvertFloat64UInt64 => (Value)(ulong)(double)x,
        OpCode.ConvertFloat64Float32 => (Value)(float)(double)x,
        OpCode.ConvertFloat64Float64 => (Value)(double)(double)x,
        _ => throw new NotSupportedException ($"Op code '{op}' is not supported"),
    };
}

