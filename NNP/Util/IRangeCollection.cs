namespace NNP.Util;

public interface IRangeCollection<T> : ICollection<T> 
{
    IRangeCollection<T> AddRange(IEnumerable<T> collection);
}
