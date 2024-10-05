using CLanguage.Compiler;

namespace CLanguage.Syntax;

public abstract class Statement
{
    public Location Location { get; protected set; }

    public void Emit (EmitContext ec)
    {
        DoEmit (ec);
    }

    protected abstract void DoEmit (EmitContext ec);

    public abstract bool AlwaysReturns { get; }

    public Block ToBlock ()
    {
        if (this is Block b)
            return b;
        b = new Block (VariableScope.Local);
        b.AddStatement (this);
        return b;
    }

    public abstract void AddDeclarationToBlock (BlockContext context);
}
