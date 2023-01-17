using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuralNetworkProcessor.Util;

public interface IRangeCollection<T> : ICollection<T> 
{
    IRangeCollection<T> AddRange(IEnumerable<T> collection);
}
