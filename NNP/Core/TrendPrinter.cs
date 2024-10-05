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
                        $"{trend} ({trend.Identity})({trend.StartPosition},{trend.EndPosition})");
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
                        $"{trend} ({trend.Identity})({trend.StartPosition},{trend.EndPosition})");
                    this.Print(trend, indents);
                }
                indents.Pop();
            }
        }
        return this;
    }
    public TrendPrinter Print(Trend trend, ListStack<string>? indents = null, bool many =false, bool tail=false ,HashSet<Trend>? visited = null)
    {
        visited ??= [];
        if (trend != null && visited.Add(trend))
        {
            indents ??= [];
            var condition = many && !tail;
            this.Print(string.Join("",indents) + (condition ? Branch : Tail));
            this.PrintLine($"{trend} ({trend.Identity})({trend.StartPosition},{trend.EndPosition})");
            if (!trend.IsLex)
            {
                indents.Push(condition ? Vert : Spaces);
                {
                    foreach (var sub_trend in trend.Line.SelectMany(line => line.Sources))
                    {
                        this.Print(sub_trend, indents, visited: visited);
                    }
                }
                indents.Pop();
            }
            else
            {
                foreach (var phase in trend.Line)
                {
                    this.Print(phase.ToString());
                }
                this.PrintLine();
            }
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
