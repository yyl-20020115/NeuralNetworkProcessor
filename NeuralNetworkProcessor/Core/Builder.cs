using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Utilities;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.RepresentationModel;
using NeuralNetworkProcessor.ZRF;
using NeuralNetworkProcessor.Util;

namespace NeuralNetworkProcessor.Core;

public static class Builder
{
    public const string CCPostfix = "_";
    public static string Serialize(Aggregation aggregation)
        => new SerializerBuilder(aggregation.GetType().Namespace)
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build().Serialize(aggregation);
    public static Aggregation Deserialize(string text)
        => new DeserializerBuilder(typeof(Aggregation).Namespace, typeof(Aggregation).Assembly)
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build().Deserialize<Aggregation>(text);
    public static string SerializeResults(Results results)
        => new SerializerBuilder(results.GetType().Namespace)
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build().Serialize(results);
    public static Results DeserializeResults(string text)
        => new DeserializerBuilder(typeof(Results).Namespace, typeof(Results).Assembly)
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build().Deserialize<Results>(text);
    //There should always be a limit on optionals
    //otherwise it will result in a great amount of expansion of trends
    //if you need more optionals than the limit,
    //try break the trands into shorter ones
    public const int DefaultMaxOptionals = Knowledge.DefaultMaxOptionals;
    public delegate void Looper<T>(Action<T> action);
    public static Looper<Definition> GetDefinitionsLooper(Knowledge knowledge)
        =>  knowledge.Definitions.ForEach
            ;
    public static Looper<Definition> GetDefinitionsLooper(List<Definition> definitions)
        =>  definitions.ForEach
            ;
    public static Looper<Description> GetDescriptionsLooper(Knowledge knowledge)
        => knowledge.Definitions.SelectMany(d => d.Descriptions).ToList().ForEach
            ;
    public static Looper<Cluster> GetClustersLooper(Aggregation aggregation)
        => aggregation.Clusters.ForEach
        ;
    public static Looper<Trend> GetTrendsLooper(Aggregation aggregation, Func<Trend, bool> f)
        => aggregation.Clusters.SelectMany(c => c.Trends).Where(f).ToList().ForEach
        ;
    public static Looper<Cell> GetCellsLooper(Aggregation aggregation)
        => aggregation.Clusters.SelectMany(c => c.Trends).SelectMany(t => t.Cells).ToList().ForEach
        ;
    public static Looper<Description> GetDescriptionsLooper(Definition definition)
        =>definition.Descriptions.ForEach
        ;
    public static Looper<Phrase> GetPhrasesLooper(Definition definition)
        => definition.Descriptions.SelectMany(d => d.Phrases).ToList().ForEach
        ;
    public static Aggregation Rebuild(TextReader reader, Aggregation previous = null, int MaxOptionals = DefaultMaxOptionals)
        => ZRF.Parser.Parse(reader) is ParseResult result &&
              result.Status == ParseStatus.OK
            ? Rebuild(result.Knowledge, previous, MaxOptionals)
            : null
            ;

    public static Aggregation Rebuild(YamlStream stream, Aggregation previous = null, int MaxOptionals = DefaultMaxOptionals)
        => ZRF.Parser.Parse(stream) is ParseResult result &&
              result.Status == ParseStatus.OK
            ? Rebuild(result.Knowledge, previous, MaxOptionals)
            : null
            ;

    public static Aggregation Rebuild(Knowledge knowledge, Aggregation previous = null, int MaxOptionals = DefaultMaxOptionals)
        => !knowledge.AnyDescriptionsWithMoreOptionalsThan(MaxOptionals)
            ? Relink(BuildAggregation(knowledge))
            : null
            ;
    public static Aggregation Relink(Aggregation aggregation, bool detect = true)
    {
        GetCellsLooper(aggregation)(
            //every cell should link to CommonCluster
            //terminal and range clusters are not related here
            c => c.Bind(aggregation.Clusters.Where(
                cc => cc.Name == c.Text))
        );
        if (detect)
        {
            GetTrendsLooper(aggregation,
                _ =>true)
                (trend => trend.IsDeepRecurse = Algorithms.DetectDeepRecurse(trend));

            var allDeepTrends = aggregation.Clusters.SelectMany(cluster => cluster.Trends)
              .Where(trend =>trend.IsDeepRecurse).ToHashSet();

            foreach (var mdt in allDeepTrends)
                foreach(var ct in mdt.Cells)
                    if (mdt.IsSurrounded(ct) && ct.HasAnyDeepSource)
                        Algorithms.MarkDeepRecurseClosure(ct, allDeepTrends, ct.DeepClosureTrends, 3);
            GetCellsLooper(aggregation)(
                c => c.HasLowerRecurse = Algorithms.HasAnyTailRecurse(c));
        }
        return aggregation;
    }
    public static Aggregation BuildAggregation(Knowledge knowledge, Aggregation pre = null)
    {
        var top = new List<Definition>(knowledge.Definitions);
        var all = new List<Cluster>();
        var idict = new Dictionary<int, CharacterCluster>();
        var udict = new Dictionary<UnicodeClass, RangeCluster>();

        //we never rebuild anything that is already built
        var blt = pre != null
            ? new HashSet<Definition>(pre.Clusters.Select(c => c.Definition))
            : new ()
            ;
        GetDefinitionsLooper(top.ToList())(definition =>
        {
            if (blt.Add(definition)) all.Add(
                BuildCluster(definition, top, all, idict, udict));
        });

        return new Aggregation(
            knowledge.Topic,
            all.OrderBy(d => d.Index).ToList()) { Knowledge = knowledge }.BackBind();
    }

    /// <summary>
    /// Plain all optionals
    /// </summary>
    /// <param name="description"></param>
    /// <returns></returns>
    public static List<Trend> BuildTrends(Description description, Dictionary<(Description,Phrase),Cell> dict)
    {
        var Trends = new List<Trend>();
        var Cells = new List<Cell>();
        var OptPos = new List<int>();
        var i = 0;
        foreach (var phrase in description.Phrases)
        {
            var copy = phrase.Copy();
            if (copy.Optional)
            {
                OptPos.Add(i);
                copy.Optional = false;
            }
            if (!dict.TryGetValue((description,copy),out var c))
                c = BuildCellFrom(copy);
            Cells.Add(c);
            i++;
        }
        var choices = new List<List<Cell>>() { Cells };
        foreach (var pos in OptPos)
            foreach (var choice in choices.ToArray())
            {
                var copy = new List<Cell>(choice);
                choices.Add(copy);
                copy[pos] = null;
            }
        i = 0;
        foreach (var choice in choices)
        {
            var cs = choice.Where(c => c != null)
                .Select(c => c.Duplicate()).ToList();
            //we do not allow 0 length array here!
            //this can be caused by all optionals (a? b? c? d?)
            //all optionals is not valid situation
            //all optionals derived from same description share
            //this description
            if (cs.Count > 0) Trends.Add(
                new Trend(cs, description){Index = i++ }.BackBind());
            foreach(var c in cs)
                foreach(var s in c.Sources)
                {
                    s.BindTarget(c);
                    s.Targets.RemoveWhere(t => t.Owner == null);
                }
        }
        return Trends;
    }
    public static Cell BuildCellFrom(Phrase c) 
        => new(c.Text, c) { Index = c.Index };
    public static Cell BuildCellFrom(string c, int index = -1) 
        => new(c) { Index = index }; 
    public static Cluster BuildCluster(
        Definition Definition,
        List<Definition> top,
        List<Cluster> all,
        Dictionary<int, CharacterCluster> idict,
        Dictionary<UnicodeClass,RangeCluster> udict)
    {
        var all_trends = new List<List<Trend>>();
        var top_cells = new Dictionary<(Description, Phrase), Cell>();
        var termial_clusters = new List<TerminalCluster>();
        var single_literal_phrases = Definition.Descriptions.Where(d => d.Phrases.Count == 1 
            && UnicodeHelper.TryDeclose(d.Phrases[0].Text)!=d.Phrases[0].Text).ToHashSet();
        for (int i = 0;i<Definition.Descriptions.Count;i++)
        {
            var dict = new Dictionary<(Description, Phrase), Cell>();
            var description = Definition.Descriptions[i];
            for(int j = 0; j < description.Phrases.Count; j++)
            {
                var phrase = description.Phrases[j];
                if (phrase?.Text is string text)
                {
                    var declosed = UnicodeHelper.TryDeclose(text);
                    if (declosed != text && declosed.Length > 0)
                    {
                        var points_cells = new List<Cell>();
                        var points = UnicodeHelper.NextPoint(declosed).ToList();
                        foreach (var point in points)
                        {
                            TerminalCluster tc = null;
                            string name = string.Empty;
                            if (point.u == UnicodeClass.Unknown)
                            {
                                name = "\'" + char.ConvertFromUtf32(point.c) + "\'";
                                if (!idict.TryGetValue(point.c, out var dict_cluster))
                                    idict[point.c] = dict_cluster = new CharacterCluster(name, UTF32: point.c) { Index = -1 };
                                tc = idict[point.c];
                            }
                            else
                            {
                                if (!udict.TryGetValue(point.u, out var dict_cluster))
                                {
                                    udict[point.u]
                                        = dict_cluster
                                        = new RangeCluster(
                                            name = "[" + point.u + "]") { Index = -1 }
                                        .TryBindFilter(new()
                                        {
                                            Type = CharRangeType.UnicodeClass,
                                            Class = point.u,
                                        });
                                }
                                tc = udict[point.u];
                            }
                            var cell = BuildCellFrom("_" + name, i);
                            points_cells.Add(cell);
                            tc.BindTarget(cell);
                            all.Add(tc);
                            termial_clusters.Add(tc);
                        }
                        var need_subcluster = true;
                        if (points.Count == 1 && description.Phrases.Count == 1)
                        {
                            top_cells[(description, phrase)] = points_cells[0];
                            points_cells = new();
                            need_subcluster = false;
                        }
                        if (need_subcluster)
                        {
                            var dp = new Description(
                                    declosed.Select(
                                        p => new Phrase("_'" + p + "'")).ToList());

                            var subdefinition = new Definition(text,
                                new(){ dp }, true);
                            
                            dp.Definition = subdefinition;

                            top.Add(subdefinition);
                            var subcluster = new CommonCluster(
                                text, new List<Trend> {
                                    new (points_cells, dp) }, subdefinition).BackBind();

                            all.Add(subcluster);
                            var subcell = BuildCellFrom(text, i);
                            subcluster.BindTarget(subcell);
                            dict[(description, phrase)] = subcell;
                            if (description.Phrases.Count == 1)
                            {
                                top_cells[(description, phrase)] = subcell;
                                points_cells = new();
                            }
                        }
                    }
                    else
                        dict[(description, phrase)] = BuildCellFrom(phrase);
                }
            }
            if(!single_literal_phrases.Contains(description))
                all_trends.Add(BuildTrends(description, dict));
        }
        if (top_cells.Count > 0)
            for (int i = 0; i < Definition.Descriptions.Count; i++)
                all_trends.Add(BuildTrends(Definition.Descriptions[i], top_cells));

        return new CommonCluster(
            Definition.Text, all_trends.SelectMany(trends => trends).ToList(),
            Definition) { Index = Definition.Index }.BackBind();
    }
}
