using System.Reflection;
using NNP.ZRF;
using NNP.Core;
using Utilities;

namespace NNP.Reflection;

public static class ModelBuilder<N, C, V> where N : class
{
    public static V? Execute(Trend trend, Type? baseType = null, Assembly? assembly = null, string @namespace = "", C? context = default, V? value = default)
        => Process(Build(trend, baseType, assembly, @namespace, context), context, value);
    public static V? Execute(Trend trend, Dictionary<string, Type> types, C context = default, V value = default)
        => Process(Build(trend, types, context), context, value);
    public static V? Process(INode<N, C, V>? node, C? context = default, V? value = default)
        => node != null ? node.Process(context, value) : default;
    public static INode<N, C, V> Build(Trend? trend, Type? baseType = null, Assembly? assembly = null, string @namespace = "", C? context = default)
         => Build(trend,
            new Dictionary<string, Type>(assembly.GetTypes().Where(t => (string.IsNullOrEmpty(@namespace) || @namespace == t.Namespace)
            && t.IsSubclassOf(baseType ?? typeof(INode))).Select(u => new KeyValuePair<string, Type>(u.Name, u))), context);
    public static INode<N, C, V> Build(Trend? trend, Dictionary<string, Type> types, C context)
        => Build(trend, types, context, out var _);
    public static INode<N, C, V> Build(Trend? trend, Dictionary<string, Type> types, C context, out string? val)
    {
        val = null;
        if (trend != null)
        {
            if (trend.Description == Description.Default)
            {
                //use the text as return
                val = trend.Name;
                return null;
            }
            else if (types.TryGetValue(trend.Name, out var type)
                && type != null && CreateObject(type) is N n)
            {
                var names = new List<(Type Type, string Name)>();
                var pi = GetPatternPropertyInfoAt(type, trend.Index);
                //should not happen
                if (pi == null) 
                    return null;
                //string
                else if (trend.Line.Count == 1)
                {
                    object v = pi.PropertyType == typeof(string)
                        ? trend.Line[0].ToString()
                        : Build(trend.Line[0].Sources.First(), types, context)
                        ;
                    if (v == null)
                    {

                    }
                    v ??= string.Empty;
                    pi.SetValue(n, v);
                    if (n is INode<N, C, V> model)
                        model.Compose(context, pi.Name, (0, pi.Name, v));
                }
                //Try value tuple
                else if (ModelExtractor.ExtractValueTupleTypes(pi.PropertyType, names))
                {
                    //Pattern needs description
                    var cn = trend.Line.Count;
                    if (cn == 0) cn = names.Count;
                    var rs = new object[cn];
                    var datas = new object[cn];
                    //foreach (var ext in trend.SymbolExtractions)
                    //    if (ext is TextSpan _span && _span.Cell.Index is int ci)
                    //        if (ci >= 0 && ci < cn) rs[ci] = _span;

                    var subtypes = names.Select(tn => tn.Type).ToArray();
                    //for (int i = 0; i < cn; i++)
                    //    if (rs[i] is string _span && _span.Buddy != null)
                    //    {
                    //        var value = Build(
                    //            _span,
                    //            types, context, out var text);
                    //        if (value != null)
                    //            datas[i] = value;
                    //        else if (subtypes[i] == typeof(string))
                    //            datas[i] = text;
                    //        else if (subtypes[i].IsSubclassOf(typeof(INode)))
                    //            datas[i] = CreateObject(subtypes[i]);
                    //    }

                    FillValueTupleProperty(n, pi,
                        pi.PropertyType,
                        subtypes,
                        datas);
                    var parameters = new List<(int, string, object)>();
                    if (n is INode<N, C, V> model)
                    {
                        for (int i = 0; i < datas.Length; i++)
                            parameters.Add((i, names[i].Name, datas[i]));
                        model.Compose(context, pi.Name, parameters.ToArray());
                    }
                }
                return n as INode<N, C, V>;
            }
        }
        return null;
    }
    public static object FillValueTupleProperty(
        object o,
        PropertyInfo property,
        Type tupleType,
        Type[] subTypes,
        params object[] values)
    {
        var TupleTypeNames = new List<(Type Type, string Name)>();
        if (tupleType == property.PropertyType
            && values.Length == subTypes.Length
            && ModelExtractor.ExtractValueTupleTypes(property.PropertyType, TupleTypeNames)
            && subTypes.SequenceEqual(TupleTypeNames.Select(t => t.Type)))
            property.SetValue(o, ValueTupleUtils.CreateValueTupleObject(subTypes, values));
        return o;
    }
    public static PropertyInfo[] GetPatternProperties(Type type)
        => type.GetProperties()
        .Where(p => ModelExtractor.ExtractAttribute<PatternAttribute>(p) != null)
        .ToArray();

    public static PropertyInfo? GetPatternPropertyInfoAt(Type type, int index)
    {
        PropertyInfo? pi = null;
        if (GetPatternProperties(type) is PropertyInfo[] ps)
        {
            pi = ps.FirstOrDefault(p => Array.IndexOf(ps, p) == index);
            pi ??= ps.Length > 0 ? ps[0] : null;
        }
        return pi;
    }
    public static N? CreateObject(Type type)
    {
        var pl = -1;
        var cc = type.GetConstructors().Where(
            ci => (ci.GetParameters() is ParameterInfo[] ps)
                && ((pl = ps.Length) == 0 || ps.All(p => p.IsOptional)))
            .FirstOrDefault();
        return (cc != null && pl >= 0) &&
            cc.Invoke(new object[pl]) is N n ? n : default;
    }
}
