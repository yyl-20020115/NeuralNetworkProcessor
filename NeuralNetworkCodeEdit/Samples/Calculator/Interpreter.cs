using Utilities;
using NeuralNetworkProcessor.ZRF;
using NeuralNetworkProcessor.Core;
using NeuralNetworkProcessor.Reflection;
using System.Runtime.CompilerServices;

namespace NeuralNetworkCodeEdit.Calculator;

public partial record class Node : INode<Node, InterpretrContext, double>
{
    //NOTICE: This supports multiple patterns in parallel
    public virtual string PatternName { get; protected set; } = string.Empty;
    public virtual object PatternTuple { get; protected set; } = null;
    public virtual object this[int index]
        => ValueTupleUtils.GetValueTupleElement(this.PatternTuple as ITuple, index);
    public virtual Node Compose(InterpretrContext context, string pattern, params (int index, string name, object value)[] parameters)
    {
        this.PatternName = pattern;
        if (parameters.Length > 0)
            this.PatternTuple = ValueTupleUtils.CreateValueTupleObject(
                    parameters.Select(parameter => parameter.value).ToArray());
        return this;
    }
    public virtual double Process(InterpretrContext context, double value)
        => this[0] is Node node ? node.Process(context, value) : value;
    public virtual double AsResult(InterpretrContext context, bool logic)
        => logic ? 1.0 : 0.0;
}
public partial record class Digit : Node
{
    public override string ToString() => _;
}
public partial record class Integer : Node
{
    public override string ToString()
        => this.ToString(0.0);

    public virtual string ToString(double value)
    => this.PatternTuple switch
    {
        ValueTuple<Digit>(var d) => d.ToString(),
        (Integer i, Digit d) => i.ToString() + d.ToString(),
        _ => value.ToString(),
    };

    public override double Process(InterpretrContext context, double value)
       => double.TryParse(this.ToString(value), out var result)
        ? result
        : value
        ;
}
public partial record class Term : Node
{
    public override double Process(InterpretrContext context, double value)
        => this.PatternTuple switch
        {
            (Term m, Mul, _, Factor p) => m.Process(context, value) * p.Process(context, value),
            (Term m, Div, _, Factor p) => m.Process(context, value) / p.Process(context, value),
            ValueTuple<Factor>(var p) => p.Process(context, value),
            _ => value,
        };
}
public partial record class Expression : Node
{
    public override double Process(InterpretrContext context, double value)
        => this.PatternTuple switch
        {
            (Expression a, Add, _, Term m) => a.Process(context, value) + m.Process(context, value),
            (Expression a, Sub, _, Term m) => a.Process(context, value) - m.Process(context, value),
            ValueTuple<Term>(var m) => m.Process(context, value),
            _ => value,
        };
}

public partial record class Factor : Node
{
    public override double Process(InterpretrContext context, double value)
        => this.PatternTuple switch
        {
            ValueTuple<Integer>(var i) => i.Process(context, value),
            (LParen, _, Expression e, RParen) => e.Process(context, value),
            _ => value,
        };
}
public partial record class Top : Node
{
    public override double Process(InterpretrContext context, double value)
        => this.PatternTuple switch
        {
            (_, Expression e, _) => e.Process(context, value),
            _ => value,
        };
}

public partial record class Interpreter
{
    public readonly FastParser Parser;

    public Interpreter()
    {
        if (ModelExtractor.Extract(
            typeof(Compiler).Assembly,
            typeof(Node),
            typeof(Node).Namespace,
            nameof(Calculator)) is Knowledge knowledge)
            this.Parser = new FastParser().Bind(Builder.Rebuild(knowledge));
    }

    public virtual List<Results> Parse(string expression)
        => this.Parser.Parse(expression);
    public virtual double Run(string expression)
        => this.Run(expression, new() { Interpreter = this });

    public virtual double Run(string expression, InterpretrContext context)
    {
        var results = this.Parse(expression);
        return results.Count switch
        {
            > 0 => ModelBuilder<Node, InterpretrContext, double>.Execute(
                results.First(), typeof(Node), typeof(Node).Assembly, context: context, value: double.NaN),
            _ => double.NaN
        };
    }
    public virtual double Run(List<Results> results)
        => this.Run(results, new() { Interpreter = this });
    public virtual double Run(List<Results> results, InterpretrContext context)
        => this.BuildFirstResult(results) is Node node
        ? ModelBuilder<Node, InterpretrContext, double>.Process(node, context, double.NaN)
        : double.NaN ;

    public virtual double Run(Node node)
        => this.Run(node, new() { Interpreter = this });
    public virtual double Run(Node node, InterpretrContext context)
        => ModelBuilder<Node, InterpretrContext, double>.Process(node, context, double.NaN);
    public virtual Node BuildFirstResult(List<Results> results)
        => results != null && results.Count > 0
            ? ModelBuilder<Node, string, double>.Build(
                results.First(), typeof(Node), typeof(Node).Assembly) as Node
            : null;
}
