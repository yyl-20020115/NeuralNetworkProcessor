using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IKVM.Reflection;
using IKVM.Reflection.Emit;
using NeuralNetworkProcessor.Core;
using NeuralNetworkProcessor.Reflection;
using NeuralNetworkProcessor.ZRF;

namespace NeuralNetworkProcessor.Samples.Calculator;

public class Compiler
{
    public string DefaultGlobalAssembliesCachePath { get; set; }
        = "C:\\Windows\\Microsoft.net\\Framework\\v4.0.30319\\";

    public Universe Universe { get; } = new();
    public FastParser Parser { get; private set; }
    public Compiler()
    {
        if (ModelExtractor.Extract(
            typeof(Compiler).Assembly,
            typeof(Node),
            typeof(Node).Namespace,
            nameof(Calculator)) is Knowledge knowledge)
            this.Parser = new FastParser().Bind(Builder.Rebuild(knowledge));
        this.Universe.AssemblyResolve += Universe_AssemblyResolve;
    }

    protected virtual Assembly Universe_AssemblyResolve(object sender, IKVM.Reflection.ResolveEventArgs args)
        => this.Universe.LoadFile(
             Path.Combine(DefaultGlobalAssembliesCachePath, $"{args.Name}.dll"));
    public virtual List<Results> Parse(string expression)
        => this.Parser.Parse(expression);
    public virtual Node Build(List<Results> results)
        => results != null && results.Count > 0
            ? ModelBuilder<Node, InterpretrContext, double>.Build(
                results.First(), typeof(Node), typeof(Node).Assembly) as Node
            : null;

    public virtual AssemblyBuilder Compile(
        string expression,
        string fileName,
        string functionName = "CalcFunction",
        string moduleName = "CalcModule",
        string assemblyName = "Calculator",
        PortableExecutableKinds kind = PortableExecutableKinds.ILOnly,
        ImageFileMachine machine = ImageFileMachine.I386)
        => this.Compile(this.Parse(expression), fileName, functionName, moduleName, assemblyName, kind, machine);
    public virtual AssemblyBuilder Compile(
        List<Results> results,
        string fileName,
        string functionName = "CalcFunction",
        string moduleName = "CalcModule",
        string assemblyName = "Calculator",
        PortableExecutableKinds kind = PortableExecutableKinds.ILOnly,
        ImageFileMachine machine = ImageFileMachine.I386)
    {
        var root = ModelBuilder<Node, string, double>.Build(
            results.FirstOrDefault(), typeof(Node),
            typeof(Node).Assembly, typeof(Node).Namespace) as Node;
        var builder = this.Emit(
                root,
                functionName,
                moduleName,
                fileName,
                assemblyName);
        builder.Save(fileName, kind, machine);
        return builder;
    }
    public virtual AssemblyBuilder Emit(Node root,
        string functionName,
        string moduleName,
        string fileName,
        string assemblyName)
    {
        var builder =
            this.Universe.DefineDynamicAssembly
            (new(assemblyName),
                AssemblyBuilderAccess.Save);
        this.Emit(root, functionName, moduleName, fileName, builder);
        return builder;
    }

    public virtual void Emit(Node root, string functionName, string moduleName, string fileName, AssemblyBuilder asmBuilder)
        => this.Emit(root, functionName, asmBuilder.DefineDynamicModule(moduleName, fileName));
    public virtual void Emit(Node root, string functionName, ModuleBuilder moduleBuilder)
    {
        var methodBuilder = moduleBuilder.DefineGlobalMethod(
            functionName, MethodAttributes.Public | MethodAttributes.Static, this.Universe.GetBuiltInType("System", "Double"),
            IKVM.Reflection.Type.EmptyTypes);
        this.Emit(root, methodBuilder);
        moduleBuilder.CreateGlobalFunctions();
    }
    public virtual void Emit(Node root, MethodBuilder methodBuilder)
    {
        var generator = methodBuilder.GetILGenerator();
        this.Emit(root, generator);
        //NOTICE: this is used for fixing values
        if (generator.ILOffset == 0) generator.Emit(OpCodes.Ldc_R8, 0.0);
        generator.Emit(OpCodes.Ret);
    }
    public virtual void Emit(Node node, ILGenerator g)
    {
        switch (node)
        {
            case Top top:
                this.Emit(top.PatternTuple, g);
                break;
            case Expression expression:
                this.Emit(expression.PatternTuple, g);
                break;
            case Term term:
                this.Emit(term.PatternTuple, g);
                break;
            case Factor factor:
                this.Emit(factor.PatternTuple, g);
                break;
            case Integer integer:
                if (double.TryParse(integer.ToString(), out var v))
                    g.Emit(OpCodes.Ldc_R8, v);
                else
                    g.Emit(OpCodes.Ldc_R8, 0.0);
                break;
        }
    }
    public virtual void Emit(object tuple, ILGenerator generator)
    {
        switch (tuple)
        {
            case (_, Expression expression, _):
                this.Emit(expression, generator);
                break;
            case (Term term, Mul, _, Factor factor):
                this.Emit(term, generator);
                this.Emit(factor, generator);
                generator.Emit(OpCodes.Mul);
                break;
            case (Term term, Div, _, Factor factor):
                this.Emit(term, generator);
                this.Emit(factor, generator);
                generator.Emit(OpCodes.Div);
                break;
            case (Expression expression, Add, _, Term term):
                this.Emit(expression, generator);
                this.Emit(term, generator);
                generator.Emit(OpCodes.Add);
                break;
            case (Expression expression, Sub, _, Term term):
                this.Emit(expression, generator);
                this.Emit(term, generator);
                generator.Emit(OpCodes.Sub);
                break;
            case (LParen, _, Expression expression, RParen):
                this.Emit(expression, generator);
                break;
            case ValueTuple<Term>(var term):
                this.Emit(term, generator);
                break;
            case ValueTuple<Factor>(var factor):
                this.Emit(factor, generator);
                break;
            case ValueTuple<Expression>(var expression):
                this.Emit(expression, generator);
                break;
            case ValueTuple<Integer>(var integer):
                this.Emit(integer, generator);
                break;
        }
    }
}
