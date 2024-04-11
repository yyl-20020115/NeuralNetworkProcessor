using NNP.Util;
using NNP.ZRF;
using Utilities;
using System.Numerics;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NNP.Core;

public static class Builder
{
    public static string Serialize(Concept concept)
        => new SerializerBuilder(concept.GetType().Namespace ?? string.Empty)
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build().Serialize(concept);
    public static Concept Deserialize(string text)
        => new DeserializerBuilder(typeof(Concept).Namespace ?? string.Empty, typeof(Concept).Assembly)
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build().Deserialize<Concept>(text);
    public static readonly BigInteger one = BigInteger.One;
    public static readonly BigInteger zero = BigInteger.Zero;

    public static (List<Trend> trends, List<Phase> phases) Build(Concept concept)
    {
        var global_index = 0;
        var phases = new List<Phase>();
        var trends = new List<Trend>();
        var descriptions = new List<Description>();
        //Expand all optionals
        foreach (var description in concept.Definitions.SelectMany(d => d.Descriptions))
        {
            if (description.Phrases.Any(p => p.Optional))
            {
                var count = description.Phrases.Count;
                var max = BigInteger.Zero;
                var hit = BigInteger.Zero;
                for (var i = 0; i < count; i++)
                {
                    var current = one << i;
                    max |= current;
                    if (description.Phrases[i].Optional)
                        continue;
                    hit |= current;
                }
                var hits = new HashSet<BigInteger>();
                for (var t = zero; t <= max; t++)
                {
                    var s = t | hit;
                    if (hits.Add(s))
                    {
                        var phrases = new List<Phrase>();
                        for (var i = 0; i < count; i++)
                            if ((s & (one << i)) != zero) //disable optionals 
                                phrases.Add(description.Phrases[i] with { Optional = false });
                        if (phrases.Count > 0)
                            descriptions.Add(new(phrases)
                            {
                                Definition = description.Definition
                            });
                    }
                }
            }
            else
            {
                descriptions.Add(description);
            }
        }
        foreach (var description in descriptions)
        {
            var current_index = 0;
            var trend = new Trend(description.Definition.Text, global_index++, description);
            foreach (var phrase in description.Phrases)
            {
                var phrase_trend = new Trend();
                var text = phrase.Text;
                var declosed = UnicodeHelper.TryDeclose(text);

                Phase _phase;
                if (declosed != text && declosed.Length > 0)
                {
                    var char_index = 0;
                    var chars_trend = new Trend(text);
                    foreach (var (unicode_class, c, len) in UnicodeHelper.NextPoint(declosed))
                    {
                        chars_trend.Line.Add(
                            _phase = (unicode_class == UnicodeClass.Unknown
                            ? new CharacterPhase($"\'{char.ConvertFromUtf32(c)}\'", chars_trend, char_index++, UTF32: c)
                            : new CharrangePhase($"[{unicode_class}]", chars_trend, char_index++)
                             .TryBindFilter(new()
                             {
                                 Type = CharRangeType.UnicodeClass,
                                 Class = unicode_class,
                             })));
                        phases.Add(_phase);
                    }

                    trend.Line.Add(_phase = new(text, phrase_trend, current_index++, [chars_trend]));
                }
                else
                {
                    trend.Line.Add(_phase = new(text, phrase_trend, current_index++));
                }
                phases.Add(_phase);
            }
            trends.Add(trend);
        }

        foreach (var trend in trends)
        {
            foreach (var phase in trend.Line)
            {
                var founds = trends.Where(t => t.Name == phase.Name).ToArray();
                phase.Sources.UnionWith(founds);
                foreach (var found in founds) found.Targets.Add(phase);
            }
        }

        return (trends, phases);
    }
}
