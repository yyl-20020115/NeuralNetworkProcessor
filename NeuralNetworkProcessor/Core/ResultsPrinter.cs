using System.IO;
using System.Linq;
using System.Collections.Generic;
using Utilities;

namespace NeuralNetworkProcessor.Core;

public class ResultsPrinter
{
    public const string Spaces  = "        ";
    public const string Branch  = "├──  ";
    public const string Vert    = "│      ";
    public const string Tail    = "└──  ";
    public const int CornerLength = 4;
    public int LineCount { get; protected set; } = 0;
    public TextWriter Writer { get; protected set; }
    public ResultsPrinter(TextWriter writer = null)
        => this.Writer = writer ?? new StringWriter();

    public ResultsPrinter PrintList(List<Results> resultsList, ListStack<string> pres = null)
    {
        pres ??= new ListStack<string>();
        var count = resultsList.Count;
        for (int i = 0; i < count; i++)
        {
            var results = resultsList[i];
            if (i < count - 1)
            {
                this.Print(count > 1 ? Branch : Tail);
                this.Print(pres.Aggregate("", (a, b) => a + b));
                pres.Push(count > 1 ? Vert : Spaces);
                this.PrintLine(
                    $"{results.Symbol.Text}({results.Position},{results.EndPosition})=\"{results.Extract()}\"");
                this.Print(results, pres);
                pres.Pop();
            }
            else
            {
                this.Print(Tail + pres.Aggregate("",(a, b) => a + b));
                pres.Push(Spaces);
                this.PrintLine( 
                    $"{results.Symbol.Text}({results.Position},{results.EndPosition})=\"{results.Extract()}\"");
                this.Print(results, pres);
                pres.Pop();
            }
        }
        return this;
    }
    public ResultsPrinter Print(Results results, ListStack<string> pres, bool many =false, bool tail=false)
    {
        if (results != null)
        {
            var rc = results.Patterns.Length;
            var cond = many && !tail;
            for (int i = 0; i < rc; i++)
            {
                var pattern = results.Patterns[i];
                this.Print(pres.Aggregate("", (a, b) => a + b)
                    + (cond ? Branch : Tail));
                this.PrintLine($"{pattern.Description}:({pattern.Position},{pattern.EndPosition})");
                pres.Push(cond ? Vert : Spaces);
                this.Print(pattern, pres);
                pres.Pop();
            }
        }
        return this;
    }
    public ResultsPrinter Print(Pattern pattern, ListStack<string> pres)
    {
        var length = pattern.SymbolExtractions.Length;
        var many = length > 1;
        for (int i = 0; i < length; i++)
        {
            var extraction = pattern.SymbolExtractions[i];
            if (extraction is TextSpan span && span.Buddy!=null)
                this.Print(span.Buddy, pres, many, i == length - 1);
            else
            {
                this.Print(pres.Aggregate("", (a, b) => a + b) + Tail);
                this.PrintLine($"'{extraction.Text}'");
            }
        }
        return this;
    }
    public ResultsPrinter PrintLine(string text = "")
    {
        this.Writer.WriteLine(text);
        this.LineCount++;
        return this;
    }
    public ResultsPrinter Print(string text = "")
    {
        this.Writer.Write(text);
        return this;
    }
    public override string ToString() 
        => this.Writer.ToString();
}
