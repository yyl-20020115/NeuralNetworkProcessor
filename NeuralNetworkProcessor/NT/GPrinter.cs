using System.Collections.Generic;
using System.IO;
using System.Linq;
using Utilities;

namespace NeuralNetworkProcessor.NT;

public class GPrinter(TextWriter writer = null)
{
    public const string SpacesText = "        ";
    public const string BranchText = "├──  ";
    public const string VertText =   "│      ";
    public const string TailText =   "└──  ";
    public const int CornerLength = 4;
    public int LineCount { get; protected set; } = 0;
    public TextWriter Writer { get; protected set; } = writer ?? new StringWriter();

    protected ListStack<string> stack = new();
    protected string Indent => this.stack.Aggregate("",(a, b) => a + b);
    protected void Enter(bool v) => this.stack.Push(v ? VertText : SpacesText);
    protected void Leave() => this.stack.Pop();
    public GPrinter Print(List<GPath> paths)
    {
        var pc = paths.Count;
        for(int i = 0; i < pc; i++)
        {
            var path = paths[i];
            var branch = pc > 1 && i < pc - 1;
            this.Print(this.Indent + (branch ? BranchText : TailText));
            this.Enter(branch);
            this.PrintLine(path?.Network?.Name ?? "_");
            if (path.SubPaths.Count > 0)
                this.Print(path.SubPaths);
            else
                this.Print(path);
            this.Leave();
        }
        return this;
    }
    public GPrinter Print(GPath path)
    {
        path.Points.ForEach(path => this.Print(path));
        return this;
    }
    public GPrinter Print(GPoint point)
        => this.Print(point.Flattern().ToString());

    public GPrinter PrintLine(string text = "")
    {
        this.Writer.WriteLine(text);
        this.LineCount++;
        return this;
    }
    public GPrinter Print(string text = "")
    {
        this.Writer.Write(text);
        return this;
    }
    public override string ToString() 
        => this.Writer.ToString();
}
