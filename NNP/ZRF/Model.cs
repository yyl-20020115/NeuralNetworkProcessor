using YamlDotNet.Serialization;

namespace NNP.ZRF;

public interface IZRFElement
{
    string Text { get; }
}
public interface IZRFSymbol : IZRFElement
{
    int Index { get; }
}
//use override GetHashCode() to disable auto referencing in yaml
public sealed record Phrase(string Text, bool Optional = false) : IZRFSymbol
{
    public static readonly Phrase Default = new("", false);
    [YamlIgnore]
    public int Index { get; set; }
    public string Text { get; set; } = Text;
    public string Extension { get; set; } = "";
    public bool Optional { get; set; } = Optional;
    [YamlIgnore]
    public Description Description { get; set; } = Description.Default;
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
public sealed record Description(List<Phrase> Phrases) : IZRFElement
{
    public static readonly Description Default = new([]);
    public string Text => this.ToString();

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
    public Description() : this([]) { }
    //public override int GetHashCode() => base.GetHashCode();
    public override string ToString()
         => this.Phrases.Aggregate("", (a, b) => a + (string.IsNullOrEmpty(a) ? "":" ") + b.ToString());
}
public sealed record Definition(string Text, List<Description> Descriptions,bool IsDynamicBuilt = false) : IZRFSymbol, IZRFElement
{
    public static readonly Definition Default = new("", []);
    public static implicit operator Phrase(Definition definition)
        => new(definition.Text);

    public string Text { get; set; } = Text;
    [YamlIgnore]
    public int Index { get; set; }
    public List<Description> Descriptions { get; set; } = Descriptions;

    [YamlIgnore]
    public Concept Concept { get; set; } = Concept.Default;
    public Definition BackBind()
    {
        var i = 0;
        foreach (var d in this.Descriptions) { d.Bind(this); d.Index = i++; }
        return this;
    }
    public Definition Bind(Concept Knowledge)
    {
        this.Concept = Knowledge;
        return this.BackBind();
    }
    public Definition() : this("", []) { }
    //public override int GetHashCode() => base.GetHashCode();
    public override string ToString()
        => this.Text +":"+ this.Descriptions.Aggregate("", (a, b) => a + (string.IsNullOrEmpty(a)?"":"|") + b.ToString()) +";";
}
public sealed record Concept(string Topic, List<Definition> Definitions) :IZRFElement
{
    public const int DefaultMaxOptionals = 6;
    public static readonly Concept Default = new("", []);
    public string Text =>nameof(Concept);

    public List<Definition> Definitions { get; set; } = Definitions ?? [];
    public Concept BackBind()
    {
        var i = 0;
        foreach (var d in this.Definitions) { d.Bind(this); d.Index = i++; }
        return this;
    }
    public IList<Description> GetDescriptionsWithMoreOptionalsThan(int MaxOptionals = DefaultMaxOptionals) 
        => this.Definitions.SelectMany(d => d.Descriptions).Where(d => d.Phrases.Count(c => c.Optional) > MaxOptionals).ToArray();
    public bool AnyDescriptionsWithMoreOptionalsThan(int MaxOptionals = DefaultMaxOptionals)
        => this.Definitions.SelectMany(d => d.Descriptions).Any(d => d.Phrases.Count(c => c.Optional) > MaxOptionals);
    public Concept() : this("", []) { }
    //public override int GetHashCode() => base.GetHashCode();
    public override string ToString()
        =>  this.Definitions.Aggregate(this.Topic, (a, b) => a + (string.IsNullOrEmpty(a)?"":" ") + b.ToString());
    public Concept Compact()
    {
        var defs = new List<Definition>(this.Definitions);
        this.Definitions.Clear();
        defs.Select(d => d.Text).Distinct().ToList().ForEach(
            name => this.Definitions.Add(new (name,
                    defs.Where(d => d.Text == name)
                    .SelectMany(d => d.Descriptions).ToList())));
        return this;
    }
    public Concept Copy()
        => new(this.Topic, new (this.Definitions) { Definition.Default });
}
