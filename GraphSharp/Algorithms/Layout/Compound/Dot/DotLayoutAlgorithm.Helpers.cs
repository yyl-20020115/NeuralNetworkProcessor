using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using QuickGraph;

namespace GraphSharp.Algorithms.Layout.Compound.Dot
{
    /// <typeparam name="TVertex">The type of the vertices.</typeparam>
    /// <typeparam name="TEdge">The type of the edges.</typeparam>
    /// <typeparam name="TGraph">The type of the graph.</typeparam>
    public partial class DotLayoutAlgorithm<TVertex, TEdge, TGraph>         
    {

        private const int HLB = 0;  /* hard left bound */
        private const int HRB = 1;  /* hard right bound */
        private const int SLB = 2;  /* soft left bound */
        private const int SRB = 3;	/* soft right bound */
        private const int UP = 0;
        private const int DOWN = 1;
        private const int CL_CROSS = 1;
        /*
        * position(g): set ND_coord(n) (x and y) for all nodes n of g, using g.Rank.
        * (the graph may be modified by merging certain edges with a common endpoint.)
        * the coordinates are computed by constructing and ranking an auxiliary graph.
        * then leaf nodes are inserted in the fast graph.  cluster boundary nodes are
        * created and correctly separated.
        */
        private class AdjMatrix
        {
            public int nrows;
            public int ncols;
            public int[] data;
        }
        private class RankData
        {
            public List<VertexData> v = new List<VertexData>();     /* ordered list of nodes in rank    */
            public double ht1; /* height below/above centerline    */
            public double ht2; /* height below/above centerline    */
            public double pht1;    /* as above, but only primitive nodes   */
            public double pht2;    /* as above, but only primitive nodes   */
            public bool candidate;  /* for transpose () */
            public bool valid;
            public int cache_nc;       /* caches number of crossings */
            public AdjMatrix flat;
        }
        private bool PortsEquals(EdgeData e, EdgeData f)
        {
            return e!=null && f!=null && e.HeadIndex == f.HeadIndex && e.TailIndex == f.TailIndex;
        }
        #region Cluster
        private VertexData MapInterClusterNode(VertexData n) => ((n.ClusterGraph == null) || n.ClusterGraph.Expanded) ? n : n.ClusterGraph.RankLeaders[n.Rank];

        /* make d slots starting at position pos (where 1 already exists) */
        private void MakeSlots(GraphData root, int r, int pos, int d)
        {
            VertexData v = null;
            var vlist = root.Ranks[r].v;
            if (d <= 0)
            {
                for (int i = pos - d + 1; i < root.Ranks[r].v.Count; i++)
                {
                    v = vlist[i];
                    v.Order = i + d - 1;
                    vlist[v.Order] = v;
                }
                for (int i = root.Ranks[r].v.Count + d - 1; i < root.Ranks[r].v.Count; i++)
                    vlist[i] = null;
            }
            else
            {
                for (int i = root.Ranks[r].v.Count - 1; i > pos; i--)
                {
                    v = vlist[i];
                    v.Order = i + d - 1;
                    vlist[v.Order] = v;
                }
                for (int i = pos + 1; i < pos + d; i++)
                    vlist[i] = null;
            }
        }

        private VertexData CloneVirtualNode(GraphData g, VertexData vn)
        {
            int r = vn.Rank;
            this.MakeSlots(g, r, vn.Order, 2);
            var rv = CreateVirtualNode(g);
            rv.lw = vn.lw;
            rv.rw = vn.rw;
            rv.Rank = vn.Rank;
            rv.Order = vn.Order + 1;
            g.Ranks[r].v[rv.Order] = rv;
            return rv;
        }

        private void MapPath(VertexData from, VertexData to, EdgeData orig, EdgeData ve, EdgeTypes type)
        {
            VertexData u, v;
            EdgeData e;
            if (ve.Tail == from && ve.Head == to)
                return;

            if (ve.Count > 1)
            {
                orig.Virtual = null;
                if (to.Rank - from.Rank == 1)
                {
                    if ((e = this.FindFastEdge(from, to)) != null && (PortsEquals(orig, e)))
                    {
                        this.MergeOneWay(orig, e);
                        if ((from.NodeType == NodeTypes.Normal)
                            && (to.NodeType == NodeTypes.Normal))
                            OtherEdge(orig);
                        return;
                    }
                }
                u = from;
                for (int r =(from.Rank); r <(to.Rank); r++)
                {
                    if (r < to.Rank - 1)
                        v = CloneVirtualNode(from.Root, ve.Head);
                    else
                        v = to;
                    e = AttachNewVirtualEdge(u, v, orig);
                    e.EdgeType = type;
                    u = v;
                    ve.Count--;
                    ve = ve.Head.FastOutEdges.FirstOrDefault();
                }
            }
            else
            {
                if (to.Rank - from.Rank == 1)
                {
                    if ((ve = this.FindFastEdge(from, to))!=null && (PortsEquals(orig, ve)))
                    {
                        /*ED_to_orig(ve) = orig; */
                        orig.Virtual = ve;
                        ve.EdgeType = type;
                        ve.Count++;
                        if ((from.NodeType==  NodeTypes.Normal)
                            && (to.NodeType == NodeTypes.Normal))
                            OtherEdge(orig);
                    }
                    else
                    {
                        orig.Virtual = null;
                        ve = AttachNewVirtualEdge(from, to, orig);
                        ve.EdgeType = type;
                    }
                }
                if (to.Rank - from.Rank > 1)
                {
                    e = ve;
                    if (ve.Tail != from)
                    {
                        orig.Virtual = null;
                        e = orig.Virtual = this.NewVirtualEdge(from, ve.Head, orig);
                        DeleteFastEdge(ve);
                    }
                    else
                        e = ve;
                    while (e.Head.Rank != to.Rank)
                        e = e.Head.FastOutEdges.FirstOrDefault();
                    if (e.Head != to)
                    {
                        ve = e;
                        e = this.NewVirtualEdge(e.Tail, to, orig);
                        e.EdgeType = type;
                        DeleteFastEdge(ve);
                    }
                }
            }
        }

        private void MakeInterClusterChain(GraphData g, VertexData from, VertexData to, EdgeData orig)
        {
            var u = MapInterClusterNode(from);
            var v = MapInterClusterNode(to);

            this.MapPath(u, v, orig, orig.Virtual, (u == from) && (v == to) ? EdgeTypes.Virtual: EdgeTypes.ClusterEdge);
        }

        /*
         * attach and install edges between clusters.
         * essentially, class2() for interclust edges.
         */
        private void InterCluster(GraphData subg)
        {
            GraphData g = subg.DotRoot;
            EdgeData prev = null;

            foreach(var n in subg.TopVertexDatas)
            {
                /* N.B. n may be in a sub-cluster of subg */
                prev = null;
                foreach(var e in n.FastOutEdges)// (e = agfstedge(g, n); e; e = next)
                {
                    if (subg.AllEdgeDatas.Contains(e))
                        continue;

                    /* short/flat multi edges */
                    if (Mergeable(prev, e))
                    {
                        if ((e.Tail.Rank) == (e.Head.Rank))
                            e.Virtual = prev;
                        else
                            e.Virtual = null;
                        if (prev.Virtual == null)
                            continue;   /* internal edge */
                        MergeChain(subg, e, prev.Virtual, false);
                        SafeOtherEdge(e);
                        continue;
                    }

                    /* flat edges */
                    if ((e.Tail.Rank) == (e.Head.Rank))
                    {
                        EdgeData fe;
                        if ((fe = this.FindFlatEdge(e.Tail, e.Head)) == null)
                        {
                            FlatEdge(g, e);
                            prev = e;
                        }
                        else if (e != fe)
                        {
                            SafeOtherEdge(e);
                            if (e.Virtual==null) MergeOneWay(e, fe);
                        }
                        continue;
                    }

                    /* forward edges */
                    if ((e.Head.Rank) > (e.Tail.Rank))
                    {
                        MakeInterClusterChain(g, e.Tail, e.Head, e);
                        prev = e;
                        continue;
                    }

                    /* backward edges */
                    else
                    {
                        /*
                        I think that make_interclust_chain should create call other_edge(e) anyway
                                        if (agcontains(subg,e.Tail)
                                            && agfindedge(g,e.Head,e.Tail)) other_edge(e);
                        */
                        MakeInterClusterChain(g, e.Head, e.Tail, e);
                        prev = e;
                    }
                }
            }
        }

        private void MergeRanks(GraphData subg)
        {
            int r, pos, ipos;
            GraphData root = subg.DotRoot;
            if ((subg.MinRank) > 0)
                root.Ranks[subg.MinRank - 1].valid = false;
            for (r = subg.MinRank; r <= subg.MaxRank; r++)
            {
                int d = subg.Ranks[r].v.Count;
                ipos = pos = (subg.RankLeaders[r].Order);
                MakeSlots(root, r, pos, d);
                for (int i = 0; i < subg.Ranks[r].v.Count; i++)
                {
                    var v = root.Ranks[r].v[pos] =subg.Ranks[r].v[i];
                    v.Order = pos++;
                    /* real nodes automatically have v->root = root graph */
                    if (v.NodeType ==  NodeTypes.Virtual)
                        v.Root = root;
                    this.DeleteFastNode(subg, v);
                    this.FastNode(root, v);
                }
                subg.Ranks[r].v = root.Ranks[r].v.Skip(ipos).ToList();
                root.Ranks[r].valid = false;
            }
            if (r < root.MaxRank)
                root.Ranks[r].valid = false;
            subg.Expanded = true;
        }

        private void RemoveRankLeaders(GraphData g)
        {
            for (int r = g.MinRank; r <= g.MaxRank; r++)
            {
                var v = g.RankLeaders[r];

                /* remove the entire chain */
                foreach(var e in v.FastOutEdges)
                    this.DeleteFastEdge(e);
                foreach (var e in v.FastInEdges)
                    this.DeleteFastEdge(e);
                this.DeleteFastNode(g.DotRoot, v);
                g.RankLeaders[r] = null;
            }
        }

        /* delete virtual nodes of a cluster, and install real nodes or sub-clusters */
        private void ExpandCluster(GraphData subg)
        {
            /* build internal structure of the cluster */
            this.ClassifyForPositioning(subg);
            subg.Components.Clear();
            subg.Components.Add(subg.NList);
            this.AllocateRanks(subg);
            this.BuildRanks(subg, 0);
            this.MergeRanks(subg);

            /* build external structure of the cluster */
            this.InterCluster(subg);
            this.RemoveRankLeaders(subg);
        }

        private void BuildSkeleton(GraphData g, GraphData subg)
        {
            VertexData prev = null;

            subg.RankLeaders = new List<VertexData>(); 
            for (int r = subg.MinRank; r <= subg.MaxRank; r++)
            {
                var v =subg.RankLeaders[r] = CreateVirtualNode(g);
                v.Rank = r;
                v.RankType = RankTypes.Cluster;
                v.ClusterGraph = subg;
                if (prev!=null)
                {
                    var e = AttachNewVirtualEdge(prev, v, null);
                    e.Penalty *= CL_CROSS;
                }
                prev = v;
            }

            /* set the counts on virtual edges of the cluster skeleton */
            foreach(var v in subg.TopVertexDatas)
            {
               var  rl = subg.RankLeaders[v.Rank];
                rl.UF_Size++;
                foreach(var e in v.FastOutEdges)
                {
                    for (int r = e.Tail.Rank; r < e.Head.Rank; r++)
                    {
                        rl.FastOutEdges.FirstOrDefault().Count++;
                    }
                }
            }
            for (int r = subg.MinRank; r <=subg.MaxRank; r++)
            {
                var rl = subg.RankLeaders[r];
                if (rl.UF_Size > 1)
                    rl.UF_Size--;
            }
        }

        private void InstallCluster(GraphData g, VertexData n, int pass, Queue<VertexData> q)
        {
            GraphData clust = n.ClusterGraph;

            if (clust.Installed != pass + 1)
            {
                for (int r = clust.MinRank; r <= clust.MaxRank; r++)
                    InstallInRanks(g, clust.RankLeaders[r]);
                for (int r = clust.MinRank; r <= clust.MaxRank; r++)
                    EnqueueNeighbors(q,clust.RankLeaders[r], pass);
               clust.Installed = pass + 1;
            }
        }
        #endregion
        #region Class2
        private void IncreaseWidth(GraphData g, VertexData v)
        {
            int width = g.NodeSep / 2;
            v.lw += width;
            v.rw += width;
        }

        private VertexData PlainVNode(GraphData g, EdgeData orig)
        {
            var v = CreateVirtualNode(g);
            this.IncreaseWidth(g, v);
            return v;
        }

        private VertexData GetLeaderOf(GraphData g, VertexData v)
        {
            return (v.RankType !=  RankTypes.Cluster)
                /*assert(v == UF_find(v));  could be leaf, so comment out */
                ? UF_find(v)
                : v.ClusterGraph.RankLeaders[v.Rank];

        }

        /* make_chain:
         * Create chain of dummy nodes for edge orig.
         */
        private void MakeChain(GraphData g, VertexData from, VertexData to, EdgeData orig)
        {
            VertexData u = from, v = null;

            for (int r = from.Rank + 1; r <= to.Rank; r++)
            {
                if (r < to.Rank)
                {
                    v = PlainVNode(g, orig);
                    v.Rank = r;
                }
                else
                    v = to;
                var e = AttachNewVirtualEdge(u, v, orig);
                VirtualWeight(e);
                u = v;
            }
        }

        private void InterClusterRep(GraphData g, EdgeData e)
        {
            var t = GetLeaderOf(g, e.Tail);
            var h = GetLeaderOf(g, e.Head);
            if (t.Rank > h.Rank)
            {
                VertexData t0 = t;
                t = h;
                h = t0;
            }
            if (t.ClusterGraph != h.ClusterGraph)
            {
                var ve = this.FindFastEdge(t, h);
                if (ve != null)
                {
                    this.MergeChain(g, e, ve, true);
                    return;
                }
                if (t.Rank == h.Rank)
                    return;
                this.MakeChain(g, t, h, e);

                /* mark as cluster edge */
                for (ve = e.Virtual; ve != null && (ve.Head.Rank <= h.Rank););

                ve = ve.Head.FastOutEdges.FirstOrDefault();
                ve.EdgeType = EdgeTypes.ClusterEdge;
            }
            /* else ignore intra-cluster edges at this point */
        }

        private bool IsClusterEdge(EdgeData e)
            => e.Tail.RankType == RankTypes.Cluster
                || e.Head.RankType == RankTypes.Cluster;

        private void MergeChain(GraphData g, EdgeData e, EdgeData f, bool flag)
        {
            EdgeData rep;
            int lastrank = Math.Max(e.Tail.Rank,e.Head.Rank);

            e.Virtual = f;
            rep = f;
            do
            {
                /* interclust multi-edges are not counted now */
                if (flag)
                    rep.Count += e.Count;
                rep.Penalty += e.Penalty;
                rep.Weight += e.Weight;
                
                if (rep.Head.Rank == lastrank)
                    break;
                IncreaseWidth(g, rep.Head);
                rep = rep.Head.FastOutEdges.FirstOrDefault();
            } while (rep!=null);
        }

        private bool Mergeable(EdgeData e, EdgeData f)
        {
            if (e!=null && f!=null && (e.Tail == f.Tail) && (e.Head == f.Head) &&
                /*(e.Label == f.Label) &&*/  PortsEquals(e, f))
                return true;
            return false;
        }

        private EdgeData FindTopEdge(GraphData g,VertexData h,VertexData t)
        {
            foreach(var e in g.TopEdgeDatas)
            {
                if(e.Head == h && e.Tail == t)
                {
                    return e;
                }
            }
            
            return null;
        }
        private void MarkClusters(GraphData g)
        {
            /* remove sub-clusters below this level */
            foreach(var n in g.TopVertexDatas)
            {
                if (n.RankType == RankTypes.Cluster)
                {
                    UF_Singleton(n);
                }
                n.ClusterGraph = null;
            }

            foreach(var c in g.Clusters)
            {
                foreach(var n in c.TopVertexDatas.ToList())
                {
                    if (n.RankType!= RankTypes.Normal)
                    {
                        c.TopVertexDatas.Remove(n);
                        continue;
                    }
                    UF_setname(n,c.Leader);
                    n.ClusterGraph = c;
                    n.RankType = RankTypes.Cluster;

                    /* here we mark the vnodes of edges in the cluster */
                    foreach(var orig in c.TopEdgeDatas)
                    {
                        EdgeData e = null;
                        if ((e = orig.Virtual)!=null)
                        {
                            VertexData vn = null;
                            while (e!=null && (vn = e.Head).NodeType ==  NodeTypes.Virtual)
                            {
                                vn.ClusterGraph = c;
                                e = e.Head.FastOutEdges.FirstOrDefault();
                                /* trouble if concentrators and clusters are mixed */
                            }
                        }
                    }
                }
            }
        }
        private void ClassifyForPositioning(GraphData g)
        {
            VertexData t, h;
            EdgeData prev, opp;
            g.NList.Clear();

            this.MarkClusters(g);

            foreach(var cluster in g.Clusters)
                this.BuildSkeleton(g, cluster);
            foreach (var n in g.TopVertexDatas)
            {
                foreach (var e in n.TopOutEdges)
                {
                    if ((e.Head.WeightClass) <= 2)
                        (e.Head.WeightClass)++;
                    if ((e.Tail.WeightClass) <= 2)
                        (e.Tail.WeightClass)++;
                }
            }
            foreach(var n in g.TopVertexDatas)
            {
                if ((n.ClusterGraph == null) && (n == UF_find(n)))
                {
                    this.FastNode(g, n);
                }
                prev = null;
                foreach(var e in n.TopOutEdges)
                {
                    /* already processed */
                    if (e.Virtual!=null)
                    {
                        prev = e;
                        continue;
                    }

                    /* edges involving sub-clusters of g */
                    if (this.IsClusterEdge(e))
                    {
                        /* following is new cluster multi-edge code */
                        if (this.Mergeable(prev, e))
                        {
                            if (prev.Virtual!=null)
                            {
                                this.MergeChain(g, e, prev.Virtual, false);
                                this.OtherEdge(e);
                            }
                            else if (e.Tail.Rank ==e.Head.Rank)
                            {
                                this.MergeOneWay(e, prev);
                                this.OtherEdge(e);
                            }
                            /* else is an intra-cluster edge */
                            continue;
                        }
                        this.InterClusterRep(g, e);
                        prev = e;
                        continue;
                    }
                    /* merge multi-edges */
                    if (prev!=null && (e.Tail == prev.Tail) && (e.Head == prev.Head))
                    {
                        if ((e.Tail.Rank) == (e.Head.Rank))
                        {
                            this.MergeOneWay(e, prev);
                            this.OtherEdge(e);
                            continue;
                        }
                        if (this.PortsEquals(e, prev))
                        {
                            this.MergeChain(g, e, (prev.Virtual), true);
                            this.OtherEdge(e);

                            continue;
                        }
                        /* parallel edges with different labels fall through here */
                    }

                    /* self edges */
                    if (e.Tail == e.Head)
                    {
                        this.OtherEdge(e);
                        prev = e;
                        continue;
                    }

                    t = UF_find(e.Tail);
                    h = UF_find(e.Head);

                    /* non-leader leaf nodes */
                    if ((e.Tail != t) || (e.Head != h))
                    {
                        /* FIX need to merge stuff */
                        continue;
                    }
                    /* flat edges */
                    if (e.Tail.Rank == e.Head.Rank)
                    {
                        this.FlatEdge(g, e);
                        prev = e;
                        continue;
                    }

                    /* forward edges */
                    if (e.Head.Rank > e.Tail.Rank)
                    {
                        this.MakeChain(g, e.Tail, e.Head, e);
                        prev = e;
                        continue;
                    }

                    /* backward edges */
                    else
                    {
                        /*other_edge(e); */
                        /* avoid when opp==e in undirected graph */
                        if ((opp = this.FindTopEdge(g, e.Head,e.Tail))!=null && (opp.Head!= e.Head))
                        {
                            /* shadows a forward edge */
                            if (opp.Virtual == null)
                                MakeChain(g, opp.Tail, opp.Head, opp);
                            if (PortsEquals(e, opp))
                            {
                                /* see above.  this is getting out of hand */
                                OtherEdge(e);
                                MergeChain(g, e, opp.Virtual, true);
                                continue;
                            }
                        }
                        this.MakeChain(g, e.Head, e.Tail, e);
                        prev = e;
                    }
                }
            }
            /* since decompose() is not called on subgraphs */
            if (g != g.DotRoot)
            {
                g.Components.Clear();
                g.Components.Add(g.NList);
            }
        }
        #endregion
        #region FlatEdge

        private void FindLR(VertexData u, VertexData v, ref int lp, ref int rp)
        {
            var l = u.Order;
            var r = v.Order;
            if (l > r)
            {
                int t = l;
                l = r;
                r = t;
            }
            lp = l;
            rp = r;
        }

        private void SetBounds(VertexData v, int[] bounds, int lpos, int rpos)
        {
            int l = 0, r = 0, ord;
            //bool not_interested = false;

            if (v.NodeType ==  NodeTypes.Virtual)
            {
                ord = v.Order;
                if (v.FastInEdges.Count == 0)
                {   /* flat */
                    FindLR(v.FastOutEdges[0].Head, v.FastOutEdges[1].Head, ref l, ref r);
                    /* the other flat edge could be to the left or right */
                    if (r <= lpos)
                        bounds[SLB] = bounds[HLB] = ord;
                    else if (l >= rpos)
                        bounds[SRB] = bounds[HRB] = ord;
                    /* could be spanning this one */
                    else if ((l < lpos) && (r > rpos))
                    {
                        //not_interested = true;    /* ignore */
                    }
                    /* must have intersecting ranges */
                    else
                    {
                        if ((l < lpos) || ((l == lpos) && (r < rpos)))
                            bounds[SLB] = ord;
                        if ((r > rpos) || ((r == rpos) && (l > lpos)))
                            bounds[SRB] = ord;
                    }
                }
                else
                {       /* forward */
                    bool onleft, onright;
                    onleft = onright = false;
                    foreach(var f in v.FastOutEdges)
                    {
                        if (f.Head.Order <= lpos)
                        {
                            onleft = true;
                            continue;
                        }
                        if (f.Head.Order >= rpos)
                        {
                            onright = true;
                            continue;
                        }
                    }
                    if (onleft && (onright == false))
                        bounds[HLB] = ord + 1;
                    if (onright && (onleft == false))
                        bounds[HRB] = ord - 1;
                }
            }
        }

        /* checkFlatAdjacent:
         * Check if tn and hn are adjacent.
         * If so, set adjacent bit on all related edges.
         * Assume e is flat.
         */
        private void CheckFlatAdjacent(EdgeData e)
        {
            var tn = e.Tail;
            var hn = e.Head;
            int i, lo, hi;

            if (tn.Order<hn.Order)
            {
                lo = tn.Order;
                hi = hn.Order;
            }
            else
            {
                lo = hn.Order;
                hi = tn.Order;
            }
            if (tn.Root == null)
            {
                //NOTICE: this is a patch
            }
            else
            {
                var rank = tn.Root.Ranks[tn.Rank];
                for (i = lo + 1; i < hi; i++)
                {
                    var n = rank.v[i];
                    if ((n.NodeType == NodeTypes.Virtual) ||
                        n.NodeType == NodeTypes.Normal)
                        break;
                }
                if (i == hi)
                {  /* adjacent edge */
                    do
                    {
                        e.Adjacent = true;
                        e = e.Virtual;
                    } while (e != null);
                }
            }
        }

        /* flat_edges:
         * Process flat edges.
         * First, mark flat edges as having adjacent endpoints or not.
         *
         * Second, if there are edge labels, nodes are placed on ranks 0,2,4,...
         * If we have a labeled flat edge on rank 0, add a rank -1.
         *
         * Finally, create label information. Add a virtual label node in the
         * previous rank for each labeled, non-adjacent flat edge. If this is
         * done for any edge, return true, so that main code will reset y coords.
         * For labeled adjacent flat edges, store label width in representative edge.
         * FIX: We should take into account any extra height needed for the latter
         * labels.
         *
         * We leave equivalent flat edges in ND_other. Their ED_virt field should
         * still point to the class representative.
         */
        private bool FlatEdges(GraphData g)
        {
            bool reset = false;

            foreach(var n in g.NList)
            {
                if (n.FlatOutEdges.Count>0)
                {
                    foreach(var e in n.FlatOutEdges)
                    {
                        CheckFlatAdjacent(e);
                    }
                }
                foreach(var e in n.OtherEdges)
                {
                    if ((e.Head.Rank) == (e.Tail.Rank))
                        CheckFlatAdjacent(e);
                }
            }

            RecallSaveVLists(g);
            foreach(var n in g.NList)
            {
                /* if n is the tail of any flat edge, one will be in flat_out */
                if (n.FlatOutEdges.Count>0)
                {
                    /* look for other flat edges with labels */
                    foreach(var e in n.OtherEdges)
                    {
                        if ((e.Tail.Rank) != (e.Head.Rank)) continue;
                        if (e.Tail == e.Head) continue; /* skip loops */
                        var le = e;
                        while ((le.Virtual!=null)) le = (le.Virtual);
                        e.Adjacent = le.Adjacent;
                    }
                }
            }
            //NOTICE:always assume there is no edge label!
            reset = false;
            if (reset)
            {
                //CheckLabelOrder(g);
                //RecallResetVLists(g);
            }
            return reset;
        }
        #endregion
        #region FastEdges
        private EdgeData FindFlatEdge(VertexData u, VertexData v)
        {
            return this.FFE(u,u.FlatOutEdges, v, v.FlatInEdges);
        }
        /* safe_list_append - append e to list L only if e not already a member */
        private void SafeListAppend(EdgeData e,List<EdgeData> L)
        {
            if (!L.Contains(e))
            {
                L.Add(e);
            }
        }
        /* disconnects e from graph */
        private void DeleteFastEdge(EdgeData e)
        {
            e.Tail.FastOutEdges.Remove(e);
            e.Head.FastInEdges.Remove(e);
        }
        private void SafeDeleteFastEdge(EdgeData e)
        {
            if (e.Tail.FastOutEdges.Contains(e))
            {
                e.Tail.FastOutEdges.Remove(e);
            }
            if (e.Head.FastInEdges.Contains(e))
            {
                e.Head.FastInEdges.Remove(e);
            }
        }
        private void OtherEdge(EdgeData e)
        {
            e.Tail.OtherEdges.Add(e);
        }
        private void SafeOtherEdge(EdgeData e)
        {
            SafeListAppend(e, e.Tail.OtherEdges);
        }
        private void FastNodeAppend(GraphData g, VertexData u, VertexData v)
        {
            //NOTICE: u and v are linked in dot
            int i = g.NList.IndexOf(u);
            if (i >= 0)
            {
                g.NList.Insert(i + 1, v);
            }
        }
        private void DeleteFastNode(GraphData g, VertexData n)
        {
            g.NList.Remove(n);
        }
        private void FlatEdge(GraphData g, EdgeData e)
        {
            e.Tail.FlatOutEdges.Add(e);
            e.Head.FlatInEdges.Add(e);
            g.DotRoot.HasFlatEdge = g.HasFlatEdge = true;
        }
        private void DeleteFlatEdge(EdgeData e)
        {
            if(e.Original!=null && e.Original.Virtual == e)
            {
                e.Original.Virtual = null;
            }
            e.Tail.FlatOutEdges.Remove(e);
            e.Head.FlatInEdges.Remove(e);
        }
        private void BasicMerge(EdgeData e, EdgeData rep)
        {
            if (rep.MinLength < e.MinLength)
                rep.MinLength = e.MinLength;
            while (rep!=null)
            {
                rep.Count += e.Count;
                rep.Penalty += e.Penalty;
                rep.Weight += e.Weight;
                rep = rep.Virtual;
            }
        }
        private void MergeOneWay(EdgeData e, EdgeData rep)
        {
            if (rep == e.Virtual)
                return;
            e.Virtual = rep;
                this.BasicMerge(e, rep);            
        }
        private void Unrep(EdgeData rep, EdgeData e)
        {
            rep.Count -= e.Count;
            rep.Penalty -= e.Penalty;
            rep.Weight -= e.Weight;
        }
        private void UnmergeOneWay(EdgeData e)
        {
            EdgeData rep, nextrep;
            for (rep = e.Virtual; rep!=null; rep = nextrep)
            {
                Unrep(rep, e);
                nextrep = rep.Virtual;;
                if (rep.Count == 0)
                    SafeDeleteFastEdge(rep); /* free(rep)? */

                /* unmerge from a virtual edge chain */
                while ((rep.EdgeType ==  EdgeTypes.Virtual)
                    && (rep.Head.NodeType ==  NodeTypes.Virtual)
                    && (rep.Head.FastOutEdges.Count== 1))
                {
                    rep = rep.Head.FastOutEdges.FirstOrDefault();
                    Unrep(rep, e);
                }
            }
            e.Virtual = null;
        }
        #endregion
        private const int POINTS_PER_INCH = 72;

        private double POINTS(double a_inches) => (Math.Round((a_inches) * POINTS_PER_INCH));
        private double INCH2PS(double a_inches) => ((a_inches) * (double)POINTS_PER_INCH);
        private double PS2INCH(double a_points) => ((a_points) / (double)POINTS_PER_INCH);

        private void SetNodeSize(VertexData n, bool flip)
        {
            double w = 0.0;

            if (flip)
            {
                w = INCH2PS(n.height);
                n.lw = n.rw = w / 2;
                n.ht = INCH2PS(n.width);
            }
            else
            {
                w = INCH2PS(n.width);
                n.lw = n.rw = w / 2;
                n.ht = INCH2PS(n.height);
            }
        }
    }
}
