using NeuralNetworkProcessor.Core;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Utilities;

namespace NNPlatform;

public static class DocumentBuilder
{
    public static int TabSpaceCount = 2;
    public static string NewLine => Environment.NewLine;
    public static Border GenerateTextBorder(TextBlock block, double thickness = 1.0)
        => new() { Child = block, BorderThickness = new (thickness) };
    public static TextBlock GenerateTextBlock(TextBlock block, object o)
        => new () { Tag = (block, o) };
    public static Run GenerateSpacesRun(int n = 1, TextBlock parent = null)
       => GenerateCommonTextRun(new string(' ', n), parent);
    public static Run GenerateSpacesRun(int n = 1, TextElement parent = null)
       => GenerateCommonTextRun(new string(' ', n), parent);
    public static Run GenerateCommonTextRun(string text = "", TextBlock parent = null) =>
        new(text) { Tag = (text, parent) };
    public static Run GenerateCommonTextRun(string text = "", TextElement parent = null) =>
        new(text) { Tag = (text, parent) };
    public static LineBreak GenerateLineBreak(TextBlock parent = null)
        => new() { Tag = (NewLine, parent) };
    public static LineBreak GenerateLineBreak(TextElement parent = null)
        => new() { Tag = (NewLine, parent) };
    public static IEnumerable<string> NextDoucumentText(string text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            char h = text[i];
            char l = i < text.Length - 1 ? text[i] : '\0';
            if (char.IsSurrogatePair(h, l))
            {
                i++;
                yield return char.ConvertFromUtf32(char.ConvertToUtf32(h, l));
            }
            else if (i + NewLine.Length < text.Length 
                && text.Substring(i, NewLine.Length) == NewLine)
            {
                yield return NewLine;
                i += NewLine.Length - 1;
            }
            else if (h == '\t')
            {
                yield return h.ToString();
            }
            else if (h == ' ')
            {
                var s = h.ToString();
                for (i++; i < text.Length; i++)
                    if (text[i] == ' ')
                    {
                        s += text[i];
                    }
                    else
                    {
                        i--; break;
                    }
                yield return s;
            }
            else
            {
                var s = h.ToString();
                for (i++; i < text.Length; i++)
                    if (text[i] is ' ' or '\t' or '\r' or '\n')
                    {
                        i--; break;
                    }
                    else
                    {
                        s += text[i].ToString();
                    }
                yield return s;
            }
        }
    }
    public static IEnumerable<Inline> NextDoucumentPart(string text, TextElement parent = null)
    {
        foreach (var part in NextDoucumentText(text))
            if (part.Length == 0)
                continue;
            else if (part == NewLine)
                yield return GenerateLineBreak(parent);
            else if (part[0] == '\t')
                yield return GenerateCommonTextRun(part, parent);
            else if (part[0] == ' ')
                yield return GenerateSpacesRun(part.Length, parent);
            else
                yield return GenerateCommonTextRun(part, parent);
    }

    public static Span GenerateSpan(string text, DependencyObject parent)
    {
        var span = new Span { Tag = (text, parent) };
        foreach (var e in NextDoucumentPart(text, span)) span.Inlines.Add(e);
        return span;
    }
    public static Span GenerateSpan(string text, Span parent = null)
    {
        var Span = GenerateSpan(text, parent as DependencyObject);
        if (Span != null) parent?.Inlines?.Add(Span);
        return Span;
    }
    public static Span GenerateSpan(string text, TextBlock parent = null)
    {
        var Span = GenerateSpan(text, parent as DependencyObject);
        if (Span != null) parent?.Inlines?.Add(Span);
        return Span;
    }
    public static Span GenerateSpan(TextBlock parent = null)
    {
        var Span = new Span { Tag = (string.Empty, parent) };
        if (Span != null) parent?.Inlines?.Add(Span);
        return Span;
    }
    public static StackPanel GenerateStackPanel(Orientation orientation, TextBlock parent = null)
    {
        var SP = new StackPanel { Orientation = orientation, Tag = (string.Empty, parent) };
        if (SP != null) parent?.Inlines?.Add(SP);
        return SP;
    }
    public delegate bool OnVisitResultsFunction(Results rs, TextBlock block);
    public static TextBlock Build(
        TextBlock block,
        Extraction extraction,
        DualDictionary<DependencyObject, Extraction> dict = null)
    {
        dict ??= new();
        switch (extraction)
        {
            case TextSpan sp:
                if (sp.Buddy != null)
                    dict.AddOrUpdate(sp, Build(block, sp.Buddy, dict));
                else
                    dict.AddOrUpdate(sp, GenerateSpan(sp.Text, block));
                break;
            case Pattern pt:
                {
                    //block is already inside of dict for pattern
                    foreach (var ext in pt.SymbolExtractions)
                    {
                        var sub_block = GenerateTextBlock(block, ext);
                        
                        block.Inlines.Add(sub_block);
                        
                        dict.AddOrUpdate(ext, Build(sub_block, ext, dict));
                    }
                }
                break;
            case Results rs:
                {
                    //Results: 
                    //  Result[TextBlock]: Pattern[Span]
                    //    Pattern[Span]: Symbol[TextBlock] Symbol[TextBlock] ...
                    //      Symbol[TextBlock]: Text[Span[Run/Span/TextBlock]]
                    foreach (var pt in rs.Patterns)
                    {
                        var sub_block = GenerateTextBlock(block, pt);

                        block.Inlines.Add(sub_block);
                        //use like breaks to separate results
                        dict.AddOrUpdate(pt, Build(sub_block, pt, dict));
                    }
                }
                break;
            default:
                break;
        }
        return block;
    }
}
