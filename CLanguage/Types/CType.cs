﻿using System;
using CLanguage.Syntax;
using CLanguage.Compiler;

namespace CLanguage.Types;

public abstract class CType
{
    public TypeQualifiers TypeQualifiers { get; set; }

    public abstract int GetByteSize (EmitContext c);
    public abstract int NumValues { get; }

    public static readonly CVoidType Void = new ();

    public virtual bool IsIntegral => false;

    public virtual bool IsVoid => false;

    readonly Lazy<CPointerType> pointer;

    public CPointerType Pointer => pointer.Value;

    public bool IsVoidPointer => this switch {
        CPointerType pt => pt.InnerType.IsVoid || pt.InnerType.IsVoidPointer,
        _ => false,
    };

    public bool IsPointer => this switch {
        CPointerType => true,
        _ => false,
    };

    public CType () => pointer = new Lazy<CPointerType> (CreatePointerType);

    protected virtual CPointerType CreatePointerType () => new CPointerType (this);

    public virtual int ScoreCastTo (CType otherType) => Equals (otherType) ? 1000 : 0;

    public virtual object GetClrValue (Value[] values, MachineInfo machineInfo) => throw new NotSupportedException ($"Cannot get CLR type from {this}");
}
