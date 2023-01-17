using NeuralNetworkProcessor.ZRF;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Serialization;

namespace NeuralNetworkProcessor.Core;

public sealed record Cell(string Text, Phrase Phrase = null) : NeuralEntity
{
    public static readonly Cell Default = new("",Phrase.Default);
    public string Text { get; set; } = Text ?? string.Empty;
    public HashSet<Trend> DeepClosureTrends { get; } = new HashSet<Trend>();
    [YamlIsLink]
    public Phrase Phrase { get; set; } = Phrase ?? Phrase.Default;
    [YamlIgnore]
    public bool Optional => this.Phrase != null && this.Phrase.Optional;
    public int Index { get; set; } = -1;
    //public int SourceIndex { get; set; } = -1;
    [YamlIsLink]
    public Trend Owner { get; set; } = null;
    public bool HasAnyDeepSource => this.Sources.Any(s => s.HasDeepRecurse);
    public bool HasLowerRecurse { get; set; }
    [YamlHasLinks]
    public HashSet<Cluster> Sources { get; set; } = new();
    public Cell() : this("",Phrase.Default) { }
    public Cell Bind(Trend Owner)
    {
        this.Owner = Owner;
        return this;
    }
    [YamlIgnore]
    public Cluster SuperOwner => this.Owner.Owner;
    public Cell Bind(IEnumerable<Cluster> Sources)
    {
        this.Sources.UnionWith(Sources);
        foreach(var s in this.Sources.ToArray()) s.BindTarget(this);
        return this;
    }
    public override string ToString() 
        => this.Text + (this.Optional ? "?" : "");

    public Cell Duplicate() 
        => new(this);
}
