using System.Reflection;

namespace XbyakSharp;
[System.AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
sealed class InstructionAttribute : Attribute
{
    readonly string Text;
    public InstructionAttribute(string text = "")
    {
        this.Text = text;
    }
    public bool IsPseudoCode { get; set; }
}

public record class Instruction
{
    public static List<Instruction> Extract(Type generatorType,Type instructionType, bool slashToDot = true)
    {
        var instructions = new List<Instruction>();
        if (generatorType != null)
        {
            var methods = generatorType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach (var method in methods)
            {
                if (method.GetCustomAttribute<InstructionAttribute>() is InstructionAttribute i)
                {
                    if (instructionType.Assembly.CreateInstance(instructionType.Name)
                        is Instruction instr)
                    {
                        instr.IsFaked = i.IsPseudoCode;
                        instr.Name = slashToDot? method.Name.Replace('_','.') : method.Name;
                        instr.Arguments = method.GetParameters().
                            Select(p => (p?.Name, p?.ParameterType, p?.DefaultValue)).ToList();
                        instr.GeneratorMethod = method;
                        instructions.Add(instr);
                    }
                }
            }
        }

        return instructions;
    }
    public string Name { get; private set; }
    public bool IsFaked { get; private set; }
    public List<(string name, Type type, object value)> Arguments { get;  set; } = new();
    public MethodInfo GeneratorMethod { get; private set; }
    public void Emit(ICodeGenerator generator)
    {
        this.GeneratorMethod?.Invoke(generator, this.Arguments.Select(a => a.value).ToArray());
    }
}
