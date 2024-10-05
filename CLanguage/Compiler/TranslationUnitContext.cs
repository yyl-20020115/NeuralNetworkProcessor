using CLanguage.Syntax;
using CLanguage.Types;
using System.Linq;

namespace CLanguage.Compiler;

public class TranslationUnitContext (TranslationUnit translationUnit, ExecutableContext exeContext) : BlockContext(translationUnit, exeContext)
{
    public TranslationUnit TranslationUnit { get; } = translationUnit;

    public override CType ResolveTypeName (string typeName) => TranslationUnit.Typedefs.TryGetValue (typeName, out var tt)
            ? tt
            : TranslationUnit.Structures.TryGetValue (typeName, out var st) ? st : base.ResolveTypeName (typeName);

    public override ResolvedVariable? TryResolveVariable (string name, CType[]? argTypes)
    {
        // HACK: There is some confusion about when and why we're looking up variables
        // Sometimes, we just need the type when we're building types
        // Other times, we need its real memory address
        // This function can provide the type, but not the address
        // So we ask our parent (which is probably a ExeContext) for the variable first
        var v = ParentContext?.TryResolveVariable (name, argTypes);
        if (v != null)
            return v;

        foreach (var e in TranslationUnit.Enums) {
            var em = e.Value.Members.FirstOrDefault (x => x.Name == name);
            if (em != null) {
                return new ResolvedVariable (em.Value, e.Value);
            }
        }

        return base.TryResolveVariable (name, argTypes);
    }
}
