using CLanguage.Compiler;
using System.Collections.Generic;

namespace CLanguage.Types;

public class CEnumType (string name) : CType
{
    public string Name { get; set; } = name;
    public List<CEnumMember> Members { get; set; } = [];

    public int NextValue => Members.Count > 0 ? Members[Members.Count - 1].Value + 1 : 0;

    public override int NumValues => 1;

    public override bool IsIntegral => true;

    public override int GetByteSize (EmitContext c) => CBasicType.SignedInt.GetByteSize (c);
}
