﻿using YamlDotNet.RepresentationModel;

namespace NNP.ZRF;

public static class YamlParser
{
    public static ParseResult Parse(TextReader reader, string language = "")
    {
        if (reader == null) return new ParseResult(ParseStatus.InvalidReader);
        var stream = new YamlStream();
        stream.Load(reader);
        var defs = new List<Definition>();
        foreach (var doc in stream.Documents)
        {
            if (doc.RootNode is not YamlMappingNode root) continue;
            foreach (var def in root.Children)
            {
                var ds = new List<Description>();
                if (def.Value is YamlSequenceNode def_descriptions)
                {
                    foreach (var description_child in def_descriptions.Children)
                    {
                        if (description_child is not YamlSequenceNode phrases) continue;
                        var ps = new List<Phrase>();
                        foreach (var phrase in phrases)
                        {
                            if (phrase is not YamlScalarNode phrase_item) continue;
                            var s = phrase_item.ToString();
                            bool opt;
                            if (opt = s.EndsWith('^')) s = s.TrimEnd('^');
                            ps.Add(new Phrase(s, opt));
                        }
                        ds.Add(new Description(ps));
                    }
                }
                defs.Add(new Definition(def.Key.ToString(), ds));
            }
        }
        return new(ParseStatus.OK, new Concept(language, defs).Compact().BackBind());
    }
}
