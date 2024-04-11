using NNP.ZRF;
using System.Text;

namespace NNP.Core;

public class Trend(string Name = "", int Index = -1, Description? description = null)
{
    public readonly string Name = Name;
    public readonly int Index = Index;
    public readonly List<Phase> Line = [];
    public readonly HashSet<Phase> Targets = [];
    public readonly Description Description = description ?? Description.Default;

    public StringBuilder Flattern(StringBuilder builder)
    {
        foreach(var phase in Line) phase.Flattern(builder);
        return builder;
    }
    public string Flattern()
        => this.Flattern(new()).ToString();

    public override string ToString()
        => $"{this.Name} : {string.Join(", ",this.Line)}";
}
