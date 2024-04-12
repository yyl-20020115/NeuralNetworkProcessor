using NNP.Util;
using NNP.ZRF;
using Utilities;
using System.Numerics;

namespace NNP.Core;

public static class Builder
{
    public static readonly BigInteger _1_ = BigInteger.One;
    public static readonly BigInteger _0_ = BigInteger.Zero;
    public static (List<Trend> trends, List<Phase> phases,List<TerminalPhase> terminals) Build(Concept concept)
    {
        var global_index = 0;
        var terminals = new List<TerminalPhase>();
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
                    var current = _1_ << i;
                    max |= current;
                    if (description.Phrases[i].Optional)
                        continue;
                    hit |= current;
                }
                var hits = new HashSet<BigInteger>();
                for (var t = _0_; t <= max; t++)
                {
                    var s = t | hit;
                    if (hits.Add(s))
                    {
                        var phrases = new List<Phrase>();
                        for (var i = 0; i < count; i++)
                            if ((s & (_1_ << i)) != _0_) //disable optionals 
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
                var text = phrase.Text;
                var declosed = UnicodeHelper.TryDeclose(text);
                Phase trend_phase ;
                trend.Line.Add(trend_phase = new Phase(text, trend, current_index++));
                phases.Add(trend_phase);
                if (declosed != text && declosed.Length > 0)
                {
                    var phrase_trend = new Trend(text, IsLex: true, Target: trend_phase);
                    var char_index = 0;
                    foreach (var (unicode_class, c, len) in UnicodeHelper.NextPoint(declosed))
                    {
                        TerminalPhase terminal_phase;
                        phrase_trend.Line.Add(
                            terminal_phase = (unicode_class == UnicodeClass.Unknown
                            ? new CharacterPhase($"\'{char.ConvertFromUtf32(c)}\'", phrase_trend, char_index++, UTF32: c)
                            : new CharrangePhase($"[{unicode_class}]", phrase_trend, char_index++)
                             .TryBindFilter(new()
                             {
                                 Type = CharRangeType.UnicodeClass,
                                 Class = unicode_class,
                             })));
                        terminals.Add(terminal_phase);
                    }
                }

            }
            trends.Add(trend);
        }

        foreach (var trend in trends)
        {
            foreach (var phase in trend.Line)
            {
                var founds = trends.Where(t => t.Name == phase.Name).ToArray();
                if (founds.Length > 0)
                {
                    phase.Sources.UnionWith(founds);
                    foreach (var found in founds) found.Targets.Add(phase);
                }
            }
            trend.BranchNames.UnionWith(trend.Description.GetBranches(concept));
        }
        
        return (trends, phases, terminals);
    }
}
