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
        private const int BOTTOM_IX = 0;
        private const int RIGHT_IX = 1;
        private const int TOP_IX = 2;
        private const int LEFT_IX = 3;

        private double LargeMinLength => (double)ushort.MaxValue;

        /* connectGraph:
         * When source and/or sink nodes are defined, it is possible that
         * after the auxiliary edges are added, the graph may still have 2 or
         * 3 components. To fix this, we put trivial constraints connecting the
         * first items of each rank.
         */
        private void ConnectGraph(GraphData g)
        {
            bool found = false;
            VertexData tp = null;
            VertexData hp = null;
            VertexData sn = null;

            for (int r = g.MinRank; r <=g.MaxRank; r++)
            {
                var rp =  r;
                found = false;
                for (int i = 0; i < g.Ranks[rp].v.Count; i++)
                {
                    tp = g.Ranks[rp].v[i];
                    if (tp.SaveOutEdges.Count>0)
                    {
                        foreach(var e in tp.SaveOutEdges)
                        {
                            if (e.Head.Rank > r || e.Tail.Rank > r)
                            {
                                found = true;
                                break;
                            }
                        }
                        if (found) break;
                    }
                    if (tp.SaveInEdges.Count>0)
                    {
                        foreach(var e in tp.SaveInEdges)
                        {
                            if (e.Tail.Rank>r || e.Head.Rank>r)
                            {
                                found = true;
                                break;
                            }
                        }
                        if (found) break;
                    }
                }
                if (found || tp == null) continue;

                tp = g.Ranks[rp].v[0];

                if (r <g.MaxRank)
                    hp = g.Ranks[rp + 1].v[0] ;
                else
                    hp = g.Ranks[rp - 1].v[0];

                sn = CreateVirtualNode(g);
                sn.NodeType = NodeTypes.Slack;
                MakeAuxEdge(sn, tp, 0, 0);
                MakeAuxEdge(sn, hp, 0, 0);
                sn.Rank = Math.Min(tp.Rank, hp.Rank);
            }
        }

        private void SetPosition(GraphData g)
        {
            if (g.NList.Count ==0)
                return;         /* ignore empty graph */
            this.MarkLowClusters(g);    /* we could remove from splines.c now */

            this.SetY(g);

            this.ExpandLeaves(g);
            if (this.FlatEdges(g))
                this.SetY(g);
            this.CreateAuxEdges(g);

            this.LastVirtualNodeIndex = 0;
            if (this.DoRank(g, g.NList, BalanceMode.LeftRight, this.Parameters.MaxIterators)!= RankResult.OK)
            {
                this.ConnectGraph(g);
                this.DoRank(g, g.NList, BalanceMode.LeftRight, this.Parameters.MaxIterators);
            }
            this.SetX(g);
            this.RemoveAuxEdges(g);    /* must come after set_aspect since we now
				                         * use GD_ln and GD_rn for bbox width.
				                         */
            this.RotateCoordinates(g);
            this.TranslateCoordinates(g);
        }

        private void RotateCoordinates(GraphData g)
        {
            var rank = g.Ranks;

            for (int i = g.MinRank; i <= g.MaxRank; i++)
            {
                for (int j = 0; j < rank[i].v.Count; j++)
                {
                    var v = rank[i].v[j];
                    if (v != null)
                    {
                        v.Location = this.RotatePoint(v.Location, this.Parameters.Direction);
                    }
                }
            }
        }
        private void TranslateCoordinates(GraphData g)
        {
            double mx = double.MaxValue;
            double my = double.MaxValue;
            var rank = g.Ranks;

            for (int i = g.MinRank; i <= g.MaxRank; i++)
            {
                for (int j = 0; j < rank[i].v.Count; j++)
                {
                    var v = rank[i].v[j];
                    if (v.Location.X < mx)
                    {
                        mx = v.Location.X;
                    }
                    if (v.Location.Y < my)
                    {
                        my = v.Location.Y;
                    }
                }
            }
            for (int i = g.MinRank; i <= g.MaxRank; i++)
            {
                for (int j = 0; j < rank[i].v.Count; j++)
                {
                    var v = rank[i].v[j];
                    v.Location.X -= mx;
                    v.Location.Y -= my;
                }
            }
        }
        private bool Go(VertexData u, VertexData v)
        {
            if (u == v)
                return true;
            foreach(var e in u.RealOutEdges)
            {
                if (Go(e.Head, v))
                    return true;
            }
            return false;
        }

        private bool CanSearch(VertexData u, VertexData v)
        {
            return Go(u, v);
        }

        private EdgeData MakeAuxEdge(VertexData tail, VertexData head, double len, int wt)
        {
            var e = new EdgeData()
            {
                Tail = tail,
                Head = head,
                IsAux = true,
                MinLength = (int)Math.Round(len > LargeMinLength ? LargeMinLength : len),
                Weight = wt,
            };

            return this.FastEdge(e);
        }

        private void AllocateAuxEdges(GraphData g)
        {
            /* allocate space for aux edge lists */
            foreach(var n in g.NList)
            {
                n.SaveInEdges.Clear();
                n.SaveOutEdges.Clear();
                n.SaveInEdges.AddRange(n.FastInEdges);
                n.SaveOutEdges.AddRange(n.FastOutEdges);
                n.FastInEdges.Clear();
                n.FastOutEdges.Clear();
            }
        }

        private int SelfRightSpace(EdgeData e)
        {
            //TODO:
            return 8;
        }
        /* make_LR_constraints:
         */
        private void MakeLRConstrains(GraphData g)
        {
            int sw;         /* self width */
            int m0, m1;
            double width;
            int[] sep=new int[2];
            int nodesep;      /* separation between nodes on same rank */
            EdgeData e, e0, e1, ff;
            VertexData u, v, t0, h0;
            var rank = g.Ranks;

            /* Use smaller separation on odd ranks if g has edge labels */
            //if (false)
            //{
            //    sep[0] = g.NodeSep;
            //    sep[1] = 5;
            //}
            //else
            {
                sep[1] = sep[0] = g.NodeSep;
            }
            /* make edges to constrain left-to-right ordering */
            for (int i = g.MinRank; i <=g.MaxRank; i++)
            {
                double last = (rank[i].v[0].Rank) = 0;
                nodesep = sep[i & 1];
                for (int j = 0; j < rank[i].v.Count; j++)
                {
                    u = rank[i].v[j];
                    u.Mval = u.rw;  /* keep it somewhere safe */
                    if (u.OtherEdges.Count> 0)
                    {   /* compute self size */
                        /* FIX: dot assumes all self-edges go to the right. This
                                 * is no longer true, though makeSelfEdge still attempts to
                                 * put as many as reasonable on the right. The dot code
                                 * should be modified to allow a box reflecting the placement
                                 * of all self-edges, and use that to reposition the nodes.
                                 * Note that this would not only affect left and right
                                 * positioning but may also affect interrank spacing.
                                 */
                        sw = 0;
                        foreach(var ex in u.OtherEdges)
                        {
                            if (ex.Tail == ex.Head)
                            {
                                sw += SelfRightSpace(ex);
                            }
                        }
                        u.rw += sw; /* increment to include self edges */
                    }
                    v = j+1< rank[i].v.Count ? rank[i].v[j + 1] : null;
                    if (v!=null)
                    {
                        width =u.rw + u.lw + nodesep;
                        e0 = MakeAuxEdge(u, v, width, 0);
                        last = (v.Rank =(int)( last + width));
                    }

                    /* constraints from labels of flat edges on previous rank */
                    if ((e = (EdgeData)u.Alg)!=null)
                    {
                        e0 =u.SaveOutEdges[0];
                        e1 =u.SaveOutEdges[1];
                        if (e0.Head.Order > e1.Head.Order)
                        {
                            ff = e0;
                            e0 = e1;
                            e1 = ff;
                        }
                        m0 = (e.MinLength * g.NodeSep) / 2;
                        m1 = (int)(m0 + e0.Head.rw +e0.Tail.lw);
                        /* these guards are needed because the flat edges
                         * work very poorly with cluster layout */
                        if (CanSearch(e0.Tail, e0.Head) == false)
                            MakeAuxEdge(e0.Head,e0.Tail, m1,
                                e.Weight);
                        m1 =(int)( m0 +e1.Tail.rw + e1.Head.lw);
                        if (CanSearch(e1.Head, e1.Tail) == false)
                            MakeAuxEdge(e1.Tail, e1.Head, m1,
                                e.Weight);
                    }

                    /* position flat edge endpoints */
                    foreach(var fe in u.FlatOutEdges)
                    {
                        if (fe.Tail.Order < fe.Head.Order)
                        {
                            t0 = fe.Tail;
                            h0 = fe.Head;
                        }
                        else
                        {
                            t0 = fe.Head;
                            h0 = fe.Tail;
                        }

                        width = (t0.rw) + (h0.rw);
                        m0 =(int)( (fe.MinLength) * (g.NodeSep) + width);

                        if ((e0 = this.FindFastEdge(t0, h0))!=null)
                        {
                            /* flat edge between adjacent neighbors
                                     * ED_dist contains the largest label width.
                                     */
                            m0 =(int) Math.Max(m0, width + g.NodeSep + Math.Round(fe.Dist));
                            if (m0 > LargeMinLength)
                                m0 = (int)LargeMinLength;
                            (e0.MinLength) = Math.Max(e0.MinLength, m0);
                            (e0.Weight) = Math.Max(e0.Weight, fe.Weight);
                        }

                        /* labeled flat edges between non-neighbors have already
                                 * been constrained by the label above.
                                 */
                    }
                }
            }
        }

        /* make_edge_pairs: make virtual edge pairs corresponding to input edges */
        private void MakeEdgePairs(GraphData g)
        {
            for(int i = 0;i<g.NList.Count;i++)
            {
                var n = g.NList[i];
                if (n.SaveOutEdges.Count > 0)
                {
                    foreach (var e in n.SaveOutEdges)
                    {
                        var sn = this.CreateVirtualNode(g);
                        sn.NodeType = NodeTypes.Slack;
                        int m0 = (int)(e.HeadIndex - e.TailIndex), m1 = 0;
                        if (m0 > 0)
                            m1 = 0;
                        else
                        {
                            m1 = -m0;
                            m0 = 0;
                        }

                        MakeAuxEdge(sn, e.Tail, m0 + 1, e.Weight);
                        MakeAuxEdge(sn, e.Head, m1 + 1, e.Weight);
                        sn.Rank =
                             Math.Min(e.Tail.Rank - m0 - 1,
                                 e.Head.Rank - m1 - 1);
                    }
                }
            }
        }

        private void ContainClusterNodes(GraphData g)
        {
            int c;
            EdgeData e;

            if (g != g.DotRoot)
            {
                ContainNodes(g);
                if ((e = this.FindFastEdge(g.ln, g.rn))!=null)   /* maybe from lrvn()?*/
                    e.Weight += 128;
                else
                    MakeAuxEdge(g.ln, g.rn, 1, 128);  /* clust compaction edge */
            }
            for (c = 1; c <= g.Clusters.Count; c++)
                ContainClusterNodes(g.Clusters[c]);
        }

        private bool VNodeNotRelatedTo(GraphData g, VertexData v)
        {
            EdgeData e = null;

            if (v.NodeType !=  NodeTypes.Virtual)
                return false;

            for (e = v.SaveOutEdges[0]; e.Original!=null; e = e.Original) ;

            if (g.TopVertexDatas.Contains(e.Tail))
                return false;
            if (g.TopVertexDatas.Contains(e.Head))
                return false;
            return true;
        }

        /* keepout_othernodes:
         * Guarantee nodes outside the cluster g are placed outside of it.
         * This is done by adding constraints to make sure such nodes have
         * a gap of margin from the left or right bounding box node ln or rn.
         *
         * We could probably reduce some of these constraints by checking if
         * the node is in a cluster, since elsewhere we make constrain a
         * separate between clusters. Also, we should be able to skip the
         * first loop if g is the root graph.
         */
        private void KeepOutOtherNodes(GraphData g)
        {
            int i, c, r, margin;
            VertexData u, v;

            margin = CL_OFFSET;
            for (r = g.MinRank; r <=g.MaxRank; r++)
            {
                if (g.Ranks[r].v.Count == 0)
                    continue;
                v = g.Ranks[r].v[0];
                if (v == null)
                    continue;
                for (i = v.Order - 1; i >= 0; i--)
                {
                    u = g.DotRoot.Ranks[r].v[i];
                    /* can't use "is_a_vnode_of" because elists are swapped */
                    if ((u.NodeType == NodeTypes.Normal) || VNodeNotRelatedTo(g, u))
                    {
                        MakeAuxEdge(u, g.ln, margin + u.rw, 0);
                        break;
                    }
                }
                for (i = v.Order + g.Ranks[r].v.Count; i < g.DotRoot.Ranks[r].v.Count;
                    i++)
                {
                    u = g.DotRoot.Ranks[r].v[i];
                    if ((u.NodeType == NodeTypes.Normal) || VNodeNotRelatedTo(g, u))
                    {
                        MakeAuxEdge(g.rn, u, margin + u.lw, 0);
                        break;
                    }
                }
            }

            for (c = 1; c <=g.Clusters.Count; c++)
                KeepOutOtherNodes(g.Clusters[c]);
        }

        /* contain_subclust:
         * Make sure boxes of subclusters of g are offset from the
         * box of g. This is done by a constraint between the left and
         * right bounding box nodes ln and rn of g and a subcluster.
         * The gap needs to include any left or right labels.
         */
        private void ContainSubCluster(GraphData g)
        {
            int margin, c;
            GraphData subg;

            margin = CL_OFFSET;
            MakeLRVN(g);
            for (c = 1; c <= g.Clusters.Count; c++)
            {
                subg = g.Clusters[c];
                MakeLRVN(subg);
                MakeAuxEdge(g.ln, subg.ln,
                    margin + g.Borders[LEFT_IX].X, 0);
                MakeAuxEdge(subg.rn, g.rn,
                    margin + g.Borders[RIGHT_IX].X, 0);
                ContainSubCluster(subg);
            }
        }

        /* separate_subclust:
         * Guarantee space between subcluster of g.
         * This is done by adding a constraint between the right bbox node rn
         * of the left cluster and the left bbox node ln of the right cluster.
         * This is only done if the two clusters overlap in some rank.
         */
        private void SeparateSubCluster(GraphData g)
        {
            int i, j, margin;
            GraphData low, high;
            GraphData left, right;

            margin = CL_OFFSET; ;
            for (i = 1; i <= g.Clusters.Count; i++)
                MakeLRVN(g.Clusters[i]);
            for (i = 1; i <= g.Clusters.Count; i++)
            {
                for (j = i + 1; j <= g.Clusters.Count; j++)
                {
                    low = g.Clusters[i];
                    high = g.Clusters[j];
                    if (low.MinRank > high.MinRank)
                    {
                        GraphData temp = low;
                        low = high;
                        high = temp;
                    }
                    if (low.MaxRank < high.MaxRank)
                        continue;
                    if ((low.Ranks[high.MinRank].v[0].Order)
                        < (high.Ranks[high.MinRank].v[0].Order))
                    {
                        left = low;
                        right = high;
                    }
                    else
                    {
                        left = high;
                        right = low;
                    }
                    MakeAuxEdge(left.rn,right.ln, margin, 0);
                }
                SeparateSubCluster(g.Clusters[i]);
            }
        }

        /* pos_clusters: create constraints for:
         *	node containment in clusters,
         *	cluster containment in clusters,
         *	separation of sibling clusters.
         */
        private void PosClusters(GraphData g)
        {
            if (g.Clusters.Count > 0)
            {
                ContainClusterNodes(g);
                KeepOutOtherNodes(g);
                ContainSubCluster(g);
                SeparateSubCluster(g);
            }
        }
        private void CreateAuxEdges(GraphData g)
        {
            AllocateAuxEdges(g);
            MakeLRConstrains(g);
            MakeEdgePairs(g);
            PosClusters(g);
        }

        private void RemoveAuxEdges(GraphData g)
        {
            foreach(var n in g.NList)
            {
                n.FastOutEdges.Clear();
                n.FastInEdges.Clear();

                n.FastOutEdges.AddRange(n.SaveOutEdges);
                n.FastInEdges.AddRange(n.SaveInEdges);
            }
            /* cannot be merged with previous loop */
            g.NList.RemoveAll(n => n.NodeType == NodeTypes.Slack);
        }

        /* set_xcoords:
         * Set x coords of nodes.
         */
        private void SetX(GraphData g)
        {
            var rank = g.Ranks;

            for (int i = g.MinRank; i <=g.MaxRank; i++)
            {
                for (int j = 0; j < rank[i].v.Count; j++)
                {
                    var v = rank[i].v[j];
                    v.Location.X = v.Rank;
                    v.Rank = i;
                }
            }
        }


        /* adjustRanks:
         * Recursively adjust ranks to take into account
         * wide cluster labels when rankdir=LR.
         * We divide the extra space between the top and bottom.
         * Adjust the ht1 and ht2 values in the process.
         */
        private void AdjustRanks(GraphData g, int margin_total)
        {
            int margin;
            double ht1, ht2;

            var rank = g.DotRoot.Ranks;
            if (g == g.DotRoot)
                margin = 0;
            else
                margin = CL_OFFSET;

            ht1 = g.ht1;
            ht2 = g.ht2;

            for (int c = 1; c <= g.Clusters.Count; c++)
            {
                GraphData subg = g.Clusters[c];
                AdjustRanks(subg, margin + margin_total);
                if (subg.MaxRank ==g.MaxRank)
                    ht1 = Math.Max(ht1, subg.ht1 + margin);
                if (subg.MinRank == g.MinRank)
                    ht2 = Math.Max(ht2, subg.ht2 + margin);
            }

            g.ht1 = ht1;
            g.ht2 = ht2;

            /* update the global ranks */
            if (g != g.DotRoot)
            {
                rank[g.MinRank].ht2 = Math.Max(rank[g.MinRank].ht2, g.ht2);
                rank[g.MaxRank].ht1 = Math.Max(rank[g.MaxRank].ht1, g.ht1);
            }
        }

        /* clust_ht:
         * recursively compute cluster ht requirements.  assumes subg.ht1 and ht2
         * are computed from primitive nodes only.  updates ht1 and ht2 to reflect
         * cluster nesting and labels.  also maintains global rank ht1 and ht2.
         * Return true if some cluster has a label.
         */
        private int GetClusterHT(GraphData g)
        {
            int c;
            double ht1, ht2;
            GraphData subg;
            var rank = g.DotRoot.Ranks;
            int margin = CL_OFFSET, haveClustLabel = 0;


            ht1 = g.ht1;
            ht2 = g.ht2;

            /* account for sub-clusters */
            for (c = 1; c <= g.Clusters.Count; c++)
            {
                subg = g.Clusters[c];
                haveClustLabel |= GetClusterHT(subg);
                if ((subg.MaxRank) ==g.MaxRank)
                    ht1 = Math.Max(ht1, subg.ht1 + margin);
                if ((subg.MaxRank) == g.MinRank)
                    ht2 = Math.Max(ht2, subg.ht2 + margin);
            }

            g.ht1 = ht1;
            g.ht2 = ht2;

            /* update the global ranks */
            if (g != g.DotRoot)
            {
                rank[g.MinRank].ht2 = Math.Max(rank[g.MinRank].ht2, ht2);
                rank[g.MaxRank].ht1 = Math.Max(rank[g.MaxRank].ht1, ht1);
            }

            return haveClustLabel;
        }

        /* set y coordinates of nodes, a rank at a time */
        private void SetY(GraphData g)
        {
            double ht2, maxht, delta, d0, d1;
            VertexData n;
            var rank = g.Ranks;
            GraphData clust;
            int lbl,r;

            ht2 = maxht = 0;

            /* scan ranks for tallest nodes.  */
            for (r = g.MinRank; r <=g.MaxRank; r++)
            {
                for (int i = 0; i < rank[r].v.Count; i++)
                {
                    n = rank[r].v[i];

                    /* assumes symmetry, ht1 = ht2 */
                    ht2 = n.ht / 2;

                    /* update global rank ht */
                    if (rank[r].pht2 < ht2)
                        rank[r].pht2 = rank[r].ht2 = ht2;
                    if (rank[r].pht1 < ht2)
                        rank[r].pht1 = rank[r].ht1 = ht2;

                    /* update nearest enclosing cluster rank ht */
                    if ((clust =n.ClusterGraph)!=null)
                    {
                        int yoff = (clust == g ? 0 : CL_OFFSET);
                        if (n.Rank == clust.MinRank)
                            clust.ht2 = Math.Max(clust.ht2, ht2 + yoff);
                        if (n.Rank == clust.MaxRank)
                            clust.ht1 = Math.Max(clust.ht1, ht2 + yoff);
                    }
                }
            }

            /* scan sub-clusters */
            lbl = GetClusterHT(g);

            /* make the initial assignment of ycoords to leftmost nodes by ranks */
            maxht = 0;
            r =g.MaxRank;
            rank[r].v[0].Location.Y = rank[r].ht1;
            while (--r >= g.MinRank)
            {
                d0 = rank[r + 1].pht2 + rank[r].pht1 + g.RankSep;   /* prim node sep */
                d1 = rank[r + 1].ht2 + rank[r].ht1 + CL_OFFSET; /* cluster sep */
                delta = Math.Max(d0, d1);
                if (rank[r].v.Count > 0)  /* this may reflect some problem */
                    rank[r].v[0].Location.Y = rank[r + 1].v[0].Location.Y + delta;
                maxht = Math.Max(maxht, delta);
            }

            /* If there are cluster labels and the drawing is rotated, we need special processing to
             * allocate enough room. We use adjustRanks for this, and then recompute the maxht if
             * the ranks are to be equally spaced. This seems simpler and appears to work better than
             * handling equal spacing as a special case.
             */
            if (lbl!=0 && g.Flip)
            {
                AdjustRanks(g, 0);
                if (g.ExactRankSep)
                {  /* recompute maxht */
                    maxht = 0;
                    r =g.MaxRank;
                    d0 = rank[r].v[0].Location.Y;
                    while (--r >= g.MinRank)
                    {
                        d1 = rank[r].v[0].Location.Y;
                        delta = d1 - d0;
                        maxht = Math.Max(maxht, delta);
                        d0 = d1;
                    }
                }
            }

            /* re-assign if ranks are equally spaced */
            if (g.ExactRankSep)
            {
                for (r =g.MaxRank - 1; r >= g.MinRank; r--)
                    if (rank[r].v.Count > 0)  /* this may reflect the same problem :-() */
                        rank[r].v[0].Location.Y =
                        rank[r + 1].v[0].Location.Y+ maxht;
            }

            /* copy ycoord assignment from leftmost nodes to others */
            foreach (var nx in g.NList)
            {
                nx.Location.Y = rank[nx.Rank].v[0].Location.Y;
            }
        }

        /* dot_compute_bb:
         * Compute bounding box of g.
         * The x limits of clusters are given by the x positions of ln and rn.
         * This information is stored in the rank field, since it was calculated
         * using network simplex.
         * For the root graph, we don't enforce all the constraints on lr and
         * rn, so we traverse the nodes and subclusters.
         */
        private void ComputeBoundingBox(GraphData g, GraphData root)
        {
            int r, c;
            double x, offset;
            VertexData v;
            Point LL = new Point(), UR = new Point();

            if (g == g.DotRoot)
            {
                LL.X = (double)(int.MaxValue);
                UR.X = (double)(-int.MaxValue);
                for (r = g.MinRank; r <=g.MaxRank; r++)
                {
                    int rnkn = g.Ranks[r].v.Count;
                    if (rnkn == 0)
                        continue;
                    if ((v = g.Ranks[r].v[0]) == null)
                        continue;
                    for (c = 1; (v.NodeType !=  NodeTypes.Normal) && c < rnkn; c++)
                        v = g.Ranks[r].v[c];
                    if (v.NodeType ==  NodeTypes.Normal)
                    {
                        x = v.Location.X - v.lw;
                        LL.X = Math.Min(LL.X, x);
                    }
                    else continue;
                    /* At this Point, we know the rank contains a NORMAL node */
                    v = g.Ranks[r].v[rnkn - 1];
                    for (c = rnkn - 2; v.NodeType !=  NodeTypes.Normal; c--)
                        v = g.Ranks[r].v[c];
                    x = v.Location.X + v.rw;
                    UR.X = Math.Max(UR.X, x);
                }
                offset = CL_OFFSET;
                for (c = 1; c <= g.Clusters.Count; c++)
                {
                    x = (double)((g.Clusters[c].bb).LL.X - offset);
                    LL.X = Math.Min(LL.X, x);
                    x = (double)((g.Clusters[c].bb).UR.X + offset);
                    UR.X = Math.Max(UR.X, x);
                }
            }
            else
            {
                LL.X = (double)((g.ln.Rank));
                UR.X = (double)((g.rn.Rank));
            }
            LL.Y = ((root.Ranks)[g.MaxRank].v[0]).Location.Y - g.ht1;
            UR.Y = ((root.Ranks)[g.MinRank].v[0]).Location.Y + g.ht2;
            g.bb.LL = LL;
            g.bb.UR = UR;
        }

        private void RecBoundingBox(GraphData g, GraphData root)
        {
            int c;
            for (c = 1; c <= g.Clusters.Count; c++)
                RecBoundingBox(g.Clusters[c], root);
            ComputeBoundingBox(g, root);
        }

        /* scale_bb:
         * Recursively rescale all bounding boxes using scale factors
         * xf and yf. We assume all the bboxes have been computed.
         */
        private void ScaleBoundingBox(GraphData g, GraphData root, double xf, double yf)
        {
            int c;

            for (c = 1; c <= g.Clusters.Count; c++)
                ScaleBoundingBox(g.Clusters[c], root, xf, yf);
            g.bb.LL.X *= xf;
            g.bb.LL.Y *= yf;
            g.bb.UR.X *= xf;
            g.bb.UR.Y *= yf;
        }

        private Point ResizeLeaf(VertexData leaf, Point lbound)
        {
            SetNodeSize(leaf, leaf.ClusterGraph.Flip);
            leaf.Location.Y = lbound.Y;
            leaf.x = (int)(lbound.X + leaf.lw);
            lbound.X= lbound.Y + leaf.lw + leaf.rw + leaf.ClusterGraph.NodeSep;
            return lbound;
        }

        private Point PlaceLeaf(GraphData ing, VertexData leaf, Point lbound, int order)
        {
            VertexData leader;
            GraphData g = ing.DotRoot;

            leader = UF_find(leaf);
            if (leaf != leader)
                FastNodeAppend(ing,leader, leaf);
            leaf.Order = order;
            leaf.Rank = leader.Rank;
            g.Ranks[leaf.Rank].v[leaf.Order] = leaf;
            return ResizeLeaf(leaf, lbound);
        }

        /* make space for the leaf nodes of each rank */
        private void MakeLeafSlots(GraphData g)
        {
            int j = 0;

            for (int r = g.MinRank; r <=g.MaxRank; r++)
            {
                j = 0;
                for (int i = 0; i < g.Ranks[r].v.Count; i++)
                {
                    var v = g.Ranks[r].v[i];
                    v.Order = j;
                    if (v.RankType == RankTypes.LeafSet)
                        j = j + v.UF_Size;
                    else
                        j++;
                }
                if (j <= g.Ranks[r].v.Count)
                    continue;
                g.Ranks[r].v = new List<VertexData>(new VertexData[j]);
                for (int i = g.Ranks[r].v.Count - 1; i >= 0; i--)
                {
                    var v = g.Ranks[r].v[i];
                    g.Ranks[r].v[v.Order] = v;
                }
            }
        }
        private EdgeData MakeOut(EdgeData edge)
        {
            return edge;
        }
        private void DoLeaves(GraphData g, VertexData leader)
        {
            int j;
            Point lbound = new Point();
            VertexData n;

            if (leader.UF_Size <= 1)
                return;
            lbound.X = leader.Location.X - leader.lw;
            lbound.Y = leader.Location.Y;
            lbound = ResizeLeaf(leader, lbound);
            if (leader.RealOutEdges.Count > 0)
            {   /* in-edge leaves */
                n = leader.RealOutEdges[0].Head;
                j = (leader.Order) + 1;
                foreach(var e in n.RealInEdges)
                {
                    EdgeData e1 = this.MakeOut(e);
                    if ((e1.Tail != leader) && (UF_find(e1.Tail) == leader))
                    {
                        lbound = this.PlaceLeaf(g, e1.Tail, lbound, j++);
                        this.UnmergeOneWay(e1);
                        e1.Head.RealInEdges.Add(e1);
                    }
                }
            }
            else
            {           /* out edge leaves */
                n = leader.RealInEdges[0].Tail;
                j = (leader.Order) + 1;
                foreach(var e in n.RealOutEdges)
                {
                    if ((e.Head != leader) && (UF_find(e.Head) == leader))
                    {
                        lbound = PlaceLeaf(g, e.Head, lbound, j++);
                        UnmergeOneWay(e);
                        e.Tail.RealOutEdges.Add(e);
                    }
                }
            }
        }

        private void ExpandLeaves(GraphData g)
        {
            int d = 0;

            this.MakeLeafSlots(g);
            foreach(var n in g.NList)
            {
                if (n.InLeaf!=null)
                    this.DoLeaves(g, n.InLeaf);
                if (n.OutLeaf!=null)
                    this.DoLeaves(g, n.OutLeaf);
                if (n.OtherEdges.Count>0)
                    for(int i = 0;i<n.OtherEdges.Count;i++)
                    {
                        var e = n.OtherEdges[i];
                        if ((d = (e.Tail.Rank) - (e.Head).Rank) == 0)
                            continue;
                        var f = e.Original;
                        if (f!=null)
                        if (!PortsEquals(e, f))
                        {
                            n.OtherEdges.Add(e);
                            if (d == 1)
                                this.FastEdge(e);
                            /*else unitize(e); ### */

                            i--;
                        }
                    }
            }
        }

        /* make_lrvn:
         * Add left and right slacknodes to a cluster which
         * are used in the LP to constrain nodes not in g but
         * sharing its ranks to be to the left or right of g
         * by a specified amount.
         * The slacknodes ln and rn give the x position of the
         * left and right side of the cluster's bounding box. In
         * particular, any cluster labels on the left or right side
         * are inside.
         * If a cluster has a label, and we have rankdir!=LR, we make
         * sure the cluster is wide enough for the label. Note that
         * if the label is wider than the cluster, the nodes in the
         * cluster may not be centered.
         */
        private void MakeLRVN(GraphData g)
        {
            VertexData ln, rn;

            if (g.ln!=null)
                return;
            ln = CreateVirtualNode(g.DotRoot);
            ln.NodeType = NodeTypes.Slack;
            rn = CreateVirtualNode(g.DotRoot);
            rn.NodeType = NodeTypes.Slack;

            g.ln = ln;
            g.rn = rn;
        }

        /* contain_nodes:
         * make left and right bounding box virtual nodes ln and rn
         * constrain interior nodes
         */
        private void ContainNodes(GraphData g)
        {
            int margin, r;
            VertexData ln, rn, v;

            margin = CL_OFFSET;
            MakeLRVN(g);
            ln = g.ln;
            rn = g.rn;
            for (r = g.MinRank; r <=g.MaxRank; r++)
            {
                if (g.Ranks[r].v.Count == 0)
                    continue;
                v = g.Ranks[r].v[0];
                if (v == null)
                {
                    //agerr(AGERR, "contain_nodes clust %s rank %d missing node\n",
                    //    agnameof(g), r);
                    continue;
                }
                MakeAuxEdge(ln, v,
                    v.rw + margin + g.Borders[LEFT_IX].X, 0);
                v = g.Ranks[r].v[g.Ranks[r].v.Count - 1];
                MakeAuxEdge(v, rn,
                    v.lw + margin + g.Borders[RIGHT_IX].X, 0);
            }
        }

    }
}
