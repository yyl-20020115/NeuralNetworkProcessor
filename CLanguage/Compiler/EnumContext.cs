using CLanguage.Syntax;
using CLanguage.Types;
using System.Linq;

namespace CLanguage.Compiler;

public class EnumContext (TypeSpecifier enumTs, CEnumType et, EmitContext parentContext) : EmitContext(parentContext)
{
    private readonly TypeSpecifier enumTs = enumTs;
    private readonly CEnumType et = et;
    private readonly EmitContext emitContext = parentContext;

    public override ResolvedVariable? TryResolveVariable (string name, CType[]? argTypes)
    {
        var r = et.Members.FirstOrDefault (x => x.Name == name);
        return r != null ? new ResolvedVariable ((Value)r.Value, et) : base.TryResolveVariable (name, argTypes);
    }
}
