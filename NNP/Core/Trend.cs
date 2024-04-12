using NNP.ZRF;
using System.Text;

namespace NNP.Core;

public record class Trend(string Name = "", Description? Description = null, bool IsLex = false, Phase? Target = null)
{
    public static int IndexBase { get; protected set; } = 0;
    public readonly int Index = IndexBase++;
    public readonly string Name = Name;
    public readonly List<Phase> Line = [];
    public readonly PhaseHashSet Targets = Target != null
        ? [Target] 
        : [];

    public readonly Description Description = Description ?? Description.Default;
    public readonly bool IsLex = IsLex;
    public readonly HashSet<string> BranchNames = [];
    public int StartPosition = 0;
    public int EndPosition = 0;
    public int Progress = 0;
    public bool IsComplete => this.Progress == this.Line.Count;
    public bool IsInitiator(Phase phase) => this.Line.Count > 0 && this.Line[0] == phase;
    public bool IsAnyInitiator(HashSet<Phase> phases) => phases.Any(phase => this.IsInitiator(phase));

    public bool IsBranch(Trend trend) => trend.Name != this.Name && this.BranchNames.Contains(trend.Name);

    public HashSet<Trend> GetBranches(HashSet<Trend> trends)
        => trends.Where(this.IsBranch).ToHashSet();

    public Trend UpdatePosition()
    {

        return this;
    }
    public bool Advance(HashSet<Phase> bullets, int position)
    {
        var progress = 0;
        for (int i = 0; i < Line.Count; i++)
        {
            var p = Line[i];
            progress++;
            if (!p.Parents.Select(p => p.Index).Contains(this.Index)) continue;

            if (bullets.Select(b => b.Index).Contains(p.Index))
            {
                this.Progress = progress;
                p.Parents.Remove(this);
                break;
            }
        }
        return this.Progress == this.Line.Count;
    }
    public StringBuilder Flattern(StringBuilder builder, HashSet<object>? visited = null)
    {
        if ((visited ??= []).Add(this))
        {
            if (this.IsLex)
            {
                foreach (var phase in Line) builder.Append(phase.ToString());
            }
            else
            {
                foreach (var phase in Line) phase.Flattern(builder, visited);
            }
        }
        return builder;
    }
    public string Flattern()
        => this.Flattern(new()).ToString();

    public override string ToString()
        => $"{this.Name} : {string.Join(", ", this.Line)}";

    public string Describe() => $"{this.Name} => Line:{string.Join(",", this.Line)};Targets:{string.Join(",", this.Targets)}";

    public override int GetHashCode() => this.Name.GetHashCode() ^ this.Index;
}
