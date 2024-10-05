using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CLanguage;

[StructLayout (LayoutKind.Explicit, Size = 8)]
public struct Value
{
    [FieldOffset (0)]
    public System.Double Float64Value;
    [FieldOffset (0)]
    public System.Int64 Int64Value;
    [FieldOffset (0)]
    public System.UInt64 UInt64Value;
    [FieldOffset (0)]
    public System.Single Float32Value;
    [FieldOffset (0)]
    public System.Int32 Int32Value;
    [FieldOffset (0)]
    public System.UInt32 UInt32Value;
    [FieldOffset (0)]
    public System.Int16 Int16Value;
    [FieldOffset (0)]
    public System.UInt16 UInt16Value;
    [FieldOffset (0)]
    public System.SByte Int8Value;
    [FieldOffset (0)]
    public System.Byte UInt8Value;
    [FieldOffset (0)]
    public System.Int32 PointerValue;
    [FieldOffset (0)]
    public System.Char CharValue;

    public override string ToString () => Int32Value.ToString ();

    public static implicit operator Value (bool v) => new () {
        Int32Value = v ? 1 : 0,
    };

    public static implicit operator Value (string v) =>
        // Used for marshalling, but not actually allowed
        new();

    public static implicit operator Value (char v) => new () {
        CharValue = v,
    };

    public static implicit operator Value (float v) => new () {
        Float32Value = v,
    };

    public static implicit operator Value (double v) => new () {
        Float64Value = v,
    };

    public static implicit operator Value (ulong v) => new () {
        UInt64Value = v,
    };

    public static implicit operator Value (long v) => new () {
        Int64Value = v,
    };

    public static implicit operator Value (uint v) => new () {
        UInt32Value = v,
    };

    public static implicit operator Value (int v) => new () {
        Int32Value = v,
    };

    public static implicit operator Value (ushort v) => new () {
        UInt16Value = v,
    };

    public static implicit operator Value (short v) => new () {
        Int16Value = v,
    };

    public static implicit operator Value (byte v) => new () {
        UInt8Value = v,
    };

    public static implicit operator Value (sbyte v) => new () {
        Int8Value = v,
    };

    public static explicit operator float (Value v) => v.Float32Value;

    public static explicit operator double (Value v) => v.Float64Value;

    public static explicit operator ulong (Value v) => v.UInt64Value;

    public static explicit operator long (Value v) => v.Int64Value;

    public static explicit operator uint (Value v) => v.UInt32Value;

    public static explicit operator int (Value v) => v.Int32Value;

    public static explicit operator ushort (Value v) => v.UInt16Value;

    public static explicit operator short (Value v) => v.Int16Value;

    public static explicit operator byte (Value v) => v.UInt8Value;

    public static explicit operator sbyte (Value v) => v.Int8Value;

    public static Value Pointer (int address) => new () {
        PointerValue = address,
    };
}

static class ValueReflection
{
    public static readonly Dictionary<Type, FieldInfo> TypedFields =
        (from f in typeof (Value).GetTypeInfo ().DeclaredFields
         where f.Name.EndsWith ("Value", StringComparison.OrdinalIgnoreCase) &&
             !f.Name.StartsWith ("Pointer", StringComparison.OrdinalIgnoreCase)
         select (f.FieldType, f)).ToDictionary (x => x.FieldType, x => x.f);

    public static readonly Dictionary<Type, MethodInfo> CreateValueFromTypeMethods =
        (from m in typeof (Value).GetTypeInfo ().DeclaredMethods
         where m.Name.StartsWith ("op", StringComparison.OrdinalIgnoreCase)
         let rt = m.ReturnType
         where rt == typeof (Value)
         let ps = m.GetParameters ()
         where ps.Length == 1
         select (ps[0].ParameterType, m)).ToDictionary (x => x.ParameterType, x => x.m);

    static ValueReflection () 
        => TypedFields[typeof (string)] 
        = typeof (Value).GetTypeInfo ().GetDeclaredField (nameof (Value.PointerValue))!;
}
