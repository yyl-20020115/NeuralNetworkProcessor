using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Utilities;

public static class VectorUtils
{
    public static T[] ToArray<T>(this IList<T> list, int index, int count, T fill = default) where T : struct
        => ToArray<T>(new List<T>(list), index, count, fill);
    public static T[] ToArray<T>(this List<T> list, int index, int count, T fill = default) where T: struct
    {
        var array = new T[count];
        Array.Fill(array, fill);
        list.CopyTo(index,
            array, 0,
            index + count >= list.Count ? list.Count - index : count);
        return array;
    }
    public static bool EqualsAny<T>(this T v, IList<T> vs, T fill = default) where T : struct
        => EqualsAny(new Vector<T>(v), vs.ToVectors(fill));
    public static bool EqualsAll<T>(this T v, IList<T> vs, T fill = default) where T : struct
        => EqualsAll(new Vector<T>(v), vs.ToVectors(fill));

    public static bool Equals<T>(this IList<T> left, IList<T> right, T fill = default) where T : struct
        => left.ToVectors(fill).SequenceEqual(right.ToVectors(fill));
    public static bool EqualsAny<T>(this Vector<T> v, IList<T> vs, T fill = default) where T : struct
        => vs.ToVectors(fill).Any(s => Vector.EqualsAny(v, s));
    public static bool EqualsAll<T>(this Vector<T> v, IList<T> vs, T fill = default) where T : struct
        => vs.ToVectors(fill).Any(s => Vector.EqualsAll(v, s));
    public static bool EqualsAny<T>(this Vector<T> v, IList<Vector<T>> vs) where T : struct
        => vs.Any(s => Vector.EqualsAny(v, s));
    public static bool EqualsAll<T>(this Vector<T> v, IList<Vector<T>> vs) where T : struct
        => vs.Any(s => Vector.EqualsAll(v, s));
    public static Vector<T>[] ToVectors<T>(this IList<T> ts, T fill = default, bool align = true) where T : struct
        => ToVectors(new List<T>(ts), fill,align).ToArray();
    public static List<Vector<T>> ToVectors<T>(this List<T> ts, T fill = default, bool align = true) where T : struct
    {
        var results = new List<Vector<T>>(ts.Count / Vector<T>.Count + 1);
        if (align)
        {
            var reminder = ts.Count % Vector<T>.Count;
            var array = new T[reminder > 0 ? ts.Count + Vector<T>.Count - reminder : ts.Count];
            ts.CopyTo(array);
            if (reminder > 0) Array.Fill(array, fill, ts.Count, Vector<T>.Count - reminder);
            for (int index = 0; index < ts.Count; index += Vector<T>.Count)
                results.Add(new Vector<T>(array, index));
        }
        else
        {
            for (int index = 0; index < ts.Count; index += Vector<T>.Count)
                results.Add(new Vector<T>(ts.ToArray(index, Vector<T>.Count, fill)));
        }
        return results;
    }

}
