using System;
using System.Windows;
using System.Linq;
using QuickGraph;
using System.Collections.Generic;

namespace GraphSharp.Algorithms.Layout.Compound.Dot
{
    public partial class DotLayoutAlgorithm<TVertex, TEdge, TGraph>
        where TVertex : class
        where TEdge : IEdge<TVertex>
        where TGraph : IBidirectionalGraph<TVertex, TEdge>
    {
        private readonly List<EdgeData> _allEdgeDatas = new List<EdgeData>();
        private readonly List<EdgeData> _topEdgeDatas = new List<EdgeData>();
        /// <summary>
        /// Informations for compound vertices.
        /// </summary>
        private readonly IDictionary<TVertex, CompoundVertexData> _compoundVertexDatas =
            new Dictionary<TVertex, CompoundVertexData>();

        /// <summary>
        /// Informations for the simple vertices.
        /// </summary>
        private readonly IDictionary<TVertex, SimpleVertexData> _simpleVertexDatas =
            new Dictionary<TVertex, SimpleVertexData>();
        /// <summary>
        /// Informations for all kind of vertices.
        /// </summary>
        private readonly IDictionary<TVertex, VertexData> _allVertexDatas =
            new Dictionary<TVertex, VertexData>();
        private readonly IDictionary<TVertex, VertexData> _topVertexDatas =
            new Dictionary<TVertex, VertexData>();



        /// <summary>
        /// Temporary dictionary for the inner canvas sizes (do not depend on this!!! inside 
        /// the algorithm, use the vertexData objects instead).
        /// </summary>
        //private IDictionary<TVertex, Size> _innerCanvasSizes = null;

        /// <summary>
        /// The dictionary of the initial vertex sizes.
        /// DO NOT USE IT AFTER THE INITIALIZATION.
        /// </summary>
        private readonly IDictionary<TVertex, Size> _vertexSizes;

        /// <summary>
        /// The dictionary of the vertex bordex.
        /// DO NOT USE IT AFTER THE INITIALIZATION.
        /// </summary>
        private readonly IDictionary<TVertex, Thickness> _vertexBorders;


        private readonly IMutableCompoundGraph<TVertex, TEdge> _compoundGraph;

        private GraphData _rootGraph = null;
        /// <summary>
        /// Represents the root vertex.
        /// </summary>
        private CompoundVertexData _rootCompoundVertex =
            new CompoundVertexData(default(TVertex));

        #region Constructors
        public DotLayoutAlgorithm(
            TGraph visitedGraph,
            IDictionary<TVertex, Size> vertexSizes,
            IDictionary<TVertex, Thickness> vertexBorders)
            : this(visitedGraph, vertexSizes, vertexBorders, null, null)
        {
        }

        public DotLayoutAlgorithm(
            TGraph visitedGraph,
            IDictionary<TVertex, Size> vertexSizes,
            IDictionary<TVertex, Thickness> vertexBorders,
            IDictionary<TVertex, Point> vertexPositions,
            DotLayoutParameters oldParameters)
            : base(visitedGraph, vertexPositions, oldParameters)
        {
            _vertexSizes = vertexSizes;
            _vertexBorders = vertexBorders;

            if (VisitedGraph is ICompoundGraph<TVertex, TEdge> cg)
                _compoundGraph = new CompoundGraph<TVertex, TEdge>(cg);
            else
                _compoundGraph = new CompoundGraph<TVertex, TEdge>(VisitedGraph);
        }
        #endregion

        #region ICompoundLayoutAlgorithm<TVertex,TEdge,TGraph> Members

        public IDictionary<TVertex, Size> InnerCanvasSizes
        {
            get
            {
                return _compoundVertexDatas.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.InnerCanvasSize);
            }
        }

        #endregion

        #region Nested type: CompoundVertexData
        private class Box
        {
            public Point LL = new Point();
            public Point UR = new Point();
            public Box Clone() => new Box { LL = this.LL, UR = this.UR };
            public override string ToString()
            {
                return $"({this.LL})-({this.UR})";
            }
        }
        private class GraphData
        {
            public Box bb = new Box();
            public double ht1 = 0.0;
            public double ht2 = 0.0;
            public int RankSep = 12;
            public int NodeSep = 18;
            public int MinRank = int.MinValue;
            public int MaxRank = int.MaxValue;
            public int Installed = 0;
            public bool Expanded = false;
            public bool HasFlatEdge = false;
            public bool ExactRankSep = false;
            public bool Flip = false;
            public VertexData ln = null;
            public VertexData rn = null;
            public VertexData MaxSet = null;
            public VertexData MinSet = null;
            public VertexData Leader = null;
            public DotLayoutParameters Parameters = null;
            public GraphData DotRoot = null;
            public CompoundVertexData Root = null;
            public List<VertexData> TopVertexDatas = new List<VertexData>();
            public List<VertexData> AllVertexDatas = new List<VertexData>();
            public List<EdgeData> AllEdgeDatas = new List<EdgeData>();
            public List<EdgeData> TopEdgeDatas = new List<EdgeData>();

            public List<GraphData> Clusters = new List<GraphData>();
            public List<GraphData> SubGraphs = new List<GraphData>();
            public List<VertexData> NList = new List<VertexData>();
            public List<List<VertexData>> Components = new List<List<VertexData>>();
            public List<VertexData> RankLeaders = new List<VertexData>();
            public RankData[] Ranks = null;
            public Point[] Borders = new Point[4];
            public SplineInfoData sd = new SplineInfoData();
            public GraphData(CompoundVertexData root)
            {
                this.Root = root ?? throw new ArgumentNullException(nameof(root));
            }
        }
        /// <summary>
        /// Data for the compound vertices.
        /// </summary>
        private class CompoundVertexData : VertexData
        {
            /// <summary>
            /// The thickness of the borders of the compound vertex.
            /// </summary>
            public Thickness Borders;

            /// <summary>
            /// Gets the layout type of the compound vertex.
            /// </summary>

            private Size _innerCanvasSize;


            public readonly List<VertexData> Children = new List<VertexData>();


            public CompoundVertexData(TVertex vertex)
                : base(vertex)
            {

                ////calculate the size of the inner canvas
                //InnerCanvasSize = new Size(Math.Max(0.0, size.Width - Borders.Left - Borders.Right),
                //                           Math.Max(0.0, size.Height - Borders.Top - Borders.Bottom));
                //InnerVertexLayoutType = innerVertexLayoutType;
            }

            /// <summary>
            /// The size of the inner canvas of the compound vertex.
            /// </summary>
            public Size InnerCanvasSize
            {
                get { return _innerCanvasSize; }
                set
                {
                    _innerCanvasSize = value;

                    //set the size of the canvas
                    Size = new Size(_innerCanvasSize.Width + Borders.Left + Borders.Right,
                                     _innerCanvasSize.Height + Borders.Top + Borders.Bottom);
                }
            }


            public Point InnerCanvasCenter
            {
                get
                {
                    return new Point(
                        Location.X - Size.Width / 2 + Borders.Left + InnerCanvasSize.Width / 2,
                        Location.Y - Size.Height / 2 + Borders.Top + InnerCanvasSize.Height / 2
                        );
                }
                set
                {
                    Location = new Point(
                        value.X - InnerCanvasSize.Width / 2 - Borders.Left + Size.Width / 2,
                        value.Y - InnerCanvasSize.Height / 2 - Borders.Top + Size.Height / 2
                        );
                }
            }

            public void RecalculateBounds()
            {
                if (this.Children.Count == 0)
                {
                    InnerCanvasSize = new Size(); //TODO padding
                    return;
                }

                Point topLeft = new Point(double.PositiveInfinity, double.PositiveInfinity);
                Point bottomRight = new Point(double.NegativeInfinity, double.NegativeInfinity);
                foreach (var child in this.Children)
                {
                    topLeft.X = Math.Min(topLeft.X, child.Location.X - child.Size.Width / 2);
                    topLeft.Y = Math.Min(topLeft.Y, child.Location.Y - child.Size.Height / 2);

                    bottomRight.X = Math.Max(bottomRight.X, child.Location.X + child.Size.Width / 2);
                    bottomRight.Y = Math.Max(bottomRight.Y, child.Location.Y + child.Size.Height / 2);
                }
                InnerCanvasSize = new Size(bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y);
                InnerCanvasCenter = new Point((topLeft.X + bottomRight.X) / 2.0, (topLeft.Y + bottomRight.Y) / 2.0);
            }
        }

        #endregion

        #region Nested type: SimpleVertexData

        private class SimpleVertexData : VertexData
        {
            public SimpleVertexData(TVertex vertex = null)
                : base(vertex)
            {
            }
        }

        #endregion

        #region Nested type: EdgeData,VertexData
        private class PathEnd
        {
            public Box nb = new Box();            /* the node box */
            public Point np = default(Point);      /* node port */
            public PortSides sidemask = PortSides.NONE;
            public List<Box> boxes = new List<Box>();
        }

        private class Path
        {   /* internal specification for an edge spline */
            public int start = 0;
            public int end = 0;
            public Point startPoint = new Point();
            public Point endPoint = new Point();
            public double startTheta = 0.0;
            public double endTheta = 0.0;
            public bool startConstrained = false;
            public bool endConstrained = false;
            public List<Box> boxes = new List<Box>();        /* rectangular regions of subdivision */
            public object data = null;
        }

        private class SplineInfoData
        {
            public int LeftBound = 0;
            public int RightBound = 0;
            public int Splinesep = 0;
            public int Multisep = 0;
            public List<Box> RankBoxes = new List<Box>();
            public List<Box> WorkingBoxes = new List<Box>();
        }

        private class Bezier
        {
            public List<Point> list = new List<Point>();
            public bool sflag = false;
            public bool eflag = false;
            public Point sp = default(Point);
            public Point ep = default(Point);
        }

        private class Splines
        {
            public Bezier line = new Bezier();
            public Box bb = default(Box);
        }
        private enum PortSides : int
        {
            NONE = 0,
            LEFT = 1,
            RIGHT = 2,
            TOP = 4,
            BOTTOM = 8
        }
        private class EdgeDataPair
        {
            public EdgeData InEdge;
            public EdgeData OutEdge;
        }
        private class EdgeData
        {
            public int HeadIndex = 0;
            public int TailIndex = 0;
            public PortSides HeadPortSide = PortSides.NONE;
            public PortSides TailPortSide = PortSides.NONE;
            public Point TailPoint = default(Point);
            public Point HeadPoint = default(Point);
            public Box TailPortBox = new Box();
            public Box HeadPortBox = new Box();
            public bool HeadPortClip = true;
            public bool TailPortClip = true;
            public TEdge Edge = default(TEdge);
            public bool IsAux = false;
            public bool ConOppFlag = false;
            public int MinLength = 1;
            public int Weight = 1;
            public int Count = 1;
            public int Penalty = 1;
            public int CutValue = 0;
            public bool Adjacent = false;
            public double Dist = 0;
            public Splines spl = null;
            public int Slack => this.Length - this.MinLength;
            public int Length => this.Head.Rank - this.Tail.Rank;
            public int TreeIndex = -1;
            public EdgeData Original = null;
            public EdgeData Virtual = null;
            public VertexData Tail = null;
            public VertexData Head = null;
            public EdgeTypes EdgeType = EdgeTypes.Normal;
            public EdgeSplineType EdgeSplineType = EdgeSplineType.NONE;
            public string id = null;
            public EdgeData(EdgeData old)
            {
                if (old != null)
                {
                    this.Edge = old.Edge;
                    this.HeadIndex = old.HeadIndex;
                    this.TailIndex = old.TailIndex;
                    this.HeadPoint = old.HeadPoint;
                    this.TailPoint = old.TailPoint;
                    this.HeadPortBox = old.HeadPortBox;
                    this.TailPortBox = old.TailPortBox;
                    this.HeadPortSide = old.HeadPortSide;
                    this.TailPortSide = old.TailPortSide;
                    this.HeadPortClip = old.HeadPortClip;
                    this.TailPortClip = old.TailPortClip;
                    this.IsAux = old.IsAux;
                    this.ConOppFlag = old.ConOppFlag;
                    this.MinLength = old.MinLength;
                    this.Weight = old.Weight;
                    this.Count = old.Count;
                    this.Penalty = old.Penalty;
                    this.CutValue = old.CutValue;
                    this.Adjacent = old.Adjacent;
                    this.Dist = old.Dist;
                    this.spl = old.spl;
                    this.TreeIndex =old.TreeIndex;
                    this.Original = old.Original;
                    this.Virtual = old.Virtual;
                    this.Tail = old.Tail;
                    this.Head = old.Head;
                    this.EdgeType = old.EdgeType;
                    this.EdgeSplineType = old.EdgeSplineType;
                }
            }
            public EdgeData(TEdge Edge = default(TEdge))
            {
                this.Edge = Edge;
            }
            public override string ToString() => this.Edge == null ?
                string.Format("[<{0}->{1}>]", this.Tail,this.Head):
                string.Format("[{0}]", this.Edge);
        }

        private class Shape
        {
            public virtual string Name => string.Empty;
            public virtual bool Inside(Inside inside_context, Point p) => false;
        }
        /// <summary>
        /// Data for the simple vertices.
        /// </summary>
        /// 
        private abstract class VertexData
        {
            public int Index = 0;
            public VertexData InLeaf = null;
            public VertexData OutLeaf = null;

            public List<EdgeData> RealInEdges = new List<EdgeData>();
            public List<EdgeData> RealOutEdges = new List<EdgeData>();
            public List<EdgeData> TopInEdges = new List<EdgeData>();
            public List<EdgeData> TopOutEdges = new List<EdgeData>();
            public List<EdgeData> FastInEdges = new List<EdgeData>();
            public List<EdgeData> FastOutEdges = new List<EdgeData>();
            public List<EdgeData> FlatInEdges = new List<EdgeData>();
            public List<EdgeData> FlatOutEdges = new List<EdgeData>();
            public List<EdgeData> TreeInEdges = new List<EdgeData>();
            public List<EdgeData> TreeOutEdges = new List<EdgeData>();
            public List<EdgeData> OtherEdges = new List<EdgeData>();

            public List<EdgeData> SaveInEdges = new List<EdgeData>();
            public List<EdgeData> SaveOutEdges = new List<EdgeData>();
            public List<VertexData> Subs = new List<VertexData>();

            /// <summary>
            /// Gets the vertex which is wrapped by this object.
            /// </summary>
            public readonly TVertex Vertex;
            public VertexData UF_Parent = null;
            public int UF_Size = 1;
            public GraphData Root = null;
            public VertexData Parent = null;
            public EdgeData Alg = null;
            public bool HasPort = false;
            public int Rank = 0;
            public int Order = 0;
            public int SavedOrder = 0;
            public int WeightClass = 0;
            public double Mval= 0.0;
            public uint Mark = 0;
            public bool IsOnStack = false;
            public int Priority = 0;
            public int Lim = 0;
            public int Low = 0;
            public bool IsTop = false;
            public bool IsVirtual = false;
            public EdgeData Par = null;
            public SubTree SubTree = null;
            public double height = 0.0;
            public double width = 0.0;
            public int Idx => this.np!=null ? this.np.Order : -1;
            public Point Location = default(Point);
            public int FlatIndex { get => this.Low; set => this.Low = value; }
            protected VertexData(TVertex vertex)
            {
                this.Vertex = vertex;
            }

            public VertexData ClusterLeader = null;
            public GraphData ClusterGraph = null;
            public GraphData Graph = null;
            public VertexData[] RankLeaders = new VertexData[(int)RankTypes.TypesCount]; 
            public RankTypes RankType = RankTypes.Unknown;
            public NodeTypes NodeType = NodeTypes.Normal;
            public double ht = 1.0;
            public double lw = 1.0;
            public double rw = 1.0;

            public int x = 0;
            public int lo = 0;
            public int hi = 0;
            public VertexData np = null;

            /// <summary>
            /// Gets the actual size of the vertex (inner size + border + anything else...).
            /// </summary>
            public Size Size = new Size();
            public Shape Shape = new Shape();
            public override string ToString() =>
                this.Vertex == null || this.IsVirtual 
                ? string.Format("({0}:{1})",this.Index,this.NodeType) 
                : string.Format("({0})", this.Vertex);
        }

        #endregion
    }
}
