using System;
using System.Collections.Generic;

namespace SharpLua
{
    public class Function
    {
        [Flags]
        public enum VarArg : uint
        {
            None = 0,
            Has = 1,
            Is = 2,
            Needs = 4,
        }

        public string sourceName = "";
        public int lineNumber = 0;
        public int lastLineNumber = 0;
        public byte numUpvalues = 0;
        public byte numParameters = 0;
        public VarArg varArgFlag = 0;
        public byte maxStackSize = 0;

        public List<LvmInstruction> instructions;
        public List<Constant> constants;
        public List<Function> functions;

        // Debug data
        public List<int> sourceLinePositions;
        public List<Local> locals;
        public List<string> upvalues;
    }
}
