using System;
using System.IO;
using System.Text;

namespace SharpLua
{
    public class CodeGenerator : IDisposable
    {
        protected TextWriter writer = null;
        protected uint functionCount = 0;
        protected bool shouldDispose = false;
        public CodeGenerator(string path)
        {
            this.writer = new StreamWriter(path)
            {
                NewLine = "\n",
                AutoFlush = true
            };
        }
        public CodeGenerator(TextWriter writer, bool shouldDispose=false)
        {
            this.writer = writer;
            this.shouldDispose = shouldDispose;
        }

        public void Write(Function function, int indentLevel = 0)
        {
            // top level function
            if (function.lineNumber == 0 && function.lastLineNumber == 0)
            {
                //WriteConstants(function);
                this.WriteChildFunctions(function);
                this.WriteInstructions(function);
            }
            else
            {
                var indents = new string('\t', indentLevel);
                var functionHeader = indents + "function func" + functionCount + "(";
                for (int i = 0; i < function.numParameters; ++i)
                    functionHeader += "arg" + i + 
                        (i + 1 != function.numParameters ? ", " : ")");

                this.writer.Write(functionHeader);
                ++this.functionCount;
                //WriteConstants(function, indentLevel + 1);
                this.WriteChildFunctions(function, indentLevel + 1);
                this.WriteInstructions(function, indentLevel + 1);
            }
        }

        public void Dispose()
        {
            if (this.shouldDispose && this.writer !=null)
            {
                this.writer.Dispose();
                this.writer = null;
            }
        }

        private void WriteConstants(Function function, int indentLevel = 0)
        {
            var constCount = 0;
            var indents = new string('\t', indentLevel);

            foreach (var c in function.constants)
            {
                this.writer.WriteLine("{2}const{0} = {1}", constCount, c.ToString(), indents);
                ++constCount;
            }
        }

        private void WriteChildFunctions(Function function, int indentLevel = 0) 
            => function.functions.ForEach(child =>
                this.Write(child, indentLevel + 1));

        private void WriteInstructions(Function function, int indentLevel = 0)
        {
            var indents = new string('\t', indentLevel);
            foreach (var i in function.instructions)
            {
                switch (i.OpCode)
                {
                    case LvmInstruction.Op.Move:
                        this.writer.WriteLine("{2}var{0} = var{1}", i.A, i.B, indents);
                        break;

                    case LvmInstruction.Op.LoadK:
                        this.writer.WriteLine("{2}var{0} = {1}", i.A, GetConstant(i.Bx, function), indents);
                        break;

                    case LvmInstruction.Op.LoadBool:
                        this.writer.WriteLine("{2}var{0} = {1}", i.A, (i.B != 0 ? "true" : "false"), indents);
                        break;

                    case LvmInstruction.Op.LoadNil:
                        for (int x = i.A; x < i.B + 1; ++x)
                            this.writer.WriteLine("{1}var{0} = nil", x, indents);
                        break;

                    case LvmInstruction.Op.GetUpVal:
                        this.writer.WriteLine("{2}var{0} = upvalue[{1}]", i.A, i.B, indents);
                        break;

                    case LvmInstruction.Op.GetGlobal:
                        this.writer.WriteLine("{2}var{0} = _G[{1}]", i.A, GetConstant(i.Bx, function), indents);
                        break;

                    case LvmInstruction.Op.GetTable:
                        this.writer.WriteLine("{3}var{0} = var{1}[{2}]", i.A, i.B, WriteIndex(i.C, function), indents);
                        break;

                    case LvmInstruction.Op.SetGlobal:
                        this.writer.WriteLine("{2}_G[{0}] = var{1}", GetConstant(i.Bx, function), i.A, indents);
                        break;

                    case LvmInstruction.Op.SetUpVal:
                        this.writer.WriteLine("{2}upvalue[{0}] = var{1}", i.B, i.A, indents);
                        break;

                    case LvmInstruction.Op.SetTable:
                        this.writer.WriteLine("{3}var{0}[{1}] = {2}", i.A, WriteIndex(i.B, function), WriteIndex(i.C, function), indents);
                        break;

                    case LvmInstruction.Op.NewTable:
                        this.writer.WriteLine("{1}var{0} = {{}}", i.A, indents);
                        break;

                    case LvmInstruction.Op.Self:
                        this.writer.WriteLine("{2}var{0} = var{1}", i.A + 1, i.B, indents);
                        this.writer.WriteLine("{3}var{0} = var{1}[{2}]", i.A, i.B, WriteIndex(i.C, function), indents);
                        break;

                    case LvmInstruction.Op.Add:
                        this.writer.WriteLine("{3}var{0} = var{1} + var{2}", i.A, i.B, i.C, indents);
                        break;

                    case LvmInstruction.Op.Sub:
                        this.writer.WriteLine("{3}var{0} = var{1} - var{2}", i.A, i.B, i.C, indents);
                        break;

                    case LvmInstruction.Op.Mul:
                        this.writer.WriteLine("{3}var{0} = var{1} * var{2}", i.A, i.B, i.C, indents);
                        break;

                    case LvmInstruction.Op.Div:
                        this.writer.WriteLine("{3}var{0} = var{1} / var{2}", i.A, i.B, i.C, indents);
                        break;

                    case LvmInstruction.Op.Mod:
                        this.writer.WriteLine("{3}var{0} = var{1} % var{2}", i.A, i.B, i.C, indents);
                        break;

                    case LvmInstruction.Op.Pow:
                        this.writer.WriteLine("{3}var{0} = var{1} ^ var{2}", i.A, i.B, i.C, indents);
                        break;

                    case LvmInstruction.Op.Unm:
                        this.writer.WriteLine("{2}var{0} = -var{1}", i.A, i.B, indents);
                        break;

                    case LvmInstruction.Op.Not:
                        this.writer.WriteLine("{2}var{0} = not var{1}", i.A, i.B, indents);
                        break;

                    case LvmInstruction.Op.Len:
                        this.writer.WriteLine("{2}var{0} = #var{1}", i.A, i.B, indents);
                        break;

                    case LvmInstruction.Op.Concat:
                        this.writer.Write("{1}var{0} = ", i.A, indents);

                        for (int x = i.B; x < i.C; ++x)
                            this.writer.Write("var{0} .. ", x);

                        this.writer.WriteLine("var{0}", i.C);
                        break;

                    case LvmInstruction.Op.Jmp:
                        break;
                        //throw new NotImplementedException("Jmp");

                    case LvmInstruction.Op.Eq:
                        this.writer.WriteLine("{3}if ({0} == {1}) ~= {2} then", WriteIndex(i.B, function), WriteIndex(i.C, function), i.A, indents);
                        break;

                    case LvmInstruction.Op.Lt:
                        this.writer.WriteLine("{3}if ({0} < {1}) ~= {2} then", WriteIndex(i.B, function), WriteIndex(i.C, function), i.A, indents);
                        break;

                    case LvmInstruction.Op.Le:
                        this.writer.WriteLine("{3}if ({0} <= {1}) ~= {2} then", WriteIndex(i.B, function), WriteIndex(i.C, function), i.A, indents);
                        break;

                    case LvmInstruction.Op.Test:
                        this.writer.WriteLine("{2}if not var{0} <=> {1} then", i.A, i.C, indents);
                        break;

                    case LvmInstruction.Op.TestSet:
                        this.writer.WriteLine("{2}if var{0} <=> {1} then", i.B, i.C, indents);
                        this.writer.WriteLine("{2}\tvar{0} = var{1}", i.A, i.B, indents);
                        this.writer.WriteLine("end");
                        break;

                    case LvmInstruction.Op.Call:
                        var builder = new StringBuilder();
                        if (i.C != 0)
                        {
                            builder.Append(indents);
                            var indentLen = builder.Length;

                            // return values
                            for (int x = i.A; x < i.A + i.C - 2; ++x)
                                builder.AppendFormat("var{0}, ", x);

                            if (builder.Length - indentLen > 2)
                            {
                                builder.Remove(builder.Length - 2, 2);
                                builder.Append(" = ");
                            }
                        }
                        else
                        {
                            break;
                            //throw new NotImplementedException("i.C == 0");
                        }

                        // function
                        builder.AppendFormat("var{0}(", i.A);

                        if (i.B != 0)
                        {
                            var preArgsLen = builder.Length;

                            // arguments
                            for (int x = i.A; x < i.A + i.B - 1; ++x)
                                builder.AppendFormat("var{0}, ", x + 1);

                            if (builder.Length - preArgsLen > 2)
                                builder.Remove(builder.Length - 2, 2);

                            builder.Append(')');
                        }
                        else
                        {
                            break;
                            //throw new NotImplementedException("i.B == 0");
                        }

                        this.writer.WriteLine(builder.ToString());

                        break;

                    case LvmInstruction.Op.TailCall:
                        break;
                        //throw new NotImplementedException("TailCall");

                    case LvmInstruction.Op.Return:
                        this.writer.WriteLine("return");
                        break;

                    case LvmInstruction.Op.ForLoop:
                        break;
                        //throw new NotImplementedException("ForLoop");

                    case LvmInstruction.Op.ForPrep:
                        break;
                        //throw new NotImplementedException("ForPrep");

                    case LvmInstruction.Op.TForLoop:
                        break;
                        //throw new NotImplementedException("TForLoop");

                    case LvmInstruction.Op.SetList:
                        break;
                        //throw new NotImplementedException("SetList");

                    case LvmInstruction.Op.Close:
                        break;
                        //throw new NotImplementedException("Close");

                    case LvmInstruction.Op.Closure:
                        break;
                        //throw new NotImplementedException("Closure");

                    case LvmInstruction.Op.VarArg:
                        break;
                        // throw new NotImplementedException("VarArg");
                }
            }
        }

        private string GetConstant(int idx, Function function)
            => function.constants[idx].ToString();

        private int ToIndex(int value, out bool isConstant) =>
            // this is the logic from lua's source code (lopcodes.h)
            (isConstant = (value & 1 << 8) != 0) ? value & ~(1 << 8) : value;

        private string WriteIndex(int value, Function function) 
            => ToIndex(value, out var constant) is int idx && constant ? function.constants[idx].ToString() : "var" + idx;
    }
}
