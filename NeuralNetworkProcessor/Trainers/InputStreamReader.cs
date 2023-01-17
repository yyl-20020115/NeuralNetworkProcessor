using System.Collections.Generic;
using System.IO;
using System.Text;
using Utilities;

namespace NeuralNetworkProcessor.Trainers;

public static class InputStreamReader
{
    public static Input CreateInput(TextReader reader)
    {
        IEnumerable<(int,bool)> InputFunction()
        {
            foreach (var c in Read(reader)) yield return (c,false);
        }
        return InputFunction;
    }
    public static bool IsWhiteSpace(this Rune r)
        => r.IsBmp && char.IsWhiteSpace((char)r.Value);
    public static bool IsWhiteSpace(this int ch) 
        => ch >= 0 && ch < 0x10000 && char.IsWhiteSpace((char)ch);
    public static int LineIndex = 0;
    public static IEnumerable<int> Read(TextReader reader)
    {
        while (reader.ReadLine() is string line)
        {
            LineIndex++;
            foreach (var c in line.Trim().EnumerateRunes())
                if (!c.IsWhiteSpace())
                    yield return c.Value;
        }
    }
}
