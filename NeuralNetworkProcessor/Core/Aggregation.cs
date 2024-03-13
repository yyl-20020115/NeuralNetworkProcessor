using NeuralNetworkProcessor.ZRF;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NeuralNetworkProcessor.Core;

public sealed record Aggregation(string Name,List<Cluster> Clusters = null,Knowledge Knowledge = null) : NeuralEntity
{
    public string Name { get; set; } = Name ?? string.Empty;
    public Knowledge Knowledge { get; set; } = Knowledge ?? Knowledge.Default;
    public List<Cluster> Clusters { get; set; } = Clusters ?? [];
    public Aggregation() : this("", null, null) { }
    public Aggregation BackBind()
    {
        this.Clusters.ForEach(c => c.Bind(this));
        return this;
    }

    public override string ToString()
        => this.Name + this.Clusters.Aggregate("", (a, b) => a + Environment.NewLine + b);

}
