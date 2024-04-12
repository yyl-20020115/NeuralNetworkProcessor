using System.Diagnostics.CodeAnalysis;

namespace NNP.Core;

public class TrendComparer : IEqualityComparer<Trend>
{
    public static readonly TrendComparer Default = new();
    public bool Equals(Trend? x, Trend? y)
    {
        if (x is null && y is null) return true;
        if (x is null && y is not null || y is not null && x is null) return false;
        return x is not null && y is not null && x.Index==y.Index && x.Name==y.Name;
    }
    public int GetHashCode([DisallowNull] Trend trend) => trend.GetHashCode();
}

public class TrendHashSet : HashSet<Trend>
{
    public TrendHashSet() : base(TrendComparer.Default) { }
    public TrendHashSet(IEnumerable<Trend> set) : base(set,TrendComparer.Default) { }
}