using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Serialization;
using NeuralNetworkProcessor.ZRF;
using Utilities;

namespace NeuralNetworkProcessor.Core;

public abstract record Cluster(string Name, List<Trend> Trends, Definition Definition = null) : NeuralEntity
{
    public static readonly Cluster InputSourceCluster = new CommonCluster();
    [YamlIsLink]
    public Definition Definition { get; set; } = Definition ?? Definition.Default;
    public string Name { get; set; } = Name ?? string.Empty;
    public List<Trend> Trends { get; set; } = Trends ?? new();
    public int Index { get; set; } = -1;
    [YamlIgnore]
    public HashSet<Cell> Targets { get; set; } = new();
    [YamlIgnore]
    public bool HasTargets => this.Targets.Count > 0;
    [YamlIgnore]
    public bool HasTrends => this.Trends.Count > 0;
    [YamlIgnore]
    public bool IsTop => (!this.HasTargets) && this.HasTrends;
    [YamlIsLink]
    public Aggregation Owner { get; set; } = null;
    [YamlIgnore]
    public bool HasRightRecurse => this.Trends.Any(t => t.IsRightRecurse);
    [YamlIgnore]
    public bool HasLeftRecurse => this.Trends.Any(t => t.IsLeftRecurse);
    [YamlIgnore]
    public bool HasDeepRecurse => this.Trends.Any(t => t.IsDeepRecurse);
    public bool HasRecurse => this.HasLeftRecurse || this.HasRightRecurse || this.HasDeepRecurse;
    public Cluster Bind(Aggregation Owner)
    {
        this.Owner = Owner;
        return this.BackBind();
    }
    public Cluster BackBind()
    {
        this.Trends.ForEach(trend => trend.Bind(this));
        return this;
    }
    public virtual bool Accept(string Text)
        => Text == this.Name;
    public Cluster BindTarget(Cell Cell)
    {
        this.Targets.Add(Cell);
        Cell.Sources.Add(this);
        return this;
    }
    public override string ToString()
        => this.Name + this.Trends.Aggregate(" : ", (a, b) => a + Environment.NewLine + b.ToString()) + Environment.NewLine;
}
public record CommonCluster(string Name, List<Trend> Trends, Definition Definition = null) : Cluster(Name, Trends, Definition)
{
    public CommonCluster() : this("", new(), Definition.Default) { }
    public override string ToString()
        => this.Name + this.Trends.Aggregate(" : ", (a, b) => a + Environment.NewLine + b.ToString()) + Environment.NewLine;
}
public record TerminalCluster(string Name, Definition Definition = null) : Cluster(Name, new List<Trend>(), Definition)
{
    public TerminalCluster() : this("", Definition.Default) { }
    /// <summary>
    /// Terminal Clusters can have spans without corresponding cells
    /// </summary>
    /// <param name="Span"></param>
    /// <param name="Cell"></param>
    /// <returns></returns>
    public virtual bool Accept(int UTF32) => true;
    public override string ToString()
        => $"{nameof(this.Name)}:{this.Name}";
}
public record CharacterCluster(string Name, Definition Definition = null, int UTF32 = -1) : TerminalCluster(Name, Definition)
{
    public const int NULLChar = 0;
    public const int EOFChar = -1;
    public int TemplateChar { get; set; } = UTF32;
    public override bool Accept(int UTF32) => UTF32 == this.TemplateChar;
    public override string ToString()
        => $"{this.Name} : [{UnicodeClassTools.ToText(this.TemplateChar)}]";
    public CharacterCluster() : this(" ", Definition.Default) { }
}
public record RangeCluster(string Name, Definition Definition = null) : TerminalCluster(Name, Definition)
{
    public CharRangeFilter Filter { get; protected set; }
    public int UnicodeClassTemplate { get; set; } = 0;
    public int UnicodeActionTemplate { get; set; } = 0;
    public RangeCluster() : this("", Definition.Default) { }
    public RangeCluster TryBindFilter(CharRangeFilter filter)
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
public enum CharRangeType : uint
{
    Unknown = 0,
    UnicodeChar = 1,
    UnicodeRange = 2,
    UnicodeClass = 3
}
public struct CharRangeFilter
{
    public CharRangeType Type;
    public UnicodeClass Class;
    public int StartChar;
    public int EndChar;
    public override string ToString()
        => $"{nameof(CharRangeType)}:{Type},{nameof(UnicodeClass)}:{Class},{nameof(StartChar)}:{StartChar},{nameof(EndChar)}:{EndChar}";
    public bool Hit(int InputChar)
       => UnicodeClassTools.IsValidUnicode(InputChar)
        && (this.IsCharHit(InputChar)
            || this.IsClassHit(InputChar)
            || this.IsRangeHit(InputChar)
            );
    private bool IsCharHit(int InputChar)
        => this.Type == CharRangeType.UnicodeChar
        && InputChar == StartChar
        ;
    private bool IsClassHit(int InputChar)
        => (this.Type == CharRangeType.UnicodeClass)
            && ((this.Class == UnicodeClass.Any)
            || (this.Class == (UnicodeClass)Char.GetUnicodeCategory(
                UnicodeClassTools.ToText(InputChar), 0)))
           ;
    private bool IsRangeHit(int InputChar)
        => this.Type == CharRangeType.UnicodeRange
        && InputChar >= this.StartChar
        && InputChar <= this.EndChar
        ;
}
