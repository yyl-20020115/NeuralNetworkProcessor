using NNP.Util;
using NNP.ZRF;
using System.Numerics;
using Utilities;
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

    public static List<Trend> Build(Concept concept)
    {
        var global_index = 0;
        var trends = new List<Trend>();
        var descriptions = new List<Description>();
        //Expand all optionals
        foreach (var description in concept.Definitions.SelectMany(d => d.Descriptions))
        {            
            var optionals = description.Phrases.Where(p => p.Optional).ToList();
            if (optionals.Count > 0)
            {
                var count = description.Phrases.Count;
                var max = BigInteger.Zero;
                var hit = BigInteger.Zero;
                var one = BigInteger.One;
                var zero = BigInteger.Zero; 
                for (int i = 0; i < count; i++)
                {
                    var current = (one << i);
                    max |= current;
                    if (!description.Phrases[i].Optional) hit |= current;
                }
                var hits = new HashSet<BigInteger>();
                for(var t = BigInteger.Zero; t <= max; t++)
                {
                    var s = t | hit;
                    if (hits.Add(s))
                    {
                        var phrases = new List<Phrase>();
                        for(int i = 0; i < count; i++)
                        {
                            var current = (one << i);
                            if ((s & current) != zero){
                                phrases.Add(description.Phrases[i]);
                            }
                        }
                        if (phrases.Count > 0)
                        {
                            descriptions.Add(new Description(phrases) { Definition = description.Definition });
                        }
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
            var trend = new Trend(description.Definition.Text, global_index++);
            foreach (var phrase in description.Phrases)
            {
                var phrase_trend = new Trend();
                var text = phrase.Text;
                var declosed = UnicodeHelper.TryDeclose(text);
                if (declosed != text && declosed.Length > 0)
                {
                    var char_index = 0;
                    var char_trend = new Trend();
                    foreach (var (unicode_class, c, len) in UnicodeHelper.NextPoint(declosed))
                    {
                        char_trend.Line.Add(
                            unicode_class == UnicodeClass.Unknown
                            ? new CharacterPhase($"\'{char.ConvertFromUtf32(c)}\'", char_trend, char_index++, UTF32: c)
                            : new CharrangePhase($"[{unicode_class}]", char_trend, char_index++)
                             .TryBindFilter(new()
                             {
                                 Type = CharRangeType.UnicodeClass,
                                 Class = unicode_class,
                             }));
                    }

                    trend.Line.Add(new(phrase.Text, phrase_trend, current_index++, [char_trend]));
                }
                else
                {
                    trend.Line.Add(new(phrase.Text, phrase_trend, current_index++));
                }
            }
            trends.Add(trend);
        }

        foreach(var trend in trends)
        {
            foreach(var phase in trend.Line)
            {
                var founds = trends.Where(t => t.Name == phase.Name).ToArray();
                phase.Sources.UnionWith(founds);
                foreach(var found in founds)
                {
                    found.Targets.Add(phase);
                }
            }
        }

        return trends;
    }
}
