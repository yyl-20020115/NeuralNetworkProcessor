using System.Collections.Generic;
using System.Linq;
using CLanguage.Types;
using CLanguage.Interpreter;
using CLanguage.Compiler;

namespace CLanguage.Syntax;

public class Block : Statement
{
    public VariableScope VariableScope { get; }
    public List<Statement> Statements { get; } = [];

    public Block? Parent { get; set; }

    public override bool AlwaysReturns => Statements.Any (s => s.AlwaysReturns);

    public List<CompiledVariable> Variables { get; private set; } = [];
    public List<CompiledFunction> Functions { get; private set; } = [];
    public Dictionary<string, CType> Typedefs { get; private set; } = [];
    public List<Statement> InitStatements { get; private set; } = [];
    public Dictionary<string, CStructType> Structures { get; private set; } = [];
    public Dictionary<string, CEnumType> Enums { get; private set; } = [];

    public Block (VariableScope variableScope, IEnumerable<Statement> statements)
    {
        AddStatements (statements);
        VariableScope = variableScope;
    }

    public Block (VariableScope variableScope)
    {
        VariableScope = variableScope;
    }

    public override string ToString ()
    {
        return InitStatements.Count > 0
            ? $"{{[{string.Join ("; ", InitStatements)}] {string.Join("; ", Statements)}}}"
            : $"{{{string.Join("; ", Statements)}}}";
    }

    public void AddStatement (Statement? stmt)
    {
        if (stmt != null)
            Statements.Add (stmt);

        if (stmt is Block block) {
            block.Parent = this;
        }
    }

    public void AddStatements (IEnumerable<Statement> stmts)
    {
        foreach (var s in stmts) {
            AddStatement (s);
        }
    }

    protected override void DoEmit (EmitContext ec)
    {
        ec.BeginBlock (this);
        foreach (var s in Statements) {
            s.Emit (ec);
        }
        ec.EndBlock ();
    }

    public void AddVariable (string name, CType ctype)
    {
        Variables.Add (new CompiledVariable (name, 0, ctype));
    }

    public override void AddDeclarationToBlock (BlockContext context)
    {
        var subContext = new BlockContext (this, context);
        foreach (var s in Statements) {
            s.AddDeclarationToBlock (subContext);
        }
    }
}
