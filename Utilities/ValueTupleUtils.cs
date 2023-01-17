using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Utilities
{
    public static class ValueTupleUtils
    {
        public static T[] Compose<T>(T[] a, T b) => (b != null ? a.Append(b).ToArray() : a);
        public static T[] Compose<T>(params T[][] arrs) => arrs.SelectMany(a => a).ToArray();

        public static Type CreateValueTupleType(params Type[] types) => types.Length switch
        {
            0 => typeof(ValueTuple),
            1 => typeof(ValueTuple<>).MakeGenericType(types[0..1]),
            2 => typeof(ValueTuple<,>).MakeGenericType(types[0..2]),
            3 => typeof(ValueTuple<,,>).MakeGenericType(types[0..3]),
            4 => typeof(ValueTuple<,,,>).MakeGenericType(types[0..4]),
            5 => typeof(ValueTuple<,,,,>).MakeGenericType(types[0..5]),
            6 => typeof(ValueTuple<,,,,,>).MakeGenericType(types[0..6]),
            7 => typeof(ValueTuple<,,,,,,>).MakeGenericType(types[0..7]),
            _ => typeof(ValueTuple<,,,,,,,>).MakeGenericType(
                Compose(types[0..7], CreateValueTupleType(types[7..]))),
        };
        public static object? CreateValueTupleObject(params object[] values)
            => CreateValueTupleObject(values.Select(v => v?.GetType() ?? typeof(object)).ToArray(), values);
        public static object? CreateValueTupleObject(Type[] gtypes, params object[] values)
        {
            if (gtypes.Length == 0 || gtypes.Length != values.Length) return null;
            var types = new List<Type[]>();
            var lists = new List<object[]>();
            for (var i = 0; i < gtypes.Length; i += 7)
            {
                types.Add(gtypes
                    [i..(i + Math.Min(gtypes.Length - i, 7))]);
                lists.Add(values
                    [i..(i + Math.Min(gtypes.Length - i, 7))]);
            }
            Type? last = null;
            object? v = null;
            for (var i = types.Count - 1; i >= 0; i--)
            {
                var pps = Compose(types[i], last);
                last = CreateValueTupleType(Compose(types.Skip(i).ToArray()));
                v = last?.GetConstructor(pps!)?.Invoke(lists[i]);
                if (i > 0) lists[i - 1] = Compose(lists[i - 1], v)!;
            }

            return v;
        }
        public static object? GetValueTupleElement(ITuple valueTuple,int index = 0)
        {
            if (valueTuple == null ||index<0||index>=valueTuple.Length) return null;
            return valueTuple[index];
        }
    }
}
