using System.Linq;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace NeuralNetworkProcessor.ZRF;

public interface Group
{

}
public interface Symbol : Group
{
    string Text { get; }
    int Index { get; }
}
//use override GetHashCode() to disable auto referencing in yaml
public sealed record Phrase(string Text, bool Optional = false) : Symbol
{
    public static readonly Phrase Default = new("", false);
    [YamlIgnore]
    public int Index { get; set; }
    public string Text { get; set; } = Text;
    public string Extension { get; set; } = "";
    public bool Optional { get; set; } = Optional;
    [YamlIgnore]
    public Description Description { get; set; }
    public Phrase():this("",false) {}
    public Phrase Bind(Description Description)
    {
        this.Description = Description;
        return this;
    }
    public Phrase Copy() => new (this);
    //public override int GetHashCode() => base.GetHashCode();
    public override string ToString()  
        => this.Text + (this.Optional ? "?" : "");
}
public sealed record Description(List<Phrase> Phrases) : Group
{
    public static readonly Description Default = new(new List<Phrase>());
    [YamlIgnore]
    public int Index { get; set; } = 0;
    public List<Phrase> Phrases { get; set; } = Phrases;
    [YamlIgnore]
    public Definition Definition { get; set; } = Definition.Default;
    public Description BackBind()
    {
        var i = 0;
        foreach (var c in this.Phrases) { c.Bind(this); c.Index = i++; }
        return this;
    }
    public Description Bind(Definition Definition)
    {
        this.Definition = Definition;
        return this.BackBind();
    }
    public Description() : this(new List<Phrase>()) { }
    //public override int GetHashCode() => base.GetHashCode();
    public override string ToString()
         => this.Phrases.Aggregate("", (a, b) => a + (string.IsNullOrEmpty(a) ? "":" ") + b.ToString());
}
public sealed record Definition(string Text, List<Description> Descriptions,bool IsDynamicBuilt = false) : Symbol, Group
{
    public static readonly Definition Default = new("", []);
    public static implicit operator Phrase(Definition definition)
        => new(definition.Text);

    public string Text { get; set; } = Text;
    [YamlIgnore]
    public int Index { get; set; }
    public List<Description> Descriptions { get; set; } = Descriptions;

    [YamlIgnore]
    public Knowledge Knowledge { get; set; }
    public Definition BackBind()
    {
        var i = 0;
        foreach (var d in this.Descriptions) { d.Bind(this); d.Index = i++; }
        return this;
    }
    public Definition Bind(Knowledge Knowledge)
    {
        this.Knowledge = Knowledge;
        return this.BackBind();
    }
    public Definition() : this("", []) { }
    //public override int GetHashCode() => base.GetHashCode();
    public override string ToString()
        => $"{this.Text}:{this.Descriptions.Aggregate("", (a, b) => a + (string.IsNullOrEmpty(a)?"":"|") + b.ToString())};";
}
public sealed record Knowledge(string Topic, List<Definition> Definitions) :Group
{
    public const int DefaultMaxOptionals = 6;
    public static readonly Knowledge Default = new("", []);
    public List<Definition> Definitions { get; set; } = Definitions ?? [];
    public Knowledge BackBind()
    {
        var i = 0;
        foreach (var d in this.Definitions) { d.Bind(this); d.Index = i++; }
        return this;
    }
    public IList<Description> GetDescriptionsWithMoreOptionalsThan(int MaxOptionals = DefaultMaxOptionals) 
        => this.Definitions.SelectMany(d => d.Descriptions).Where(d => d.Phrases.Count(c => c.Optional) > MaxOptionals).ToArray();
    public bool AnyDescriptionsWithMoreOptionalsThan(int MaxOptionals = DefaultMaxOptionals)
        => this.Definitions.SelectMany(d => d.Descriptions).Any(d => d.Phrases.Count(c => c.Optional) > MaxOptionals);
    public Knowledge() : this("", []) { }
    //public override int GetHashCode() => base.GetHashCode();
    public override string ToString()
        =>  this.Definitions.Aggregate(this.Topic, (a, b) => a + (string.IsNullOrEmpty(a)?"":" ") + b.ToString());
    public Knowledge Compact()
    {
        var defs = new List<Definition>(this.Definitions);
        this.Definitions.Clear();
        defs.Select(d => d.Text).Distinct().ToList().ForEach(
            name => this.Definitions.Add(new (name,
                    defs.Where(d => d.Text == name)
                    .SelectMany(d => d.Descriptions).ToList())));
        return this;
    }
    public Knowledge Copy()
        => new(this.Topic, [.. this.Definitions, Definition.Default]);
}
