using NNP.ZRF;
using Utilities;

namespace NNP.Core;

public class Parser
{
    public List<Trend> Trends { get; private set; } = [];
    public List<Phase> Phases { get; private set; } = [];
    public List<TerminalPhase> Terminals { get; private set; } = [];
    public Parser Bind(Concept concept)
    {
        (this.Trends, this.Phases) = Builder.Build(concept);
        this.Terminals 
            = this.Phases
                .Where(p=>p is TerminalPhase)
                .Cast<TerminalPhase>()
                .ToList();
        return this;
    }

    public virtual List<Trend> Parse(string Text)
    => this.Parse(InputProvider.CreateInput(Text));
    public virtual List<Trend> Parse(TextReader Reader)
        => this.Parse(InputProvider.CreateInput(Reader));

    public List<Trend> Parse(Input input)
    {
        foreach (var (utf32, islast) in input())
        {
            var text = char.ConvertFromUtf32(utf32);
            var length = text.Length;

            var init_trends = this.Terminals
                .Where(t => t.Accept(utf32))
                .Select(t=>t.Parent)
                .Distinct()
                .ToList();

                    
        
        }

        return this.Trends;
    }
}
