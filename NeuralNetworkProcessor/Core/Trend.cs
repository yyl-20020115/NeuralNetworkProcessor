using System.Linq;
using NeuralNetworkProcessor.ZRF;
using System.Collections.Generic;
using YamlDotNet.Serialization;
using System.Text;

namespace NeuralNetworkProcessor.Core;

public sealed record Trend(List<Cell> Cells, Description Description = null) : NeuralEntity
{
    public static readonly Trend Default = new();
    public List<Cell> Cells { get; init; } = Cells ?? [];
    [YamlIgnore]
    public int CellsCount => this.Cells.Count;
    [YamlIsLink]
    public Description Description { get; set; } = Description ?? Description.Default;
    public List<Cell> Holes => this.Cells.Where(c => this.IsSurrounded(c) && c.HasAnyDeepSource).ToList();
    [YamlIgnore]
    public Cell StarterCell => this.Cells.Count > 0 ? this.Cells[0] : null;
    [YamlIgnore]
    public Cell FinsherCell => this.Cells.Count > 0 ? this.Cells[^1] : null;
    [YamlIgnore]
    public bool IsLeftRecurse => this.Owner != null && this.Cells.Count >= 2 && this.StarterCell.Text == this.Owner.Name;
    [YamlIgnore]
    public bool IsRightRecurse => this.Owner != null && this.Cells.Count >= 2 && this.FinsherCell.Text == this.Owner.Name;
    [YamlIgnore]
    public bool IsDeepRecurse { get; set; }
    [YamlIgnore]
    public bool IsSimpleLeftRecurse => this.IsLeftRecurse && this.CellsCount == 2;
    [YamlIgnore]
    public bool IsSimpleRightRecurse => this.IsRightRecurse && this.CellsCount == 2;
    [YamlIgnore]
    public bool IsTopTrend => this.Owner != null && this.Owner.IsTop;
    public int Index { get; set; }
    [YamlIsLink]
    public Cluster Owner { get; set; }
    public bool IsFinal(int index, bool strict = false)
        => !strict ? (index >= this.CellsCount - 1)
        : ((index >= this.CellsCount - 1) && this.CellsCount > 1);
    public bool IsFinal(Cell c, bool strict = false)
        => this.IsFinal(this.GetIndex(c), strict);
    public int GetIndex(Cell c)
        => this.Cells.IndexOf(c);
    public bool IsSurrounded(Cell c)
        => this.GetIndex(c) is int i && i > 0 && i < this.Cells.Count - 1;
    public Trend Bind(Cluster Owner)
    {
        this.Owner = Owner;
        return this.BackBind();
    }
    public Trend BackBind()
    {
        this.Cells.ForEach(cell => cell.Bind(this));
        return this;
    }
    public Trend() : this(new(), Description.Default) { }
    public string GetFocusTextAt(int index)
    {
        var builder = new StringBuilder();
        if (index < 0) builder.Append("[*] ");
        int i = 0;
        foreach (var cell in this.Cells)
        {
            if (i == index) builder.Append('[');
            builder.Append(cell.ToString());
            if (i == index) builder.Append(']');
            builder.Append(' ');
            i++;
        }
        if (index >= this.Cells.Count) builder.Append("[*]");
        return builder.ToString();
    }
    public override string ToString()
        => ((this.Owner?.Name ?? string.Empty) + " : ") + this.Cells.Aggregate("", (a, b) => a + b.ToString() + " ");
}
