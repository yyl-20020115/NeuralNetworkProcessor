using NNP.ZRF;
using Utilities;

namespace NNP.Core;

public class TrendPrinter(TextWriter? writer = null)
{
    public const string Spaces  = "        ";
    public const string Branch  = "├──  ";
    public const string Vert    = "│      ";
    public const string Tail    = "└──  ";
    public const int CornerLength = 4;
    public int LineCount { get; protected set; } = 0;
    public TextWriter Writer { get; protected set; } = writer ?? new StringWriter();

    public TrendPrinter PrintList(List<Trend> trends, ListStack<string>? indents = null)
    {
        indents ??= [];
        var count = trends.Count;
        for (int i = 0; i < count; i++)
        {
            var trend = trends[i];
            if (i < count - 1)
            {
                this.Print(count <= 1 ? Tail : Branch);
                this.Print(string.Join("",indents));
                indents.Push(count <= 1 ? Spaces : Vert);
                {
                    this.PrintLine(
                        $"{trend.Description}({0},{0})=\"{trend}\"");
                    this.Print(trend, indents);
                }
                indents.Pop();
            }
            else
            {
                this.Print(Tail + string.Join("", indents));
                indents.Push(Spaces);
                {
                    this.PrintLine(
                        $"{trend.Description}({0},{0})=\"{trend}\"");
                    this.Print(trend, indents);
                }
                indents.Pop();
            }
        }
        return this;
    }
    public TrendPrinter Print(Trend trend, ListStack<string>? indents = null, bool many =false, bool tail=false)
    {
        if (trend != null)
        {
            indents ??= [];
            var condition = many && !tail;
            this.Print(string.Join(string.Empty,indents) + (condition ? Branch : Tail));
            this.PrintLine($"{trend.Description}:({0},{0})");
            indents.Push(condition ? Vert : Spaces);
            {
                this.Print(trend, indents);
            }
            indents.Pop();
        }
        return this;
    }
    public TrendPrinter Print(Description description, ListStack<string>? indents = null)
    {
        indents ??= [];
        var length = description.Phrases.Count;
        var many = length > 1;
        for (int i = 0; i < length; i++)
        {
            var phrase = description.Phrases[i];
        }
        return this;
    }
    public TrendPrinter PrintLine(string text = "")
    {
        this.Writer.WriteLine(text);
        this.LineCount++;
        return this;
    }
    public TrendPrinter Print(string text = "")
    {
        this.Writer.Write(text);
        return this;
    }
    public override string ToString()
        => this.Writer?.ToString() ?? string.Empty;
}
