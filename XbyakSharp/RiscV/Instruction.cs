using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XbyakSharp.RiscV
{
    public record class RiscVInstruction : Instruction
    {
        public static readonly List<RiscVInstruction> Instructions 
            = Extract(typeof(CodeGenerator), typeof(RiscVInstruction))
            .Cast<RiscVInstruction>().ToList();
    }
}
