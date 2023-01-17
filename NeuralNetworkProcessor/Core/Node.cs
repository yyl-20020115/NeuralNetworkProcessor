using System;
using System.Collections.Generic;

namespace NeuralNetworkProcessor.Core;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class PatternAttribute : Attribute
{
    public const string OptionalText = "";
    public const string IgnoredText = null;
    public string[] Texts = Array.Empty<string>();
    public int[] Optionals = Array.Empty<int>();
    public bool AsPatterns = false;
    public string Alias { get; set; } = "";
    public PatternAttribute() { }
    public PatternAttribute(string text,params string[] texts)
    {
        var its = new string[1 + texts.Length];
        its[0] = text;
        Array.Copy(texts, 0, its, 1, texts.Length);
        this.Texts = its;
    }
    public PatternAttribute(int i, params int[] optionals)
    {
        var its = new int[1 + optionals.Length];
        its[0] = i;
        Array.Copy(optionals, 0, its, 1, optionals.Length);
        this.Optionals = its;
    }
    public int[] GetOptionalIndices()
    {
        if (this.Texts != null && this.Texts.Length > 0)
        {
            var ts = (string[])this.Texts.Clone();

            if (this.Optionals != null && this.Optionals.Length > 0)
                for (int i = 0; i < this.Optionals.Length; i++)
                    if (i >= 0 && i < ts.Length)
                        ts[i] = OptionalText;
            
            var list = new List<int>();
            for (int i = 0; i < ts.Length; i++)
                if (ts[i] == OptionalText)
                    list.Add(i);
            return list.ToArray();
        }else if(this.Optionals!=null && this.Optionals.Length > 0)
            return this.Optionals;
        return Array.Empty<int>();
    }
}
public interface INode { }
public interface INode<N,C,V> : INode
{
    N Compose(C context, string pattern, params (int index, string name, object value)[] parameters);
    V Process(C context = default, V value = default);
}
