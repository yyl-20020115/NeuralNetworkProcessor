using System.Text;
using Utilities;

namespace NNP.Core;

/// <summary>
/// Trend0 Line = Phase0 Phase1 Phase2 ...
/// Phase0 Sources= {
///   Trend0;
///   Trend1;
///   Trend2
///   }
/// </summary>
/// <param name="Name"></param>
/// <param name="Parent"></param>
public record class Phase(string Name, Trend Parent, int Index = -1, HashSet<Trend>? Sources = default)
{
    public readonly string Name = Name;
    public readonly Trend Parent = Parent;
    public readonly HashSet<Trend> Sources = Sources ?? [];
    public int Index = Index;
    public virtual bool Accept(string Text) => Text == this.Name;
    public override string ToString() => this.Name;

    public StringBuilder Flattern(StringBuilder builder)
    {
        if (Sources.Count == 0)
        {
            builder.Append(this.Name);
        }
        else
        {
            foreach (var source in this.Sources)
                source.Flattern(builder);
        }
        return builder;
    }
    public string Flattern() => this.Flattern(new ()).ToString();
}

public abstract record TerminalPhase(string Name, Trend Parent, int Index = -1) : Phase(Name, Parent, Index)
{
    public abstract bool Accept(int UTF32);
    public override string ToString()
        => $"{nameof(this.Name)}:{this.Name}";
}
public record CharacterPhase(string Name, Trend Phase, int Index = -1, int UTF32 = -1) : TerminalPhase(Name, Phase, Index)
{
    public const int NULLChar = 0;
    public const int EOFChar = -1;
    public readonly int TargetChar = UTF32;
    public override bool Accept(int UTF32) => UTF32 == this.TargetChar;
    public override string ToString()
        => $"['{UnicodeClassTools.ToText(this.TargetChar)}']";
}
public record CharrangePhase(string Name, Trend Phase, int Index = -1) : TerminalPhase(Name, Phase, Index)
{
    public CharRangeFilter Filter { get; protected set; }
    public int UnicodeClassTemplate { get; set; } = 0;
    public int UnicodeActionTemplate { get; set; } = 0;
    public CharrangePhase TryBindFilter(CharRangeFilter filter)
    {
        this.Filter = filter;

        if (this.Filter.Type == CharRangeType.UnicodeClass)
        {
            if (filter.Class == UnicodeClass.Any)
            {
                this.UnicodeClassTemplate = -1;
                this.UnicodeActionTemplate = -1;
            }
            else
            {
                int v = (int)filter.Class;
                if (v >= 0 && v < (int)UnicodeClass.Any)
                {
                    var t = (1 << v);
                    this.UnicodeClassTemplate |= t;
                    this.UnicodeActionTemplate |= t;
                }
            }
        }
        return this;
    }
    public override bool Accept(int UTF32)
    {
        var accepted = true;
        if (this.UnicodeClassTemplate != 0)
        {
            var ci = -1;
            if ((UTF32 & 0xffff0000) == 0)
                ci = (int)char.GetUnicodeCategory((char)(UTF32 & 0x0000ffff));
            else
            {
                var t = UnicodeClassTools.ToText(UTF32);
                if (!string.IsNullOrEmpty(t)) ci = (int)char.GetUnicodeCategory(t, 0);
            }
            if (ci >= 0 && ci < (int)UnicodeClass.Any)
            {
                var t = 1 << ci;
                if ((t & this.UnicodeClassTemplate) != 0)
                    accepted &= ((t & this.UnicodeActionTemplate) != 0);
            }
            else accepted = false;
        }
        accepted &= this.Filter.Type != CharRangeType.UnicodeClass && this.Filter.Hit(UTF32);
        return accepted;
    }
    public override string ToString()
        => "[" + this.Filter.ToString() + "]";
}
