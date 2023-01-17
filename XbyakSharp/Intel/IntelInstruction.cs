namespace XbyakSharp.Intel
{
    public record class IntelInstruction :Instruction
    {
        public static readonly List<Instruction> Instructions 
            = Extract(typeof(CodeGenerator),typeof(IntelInstruction));
    }
}
