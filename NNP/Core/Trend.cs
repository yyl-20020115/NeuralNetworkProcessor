using NNP.ZRF;
using System.Diagnostics;
using System.Text;

namespace NNP.Core;

public record class Trend(string Name = "", Description? Description = null, bool IsLex = false, Phase? Target = null)
{
    public static long IdentityBase = 0;
    public long Identity = IdentityBase++;
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
    public bool IsTop => this.Targets.Count == 0;
    public bool IsLeftRecursive =>this.Line.Count>0 && this.Line[0].Name==this.Name;
    public bool IsInitiator(Phase phase) => this.Line.Count > 0 && this.Line[0].Name == phase.Name;
    public bool IsAnyInitiator(HashSet<Phase> phases) => phases.Any(phase => this.IsInitiator(phase));

    public bool IsBranch(Trend trend) => trend.Name != this.Name && this.BranchNames.Contains(trend.Name);

    public HashSet<Trend> GetBranches(HashSet<Trend> trends)
        => trends.Where(this.IsBranch).ToHashSet();

    public string Formatted => new TrendPrinter().Print(this).ToString();
    public Trend UpdatePosition()
    {

        return this;
    }
    public bool Advance(HashSet<Phase> bullets, int position)
    {
        for (int i = this.Progress; i < Line.Count; i++)
        {
            var phase = Line[i];
            if (!phase.Parents.Any(p => p.Name == this.Name)) 
                continue;
            else if (bullets.Any(b => b.Name == phase.Name))
            {
                this.Progress = i + 1;
                phase.Parents.Remove(this);
                break;
            }
            else
            {   //always break since no need to continue
                break;
            }
        }
        return this.IsComplete;
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
        => $"{this.Name} : {string.Join(", ", this.Line.Select(line=>line.Name))}";

    public string Describe() => $"{this.Name} => Line:{string.Join(",", this.Line)};Targets:{string.Join(",", this.Targets)}";

    public override int GetHashCode() => this.Name.GetHashCode() ^ (int)this.Identity;
}
