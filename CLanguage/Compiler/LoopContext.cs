using System;
using CLanguage.Interpreter;

namespace CLanguage.Compiler;

public class LoopContext (Label breakLabel, Label? continueLabel, EmitContext parentContext) : EmitContext(parentContext)
{
    public Label LoopBreakLabel { get; } = breakLabel ?? throw new ArgumentNullException (nameof (breakLabel));
    public Label? LoopContinueLabel { get; } = continueLabel;

    public override Label? BreakLabel => LoopBreakLabel;
    public override Label? ContinueLabel => LoopContinueLabel ?? ParentContext?.ContinueLabel;
}
