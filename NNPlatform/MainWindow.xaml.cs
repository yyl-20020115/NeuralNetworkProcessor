using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using GraphSharp.Controls;
using NeuralNetworkProcessor.Core;
using NeuralNetworkProcessor.Reflection;
using Utilities;
using NeuralNetworkProcessorSample.Samples.Calculator;

namespace NNPlatform;

public static class ItemsControlHelper
{
    public static T Append<T, E>(this T it, E e, bool clear = false) where T : ItemsControl
    {
        if (clear) it.Items.Clear();
        it.Items.Add(e);
        return it;
    }
    public static T Append<T, E>(this T it, IEnumerable<E> ts, bool clear = false) where T : ItemsControl
    {
        if (clear) it.Items.Clear();
        foreach (var t in ts) it.Items.Add(t);
        return it;
    }
}
/// <summary>
/// MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    protected int position = 0;
    protected Input input = null;
    protected FastCompiler WorkingCompiler = new();
    protected List<Results> WorkingResults = null;
    public MainWindow()
    {
        InitializeComponent();
    }
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        this.graphLayout.Margin = new Thickness(10.0);
        this.graphLayout.OverlapRemovalAlgorithmType = "FSA";
        this.graphLayout.HighlightAlgorithmType = "Simple";
        this.graphLayout.LayoutAlgorithmType = "Dot";
        this.graphLayout.LayoutMode = LayoutMode.Automatic;

        this.UpdateGraphs();
    }
    protected void UpdateGraphs()
    {
        this.Edit.Rebind(this.WorkingCompiler.Parser);
        this.UpdateAggregationTree(this.AggregationTree, this.WorkingCompiler.Parser.Aggregation);
        this.graphLayout.Graph = GraphGenerator.GenerateNetwork(this.WorkingCompiler.Parser.Aggregation);
    }
    protected void UpdateAggregationTree(TreeView tree, Aggregation a, bool IsExpanded = true)
        => tree.Append(new TreeViewItem { Header = a.Name, IsExpanded = IsExpanded }
            . Append(a.Clusters.Select(c => new TreeViewItem { Header = c.Definition.Text, IsExpanded = IsExpanded }
            . Append(c.Trends.Select(t =>new TreeViewItem { Header = t.Description, IsExpanded = IsExpanded }
            . Append(t.Cells.Select(s => new TreeViewItem { Header = s.Phrase, IsExpanded = IsExpanded }))))))
            , true);
    private void RelayoutButton_Click(object sender, RoutedEventArgs e)
        => this.graphLayout.Relayout();
    private void ParseStepButton_Click(object sender, RoutedEventArgs e)
    {
        if (this.input == null) this.WorkingCompiler.Parser.Reset();
        this.position = this.WorkingCompiler.Parser.ParseStep(
            position, input ??= InputProvider.CreateInput(this.InputTextBox.Text));
        if (this.position >= 0L)
        {
            this.InputTextBox.SelectionStart = (int)position;
            this.InputTextBox.SelectionLength = 1;
            this.InputTextBox.Focus();
            this.CounterText.Text = this.position.ToString();
        }
        else if (this.position == -1)
        {
            this.position = 0;
            this.CounterText.Text = this.position.ToString();
            this.input = null;
            this.UpdateResults(this.ResultsTree,
                this.WorkingResults = this.WorkingCompiler.Parser.LastResults);
        }
    }
    protected void UpdateResults(TreeView ResultsTree, List<Results> ResultsList,HashSet<Extraction> visited = null, bool IsExpanded=true)
    {
        this.Edit.SetResults(ResultsList);
        var item = default(TreeViewItem);
        ResultsTree.Items.Clear();
        ResultsList.ForEach(r => {
                ResultsTree.Items.Add(item = 
                    new TreeViewItem { 
                        Header =$"Result={r.Symbol.Text}", 
                        IsExpanded = IsExpanded, 
                        Tag = r });
                this.UpdateResultsTree(item, r, visited, IsExpanded);
            });
    }
    protected void UpdateResultsTree(TreeViewItem ResultsViewItem, Results rs, HashSet<Extraction> visited = null, bool IsExpanded = true)
    {
        var item = default(TreeViewItem);
        rs.Patterns.ToList().ForEach(p => {
                ResultsViewItem.Items.Add(item = 
                    new TreeViewItem { 
                        Header = $"Pattern[{p.Position},{p.EndPosition})={p.Definition?.Text??string.Empty}:{p.Description.ToString().Trim()}", 
                        IsExpanded = IsExpanded, 
                        Tag = p });
                this.UpdateResultsTree(item, p, visited, IsExpanded);
        });
    }
    protected void UpdateResultsTree(TreeViewItem PatternViewItem, Pattern ps, HashSet<Extraction> visited = null, bool IsExpanded = true)
    {           
        ps.SymbolExtractions.ToList().ForEach(s => {
                switch (s)
                {
                    case Results rs:
                        this.UpdateResultsTree(
                            PatternViewItem, rs, visited, IsExpanded);
                        break;
                    case TextSpan t:
                        if (t.Buddy != null
                            && t.Buddy != Results.Default)
                        {
                            var ti = new TreeViewItem
                            {
                                Header = $"Span={t.Symbol.Text}",
                                IsExpanded = IsExpanded,
                                Tag = t
                            };
                            PatternViewItem.Items.Add(ti);
                            UpdateResultsTree(ti, t.Buddy, visited, IsExpanded);
                        }
                        else
                        {
                            PatternViewItem.Items.Add(
                                new TreeViewItem
                                {
                                    Header = $"Value={t.Text}",
                                    IsExpanded = IsExpanded,
                                    Tag = t
                                });
                        }
                        break;
                }
        });
    }
    private void ParseAllButton_Click(object sender, RoutedEventArgs e)
    {
        this.WorkingResults = WorkingCompiler.Parse(this.InputTextBox.Text);
        this.UpdateResults(this.ResultsTree, this.WorkingResults);
        this.Edit.Focus();
    }
    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        this.Output.Text = string.Empty;
        this.WorkingCompiler.Parser.Reset();
    }
    public bool OnReportError(
        ErrorType Type,
        int Position, 
        int UTF32 = -1,
        IList<MatrixLine> Expectations = null) {
        this.Output.Text
            += $"At ({Position}) Error = "
            + Type + (UTF32 >= 0 ? $",Char = '{char.ConvertFromUtf32(UTF32)}'" : "")
            + Environment.NewLine
            + (Expectations!=null?
            "Expectings:"
            + (Expectations?.Aggregate("", (a, b) => 
                a + b.Trend.GetFocusTextAt(b.Pivot) + Environment.NewLine)):"");
        return true;
    }
    private void ProcessButton_Click(object sender, RoutedEventArgs e)
    {
        this.Output.Text = string.Empty;
        this.ParseAllButton_Click(sender, e);
        foreach (var result in this.WorkingResults)
        {
            var data = ModelBuilder<Node, string, double>.Execute(
                result,
                typeof(Node),
                typeof(Node).Assembly)
                ;
            this.Output.Text
                += result.ToString()
                + " = "
                + data
                + Environment.NewLine
                ;
        }
    }

    private void Edit_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        this.Edit.Focus();
    }
}
