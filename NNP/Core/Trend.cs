namespace NNP.Core;

public class Trend(string Name = "", int Index = -1)
{
    public readonly string Name = Name;
    public readonly int Index = Index;
    public readonly List<Phase> Line = [];
    public readonly HashSet<Phase> Targets = [];
    public override string ToString()
        => $"{this.Name} : {string.Join(", ",this.Line)}";
}

