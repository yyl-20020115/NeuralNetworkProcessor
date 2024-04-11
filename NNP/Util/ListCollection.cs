namespace NNP.Util;

public class ListCollection<T> : List<T>, IRangeCollection<T>
{
    IRangeCollection<T> IRangeCollection<T>.AddRange(IEnumerable<T> collection)
    {
        base.AddRange(collection);
        return this;
    }
}
