using System;
using System.Collections.Generic;

namespace Utilities
{
    public static class MoreLinq
    {
        public static TSource MaxBy<TSource, TProperty>(
            this IEnumerable<TSource> source,
            Func<TSource, TProperty> selector)
        {
            using var iterator = source.GetEnumerator();
            if (!iterator.MoveNext())
                throw new InvalidOperationException();

            var max = iterator.Current;
            var maxValue = selector(max);
            var comparer = Comparer<TProperty>.Default;

            while (iterator.MoveNext())
            {
                var current = iterator.Current;
                var currentValue = selector(current);

                if (comparer.Compare(currentValue, maxValue) > 0)
                {
                    max = current;
                    maxValue = currentValue;
                }
            }

            return max;
        }
        public static TSource MinBy<TSource, TProperty>(
            this IEnumerable<TSource> source,
            Func<TSource, TProperty> selector)
        {
            using var iterator = source.GetEnumerator();
            if (!iterator.MoveNext())
                throw new InvalidOperationException();

            var min = iterator.Current;
            var minValue = selector(min);
            var comparer = Comparer<TProperty>.Default;

            while (iterator.MoveNext())
            {
                var current = iterator.Current;
                var currentValue = selector(current);

                if (comparer.Compare(currentValue, minValue) < 0)
                {
                    min = current;
                    minValue = currentValue;
                }
            }

            return min;
        }
        public static TProperty MaxBy_<TSource, TProperty>(
            this IEnumerable<TSource> source,
            Func<TSource, TProperty> selector)
        {
            using var iterator = source.GetEnumerator();
            if (!iterator.MoveNext())
                throw new InvalidOperationException();

            var max = iterator.Current;
            var maxValue = selector(max);
            var comparer = Comparer<TProperty>.Default;

            while (iterator.MoveNext())
            {
                var current = iterator.Current;
                var currentValue = selector(current);

                if (comparer.Compare(currentValue, maxValue) > 0)
                {
                    max = current;
                    maxValue = currentValue;
                }
            }

            return maxValue;
        }
        public static TProperty MinBy_<TSource, TProperty>(
            this IEnumerable<TSource> source,
            Func<TSource, TProperty> selector)
        {
            using var iterator = source.GetEnumerator();
            if (!iterator.MoveNext())
                throw new InvalidOperationException();

            var min = iterator.Current;
            var minValue = selector(min);
            var comparer = Comparer<TProperty>.Default;

            while (iterator.MoveNext())
            {
                var current = iterator.Current;
                var currentValue = selector(current);

                if (comparer.Compare(currentValue, minValue) < 0)
                {
                    min = current;
                    minValue = currentValue;
                }
            }

            return minValue;
        }


    }
}
