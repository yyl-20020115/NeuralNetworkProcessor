using System.Diagnostics.CodeAnalysis;

namespace NNP.Core;

public class PhaseComparer : IEqualityComparer<Phase>
{
    public static readonly PhaseComparer Default = new();
    public bool Equals(Phase? x, Phase? y)
    {
        if (x is null && y is null) return true;
        if (x is null && y is not null || y is not null && x is null) return false;
        return x is not null && y is not null && x.Identity == y.Identity && x.Name == y.Name;
    }
    public int GetHashCode([DisallowNull] Phase phase) => phase.GetHashCode();
}

public class PhaseHashSet : HashSet<Phase>
{
    public PhaseHashSet() : base(PhaseComparer.Default) { }
}