﻿using System.Collections.Generic;
using System.Linq;

namespace CLanguage.Syntax;

public abstract class Declarator (Declarator? innerDeclarator)
{
    public abstract string DeclaredIdentifier { get; }
    public bool StrongBinding { get; set; }
    public Declarator? InnerDeclarator { get; set; } = innerDeclarator;

    public override string ToString () => DeclaredIdentifier;
}

public class IdentifierDeclarator : Declarator
{
    public string Identifier { get; private set; }

    public List<string> Context { get; } = new List<string> ();

    public override string DeclaredIdentifier {
        get {
            return Identifier;
        }
    }

    public IdentifierDeclarator (string id) : base (innerDeclarator: null)
    {
        Identifier = id;
    }

    public IdentifierDeclarator Push (string id)
    {
        Context.Add (Identifier);
        Identifier = id;
        return this;
    }

    public override string ToString ()
    {
        return Identifier;
    }
}

public class ArrayDeclarator : Declarator
{
    public Expression? LengthExpression { get; set; }
    public TypeQualifiers TypeQualifiers { get; set; }
    public bool LengthIsStatic { get; set; }

    public override string DeclaredIdentifier {
        get {
            return (InnerDeclarator != null) ? InnerDeclarator.DeclaredIdentifier : "";
        }
    }

    public ArrayDeclarator (Declarator? innerDeclarator, Expression? length) : base (innerDeclarator)
    {
        LengthExpression = length;
    }
}

public class FunctionDeclarator : Declarator
{
    public List<ParameterDeclaration> Parameters { get; set; }

    public override string DeclaredIdentifier {
        get {
            return (InnerDeclarator != null) ? InnerDeclarator.DeclaredIdentifier : "";
        }
    }

    public bool CouldBeCtorCall => Parameters.Count == 0 || Parameters.All (x => x.CtorArgumentValue != null);

    public FunctionDeclarator (Declarator innerDeclarator, List<ParameterDeclaration> parameters)
        : base (innerDeclarator)
    {
        Parameters = parameters;
    }

    public FunctionDeclarator (List<ParameterDeclaration> parameters)
        : base (null)
    {
        Parameters = parameters;
    }

    public override string ToString ()
    {
        return DeclaredIdentifier + "(" + string.Join (", ", Parameters) + ")";
    }
}

public class Pointer
{
    public TypeQualifiers TypeQualifiers { get; set; }
    public Pointer? NextPointer { get; set; }

    public Pointer (TypeQualifiers qual, Pointer p)
    {
        TypeQualifiers = qual;
        NextPointer = p;
    }

    public Pointer (TypeQualifiers qual)
    {
        TypeQualifiers = qual;
    }
}

public class PointerDeclarator : Declarator
{
    public Pointer Pointer { get; private set; }

    public override string DeclaredIdentifier {
        get {
            return (InnerDeclarator != null) ? InnerDeclarator.DeclaredIdentifier : "";
        }
    }

    public PointerDeclarator (Pointer pointer, Declarator decl) : base (decl)
    {
        Pointer = pointer;
    }
}
