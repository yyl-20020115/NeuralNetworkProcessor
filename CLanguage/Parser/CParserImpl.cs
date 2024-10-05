﻿using System;
using System.Diagnostics;
using System.Linq;
using CLanguage.Syntax;

namespace CLanguage.Parser;

public partial class CParser
{
    //
    // Controls the verbosity of the errors produced by the parser
    //
    static public int yacc_verbose_flag;
    private TranslationUnit _tu;
    ParserInput lexer;

    static readonly Token[] noTokens = [];
    static readonly object[] noObjects = [];

    public CParser ()
    {
        yyVals = noObjects;
        yyVal = string.Empty;
        _tu = new TranslationUnit ("uninitialized.c");
        lexer = new ParserInput (noTokens);
        //debug = new yydebug.yyDebugSimple();
    }

    public TranslationUnit ParseTranslationUnit (string name, string code, Preprocessor.Include include, Report report)
    {
        var lexed = new LexedDocument (new Document (name, code), report);
        return ParseTranslationUnit (report, System.IO.Path.GetFileNameWithoutExtension (name), include, lexed.Tokens);
    }

    public TranslationUnit ParseTranslationUnit (Report report, string name, Preprocessor.Include include, params Token[][] tokens)
    {
        var preprocessor = new Preprocessor (include, report, tokens);
        lexer = new ParserInput (preprocessor.Preprocess ());

        _tu = new TranslationUnit (name);

        if (lexer.Tokens.Length == 0)
            return _tu;

        try {
            yyparse (lexer);
        }
        catch (NotImplementedException err) {
            report.Error (9999, lexer.CurrentToken.Location, lexer.CurrentToken.EndLocation,
                "Not Supported: " + err.Message);
        }
        catch (NotSupportedException err) {
            report.Error (9001, lexer.CurrentToken.Location, lexer.CurrentToken.EndLocation,
                "Not Supported: " + err.Message);
        }
        catch (Exception err) when (err.Message == "irrecoverable syntax error") {
            report.Error (1001, lexer.CurrentToken.Location, lexer.CurrentToken.EndLocation,
                "Syntax error");
        }
        catch (yyParser.yyUnexpectedEof) {
            report.Error (1513, lexer.CurrentToken.Location, lexer.CurrentToken.EndLocation,
                "Incomplete");
        }
        catch (Exception err) {
            Debug.WriteLine (err);
            report.Error (9000, lexer.CurrentToken.Location, lexer.CurrentToken.EndLocation,
                "Parser Error: " + err.Message);
        }

        return _tu;
    }

    public static Expression? TryParseExpression (Report report, Token[] tokens)
    {
        var p = new CParser ();
        var prefix = new[] { new Token (TokenKind.AUTO, "auto"), new Token (TokenKind.IDENTIFIER, "_"), new Token ('=') };
        var suffix = new[] { new Token (';') };
        var tu = p.ParseTranslationUnit (report, CLanguageService.DefaultCodePath, ((_, __) => null), prefix, tokens, suffix);
        if (tu.Statements.FirstOrDefault () is MultiDeclaratorStatement mds && mds.InitDeclarators != null && mds.InitDeclarators.Count == 1 && mds.InitDeclarators[0].Initializer is ExpressionInitializer ei) {
            return ei.Expression;
        }
        return null;
    }

    void AddDeclaration (object a)
    {
        if (a is not Statement statement)
            return;
        _tu.AddStatement (statement);

        if (statement is MultiDeclaratorStatement mds) {
            switch (mds.Specifiers.StorageClassSpecifier) {
                case StorageClassSpecifier.Typedef when mds.InitDeclarators != null:
                    foreach (var i in mds.InitDeclarators) {
                        lexer.AddTypedef (i.Declarator.DeclaredIdentifier);
                    }
                    break;
                case StorageClassSpecifier.None when mds.Specifiers.TypeSpecifiers.Count > 0:
                    foreach (var i in mds.Specifiers.TypeSpecifiers) {
                        if (i.Kind == TypeSpecifierKind.Class ||
                            i.Kind == TypeSpecifierKind.Struct ||
                            i.Kind == TypeSpecifierKind.Union ||
                            i.Kind == TypeSpecifierKind.Enum) {
                            //Debug.WriteLine ($"Add typedef `{i.Name}`");
                            lexer.AddTypedef (i.Name);
                        }
                    }
                    break;
            }
        }
    }

    Declarator? FixPointerAndArrayPrecedence (Declarator d)
    {
        if (d is PointerDeclarator && d.InnerDeclarator != null && d.InnerDeclarator is ArrayDeclarator) {
            var a = d.InnerDeclarator;
            var p = d;
            var i = a.InnerDeclarator;
            a.InnerDeclarator = p;
            p.InnerDeclarator = i;
            return a;
        }
        else {
            return null;
        }
    }

    Declarator MakeArrayDeclarator (Declarator? left, TypeQualifiers tq, Expression? len, bool isStatic)
    {
        if (left != null && left.StrongBinding) {
            var i = left.InnerDeclarator;
            var a = new ArrayDeclarator (i, len);
            left.InnerDeclarator = a;
            return left;
        }
        else {
            return new ArrayDeclarator (left, len);
        }
    }

    Location GetLocation (object obj)
    {
        return Location.Null;
        /*
            if (obj is Tokenizer.LocatedToken)
                return ((Tokenizer.LocatedToken) obj).Location;
            if (obj is MemberName)
                return ((MemberName) obj).Location;

            if (obj is Expression)
                return ((Expression) obj).Location;

            return lexer.Location;*/
    }
}
