using NeuralNetworkProcessor.ZRF;
using System.Collections.Generic;
using System.Linq;

namespace NeuralNetworkProcessor.Core;

public static class DefinitionPrinter
{
    public const string DefaultTab = "    ";
    public static string PrintPatternAttribute(Description description, int depth) 
        => $"{Print(depth)}[Pattern({description.Phrases.Aggregate("", (a, b) => a + (string.IsNullOrEmpty(a) ? "" : ", ") + (b.Optional ? "null" : (IsSpecial(b.Text) ? Ensure(b.Text) : "\"\"")))})]";
    public static string Ensure(string text)
        => text.Length >= 2 && text[0] == text[^1] && text[0] == '"' ? text : $"\"{text}\"";
    public static string Print(int depth, string tab = DefaultTab) 
        => new(Enumerable.Repeat(tab, depth).SelectMany(s => s).ToArray());
    public static bool IsSpecial(string text) =>
        text.Length >= 2 && ((text[0] is '+' or '-')
        || (text[0] == text[^1]&& text[0] is '\"' or '\''));
    public static bool IsList(IEnumerable<Phrase> phases)
        => phases.All(phrase => IsSpecial(phrase.Text));
    public static bool IsFullList(IEnumerable<Description> descriptions)
        => IsList(descriptions.SelectMany(d => d.Phrases));
    public static string Print(Phrase phrase, int index, bool list)
        => (list) ? ($"string _{index}") : (IsSpecial(phrase.Text)? "string": phrase.Text) + (index >= 0 ? " _" + index : "");
    public static string PrintPatternAttributeWithFullList(IEnumerable<Description> list,int depth)
        => Print(depth)+$"[Pattern(\n{list.SelectMany(l=>l.Phrases).Aggregate(Print(depth + 1), (a,b)=> a + Ensure(b.Text)+ ",\n"+ Print(depth + 1))}IsList = true)]\n{ Print(depth + 1)}string _";
    public static string Print(Description description,int depth, int index)
        => PrintPatternAttribute(description,depth)+((description.Phrases.Count == 1|| IsList(description.Phrases))
            ? Print(description.Phrases[0],index,IsList(description.Phrases))
            : "("+description.Phrases.Aggregate(
                "", (a, b) => a +(string.IsNullOrEmpty(a)?"":", " ) + Print(b,-1,false))+")"+ " _"+index);
    public static string Print(Definition definition, string baseType, int depth)           
        =>0 is int i
        ? $"{Print(depth)}public partial record {definition.Text}(\n{(IsFullList(definition.Descriptions)?PrintPatternAttributeWithFullList(definition.Descriptions,depth+1): definition.Descriptions.Aggregate("", (a, b) => a + (string.IsNullOrEmpty(a)?"":",\n") + Print(b,depth+1, i++)))}\n{ Print(depth) }) : {baseType};\n"
        : string.Empty;
    public static string Print(Knowledge knowledge, string baseType = "Node", string @namespace = "")
        => $"using NeuralNetworkProcessor.Core;\n{(
            (string.IsNullOrEmpty(@namespace) ? "" : $"namespace {@namespace}{{\n")
            + Print(string.IsNullOrEmpty(@namespace)?0:1)+$"public partial record {baseType};\n"
            + knowledge.Definitions.Aggregate(
                "",(a, b) => a + (string.IsNullOrEmpty(a) ? "" : "\n") 
                    + Print(b, baseType,(string.IsNullOrEmpty(@namespace)?0:1)))
            + (string.IsNullOrEmpty(@namespace) ? "" : "\n}"))}";
}
