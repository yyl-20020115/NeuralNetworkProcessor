using System.Collections.Generic;

namespace Utilities
{
    public static class HashLookupsHelper
    {
        public static ICollection<T> Append<T>(this ICollection<T> ts, IEnumerable<T> vs)
        {
            foreach (var v in vs) ts.Add(v);
            return ts;
        }
        public static bool Append_ReturnFalseIfAllKnown<T>(this ICollection<T> ts, IEnumerable<T> vs)
        {
            var b = false;
            foreach (var v in vs)
                if (ts is HashSet<T> ms)
                    b |= ms.Add(v);
                else
                    ts.Add(v); //always exscape path
            //ts.Add(v): true if unknown, false if known
            return b;
        }
    }

}
