using System.IO;

namespace SharpLua
{
    public static class LuaDecompiler
    {
        public static void Decompile(string input, string output) 
            => Decompile(new LuaFileReader().WithInit(input).ReadNextFunction(), new StreamWriter(output));
        public static void Decompile(Function function, TextWriter writer)
            => new CodeGenerator(writer).Write(function);
    }
}
