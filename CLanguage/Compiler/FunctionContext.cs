using System.Collections.Generic;
using System.Linq;
using CLanguage.Syntax;
using CLanguage.Types;
using CLanguage.Interpreter;

namespace CLanguage.Compiler;

class FunctionContext (Executable exe, CompiledFunction fexe, EmitContext parentContext) : BlockContext(fexe.Body ?? new Block (VariableScope.Local),
            parentContext.MachineInfo, parentContext.Report, fexe,
            parentContext)
{
    private readonly Executable exe = exe;
    private readonly CompiledFunction fexe = fexe;

    class BlockLocals
    {
        public int StartIndex;
        public int Length;
    }

    private readonly List<Block> blocks = [];
    readonly Dictionary<Block, BlockLocals> blockLocals = [];
    readonly List<CompiledVariable> allLocals = [];

    public IEnumerable<CompiledVariable> LocalVariables => allLocals;

    public override string ToString () => $"{fexe} function context";

    public override CType ResolveTypeName (string typeName)
    {
        //
        // Look for local types
        //
        foreach (var b in blocks.Reverse<Block> ()) {
            if (b.Typedefs.TryGetValue (typeName, out var t))
                return t;
        }

        //
        // Look for global types
        //
        //foreach (var t in exe.GlobalTypes) {
        //    if (t.Name == typeName) {
        //        return t.Type;
        //    }
        //}

        return base.ResolveTypeName (typeName);
    }

    public override ResolvedVariable? TryResolveVariable (string name, CType[]? argTypes)
    {
        //
        // Look for function parameters
        //
        for (var i = 0; i < fexe.FunctionType.Parameters.Count; i++) {
            var p = fexe.FunctionType.Parameters[i];
            if (p.Name == name) {
                return new ResolvedVariable (VariableScope.Arg, p.Offset, fexe.FunctionType.Parameters[i].ParameterType);
            }
        }

        //
        // Look for locals
        //
        foreach (var b in blocks.Reverse<Block> ()) {
            var blocals = blockLocals[b];
            for (var i = 0; i < blocals.Length; i++) {
                var j = blocals.StartIndex + i;
                if (allLocals[j].Name == name) {
                    return new ResolvedVariable (VariableScope.Local, allLocals[j].StackOffset, allLocals[j].VariableType);
                }
            }
        }

        //
        // This?
        //
        return name == "this" && fexe.FunctionType.IsInstance && fexe.FunctionType.DeclaringType is CStructType dtype
            ? new ResolvedVariable (VariableScope.Arg, -1, dtype.Pointer)
            : base.TryResolveVariable (name, argTypes);
    }

    public override void BeginBlock (Block b)
    {
        blocks.Add (b);
        var locs = new BlockLocals {
            StartIndex = allLocals.Count,
            Length = b.Variables.Count,
        };
        blockLocals[b] = locs;
        allLocals.AddRange (b.Variables);

        var offset = 0;
        foreach (var v in allLocals) {
            v.StackOffset = offset;
            offset += v.VariableType.NumValues;
        }
    }

    public override void EndBlock () => blocks.RemoveAt (blocks.Count - 1);

    public override Label DefineLabel () => new();

    public override void EmitLabel (Label l) => l.Index = fexe.Instructions.Count;

    public override void Emit (Instruction instruction) => fexe.Instructions.Add (instruction);

    public override Value GetConstantMemory (string stringConstant) => exe.GetConstantMemory (stringConstant);
}
