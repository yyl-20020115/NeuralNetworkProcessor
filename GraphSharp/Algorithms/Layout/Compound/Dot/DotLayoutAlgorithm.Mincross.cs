using System;
using System.Collections.Generic;
using System.Linq;
using QuickGraph;

namespace GraphSharp.Algorithms.Layout.Compound.Dot
{
    /// <typeparam name="TVertex">The type of the vertices.</typeparam>
    /// <typeparam name="TEdge">The type of the edges.</typeparam>
    /// <typeparam name="TGraph">The type of the graph.</typeparam>
    public partial class DotLayoutAlgorithm<TVertex, TEdge, TGraph> 
    {

        /*
         * dot_mincross(g) takes a ranked graphs, and finds an ordering
         * that avoids edge crossings.  clusters are expanded.
         * N.B. the rank structure is global (not allocated per cluster)
         * because mincross may compare nodes in different clusters.
         */

        /* following code deals with weights of edges of "virtual" nodes */
        private const int ORDINARY = 0;
        private const int SINGLETON = 1;
        private const int VIRTUALNODE = 2;
        private const int NTYPES = 3;

        private const int MC_SCALE = 256;

        private const int C_EE = 1;
        private const int C_VS = 2;
        private const int C_SS = 2;
        private const int C_VV = 4;

        private int[,] table = new int[NTYPES, NTYPES]  {
	        /* ordinary  */ {C_EE, C_EE, C_EE},
	        /* singleton */ {C_EE, C_SS, C_VS},
	        /* virtual   */ {C_EE, C_VS, C_VV}
        };
        private GraphData Root = null;
        private int[] Count = null;
        private int C = 0;
        /* mincross parameters */
        private int MinQuit = 8;
        private int MaxIter = 24;
        private double Convergence = 0.995;

        private int GlobalMinRank = 0, GlobalMaxRank = 0;
        private List<EdgeData> TE_list = null;
        private List<int> TI_list = null;

        private AdjMatrix NewAdjMatrix(int i, int j) => new AdjMatrix
        {
            nrows = i,
            ncols = j,
            data = new int[i * j]
        };

        private bool IsBackEdge(EdgeData e) => (e.Head.np.Order > e.Tail.np.Order);

        private VertexData FindSource(GraphData g)
            => g.TopVertexDatas.FirstOrDefault(n => n.FastInEdges.Count == 0);

        private int GetComponent(GraphData g, VertexData n, GraphData component, List<int> indices)
        {
            int backedge = 0;
            n.x = 1;

            indices[component.TopVertexDatas.Count] = n.Idx;
            component.TopVertexDatas.Add(n);

            foreach (var e in n.FastOutEdges)
            {
                if (IsBackEdge(e)) backedge++;
                if (e.Head.x == 0)
                    backedge += GetComponent(g, e.Head, component, indices);
            }
            foreach (var e in n.FastInEdges)
            {
                if (IsBackEdge(e)) backedge++;
                if (e.Tail.x == 0)
                    backedge += GetComponent(g, e.Tail, component, indices);
            }
            return backedge;
        }

        /* dot_mincross:
         * Minimize edge crossings
         * Note that nodes are not placed into g.Rank until mincross()
         * is called.
         */
        private void MinCross(GraphData g, bool doBalance = false)
        {
            this.InitMinCross(g);

            int nc = 0;
            for (int c = 0;c< g.Components.Count; c++)
            {
                nc += this.MinCross(g, 0, 2, doBalance);
                c++;
            }

            this.MinCrossMerge(g);
            //not called
            /* run mincross on contents of each cluster */
            foreach (var cluster in g.Clusters)
            {
                nc += MinCrossCluster(g, cluster, doBalance);
            }

            this.MinCrossCleanUp(g);
            foreach (var rank in g.Ranks)
            {
                rank.v.Reverse();
            }
        }

        private ref int ELT(AdjMatrix M, int i, int j)
            => ref (M.data[((i) * M.ncols) + (j)]);


        private int MinCrossCluster(GraphData par, GraphData g, bool doBalance)
        {
            this.ExpandCluster(g);
            this.FlatBreakCycles(g);
            this.FlatReorder(g);
            int nc = MinCross(g, 2, 2, doBalance);

            foreach(var c in g.Clusters)
                nc += this.MinCrossCluster(g, c, doBalance);

            this.SaveVList(g);
            return nc;
        }

        private bool LeftToRight(GraphData g, VertexData v, VertexData w)
        {
            bool rv =false;

            /* CLUSTER indicates orig nodes of clusters, and vnodes of skeletons */
            if(true)
            {
                if ((v.ClusterGraph != w.ClusterGraph) && (v.ClusterGraph!=null) && (w.ClusterGraph!=null))
                {
                    /* the following allows cluster skeletons to be swapped */
                    if ((v.RankType ==  RankTypes.Cluster)
                        && (v.NodeType ==  NodeTypes.Virtual))
                        return false;
                    if ((w.RankType ==  RankTypes.Cluster)
                        && (w.NodeType ==  NodeTypes.Virtual))
                        return false;
                    return true;
                    /*return ((ND_ranktype(v) != CLUSTER) && (ND_ranktype(w) != CLUSTER)); */
                }
            }

            AdjMatrix M = g.Ranks[v.Rank].flat;
            if (M == null)
                rv = false;
            else
            {
                if (g.Flip)
                {
                    VertexData t = v;
                    v = w;
                    w = t;
                }
                rv = ELT(M, v.FlatIndex,w.FlatIndex)!=0;
            }
            return rv;
        }

        private int InCross(VertexData v, VertexData w)
        {
            int cross = 0;

            foreach(var e2 in w.FastInEdges)
            {
                int cnt = e2.Penalty;

                int inv = e2.Tail.Order;

                foreach(var e1 in v.FastInEdges)
                {
                    int t = e1.Tail.Order - inv;
                    if ((t > 0)
                         || ((t == 0)
                           && e1.TailIndex > e2.TailIndex))
                        cross += e1.Penalty * cnt;
                }
            }
            return cross;
        }

        private int OutCross(VertexData v, VertexData w)
        {
            int cross = 0;

            foreach(var e2 in w.FastOutEdges)
            {
                int cnt = e2.Penalty;
                int inv = e2.Head.Order;

                foreach(var e1 in v.FastOutEdges)
                {
                    int t = e1.Head.Order - inv;
                    if ((t > 0)
                        || ((t == 0)
                             &&e1.HeadIndex> e2.HeadIndex))
                        cross += (e1.Penalty * cnt);
                }
            }
            return cross;

        }

        private void Exchange(VertexData v, VertexData w)
        {
            int r = v.Rank;
            int vi = v.Order;
            int wi = w.Order;
            v.Order = wi;
            this.Root.Ranks[r].v[wi] = v;
            w.Order = vi;
            this.Root.Ranks[r].v[vi] = w;
        }
        private void BalanceNodes(GraphData g, int r, VertexData v, VertexData w)
        {
            VertexData s = null;          /* separator node */
            int sepIndex = 0;
                  /* type of null nodes */
            int cntDummy = 0, cntOri = 0;
            int k = 0, m = 0, k1 = 0, m1 = 0;

            /* we only consider v and w of different types */
            if (v.NodeType == w.NodeType)
                return;

            /* count the number of dummy and original nodes */
            for (int i = 0; i < g.Ranks[r].v.Count; i++)
            {
                if (g.Ranks[r].v[i].NodeType == NodeTypes.Normal)
                    cntOri++;
                else
                    cntDummy++;
            }

            if (cntOri < cntDummy)
            {
                if (v.NodeType ==  NodeTypes.Normal)
                    s = v;
                else
                    s = w;
            }
            else
            {
                if (v.NodeType ==  NodeTypes.Normal)
                    s = w;
                else
                    s = v;
            }

            /* get the separator node index */
            for (int i = 0; i < g.Ranks[r].v.Count; i++)
            {
                if (g.Ranks[r].v[i] == s)
                    sepIndex = i;
            }

            var nullType = (s.NodeType ==  NodeTypes.Normal) ? NodeTypes.Virtual : NodeTypes.Normal;

            /* count the number of null nodes to the left and
             * right of the separator node
             */
            for (int i = sepIndex - 1; i >= 0; i--)
            {
                if (g.Ranks[r].v[i].NodeType == nullType)
                    k++;
                else
                    break;
            }

            for (int i = sepIndex + 1; i < g.Ranks[r].v.Count; i++)
            {
                if ((g.Ranks[r].v[i]).NodeType == nullType)
                    m++;
                else
                    break;
            }

            /* now exchange v,w and calculate the same counts */

            Exchange(v, w);

            /* get the separator node index */
            for (int i = 0; i < g.Ranks[r].v.Count; i++)
            {
                if (g.Ranks[r].v[i] == s)
                    sepIndex = i;
            }

            /* count the number of null nodes to the left and
             * right of the separator node
             */
            for (int i = sepIndex - 1; i >= 0; i--)
            {
                if ((g.Ranks[r].v[i]).NodeType == nullType)
                    k1++;
                else
                    break;
            }

            for (int i = sepIndex + 1; i < g.Ranks[r].v.Count; i++)
            {
                if ((g.Ranks[r].v[i]).NodeType == nullType)
                    m1++;
                else
                    break;
            }

            if (Math.Abs(k1 - m1) >Math.Abs(k - m))
            {
                this.Exchange(v, w);     //revert to the original ordering
            }
        }

        private int Balance(GraphData g)
        {
            int rv = 0;

            for (int r =g.MaxRank; r >= g.MinRank; r--)
            {
                g.Ranks[r].candidate = false;
                for (int i = 0; i < g.Ranks[r].v.Count - 1; i++)
                {
                    var v = g.Ranks[r].v[i];
                    var w = g.Ranks[r].v[i + 1];
                    if (LeftToRight(g, v, w))
                        continue;
                    int c0 = 0;
                    int c1 = 0;
                    if (r > 0)
                    {
                        c0 += InCross(v, w);
                        c1 += InCross(w, v);
                    }

                    if (g.Ranks[r + 1].v.Count > 0)
                    {
                        c0 += OutCross(v, w);
                        c1 += OutCross(w, v);
                    }

                    if (c1 <= c0)
                    {
                        BalanceNodes(g, r, v, w);
                    }
                }
            }
            return rv;
        }

        private int TransposeStep(GraphData g, int r, bool reverse)
        {
            int rv = 0;
            g.Ranks[r].candidate = false;
            for (int i = 0; i < g.Ranks[r].v.Count - 1; i++)
            {
                var v = g.Ranks[r].v[i];
                var w = g.Ranks[r].v[i + 1];

                if (LeftToRight(g, v, w))
                    continue;
                int c0 = 0;
                int c1 = 0;
                if (r > 0)
                {
                    c0 += InCross(v, w);
                    c1 += InCross(w, v);
                }
                if((r + 1 >= g.Ranks.Length))
                {
                    //skip it
                }
                else if (g.Ranks[r + 1].v.Count > 0)
                {
                    c0 += OutCross(v, w);
                    c1 += OutCross(w, v);
                }
                if ((c1 < c0) || ((c0 > 0) && reverse && (c1 == c0)))
                {
                    this.Exchange(v, w);
                    rv += (c0 - c1);
                    this.Root.Ranks[r].valid = false;
                    g.Ranks[r].candidate = true;

                    if (r > g.MinRank)
                    {
                        this.Root.Ranks[r - 1].valid = false;
                        g.Ranks[r - 1].candidate = true;
                    }
                    if (r <g.MaxRank)
                    {
                        this.Root.Ranks[r + 1].valid = false;
                        g.Ranks[r + 1].candidate = true;
                    }
                }
            }
            return rv;
        }

        private void Transpose(GraphData g, bool reverse)
        {
            int delta = 0;

            for (int r = g.MinRank; r <=g.MaxRank; r++)
                g.Ranks[r].candidate = true;
            do
            {
                delta = 0;
                for (int r = g.MinRank; r <=g.MaxRank; r++)
                {
                    if (g.Ranks[r].candidate)
                    {
                        delta += TransposeStep(g, r, reverse);
                    }
                }
                /*} while (delta > ncross(g)*(1.0 - Convergence)); */
            } while (delta >= 1);
        }

        private int MinCross(GraphData g, int startpass, int endpass, bool doBalance)
        {
            int maxthispass = 0, iter, trying, pass;
            int cur_cross, best_cross;

            if (startpass > 1)
            {
                cur_cross = best_cross = NCross();
                SaveBest(g);
            }
            else
                cur_cross = best_cross = int.MaxValue;
            for (pass = startpass; pass <= endpass; pass++)
            {
                if (pass <= 1)
                {
                    maxthispass = Math.Min(4, MaxIter);
                    if (g == g.DotRoot)
                        BuildRanks(g, pass);
                    if (pass == 0)
                        FlatBreakCycles(g);
                    FlatReorder(g);

                    if ((cur_cross = NCross()) <= best_cross)
                    {
                        SaveBest(g);
                        best_cross = cur_cross;
                    }
                    trying = 0;
                }
                else
                {
                    maxthispass = MaxIter;
                    if (cur_cross > best_cross)
                        RestoreBest(g);
                    cur_cross = best_cross;
                }
                trying = 0;
                for (iter = 0; iter < maxthispass; iter++)
                {
                    if (trying++ >= MinQuit)
                        break;
                    if (cur_cross == 0)
                        break;
                    this.MinCrossStep(g, iter);
                    if ((cur_cross = NCross()) <= best_cross)
                    {
                        SaveBest(g);
                        if (cur_cross < Convergence * best_cross)
                            trying = 0;
                        best_cross = cur_cross;
                    }
                }
                if (cur_cross == 0)
                    break;
            }
            if (cur_cross > best_cross)
                RestoreBest(g);
            if (best_cross > 0)
            {
                Transpose(g, false);
                best_cross = NCross();
            }
            if (doBalance)
            {
                for (iter = 0; iter < maxthispass; iter++)
                    Balance(g);
            }

            return best_cross;
        }
        private class NodeComparer : IComparer<VertexData>
        {
            public int Compare(VertexData x, VertexData y)
            {
                return x.Order - y.Order;
            }
        }
        private void RestoreBest(GraphData g)
        {
            for (int r = g.MinRank; r <=g.MaxRank; r++)
            {
                for (int i = 0; i < g.Ranks[r].v.Count; i++)
                {
                    var n = g.Ranks[r].v[i];
                    n.Order = n.SavedOrder;
                }
            }
            for (int r = g.MinRank; r <=g.MaxRank; r++)
            {
                this.Root.Ranks[r].valid = false;
                g.Ranks[r].v.Sort(new NodeComparer());

            }
        }


        private void SaveBest(GraphData g)
        {
            for (int r = g.MinRank; r <=g.MaxRank; r++)
            {
                for (int i = 0; i < g.Ranks[r].v.Count; i++)
                {
                    var n = g.Ranks[r].v[i];
                    n.SavedOrder =n.Order;
                }
            }
        }

        /* merges the connected components of g */
        private void MergeComponents(GraphData g)
        {
            if (g.TopVertexDatas.Count <= 1)
                return;
            var component = new List<VertexData>();

            foreach (var c in g.Components)
            {
                component.AddRange(c);
            }
            g.Components.Clear();
            g.Components.Add(component);

            g.NList.Clear();
            g.NList.AddRange(component);
            g.MinRank = GlobalMinRank;
            g.MaxRank = GlobalMaxRank;
        }

        /* merge connected components, create globally consistent rank lists */
        private void MinCrossMerge(GraphData g)
        {
            /* merge the components and rank limits */
            this.MergeComponents(g);

            /* install complete ranks */
            for (int r = g.MinRank; r <=g.MaxRank; r++)
            {
                for (int i = 0; i < g.Ranks[r].v.Count; i++)
                {
                    var v = g.Ranks[r].v[i];

                    v.Order = i;
                }
            }
        }

        private void MinCrossCleanUp(GraphData g)
        {
            if (TI_list!=null)
            {
                TI_list.Clear();
                TI_list = null;
            }
            if (TE_list!=null)
            {
                TE_list.Clear(); ;
                TE_list = null;
            }
            /* fix vlists of clusters */
            foreach (var c in g.Clusters)
            {
                this.RecallResetVLists(c);
            }
            /* remove node temporary edges for ordering nodes */
            for (int r = g.MinRank; r <=g.MaxRank; r++)
            {
                for (int i = 0; i < g.Ranks[r].v.Count; i++)
                {
                    var v = g.Ranks[r].v[i];

                    v.Order = i;

                    foreach (var e in v.FlatOutEdges.ToArray())
                    {
                        if (e.EdgeType == EdgeTypes.FlatOrder)
                        {
                            this.DeleteFlatEdge(e);
                        }
                    }
                }
                g.Ranks[r].flat = null;
            }
        }

        private VertexData Neighbor(VertexData v, int dir)
        {
            VertexData rv = null;

            if (dir < 0)
            {
                if (v.Order > 0)
                    rv = this.Root.Ranks[v.Rank].v[v.Order - 1];
            }
            else
            {
                rv = this.Root.Ranks[v.Rank].v[v.Order + 1];
            }
            return rv;
        }

        private bool IsANormalNodeOf(GraphData g, VertexData v)
        {
            return (v.NodeType ==  NodeTypes.Normal) && g.TopVertexDatas.Contains(v);
        }

        private bool IsAVNodeOfAnEdgeOf(GraphData g, VertexData v)
        {
            if ((v.NodeType == NodeTypes.Virtual)
                && (v.FastInEdges.Count == 1) && (v.FastOutEdges.Count == 1))
            {
                var e = v.FastOutEdges.FirstOrDefault();
                while (e.EdgeType!= EdgeTypes.Normal)
                    e = e.Original;
                if (g.AllEdgeDatas.Contains(e))
                    return true;
            }
            return false;
        }

        private bool InsideCluster(GraphData g, VertexData v)
        {
            return (IsANormalNodeOf(g, v) | IsAVNodeOfAnEdgeOf(g, v));
        }

        private VertexData FurthestNode(GraphData g, VertexData v, int dir)
        {
            VertexData u, rv;

            rv = u = v;
            while ((u = Neighbor(u, dir))!=null)
            {
                if (IsANormalNodeOf(g, u))
                    rv = u;
                else if (IsAVNodeOfAnEdgeOf(g, u))
                    rv = u;
            }
            return rv;
        }

        private void SaveVList(GraphData g)
        {
            if (g.RankLeaders!=null)
                for (int r = g.MinRank; r <=g.MaxRank; r++)
                {
                    g.RankLeaders.Add(g.Ranks[r].v[0]);
                }
        }

        private void RecallSaveVLists(GraphData g)
        {
            SaveVList(g);
            foreach(var c in g.Clusters)
                RecallSaveVLists(c);
        }

        private void RecallResetVLists(GraphData g)
        {
            /* fix vlists of sub-clusters */
            foreach(var c in g.Clusters)
                this.RecallResetVLists(c);

            if (g.RankLeaders != null)
            {
                for (int r = g.MinRank; r <= g.MaxRank; r++)
                {
                    var v = g.RankLeaders[r];
                    var u = this.FurthestNode(g, v, -1);
                    var w = this.FurthestNode(g, v, 1);
                    g.RankLeaders[r] = u;
                    g.Ranks[r].v = g.DotRoot.Ranks[r].v.Skip(u.Order).ToList();
                }
            }
        }

        private void InitMinCross(GraphData g)
        {
            this.Root = g;

            /* alloc +1 for the null terminator usage in do_ordering() */
            /* also, the +1 avoids attempts to alloc 0 sizes, something
	           that efence complains about */
            TE_list = new List<EdgeData>();
            TI_list = new List<int>();

            this.ClassifyForPositioning(g);
            g.Components = this.Decompose(g);
            this.AllocateRanks(g);
            this.GlobalMinRank =g.MinRank;
            this.GlobalMaxRank =g.MaxRank;
        }

        private void FlatRev(GraphData g, EdgeData e)
        {
            EdgeData rev = null;
            if (e.Head.FlatOutEdges.Count == 0)
                rev = null;
            else
                foreach (var rev2 in e.Head.FlatOutEdges)
                    if (rev2.Head == e.Tail)
                    {
                        rev = rev2;
                        break;
                    }
            
            if (rev != null)
            {
                MergeOneWay(e, rev);
                if (e.Virtual == null)
                    e.Virtual = rev;
                if ((rev.EdgeType == EdgeTypes.FlatOrder)
                    && (rev.Original == null))
                    rev.Original = e;
                e.Tail.OtherEdges.Add(e);
            }
            else
            {
                rev = this.NewVirtualEdge(e.Head, e.Tail, e);
                if (e.EdgeType == EdgeTypes.FlatOrder)
                    rev.EdgeType = EdgeTypes.FlatOrder;
                else
                    rev.EdgeType =  EdgeTypes.Reversed;

                this.FlatEdge(g, rev);
            }
        }

        private void FlatSearch(GraphData g, VertexData v)
        {
            bool hascl =false ;
            AdjMatrix M = g.Ranks[v.Rank].flat;

            v.Mark = 1;
            v.IsOnStack = true;
            hascl = (g.DotRoot.Clusters.Count) >0;
            if (v.FlatOutEdges.Count>0)
                foreach(var e in v.FlatOutEdges.ToList())
                {
                    if (hascl && !(g.TopVertexDatas.Contains(e.Tail) && g.TopVertexDatas.Contains(e.Head)))
                        continue;
                    if (e.Weight == 0)
                        continue;
                    if ((e.Head.IsOnStack) == true)
                    {
                        ELT(M, (e.Head.FlatIndex), (e.Tail.FlatIndex)) = 1;
                        this.DeleteFlatEdge(e);
                        if (e.EdgeType == EdgeTypes.FlatOrder)
                            continue;
                        FlatRev(g, e);
                    }
                    else
                    {

                        ELT(M, (e.Tail.FlatIndex), (e.Head.FlatIndex)) = 1;
                        if ((e.Head.Mark) == 0)
                            FlatSearch(g, e.Head);
                    }
                }
            v.IsOnStack = false;
        }

        private void FlatBreakCycles(GraphData g)
        {
            bool flat = false;

            for (int r = g.MinRank; r <=g.MaxRank; r++)
            {
                flat = false;
                for (int i = 0; i < g.Ranks[r].v.Count; i++)
                {
                    var v = g.Ranks[r].v[i];
                    v.Mark = 0;
                    v.IsOnStack = false;
                    v.FlatIndex = i;
                    if ((v.FlatOutEdges.Count > 0) && (flat == false))
                    {
                        g.Ranks[r].flat =
                            this.NewAdjMatrix(g.Ranks[r].v.Count, g.Ranks[r].v.Count);
                        flat = true;
                    }
                }
                if (flat)
                {
                    for (int i = 0; i < g.Ranks[r].v.Count; i++)
                    {
                        var v = g.Ranks[r].v[i];
                        if (v.Mark == 0)
                            this.FlatSearch(g, v);
                    }
                }
            }
        }

        /* allocate_ranks:
         * Allocate rank structure, determining number of nodes per rank.
         * Note that no nodes are put into the structure yet.
         */
        private void AllocateRanks(GraphData g)
        {
            g.Ranks = new RankData[g.MaxRank + 1];
            for (int r = g.MinRank; r <=g.MaxRank; r++)
            {

                g.Ranks[r] = new RankData();
               
            }
        }
        
        /* install a node at the current right end of its rank */
        private bool InstallInRanks(GraphData g, VertexData n)
        {
            int r = n.Rank;
            int i = g.Ranks[r].v.Count;

            g.Ranks[r].v.Add(n);
            n.Order = i;

            if (n.Order >(this.Root.Ranks)[r].v.Count)
            {
                return false;
            }
            if ((r < g.MinRank) || (r >g.MaxRank))
            {
                return false;
            }
            if (n.Order > this.Root.Ranks[r].v.Count)
            {
                return false;
            }
            return true;
        }

        /*	install nodes in ranks. the initial ordering ensure that series-parallel
         *	graphs such as trees are drawn with no crossings.  it tries searching
         *	in- and out-edges and takes the better of the two initial orderings.
         */
        private void BuildRanks(GraphData g, int pass)
        {
            var q = new Queue<VertexData>();

            foreach(var n in g.NList)
                n.Mark = 0;

            for (int i = g.MinRank; i <= g.MaxRank; i++)
            {
                //clear
                g.Ranks[i].v.Clear();
            }
            foreach(var n in g.NList)
            {
                var otheredges = ((pass == 0) ? n.FastInEdges : n.FastOutEdges);
                if (otheredges.FirstOrDefault() != null)
                    continue;
                if (n.Mark ==0)
                {
                    n.Mark = 1;
                    q.Enqueue(n);
                    VertexData n0 = null;

                    while (q.Count>0 && (n0 =q.Dequeue())!=null)
                    {
                        if (n0.RankType !=  RankTypes.Cluster)
                        {
                            this.InstallInRanks(g, n0);
                            this.EnqueueNeighbors(q, n0, pass);
                        }
                        else
                        {
                            this.InstallCluster(g, n0, pass, q);
                        }
                    }
                }
            }
            for (int i = g.MinRank; i <=g.MaxRank; i++)
            {
                this.Root.Ranks[i].valid = false;
                if (g.Flip && (g.Ranks[i].v.Count > 0))
                {
                    var vlist = g.Ranks[i].v;
                    int n = g.Ranks[i].v.Count - 1;
                    int ndiv2 = n / 2;
                    for (int j = 0; j <= ndiv2; j++)
                        this.Exchange(vlist[j], vlist[n - j]);
                }
            }

            if ((g ==g.DotRoot) && NCross() > 0)
                this.Transpose(g, false);
        }

        private void EnqueueNeighbors(Queue<VertexData> q, VertexData n0, int pass)
        {
            if (pass == 0)
            {
                foreach(var e in n0.FastOutEdges)
                {
                    if (e.Head.Mark == 0)
                    {
                        e.Head.Mark = 1;
                        q.Enqueue(e.Head);
                    }
                }
            }
            else
            {
                foreach(var e in n0.FastInEdges)
                {
                    if (e.Tail.Mark == 0)
                    {
                        e.Tail.Mark = 1;
                        q.Enqueue(e.Tail);
                    }
                }
            }
        }

        private bool ConstrainingFlatEdge(GraphData g, VertexData v, EdgeData e)
        {
            if (e.Weight == 0) return false;
            if (!this.InsideCluster(g, e.Tail)) return false;
            if (!this.InsideCluster(g, e.Head)) return false;
            return true;
        }


        /* construct nodes reachable from 'here' in post-order.
        * This is the same as doing a topological sort in reverse order.
        */
        private int PostOrder(GraphData g, VertexData v, List<VertexData> list, int r)
        {
            int cnt = 0;
            v.Mark = 1;
            if (v.FlatOutEdges.Count > 0)
            {
                foreach(var e in v.FlatOutEdges)
                {
                    if (!ConstrainingFlatEdge(g, v, e)) continue;
                    if (e.Head.Mark == 0)
                        cnt += PostOrder(g, e.Head, list.Skip(cnt).ToList(), r);
                }
            }
            list[cnt++] = v;
            return cnt;
        }

        private void FlatReorder(GraphData g)
        {
            int i, j, r, pos, n_search=0, local_in_cnt, local_out_cnt, base_order;
            VertexData v;
            EdgeData flat_e;

            if (g.HasFlatEdge == false)
                return;
            for (r = g.MinRank; r <=g.MaxRank; r++)
            {
                if (g.Ranks[r].v.Count == 0) continue;
                base_order = (g.Ranks[r].v[0]).Order;
                for (i = 0; i < g.Ranks[r].v.Count; i++)
                    (g.Ranks[r].v[i]).Mark = 0;
                var temprank = new List<VertexData>(i + 1);
                pos = 0;

                /* construct reverse topological sort order in temprank */
                for (i = 0; i < g.Ranks[r].v.Count; i++)
                {
                    if (g.Flip) v = g.Ranks[r].v[i];
                    else v = g.Ranks[r].v[g.Ranks[r].v.Count - i - 1];

                    local_in_cnt = local_out_cnt = 0;
                    for (j = 0; j < v.FlatInEdges.Count; j++)
                    {
                        flat_e = v.FlatInEdges[j];
                        if (ConstrainingFlatEdge(g, v, flat_e)) local_in_cnt++;
                    }
                    for (j = 0; j < v.FlatOutEdges.Count; j++)
                    {
                        flat_e = v.FlatOutEdges[j];
                        if (ConstrainingFlatEdge(g, v, flat_e)) local_out_cnt++;
                    }
                    if ((local_in_cnt == 0) && (local_out_cnt == 0))
                        temprank[pos++] = v;
                    else
                    {
                        if ((v.Mark == 0) && (local_in_cnt == 0))
                        {
                            var left = temprank.Skip(pos).ToList();
                            n_search = this.PostOrder(g, v, left, r);
                            pos += n_search;
                        }
                    }
                }

                if (pos!=0)
                {
                    if (g.Flip== false)
                    {
                        var left = 0;
                        var right = pos-1;
                        while (left < right)
                        {
                            var t = temprank[left];
                            temprank[left] = temprank[right];
                            temprank[right] = t;
                            left++;
                            right--;
                        }
                    }
                    for (i = 0; i < g.Ranks[r].v.Count; i++)
                    {
                        v = g.Ranks[r].v[i] = temprank[i];
                        v.Order = i + base_order;
                    }

                    /* nonconstraint flat edges must be made LR */
                    for (i = 0; i < g.Ranks[r].v.Count; i++)
                    {
                        v = g.Ranks[r].v[i];
                        if (v.FlatOutEdges.Count>0)
                        {
                            foreach(var e in v.FlatOutEdges.ToList())
                            {
                                if (((g.Flip == false) && ((e.Head.Order) < (e.Tail.Order))) ||
                                    ((g.Flip) && ((e.Head.Order) > (e.Tail.Order))))
                                {
                                    DeleteFlatEdge(e);
                                    
                                    FlatRev(g, e);
                                }
                            }
                        }
                    }
                    /* postprocess to restore intended order */
                }
                /* else do no harm! */
                this.Root.Ranks[r].valid = false;
            }
        }

        private void Reorder(GraphData g,int r, bool reverse, bool hasfixed)
        {
            int changed = 0;
            bool muststay, sawclust;
            var vlist = g.Ranks[r].v;
            for (int nelt = g.Ranks[r].v.Count - 1; nelt >= 0; nelt--)
            {
                var lp = 0;
                var ep = g.Ranks[r].v.Count;
                while (lp < ep)
                {
                    /* find leftmost node that can be compared */
                    while ((lp < ep) && (g.Ranks[r].v[lp].Mval < 0.0))
                        lp++;
                    if (lp >= ep)
                        break;
                    /* find the node that can be compared */
                    sawclust = muststay = false;
                    int rp = lp + 1;
                    for (; rp < ep; rp++)
                    {
                        if (sawclust && g.Ranks[r].v[rp].ClusterGraph != null)
                            continue;   /* ### */
                        if (this.LeftToRight(g, g.Ranks[r].v[lp], g.Ranks[r].v[rp]))
                        {
                            muststay = true;
                            break;
                        }
                        if (g.Ranks[r].v[rp].Mval >= 0.0)
                            break;
                        if (g.Ranks[r].v[rp].ClusterGraph!=null)
                            sawclust = true;    /* ### */
                    }
                    if (rp >= ep)
                        break;
                    if (muststay == false)
                    {
                        int p1 = (int)(g.Ranks[r].v[lp].Mval);
                        int p2 = (int)(g.Ranks[r].v[rp].Mval);
                        if ((p1 > p2) || ((p1 == p2) && (reverse)))
                        {
                            this.Exchange(g.Ranks[r].v[lp], g.Ranks[r].v[rp]);
                            changed++;
                        }
                    }
                    lp = rp;
                }
                if ((hasfixed == false) && (reverse == false))
                    ep--;
            }

            if (changed!=0)
            {
                this.Root.Ranks[r].valid = false;
                if (r > 0)
                    this.Root.Ranks[r - 1].valid = false;
            }
        }

        private void MinCrossStep(GraphData g,int pass)
        {
            int r, other, first, last, dir;
            bool hasfixed = false, reverse = (pass % 4) < 2;

            if (pass % 2 != 0)
            {
                r =g.MaxRank - 1;
                dir = -1;
            } /* up pass */
            else
            {
                r = 1;
                dir = 1;
            }               /* down pass */

            if (pass % 2 == 0)
            {   /* down pass */
                first = g.MinRank + 1;
                if (g.MinRank > this.Root.MinRank)
                    first--;
                last =g.MaxRank;
                dir = 1;
            }
            else
            {           /* up pass */
                first =g.MaxRank - 1;
                last = g.MinRank;
                if (g.MaxRank < this.Root.MaxRank)
                    first++;
                dir = -1;
            }

            for (r = first; r != last + dir; r += dir)
            {
                other = r - dir;
                hasfixed = this.Medians(g, r, other);
                Reorder(g, r, reverse, hasfixed);
            }
            Transpose(g, !reverse);
        }

        private int LocalCross(List<EdgeData> l, int dir)
        {
            bool is_out = dir > 0;
            int cross = 0;

            for (int i = 0; i<l.Count; i++)
            {
                var e = l[i];
                if (is_out)
                    for (int j = i + 1; j<l.Count; j++)
                    {
                        var f = l[j];
                        if ((f.Head.Order - e.Head.Order)
                            * (f.TailIndex - e.TailIndex) < 0)
                            cross += e.Penalty*f.Penalty;
                    }
                else
                    for (int j = i + 1; j<l.Count; j++)
                    {
                        var f = l[j];
                        if ((f.Tail.Order - e.Tail.Order)
                            *(f.HeadIndex - e.HeadIndex) < 0)
                            cross += e.Penalty * f.Penalty;
                    }
            }
            return cross;
        }

 
        private int RCross(GraphData g, int r)
        {
            int top = 0, bot = 0, cross = 0, max = 0, i = 0, k = 0;
            var rtop = g.Ranks[r].v;

            if (C <= this.Root.Ranks[r + 1].v.Count)
            {
                C = this.Root.Ranks[r + 1].v.Count + 1;
                Count = new int[C];
            }

            for (i = 0; i < g.Ranks[r + 1].v.Count; i++)
                Count[i] = 0;

            for (top = 0; top < g.Ranks[r].v.Count; top++)
            {
                if (max > 0)
                {
                    foreach(var e in rtop[top].FastOutEdges)
                    {
                        for (k = e.Head.Order + 1; k <= max; k++)
                            cross += Count[k] * e.Penalty;
                    }
                }
                foreach (var e in rtop[top].FastOutEdges)
                {
                    int inv = e.Head.Order;
                    if (inv > max)
                        max = inv;
                    Count[inv] += e.Penalty;
                }
            }
            for (top = 0; top < g.Ranks[r].v.Count; top++)
            {
                var v = g.Ranks[r].v[top];
                if(v.HasPort)
                    cross += LocalCross(v.FastOutEdges, 1);
            }
            for (bot = 0; bot < g.Ranks[r + 1].v.Count; bot++)
            {
                var v = g.Ranks[r + 1].v[bot];
                if(v.HasPort)
                    cross += LocalCross(v.FastInEdges, -1);
            }
            return cross;
        }

        private int NCross()
        {
            GraphData g = this.Root;

            int count = 0;
            for (int r = g.MinRank; r <g.MaxRank; r++)
            {
                if (g.Ranks[r].valid)
                    count += g.Ranks[r].cache_nc;
                else
                {
                    int nc = g.Ranks[r].cache_nc = RCross(g, r);
                    count += nc;
                    g.Ranks[r].valid = true;
                }
            }
            return count;
        }

        /* flat_mval:
         * Calculate a mval for nodes with no in or out non-flat edges.
         * Assume (ND_out(n).size == 0) && (ND_in(n).size == 0)
         * Find flat edge a->n where a has the largest order and set
         * n.mval = a.mval+1, assuming a.mval is defined (>=0).
         * If there are no flat in edges, find flat edge n->a where a
         * has the smallest order and set * n.mval = a.mval-1, assuming
         * a.mval is > 0.
         * Return true if n.mval is left -1, indicating a fixed node for sorting.
         */
        private bool FlatMVal(VertexData n)
        {
            if (n.FlatInEdges.Count > 0)
            {
                var fl = n.FlatInEdges;
                var nn =(fl[0].Tail);
                for (int i = 1; i<fl.Count; i++)
                {
                    var e = fl[i];
                    if (e.Tail.Order > nn.Order)
                        nn = e.Tail;
                }
                if (nn.Mval > 0)
                {
                    n.Mval = nn.Mval + 1;
                    return false;
                }
            }
            else if (n.FlatOutEdges.Count > 0)
            {
                var fl = n.FlatOutEdges;
                var nn = fl[0].Head;
                for (int i = 1; i < fl.Count; i++)
                {
                    var e = fl[i];
                    if (e.Head.Order < nn.Order)
                        nn = e.Head;
                }
                if (nn.Mval > 0)
                {
                    n.Mval = nn.Mval - 1;
                    return false;
                }
            }
            return true;
        }
        private int VAL(VertexData node, int index) => (MC_SCALE * node.Order + index);

        private bool Medians(GraphData g,int r0, int r1)
        {
            bool hasfixed = false;

            var list = TI_list;
            if (list.Count == 0)
                return hasfixed;
            var v = g.Ranks[r0].v;
            for (int i = 0; i < g.Ranks[r0].v.Count; i++)
            {
                var n = v[i];
                int j = 0;
                if (r1 > r0)
                    foreach (var e in n.FastOutEdges)
                    {
                        if (e.Penalty > 0)
                            list[j++] = VAL(e.Head, e.HeadIndex);
                    }
                else
                    foreach (var e in n.FastInEdges)
                    {
                        if (e.Penalty > 0)
                            list[j++] = VAL(e.Tail, e.TailIndex);
                    }
                switch (j)
                {
                    case 0:
                        n.Mval = -1;
                        break;
                    case 1:
                        n.Mval = list[0];
                        break;
                    case 2:
                        n.Mval = (list[0] + list[1]) / 2;
                        break;
                    default:
                        list.Sort();
                        if (j % 2 != 0)
                            n.Mval = list[j / 2];
                        else
                        {
                            /* weighted median */
                            int rm = j / 2;
                            int lm = rm - 1;
                            int rspan = list[j - 1] - list[rm];
                            int lspan = list[lm] - list[0];
                            if (lspan == rspan)
                                n.Mval = (list[lm] + list[rm]) / 2;
                            else
                            {
                                double w = list[lm] * (double)rspan + list[rm] * (double)lspan;
                                n.Mval = w / (lspan + rspan);
                            }
                        }
                        break;
                }
            }
            for (int i = 0; i < g.Ranks[r0].v.Count; i++)
            {
                var n = v[i];
                if ((n.FastOutEdges.Count == 0) && (n.FastInEdges.Count == 0))
                    hasfixed |= FlatMVal(n);
            }
            return hasfixed;
        }

        private int EndPointClass(VertexData n)
        {
            if (n.NodeType == NodeTypes.Virtual)
                return VIRTUALNODE;
            if (n.WeightClass <= 1)
                return SINGLETON;
            return ORDINARY;
        }

        private void VirtualWeight(EdgeData e)
        {
            e.Weight *= table[EndPointClass(e.Tail), EndPointClass(e.Head)];
        }
    }
}
