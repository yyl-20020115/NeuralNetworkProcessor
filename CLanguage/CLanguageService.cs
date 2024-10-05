﻿using System;
using CLanguage.Syntax;
using CLanguage.Parser;
using System.Collections.Generic;
using System.Linq;
using CLanguage.Compiler;
using CLanguage.Types;

namespace CLanguage;

public static class CLanguageService
{
    public const string DefaultCodePath = "main.cpp";

    public static TranslationUnit ParseTranslationUnit (string code)
    {
        return ParseTranslationUnit (code, new Report ());
    }

    public static TranslationUnit ParseTranslationUnit (string code, Report report)
    {
        var parser = new CParser ();
        return parser.ParseTranslationUnit (DefaultCodePath, code, ((_, __) => null), report);
    }

    public static TranslationUnit ParseTranslationUnit (string code, Report.Printer printer)
    {
        return ParseTranslationUnit (code, new Report (printer));
    }

    public static Interpreter.CInterpreter CreateInterpreter (string code, MachineInfo? machineInfo = null, Report.Printer? printer = null)
    {
        var exe = Compile (code, machineInfo, printer);

        var i = new Interpreter.CInterpreter (exe);

        return i;
    }

    public static Interpreter.Executable Compile (string code, MachineInfo? machineInfo = null, Report.Printer? printer = null)
    {
        var report = new Report (printer);

        var mi = machineInfo ?? new MachineInfo ();
        var doc = new Document (DefaultCodePath, code);
        var options = new CompilerOptions (mi, report, [doc]);

        var c = new CCompiler (options);
        var exe = c.Compile ();

        return exe;
    }

    public static ColorSpan[] Colorize (string code, MachineInfo? machineInfo = null, Report.Printer? printer = null)
    {
        var report = new Report (printer);

        var mi = machineInfo ?? new MachineInfo ();

        var doc = new Document (DefaultCodePath, code);
        var lexed = new LexedDocument (doc, report);

        var compiler = new CCompiler (new CompilerOptions (mi, report, [doc]));
        var exe = compiler.Compile ();

        IEnumerable<string> GetFuncs ()
        {
            foreach (var f in mi.InternalFunctions) {
                yield return f.Name;
            }
            foreach (var g in exe.Globals) {
                if (g.VariableType is CStructType)
                    yield return g.Name;
            }
        }
        var funcs = new HashSet<string> (GetFuncs ());

        var tokens = lexed.Tokens.Where (x => x.Kind != TokenKind.EOL).Select (ColorizeToken).ToArray ();

        return tokens;

        ColorSpan ColorizeToken (Token token) => new ColorSpan {
            Index = token.Location.Index,
            Length = token.EndLocation.Index - token.Location.Index,
            Color = GetTokenColor (token),
        };

        SyntaxColor GetTokenColor (Token token)
        {
            switch (token.Kind) {
                case TokenKind.INT:
                case TokenKind.SHORT:
                case TokenKind.LONG:
                case TokenKind.CHAR:
                case TokenKind.FLOAT:
                case TokenKind.DOUBLE:
                case TokenKind.BOOL:
                case TokenKind.TYPE_NAME:
                    return SyntaxColor.Type;
                case TokenKind.IDENTIFIER:
                    if (token.Value is string s) {
                        if (funcs.Contains (s))
                            return SyntaxColor.Function;
                        switch (s) {
                            case "uint8_t":
                            case "uint16_t":
                            case "uint32_t":
                            case "uint64_t":
                            case "int8_t":
                            case "int16_t":
                            case "int32_t":
                            case "int64_t":
                            case "boolean":
                                return SyntaxColor.Type;
                            case "include":
                            case "define":
                            case "ifdef":
                            case "ifndef":
                            case "elif":
                            case "endif":
                                return SyntaxColor.Keyword;
                        }
                    }
                    return SyntaxColor.Identifier;
                case TokenKind.CONSTANT:
                    if (token.Value is char)
                        return SyntaxColor.String;
                    return SyntaxColor.Number;
                case TokenKind.TRUE:
                case TokenKind.FALSE:
                    return SyntaxColor.Number;
                case TokenKind.STRING_LITERAL:
                    return SyntaxColor.String;
                default:
                    if (token.Kind < 128 || Lexer.OperatorTokens.Contains (token.Kind))
                        return SyntaxColor.Operator;
                    else if (Lexer.KeywordTokens.Contains (token.Kind)) {
                        return token.StringValue switch {
                            "unsigned" or "signed" => SyntaxColor.Type,
                            _ => SyntaxColor.Keyword,
                        };
                    }
                    break;
            }
            return SyntaxColor.Comment;
        }
    }

    public static void Run (string code)
    {
        Interpreter.CInterpreter.Run (code);
    }

    public static object Eval (string expression, string? includeCode = "")
    {
        var codeToCompile = (includeCode ?? "") + @"
auto __evalResult = " + expression + @";
void start() {
    __cinit();
}
";
        var exe = CCompiler.Compile (codeToCompile);
        var global = exe.Globals.First (x => x.Name == "__evalResult");
        var interpreter = new Interpreter.CInterpreter (exe);
        interpreter.Reset ("start");
        interpreter.Run ();

        var globalType = global.VariableType;
        var resultValues = new Value[globalType.NumValues];
        Array.Copy (interpreter.Stack, global.StackOffset, resultValues, 0, resultValues.Length);

        return globalType.GetClrValue (resultValues, exe.MachineInfo);
    }
}
