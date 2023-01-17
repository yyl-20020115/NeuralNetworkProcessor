using System;
using System.Collections.Generic;

namespace QuickGraph
{
    [Flags]
    public enum SubEdgeTypes
    {
        NormalEdge = 0,
        SourceSubEdge = 1,
        TargetSubEdge = 2,
        BothSubEdge = 3,
    }
    public interface ISubVertexListGraph<TVertex, TEdge>
        where TEdge : IEdge<TVertex>
    {
        bool AddSubVertex(TVertex container, TVertex sub);
        int AddSubVertexRange(TVertex container, IList<TVertex> subs);
        TVertex GetContainer(TVertex vertex);
        bool IsSubVertex(TVertex vertex);
        IList<TVertex> GetSubVertices(TVertex container);
        int GetSubsCount(TVertex container);
        bool IsTopVertex(TVertex vertex);

        IEnumerable<TVertex> TopVertices { get; }
        IEnumerable<TVertex> SubVertices { get; }

        SubEdgeTypes GetEdgeType(TEdge edge);

        TEdge GetRealEdge(TEdge edge);

        IList<TEdge> NormalEdges { get; }
        IList<TEdge> SourceSubEdges { get; }
        IList<TEdge> TargetSubEdges { get; }
        IList<TEdge> BothSubEdges { get; }
        IList<TEdge> TopEdges { get; }
    }
}
