using NNP.ZRF;

namespace NNP.Core;

public class Parser
{
    public List<Trend> Trends { get; private set; } = [];

    public Parser Bind(Concept concept)
    {
        this.Trends = Builder.Build(concept);
        return this;
    }

    public List<Trend> Parse()
    {


        return this.Trends;
    }
}
