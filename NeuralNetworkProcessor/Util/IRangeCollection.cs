using System.Collections.Generic;

namespace NeuralNetworkProcessor.Util;

public interface IRangeCollection<T> : ICollection<T> 
{
    IRangeCollection<T> AddRange(IEnumerable<T> collection);
}
