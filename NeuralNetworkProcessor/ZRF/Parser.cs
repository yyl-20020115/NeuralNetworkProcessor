using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using YamlDotNet.RepresentationModel;

namespace NeuralNetworkProcessor.ZRF;

public enum ParseStatus : int
{
    TooManyOptionals = -3,
    InvalidReader = -2,
    Failed = -1,
    OK = 0,
}
public record ParseResult(ParseStatus Status = ParseStatus.Failed, Knowledge Knowledge = null, int countOptional = 0, int lineNumber = 0);
public static class Parser
{
    //if there are too many optionals within one description
    //the expanding result will be huge: 2^n_opt
    public const string EOFTextDoubleQuote = "\"\"";
    public const string EOFTextSingleQuote = "''";
    public const int EOFChar = -1;
    public static readonly Guid EOFGuid = new(EOFChar, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    public const char OptionalSign = '?';
    public static string Trim(string text) 
        => text?.Trim(' ', '\t', '\r', '\n');
    public static IEnumerable<string> NextPart(string line)
    {
        var text = string.Empty;
        var builder = new StringBuilder();
        for (int i = 0, len = line.Length; i < len; i++)
        {
            var c = line[i];
            if (c is ' ' or '\t')
            {
                //consume ' ' and '\t's
                for (++i; i < len; i++)
                    if ((line[i]) is ' ' or '\t') continue; else break;
                i--;
                if ((text = builder.ToString().Trim()).Length > 0) yield return text;
                builder.Clear();
            }
            else if (c is '\'' or '\"')
            {
                builder.Append(c);
                for (++i; i < len; i++)
                {
                    var d = line[i];
                    builder.Append(d);
                    if (d == c)
                    {
                        if ((text = builder.ToString().Trim()).Length > 0) yield return text;
                        builder.Clear();
                        break;
                    }
                }
            }
            else
                builder.Append(c);
        }
        if ((text = builder.ToString().Trim()).Length > 0) yield return text;
    }
    public static ParseResult Parse(TextReader reader, string language = "", int MaxOptionals = Knowledge.DefaultMaxOptionals)
    {
        if (reader == null) return new ParseResult(ParseStatus.InvalidReader);
        var name = "";
        var db = new List<Definition>() { Definition.Default };
        var pb = new List<Description>();
        var fs = new List<Phrase>();
        var i = 0;
        var j = 0;
        var k = 0;
        var n = 0;
        while (reader.ReadLine() is string line)
        {
            n++;
            if ((line = Trim(line)).Length == 0 || line.StartsWith('#'))
                continue;
            else if (line.EndsWith(':'))
            {
                name = Trim(line.TrimEnd(':'));
                db.Add(new(name, pb = new()) { Index = i++ });
                j = 0;
            }
            else
            {
                //tail comment
                if ((line.LastIndexOf('#') is int ps)
                    && ps > 0 && line[ps - 1] != '&') line = Trim(line[0..ps]);
                k = 0;
                fs = new();
                foreach (var s in NextPart(line))
                    fs.Add(s.EndsWith(OptionalSign)
                        ? new(s[0..^1], true) { Index = k++ }
                        : new(s, false) { Index = k++ });

                var oc = fs.Count(s => s.Optional);
                if (oc > MaxOptionals)
                    return new(ParseStatus.TooManyOptionals, null, oc, n);
                if (fs.Count > 0) pb.Add(new(fs) { Index = j });
            }
        }
        return new(ParseStatus.OK, new Knowledge(language, db).Compact().BackBind());
    }
    public static ParseResult Parse(YamlStream stream, string language = "", int MaxOptionals = Knowledge.DefaultMaxOptionals)
    {
        if (stream == null) return new ParseResult(ParseStatus.InvalidReader);
        var defs = new List<Definition>();
        foreach (var doc in stream.Documents)
            if (doc.RootNode is YamlMappingNode root)
                foreach (var def in root.Children)
                {
                    var ds = new List<Description>();
                    if (def.Value is YamlSequenceNode def_descriptions)
                    {
                        foreach (var description_child in def_descriptions.Children)
                        {
                            if (description_child is YamlSequenceNode phrases)
                            {
                                var ps = new List<Phrase>();
                                foreach (var phrase in phrases)
                                {
                                    if (phrase is YamlScalarNode phrase_item)
                                    {
                                        var item = phrase_item.ToString();
                                        if (item.EndsWith('^') is bool opt && opt) item = item.TrimEnd('^');
                                        ps.Add(new(item, opt));
                                    }
                                }
                                ds.Add(new(ps));
                            }
                        }

                    }
                    defs.Add(new(def.Key.ToString(), ds));
                }
        return new(ParseStatus.OK, new Knowledge(language, defs).Compact().BackBind());
    }
}
