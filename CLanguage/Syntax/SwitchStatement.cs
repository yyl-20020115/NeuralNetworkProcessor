using System;
using System.Collections.Generic;

using CLanguage.Interpreter;
using CLanguage.Compiler;

namespace CLanguage.Syntax;

public class SwitchStatement : Statement
{
    public Expression Value { get; private set; }
    public List<SwitchCase> Cases { get; private set; }

    public SwitchStatement (Expression value, List<SwitchCase> cases, Location loc)
    {
        Value = value ?? throw new ArgumentNullException (nameof (value));
        Cases = cases ?? throw new ArgumentNullException (nameof (cases));
        Location = loc;
    }

    protected override void DoEmit (EmitContext initialContext)
    {
        var valueType = Value.GetEvaluatedCType (initialContext);

        Value.Emit (initialContext);

        if (Cases.Count == 0) {
            initialContext.Emit (OpCode.Pop);
            return;
        }

        var caseLabels = new List<Label> ();
        Label? defaultLabel = null;
        foreach (var c in Cases) {
            var caseLabel = initialContext.DefineLabel ();
            caseLabels.Add (caseLabel);
            if (c.Value is null) {
                if (defaultLabel is not null) {
                    initialContext.Report.Error (139, "Duplicate default labels in switch");
                }
                defaultLabel = caseLabel;
            }
        }
        var endLabel = initialContext.DefineLabel ();

        var ec = initialContext.PushLoop (breakLabel: endLabel, continueLabel: null);

        // Emit case tests
        var ioff = ec.GetInstructionOffset (valueType);
        var eqOp = (OpCode)(OpCode.EqualToInt8 + ioff);
        for (var ci = 0; ci < Cases.Count; ci++) {
            var c = Cases[ci];
            var caseLabel = caseLabels[ci];
            if (c.Value is null)
                continue;
            ec.Emit (OpCode.Dup);
            c.Value.Emit (ec);
            ec.EmitCast (c.Value.GetEvaluatedCType (ec), valueType);
            ec.Emit (eqOp);
            ec.Emit (OpCode.BranchIfTrue, caseLabel);
        }
        if (defaultLabel is object) {
            ec.Emit (OpCode.Pop);
            ec.Emit (OpCode.Jump, defaultLabel);
        }
        else {
            ec.Emit (OpCode.Pop);
            ec.Emit (OpCode.Jump, endLabel);
        }

        // Emit case statements
        for (var ci = 0; ci < Cases.Count; ci++) {
            var c = Cases[ci];
            var caseLabel = caseLabels[ci];
            ec.EmitLabel (caseLabel);
            foreach (var s in c.Statements) {
                s.Emit (ec);
            }
        }

        ec.EmitLabel (endLabel);
    }

    public override string ToString () => $"switch ({Value}) {string.Join ("", Cases)};";

    public override void AddDeclarationToBlock (BlockContext context)
    {
        foreach (var c in Cases) {
            foreach (var s in c.Statements) {
                s.AddDeclarationToBlock (context);
            }
        }
    }

    public override bool AlwaysReturns => false;
}

public class SwitchCase (Expression? value, List<Statement> statements)
{
    public Expression? Value { get; private set; } = value;
    public List<Statement> Statements { get; private set; } = statements;
}
