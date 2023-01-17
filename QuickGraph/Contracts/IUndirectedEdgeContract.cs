using System;
using System.Diagnostics.Contracts;
using System.Collections.Generic;

namespace QuickGraph.Contracts
{
    [ContractClassFor(typeof(IUndirectedEdge<>))]
    abstract class IUndirectedEdgeContract<TVertex>
        : IUndirectedEdge<TVertex>
    {
        [ContractInvariantMethod]
        void IUndirectedEdgeInvariant()
        {
            IUndirectedEdge<TVertex> ithis = this;
            Contract.Invariant(Comparer<TVertex>.Default.Compare(ithis.Source, ithis.Target) <= 0);
        }

        public IEdge<TVertex> Clone()
        {
            throw new NotImplementedException();
        }

        #region IEdge<TVertex> Members

        public TVertex Source
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public TVertex Target
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        #endregion
    }
}
