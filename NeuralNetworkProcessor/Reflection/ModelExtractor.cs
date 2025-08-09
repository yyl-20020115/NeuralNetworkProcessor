using System;
using System.Linq;
using System.Reflection;
using NeuralNetworkProcessor.ZRF;
using NeuralNetworkProcessor.Core;
using System.Collections.Generic;

namespace NeuralNetworkProcessor.Reflection;

public static class ModelExtractor
{
    public static Knowledge Extract(
        Assembly assembly, Type baseType = null, string @namespace = "", string name = "")
        => Extract(assembly.GetTypes().Where(
            type => (string.IsNullOrEmpty(@namespace) || @namespace == type.Namespace)
            && type.IsSubclassOf(baseType ?? typeof(INode))), name).Compact().BackBind();
    public static Knowledge Extract(IEnumerable<Type> types, string name = "")
        => new(name, types.Select(
            type => new Definition(
                type.Name,
                type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .SelectMany(p => ExtractProperty(p)).ToList()
            )).ToList());
    public static T ExtractAttribute<T>(PropertyInfo property) where T:Attribute
        =>  property.GetCustomAttribute<T>() is T pt
            ? pt
            : property?.DeclaringType
                ?.GetConstructors(BindingFlags.Instance| BindingFlags.Public)
                ?.Where(c=>c.GetParameters().All(pu=>pu.IsOptional))
                ?.FirstOrDefault()
                ?.GetParameters()
                ?.Where(m => m.Name == property.Name && m.GetCustomAttribute<T>() != null)
                ?.FirstOrDefault()?.GetCustomAttribute<T>();
    public static List<Description> ExtractProperty(PropertyInfo property)
    {
        var type = property.PropertyType;
        var attribute = ExtractAttribute<PatternAttribute>(property);
        var names = new List<(Type Type, string Name)>();
        if (attribute == null)
            return [];
        else if (attribute.AsPatterns)
        {
            return [.. attribute.Texts.Select(
                text => new Description([
                    new ($"\"{text}\"")
                ]))];
        }
        else if (ExtractValueTupleTypes(type, names))
        {
            var phases = names.Select(
                type => new Phrase(type.Type.Name) { Extension = type.Name }).ToList();
            if (attribute.Texts != null)
                for (var i = 0; i < Math.Min(attribute.Texts.Length, phases.Count); i++)
                {
                    var text = attribute.Texts[i];
                    if (text == null) phases[i].Optional = true;
                    else if (text.Length == 0) continue;
                    else phases[i].Text = $"\"{text}\""; //we should keep this text
                }
            if (attribute.Optionals != null)
                foreach (var i in attribute.Optionals)
                    if (i >= 0 && i < phases.Count)
                        phases[i].Optional = true;
            return [new(phases)];
        }
        else //ty is simple type
        {
            var opt = (attribute != null)
                    && ((attribute.Texts != null
                    && attribute.Texts.Length >= 1
                    && attribute.Texts[0] == "")
                    || (attribute.Optionals != null
                    && Array.IndexOf(attribute.Optionals, 0) >= 0));

            var text = attribute?.Texts != null && attribute.Texts.Length > 0 ?$"\"{attribute.Texts[0]}\""  : null;
            return
            [
                new([new (text ?? type.Name, opt)])
            ];
        }
    }
    public static bool IsValueTupleType(Type type)
        => type.IsValueType && type.Name.StartsWith("ValueTuple`");
    public static bool ExtractValueTupleTypes(Type type, List<(Type,string)> types)
    {
        if(IsValueTupleType(type))
        {
            var fields = type.GetFields();
            var length = fields.Length;
            var ext = (length == 8)
                && (fields[7]?.FieldType?.Name?.StartsWith("ValueTuple`"))
                .GetValueOrDefault();
            types.AddRange(fields.Take(ext?7:length).Select(f =>(f.FieldType, f.Name)));
            if (ext) ExtractValueTupleTypes(fields[7].FieldType, types);
            return true;
        }
        return false;
    }
}
