using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using QuickGraph;

namespace GraphSharp.Algorithms.Layout.Compound.Dot
{
    /// <typeparam name="TVertex">The type of the vertices.</typeparam>
    /// <typeparam name="TEdge">The type of the edges.</typeparam>
    /// <typeparam name="TGraph">The type of the graph.</typeparam>
    public partial class DotLayoutAlgorithm<TVertex, TEdge, TGraph> :
        DefaultParameterizedLayoutAlgorithmBase<TVertex, TEdge, TGraph, DotLayoutParameters>,
        ICompoundLayoutAlgorithm<TVertex, TEdge, TGraph>
        where TVertex : class
        where TEdge : IEdge<TVertex>
        where TGraph : IBidirectionalGraph<TVertex, TEdge>
    {

        private const int CL_BACK  = 10;	/* cost of backward pointing edge */
        private const int CL_OFFSET	= 8;	/* margin of cluster box in PS points */

        private DotLayoutParameters parameters = null;
        public override DotLayoutParameters Parameters {
            get => this.parameters;
            protected set => this.parameters = value;
        }


        private enum RankTypes : int
        {
            Unknown = 0,
            Min = 1,
            Max = 2,
            Source = 3,
            Sink = 4,
            Same = 5,
            Normal = 6,
            Cluster = 7,
            LeafSet = 8,
            TypesCount = 9,
        }

        private enum NodeTypes : int
        {
            Unknown = 0,
            Normal = 1,
            Virtual = 2,
            Slack = 3,
        }
        private enum EdgeTypes : int
        {
            Unknown = 0,
            Normal = 1,
            Virtual = 2,
            FlatOrder = 3,
            ClusterEdge = 4,
            Ignored = 5,
            Reversed = 6,
        }
        private enum EdgeSplineType : int
        {
            NONE = 0,
            REGULAREDGE = 1,
            FLATEDGE    = 2,
            SELFWPEDGE  = 4,
            SELFNPEDGE  = 8,
            SELFEDGE    = 8,
            FWDEDGE     =16,
            BWDEDGE     =32,

            EDGETYPEMASK	=  15,	/* the OR of the above */
            MAINGRAPH = 64,
            AUXGRAPH = 128,
            GRAPHTYPEMASK = 192	/* the OR of the above */
        }
        private IList<Point> RotatePoints(IList<Point> ps, DotLayoutDirection direction, bool backward = false)
        {
            for(int i = 0;i<ps.Count;i++)
            {
                ps[i] = this.RotatePoint(ps[i], direction, backward);
            }
            return ps;
        }
        private Rect RotateRect(Rect r, DotLayoutDirection direction, bool backward = false)
        {
            return new Rect(this.RotatePoint(r.Location, direction, backward), this.RotateSize(r.Size, direction, backward));
        }

        private Point RotatePoint(Point p, DotLayoutDirection direction, bool backward = false)
        {
            var r = default(Point);
            if (backward)
            {
                switch (direction)
                {
                    case DotLayoutDirection.BottomToTop:
                        r = new Point(+p.X, -p.Y);
                        break;
                    case DotLayoutDirection.TopToBottom:
                        r = new Point(+p.X, +p.Y);
                        break;
                    case DotLayoutDirection.LeftToRight:
                        r = new Point(-p.Y, -p.X);
                        break;
                    case DotLayoutDirection.RightToLeft:
                        r = new Point(+p.Y, -p.X);
                        break;
                    default:
                        r = p;
                        break;
                }

            }
            else
            {
                switch (direction)
                {
                    case DotLayoutDirection.BottomToTop:
                        r = new Point(+p.X, +p.Y);
                        break;
                    case DotLayoutDirection.TopToBottom:
                        r = new Point(+p.X, -p.Y);
                        break;
                    case DotLayoutDirection.LeftToRight:
                        r = new Point(-p.Y, -p.X);
                        break;
                    case DotLayoutDirection.RightToLeft:
                        r = new Point(+p.Y, -p.X);
                        break;
                    default:
                        r = p;
                        break;
                }

            }

            return r;
        }
        private Size RotateSize(Size s, DotLayoutDirection direction, bool backward = false)
        {
            var r = default(Size);
            switch (direction)
            {
                case DotLayoutDirection.BottomToTop:
                    r = new Size(s.Width, s.Height);
                    break;
                case DotLayoutDirection.TopToBottom:
                    r = new Size(s.Width, s.Height);
                    break;
                case DotLayoutDirection.LeftToRight:
                    r = new Size(s.Height,s.Width);
                    break;
                case DotLayoutDirection.RightToLeft:
                    r = new Size(s.Height, s.Width);
                    break;
                default:
                    r = s;
                    break;
            }

            return r;
        }
        private void DeleteEdge(EdgeData e)
        {
            e.Tail.FastOutEdges.Remove(e);
            e.Head.FastInEdges.Remove(e);
        }
        private void ReverseEdge(EdgeData e)
        {
            this.DeleteEdge(e);

            var f = this.FindFastEdge(e.Head, e.Tail);
            if (f != null)
            {
                this.MergeOneWay(e, f);
            }
            else
            {
                this.AttachNewVirtualEdge(e.Head, e.Tail, e);
            }
        }
  
        private void DotScanRanks(GraphData g)
        {
            g.MinRank = int.MaxValue;
            g.MaxRank = -1;
            VertexData leader = null;
            foreach(var n in g.TopVertexDatas)
            {
                if (g.MaxRank < n.Rank)
                    g.MaxRank = n.Rank;
                if (g.MinRank > n.Rank)
                    g.MinRank = n.Rank;
                if (leader == null)
                    leader = n;
                else
                {
                    if (n.Rank < leader.Rank)
                        leader = n;
                }
            }

            g.Leader = leader;
        }

        private List<VertexData> SearchComponent(VertexData n, uint Cmark)
        {
            var stack = new Stack<VertexData>();

            var component = new List<VertexData>();

            var vec = new List<EdgeData>[4];

            VertexData other = null;
            stack.Push(n);
            n.Mark = Cmark + 1;

            while (stack.Count>0)
            {
                n = stack.Pop();

                if (n.Mark == Cmark) continue;

                n.Mark = Cmark;

                component.Add(n);

                vec[0] = n.FastOutEdges;
                vec[1] = n.FastInEdges;
                vec[2] = n.FlatOutEdges;
                vec[3] = n.FlatInEdges;

                for (int c = 3; c >= 0; c--)
                {
                    if (vec[c]!=null)
                    {
                        for (int i = vec[c].Count - 1; i >= 0; i--)
                        {
                            EdgeData e = vec[c][i];

                            if ((other = e.Head) == n)
                                other = e.Tail;
                            if ((other.Mark != Cmark) && (other == this.UF_find(other)))
                            {
                                stack.Push(other);
                                other.Mark = Cmark + 1;
                            }
                        }
                    }
                }
            }
            return component;
        }
        private List<List<VertexData>> Decompose(GraphData g,uint Cmark = 1)
        {
            var components = new List<List<VertexData>>();

            foreach (var v in g.TopVertexDatas)
            {
                if (v != this.UF_find(v))
                {
                    continue;
                }
                if (v.Mark != Cmark)
                {
                    components.Add(this.SearchComponent(v,Cmark));
                }
            }
            return components;
        }

        private void ExpandRankSets(GraphData g)
        {
            g.MinRank = int.MaxValue;
            g.MaxRank = -1;
            foreach (var n in g.TopVertexDatas)
            {
                var leader = this.UF_find(n);
                if(leader!=n && n.Rank == 0)
                {
                    n.Rank += leader.Rank;
                }
                if (g.MaxRank < n.Rank)
                {
                    g.MaxRank = n.Rank;
                }
                if (g.MinRank > n.Rank)
                {
                    g.MinRank = n.Rank;
                }
                if(n.RankType!=RankTypes.Unknown && n.RankType != RankTypes.LeafSet)
                {
                    n.UF_Parent = null;
                    n.RankType = RankTypes.Normal;
                }
            }
        }
        private void DFS(VertexData n)
        {
            if (n.Mark ==0)
            {
                n.Mark = 1;
                n.IsOnStack = true;
                for(int i = 0;i<n.FastOutEdges.Count;i++)
                {
                    var e = n.FastOutEdges[i];
                    var w = e.Head;
                    if (w.IsOnStack)
                    {
                        this.ReverseEdge(e);
                        i--;
                    }else if (w.Mark==0)
                    {
                        this.DFS(w);
                    }
                }
                n.IsOnStack = false;
            }
        }
        private void Acyclic(GraphData g)
        {
            foreach(var c in g.Components)
            {
                c.ForEach(n => n.Mark = 0);
                c.ForEach(n => this.DFS(n));    
            }
        }

        private EdgeData FindFastEdge(VertexData tail, VertexData head)
        {
            return this.FFE(tail, tail.FastOutEdges, head, head.FastInEdges);
        }
        private EdgeData FFE(VertexData tail,List<EdgeData> tailEdges,VertexData head,List<EdgeData> headEdges)
        {
            if(tailEdges.Count>0 && headEdges.Count > 0)
            {
                if (tailEdges.Count < headEdges.Count)
                {
                    return tailEdges.FirstOrDefault(e => e.Head == head);
                }
                else
                {
                    return headEdges.FirstOrDefault(e => e.Tail == tail);
                }
            }
            else
            {
                return null;
            }
        }
        private EdgeData AttachNewVirtualEdge(VertexData t, VertexData h, EdgeData o)
        {
            return this.FastEdge(this.NewVirtualEdge(t, h, o));
        }
        private EdgeData FastEdge(EdgeData e)
        {
            e.Tail.FastOutEdges.Add(e);
            e.Head.FastInEdges.Add(e);
            return e;
        }
        private EdgeData NewVirtualEdge(VertexData t,VertexData h,EdgeData o)
        {
            var e = new EdgeData()
            {
                Tail = t,
                Head = h,
                EdgeType = EdgeTypes.Virtual,
            };
            if (o != null)
            {
                e.Edge = o.Edge;
                e.Original = o;
                e.Count = o.Count;
                e.Penalty = o.Penalty;
                e.MinLength = o.MinLength;
                e.Weight = o.Weight;
                if(o.Virtual == null)
                {
                    o.Virtual = e;
                }
                if (e.Tail == o.Tail)
                    e.TailIndex = o.TailIndex;
                else if (e.Tail == o.Head)
                    e.TailIndex = o.HeadIndex;
                if (e.Head == o.Head)
                    e.HeadIndex = o.HeadIndex;
                else if (e.Head == o.Tail)
                    e.HeadIndex = o.TailIndex;
            }
            return e;
        }

        private void CleanUpRanking(GraphData g)
        {
            foreach (var c in g.Components)
            {
                foreach (var n in c)
                {
                    n.FastInEdges.Clear();
                    n.FastOutEdges.Clear();
                    n.Mark = 0;
                }
            }
            foreach (var n in g.TopVertexDatas)
            {
                foreach (var e in n.TopOutEdges)
                {
                    var f = e.Virtual;
                    if (f != null && f.Original == e)
                    {
                        foreach (var n1 in g.TopVertexDatas)
                        {
                            foreach (var e1 in n.TopOutEdges)
                            {
                                if (e != e1)
                                {
                                    var f1 = e1.Virtual;

                                    if (f1 != null && f == f1)
                                    {
                                        e1.Virtual = null;
                                    }
                                }
                            }
                        }
                    }
                    e.Virtual = null;
                }
            }
            g.Components.Clear();
        }
        private void UF_Singleton(VertexData n)
        {
            n.UF_Parent = null;
            n.UF_Size = 1;
            n.RankType = RankTypes.Normal;
        }


        private void UF_setname(VertexData u, VertexData v)
        {
            u.UF_Parent = v;
            v.UF_Size += u.UF_Size;
        }

        private void FastNode(GraphData g,VertexData n)
        {
            g.NList.Add(n);
        }
        private int LastVirtualNodeIndex = 0;
        private VertexData CreateVirtualNode(GraphData g)
        {
            VertexData n = new SimpleVertexData(default(TVertex))
            {
                NodeType = NodeTypes.Virtual,
                Root = g.DotRoot,
                IsVirtual = true,
                Index = LastVirtualNodeIndex++
            };

            this.FastNode(g, n);
            return n;
        }

        private void MarkLowClusters(GraphData g)
        {
            /* first, zap any previous cluster labelings */
            foreach(var n in g.TopVertexDatas)
            {
                n.ClusterGraph = null;
                foreach(var orig in n.TopOutEdges)
                {
                    var e = orig.Virtual;
                    if (e!=null)
                    {
                        VertexData vn = e.Head;
                        while (e!=null && vn.NodeType == NodeTypes.Virtual)
                        {
                            vn.ClusterGraph = null;
                            e = e.Head.TopOutEdges.FirstOrDefault();
                        }
                    }
                }
            }

            /* do the recursion */
            MarkLowClustersBasic(g);
        }

        private void MarkLowClustersBasic(GraphData g)
        {
            foreach(var c in g.Clusters)
            {
                this.MarkLowClustersBasic(c);
            }
            /* see what belongs to this graph that wasn't already marked */
            foreach(var n in g.TopVertexDatas)
            {
                if (n.ClusterGraph == null)
                    n.ClusterGraph = g;
                foreach(var orig in n.TopOutEdges)
                {
                    var e = orig.Virtual;

                    if (e!=null)
                    {
                        var vn = e.Head;
                        while (e!=null && vn.NodeType == NodeTypes.Virtual)
                        {
                            if (vn.ClusterGraph == null)
                                vn.ClusterGraph = g;
                            e = e.Head.TopOutEdges.FirstOrDefault();
                        }
                    }
                }
            }
        }

        private VertexData UF_find(VertexData n)
        {
            while (n.UF_Parent!=null && n.UF_Parent != n)
            {
                if (n.UF_Parent.UF_Parent!=null)
                    n.UF_Parent = n.UF_Parent.UF_Parent;
                n = n.UF_Parent;
            }
            return n;
        }
        private void InterClusterWork(GraphData g, VertexData t, VertexData h, EdgeData e)
        {
            VertexData v, t0, h0;
            int offset, t_len, h_len, t_rank, h_rank;
            EdgeData rt, rh;

            if (e.Tail.ClusterGraph != null)
                t_rank = e.Tail.Rank - e.Tail.ClusterGraph.Leader.Rank;
            else
                t_rank = 0;
            if (e.Head.ClusterGraph != null)
                h_rank = e.Head.Rank - e.Head.ClusterGraph.Leader.Rank;
            else
                h_rank = 0;
            offset = e.MinLength + t_rank - h_rank;
            if (offset > 0)
            {
                t_len = 0;
                h_len = offset;
            }
            else
            {
                t_len = -offset;
                h_len = 0;
            }

            v = CreateVirtualNode(g);
            v.NodeType = NodeTypes.Slack;

            t0 = UF_find(t);
            h0 = UF_find(h);
            rt = this.MakeAuxEdge(v, t0, t_len, CL_BACK * e.Weight);
            rh = this.MakeAuxEdge(v, h0, h_len, e.Weight);
            rt.Original = rh.Original = e;
        }
        private void BuildFastEdges(GraphData g)
        {
            this.MarkClusters(g);
            foreach (var n in g.TopVertexDatas)
            {
                foreach (var e in n.TopOutEdges)
                {
                    if (e.Virtual != null)
                        continue;

                    var t = this.UF_find(e.Tail);
                    var h = this.UF_find(e.Head);

                    if (t == h)
                        continue;

                    if (t.ClusterGraph != null || h.ClusterGraph != null)
                    {
                        this.InterClusterWork(g, t, h, e);
                        continue;
                    }
                
                    var rep = this.FindFastEdge(t, h);
                    if (rep != null)
                    {
                        this.MergeOneWay(e, rep);
                    }
                    else
                    {
                        this.AttachNewVirtualEdge(t, h, e);
                    }
                }
            }
        }
        //setup all dotroot to a single root
        private void InitSubGraphs(GraphData g,GraphData dotRoot)
        {
            if (g != null)
            {
                g.DotRoot = dotRoot;
                g.SubGraphs.ForEach(s => this.InitSubGraphs(s, dotRoot));
            }
        }
        private void Ranking(GraphData g)
        {         
            this.BuildFastEdges(g);
            
            g.Components = this.Decompose(g);

            this.Acyclic(g);

            foreach (var c in g.Components)
            {
                this.DoRank(g,c,
                    g.Clusters.Count == 0 ? BalanceMode.TopBottom : BalanceMode.None, 
                    g.Parameters.MaxIterators);
            }

            this.ExpandRankSets(g);

            this.CleanUpRanking(g);

        }
        private class VertexRankComparer : IComparer<VertexData>
        {
            public int Compare(VertexData x, VertexData y)
            {
                return x.Rank - y.Rank;
            }
        }
        private void DumpRanks(GraphData g)
        {
            using (StreamWriter writer = new StreamWriter("\\WorkingCurrent\\gv\\debug-sparser-ranks.txt"))
            {
                var vertexDatas = g.TopVertexDatas.ToList();
                vertexDatas.Sort(new VertexRankComparer());

                foreach(var n in vertexDatas)
                {
                    writer.WriteLine("{0:D4}:{1}", n.Rank, n.ToString());
                }
            }
        }
        private void DumpNodesAndEdges(List<VertexData> nodes, List<EdgeData> edges)
        {
            using (StreamWriter writer = new StreamWriter("\\WorkingCurrent\\gv\\debug-sparser-nodes-and-edges.txt"))
            {
                writer.WriteLine("nodes:{0}", nodes.Count);
                foreach (var n in nodes)
                {
                    writer.WriteLine(n.ToString());
                }
                writer.WriteLine();
                writer.WriteLine("edges:{0}", edges.Count);
                foreach (var e in edges)
                {
                    writer.WriteLine(e.ToString());
                }
            }
        }

        private void DumpMinCross(GraphData g)
        {
            using (StreamWriter writer = new StreamWriter("\\WorkingCurrent\\gv\\debug-sparser-mincross.txt"))
            {
                for(int i = g.MinRank; i <= g.MaxRank; i++)
                {
                    writer.WriteLine("Rank={0}:", i);

                    foreach (var n in g.Ranks[i].v)
                    {
                        writer.WriteLine("{0}", n);
                    }
                    writer.WriteLine();
                }
            }
        }
        private void DumpPositions(GraphData g)
        {
            using (StreamWriter writer = new StreamWriter("\\WorkingCurrent\\gv\\debug-sparser-positions.txt"))
            {
                foreach(var n in g.TopVertexDatas)
                {
                    writer.WriteLine("{0}:({1},{2})", n, n.Location.X, n.Location.Y);
                }
            }
        }

        /// <summary>
        /// This method is the skeleton of the layout algorithm.
        /// </summary>
        protected override void InternalCompute()
        {
            this.Init(_vertexSizes, _vertexBorders);

            this._rootGraph = new GraphData(this._rootCompoundVertex)
            {
                Parameters = this.parameters
            };
            this._rootGraph.AllVertexDatas.AddRange(this._allVertexDatas.Values);
            this._rootGraph.TopVertexDatas.AddRange(this._topVertexDatas.Values);
            this._rootGraph.AllEdgeDatas.AddRange(this._allEdgeDatas);
            this._rootGraph.TopEdgeDatas.AddRange(this._topEdgeDatas);
            
            if (this._rootGraph.TopVertexDatas.Count > 0)
            {
                this.InitSubGraphs(this._rootGraph, this._rootGraph);

                this.Ranking(this._rootGraph);

                this.MinCross(this._rootGraph);

                this.SetPosition(this._rootGraph);

                this.SavePositions(this._rootGraph);
            }
        }

        private void SavePositions(GraphData g)
        {
            foreach (var v in g.TopVertexDatas)
            {
                VertexPositions[v.Vertex] = v.Location;
            }
            
            //raise the event
            this.OnIterationEnded(0, 100.0, "done", true);
        }

        
    }
}