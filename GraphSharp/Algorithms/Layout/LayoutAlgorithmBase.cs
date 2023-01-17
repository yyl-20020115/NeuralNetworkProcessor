using System.Collections.Generic;
using QuickGraph;
using System.Windows;
using System.Diagnostics;
using System.Diagnostics.Contracts;

namespace GraphSharp.Algorithms.Layout
{
	public abstract class LayoutAlgorithmBase<TVertex, TEdge, TGraph, TVertexInfo, TEdgeInfo> : LayoutAlgorithmBase<TVertex, TEdge, TGraph>, ILayoutAlgorithm<TVertex, TEdge, TGraph, TVertexInfo, TEdgeInfo>
		where TVertex : class
		where TEdge : IEdge<TVertex>
		where TGraph : IVertexAndEdgeListGraph<TVertex, TEdge>
	{
		protected readonly IDictionary<TVertex, TVertexInfo> vertexInfos = new Dictionary<TVertex, TVertexInfo>();
		protected readonly IDictionary<TEdge, TEdgeInfo> edgeInfos = new Dictionary<TEdge, TEdgeInfo>();

		protected LayoutAlgorithmBase( TGraph visitedGraph )
			: base( visitedGraph )
		{
            this.IterationEnded += LayoutAlgorithmBase_IterationEnded;
		}

        protected LayoutAlgorithmBase( TGraph visitedGraph, IDictionary<TVertex, Point> vertexPositions )
			: base( visitedGraph, vertexPositions )
		{
            this.IterationEnded += LayoutAlgorithmBase_IterationEnded;
        }

        protected virtual void FireIterationEndedEvent(object sender, ILayoutInfoIterationEventArgs<TVertex, TEdge, TVertexInfo, TEdgeInfo> e)
        {
            this.IterationEnded?.Invoke(sender, e);
        }
        protected virtual void LayoutAlgorithmBase_IterationEnded(object sender, ILayoutInfoIterationEventArgs<TVertex, TEdge, TVertexInfo, TEdgeInfo> e)
        {
        }
        public IDictionary<TVertex, TVertexInfo> VertexInfos
		{
			get { return vertexInfos; }
		}

		public IDictionary<TEdge, TEdgeInfo> EdgeInfos
		{
			get { return edgeInfos; }
		}

		public new event LayoutIterationEndedEventHandler<TVertex, TEdge, TVertexInfo, TEdgeInfo> IterationEnded;

		public override object GetVertexInfo( TVertex vertex )
		{
			TVertexInfo info;
			if ( VertexInfos.TryGetValue( vertex, out info ) )
				return info;

			return null;
		}

		public override object GetEdgeInfo( TEdge edge )
		{
			TEdgeInfo info;
			if ( EdgeInfos.TryGetValue( edge, out info ) )
				return info;

			return null;
		}
	}

	public abstract class LayoutAlgorithmBase<TVertex, TEdge, TGraph> : AlgorithmBase, ILayoutAlgorithm<TVertex, TEdge, TGraph>
		where TVertex : class
		where TEdge : IEdge<TVertex>
		where TGraph : IVertexAndEdgeListGraph<TVertex, TEdge>
	{
        public virtual bool DynamicRouting { get; set; } = true;

        public virtual void PostLayoutProcess(IDictionary<TVertex, Rect> dict)
        {

        }

        public virtual Point[] RouteEdge(TEdge edge,Rect tailRect, Rect headRect)
        {
            return null;
        }

        private readonly Dictionary<TVertex, Point> vertexPositions;
		private readonly TGraph visitedGraph;

		public IDictionary<TVertex, Point> VertexPositions
		{
			get { return vertexPositions; }
		}

		public TGraph VisitedGraph
		{
			get { return visitedGraph; }
		}

		public event LayoutIterationEndedEventHandler<TVertex, TEdge> IterationEnded;

	    public event ProgressChangedEventHandler ProgressChanged;

		public bool ReportOnIterationEndNeeded
		{
			get { return IterationEnded != null; }
		}

	    public bool ReportOnProgressChangedNeeded
	    {
            get { return ProgressChanged != null; }
	    }

		protected LayoutAlgorithmBase( TGraph visitedGraph ) :
			this( visitedGraph, null )
		{
		}

		protected LayoutAlgorithmBase( TGraph visitedGraph, IDictionary<TVertex, Point> vertexPositions )
		{
            this.visitedGraph = visitedGraph;
			if ( vertexPositions != null )
				this.vertexPositions = new Dictionary<TVertex, Point>( vertexPositions );
			else
				this.vertexPositions = new Dictionary<TVertex, Point>( visitedGraph.VertexCount );
		}

		protected virtual void OnIterationEnded( ILayoutIterationEventArgs<TVertex> args )
		{
			if ( IterationEnded != null )
			{
				IterationEnded( this, args );

				//if the layout should be aborted
				if ( args.Abort )
					Abort();
			}
		}

        protected virtual void OnProgressChanged( double percent )
        {
            if ( ProgressChanged != null )
                ProgressChanged( this, percent );
        }

		public virtual object GetVertexInfo( TVertex vertex )
		{
			return null;
		}

		public virtual object GetEdgeInfo( TEdge edge )
		{
			return null;
		}
	}
}