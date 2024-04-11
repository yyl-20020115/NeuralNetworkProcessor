using NeuralNetworkCodeEdit.Calculator;
using NNP.Core;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace NeuralNetworkCodeEdit;

/// <summary>
/// NCEdit.xaml 的交互逻辑
/// </summary>
public partial class NCEdit : UserControl
{
    public NCEdit()
    {
        InitializeComponent();
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        this.Build();
    }

    public void Build()
    {
        var input = "2+((3+4))";
        var block = new TextBlock() {  };
        var trees = new FastCompiler().Parse(input);

        //var ret = new ResultsPrinter().PrintList(trees).ToString();

        this.BuildWith(trees, block);

        this.MainGraid.Children.Add(block);
    }

    protected void BuildWith(IEnumerable<SymbolExtraction> extractions, TextBlock block)
    {
        foreach (var result in extractions)
        {
            if (result is Results results)
            {
                foreach (var pattern in results.Patterns)
                {
                    this.BuildWith(pattern.SymbolExtractions, block);
                }
            }
            else if (result is TextSpan textSpan)
            {
                if (textSpan.AccompanyResults is not null)
                {
                    foreach (var pattern in textSpan.AccompanyResults.Patterns)
                    {
                        this.BuildWith(pattern.SymbolExtractions, block);
                    }
                }
                else
                {
                    var text = textSpan.Text ?? string.Empty;
                    if (text.Contains('\r') || text.Contains('\n'))
                    {
                        block.Inlines.Add(new LineBreak());
                    }
                    else
                    {
                        block.Inlines.Add(new Run(text));
                    }
                }
            }
        }

    }


}
