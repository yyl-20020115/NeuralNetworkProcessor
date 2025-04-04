﻿using NNP.Calculator;
using NNP.Core;
using System.Reflection;

namespace NNP;

public static class ProgramEntry
{
    public static void Main(string[] args)
    {
        var input = Path.Combine(Environment.CurrentDirectory,
            args.Length > 0 ? args[0] : "input.txt");

        using var reader = new StreamReader(input);
        var root = Path.Combine(
            Environment.CurrentDirectory, "calc-");
        var text = string.Empty;
        var compile = false;
        var separator = new string('=', 64);
        var lines = new List<(string, int)>();

        int i = 0;
        while (null != (text = reader.ReadLine()))
        {
            text = text.Trim();
            if (text.Length == 0 || text.StartsWith('#')) continue;
            lines.Add((text, i++));
        }

        lines./*AsParallel().ForAll*/ForEach(line =>
        {
            var (l, i) = line;
            var output_file = Path.Combine(
                Environment.CurrentDirectory,
                $"output_{i:D4}.txt");

            using var writer = new StreamWriter(output_file);
            var path = root + i + ".dll";
            var compiler = new FastCompiler();
            var interpreter = new Interpreter();
            var branches = compiler.Parse(l);
            var printer = new TrendPrinter();
            printer.PrintList(branches);

            //var node = compiler.Build(branches);
            //var result = interpreter.Run(node);

            var dump = branches.Count > 0 ? branches[0].Flattern() : string.Empty;

            writer.WriteLine($"Input({i}):\"{l}\"");
//            writer.WriteLine($"Result: {dump} = {result}");
            writer.WriteLine($"PASSED: {(dump == l ? "YES" : "NO")}");
            writer.WriteLine($"Tree:");
            writer.Write(printer);
            writer.WriteLine(separator);

            if (compile)
            {
                compiler.Compile(
                    branches,
                    path,
                    "CalcFunction" + i,
                    "CalcModule" + i,
                    "Calculator" + i
                    );
                var assembly = Assembly.LoadFrom(path);

                var module = assembly?.GetModules()?.FirstOrDefault();
                var method = module?.GetMethods(BindingFlags.Public | BindingFlags.Static)?.FirstOrDefault();
                var @object = method?.Invoke(null, null);
                writer.WriteLine($"{l} = {dump} = {@object}");
            }
        });
    }

}
