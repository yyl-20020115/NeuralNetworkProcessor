﻿using System.Collections.Generic;
using QuickGraph;
using System.Diagnostics.Contracts;
using System;

namespace GraphSharp
{
	public interface ICompoundGraph<TVertex, TEdge> : IBidirectionalGraph<TVertex, TEdge>
		where TEdge : IEdge<TVertex>
	{

        bool AddChildVertex( TVertex parent, TVertex child );
		int AddChildVertexRange( TVertex parent, IEnumerable<TVertex> children );
		TVertex GetParent( TVertex vertex );
		bool IsChildVertex( TVertex vertex );
		IEnumerable<TVertex> GetChildrenVertices( TVertex vertex );
		int GetChildrenCount( TVertex vertex );
		bool IsCompoundVertex( TVertex vertex );

		IEnumerable<TVertex> CompoundVertices { get; }
        IEnumerable<TVertex> SimpleVertices { get; }
    }
}
