using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using QuickGraph;

namespace GraphSharp.Algorithms.Layout.Compound.Dot
{
    /// <typeparam name="TVertex">The type of the vertices.</typeparam>
    /// <typeparam name="TEdge">The type of the edges.</typeparam>
    /// <typeparam name="TGraph">The type of the graph.</typeparam>
    public partial class DotLayoutAlgorithm<TVertex, TEdge, TGraph>
    {
        private void DoTopBottomBalance(GraphData g,List<VertexData> component)
        {
            int i = 0, low = 0, high = 0, choice = 0;
            int inweight = 0, outweight = 0;

            this.ScanRankAndNormalize(g, component);

            int[] nrank = new int[g.MaxRank + 1];

            component.ForEach(c => {
                if (c.NodeType == NodeTypes.Normal)
                    nrank[c.Rank]++; });

            foreach (var n in component)
            {
                if (n.NodeType != NodeTypes.Normal)
                    continue;
                inweight = outweight = 0;
                low = 0;
                high = g.MaxRank;
                foreach (var e in n.FastInEdges)
                {
                    inweight += e.Weight;
                    low = Math.Max(low, e.Tail.Rank + e.MinLength);
                }
                foreach (var e in n.FastOutEdges)
                {
                    outweight += e.Weight;
                    high = Math.Min(high, e.Head.Rank - e.MinLength);
                }
                if (low < 0)
                    low = 0;        /* vnodes can have ranks < 0 */
                if (inweight == outweight)
                {
                    choice = low;
                    for (i = low + 1; i <= high; i++)
                        if (nrank[i] < nrank[choice])
                            choice = i;
                    nrank[n.Rank]--;
                    nrank[choice]++;
                    n.Rank = choice;
                }
                n.TreeInEdges.Clear();
                n.TreeOutEdges.Clear();
                n.Mark = 0;
            }
        }
        private void DoLeftRightBalance(List<EdgeData> treeEdges)
        {
            int delta = 0;
            foreach (var e in treeEdges)
            {
                if (e.CutValue == 0)
                {
                    var f = this.EnterEdge(e);
                    if (f == null)
                        continue;
                    delta = f.Slack;
                    if (delta <= 1)
                        continue;
                    if (e.Tail.Lim < e.Head.Lim)
                        Rerank(e.Tail, delta / 2);
                    else
                        Rerank(e.Head, -delta / 2);
                }
            }
        }
        private bool InitGraph(List<VertexData> component)
        {
            bool feasible = true;

            component.ForEach(c => c.Mark = 0);

            foreach (var n in component)
            {
                n.Priority = 0;
                foreach (var e in n.FastInEdges)
                {
                    n.Priority++;
                    e.CutValue = 0;
                    e.TreeIndex = -1;
                    if (feasible && (e.Head.Rank - e.Tail.Rank) < e.MinLength)
                    {
                        feasible = false;
                    }
                }
                n.TreeInEdges.Clear();
                n.TreeOutEdges.Clear();
            }

            return feasible;
        }
        private void InitRank(List<VertexData> component)
        {
            var Q = new Queue<VertexData>();

            foreach (var n in component)
            {
                if (n.Priority == 0)
                {
                    Q.Enqueue(n);
                }
            }
            VertexData v = null;
            while (Q.Count > 0)
            {
                v = Q.Dequeue();
                v.Rank = 0;
                foreach (var e in v.FastInEdges)
                {
                    v.Rank = Math.Max(v.Rank, e.Tail.Rank + e.MinLength);
                }
                foreach (var e in v.FastOutEdges)
                {
                    if (--e.Head.Priority <= 0)
                    {
                        Q.Enqueue(e.Head);
                    }
                }
            }
        }
        private void ScanRankAndNormalize(GraphData g,List<VertexData> component)
        {
            g.MinRank = int.MaxValue;
            g.MaxRank = int.MinValue;
            component.ForEach(
                c => { if (c.NodeType == NodeTypes.Normal)
                    { g.MinRank = Math.Min(g.MinRank, c.Rank);
                        g.MaxRank = Math.Max(g.MaxRank, c.Rank); } });

            if (g.MinRank != 0)
            {
                component.ForEach(c => c.Rank -= g.MinRank);
                g.MaxRank -= g.MinRank;
                g.MinRank = 0;
            }
        }
        private void AddTreeEdge(List<VertexData> treeNodes, List<EdgeData> treeEdges, EdgeData e)
        {
            e.TreeIndex = treeEdges.Count;
            treeEdges.Add(e);
            if (e.Tail.Mark == 0) treeNodes.Add(e.Tail);
            if (e.Head.Mark == 0) treeNodes.Add(e.Head);
            e.Tail.Mark = 1;
            e.Tail.TreeOutEdges.Add(e);
            e.Head.Mark = 1;
            e.Head.TreeInEdges.Add(e);
        }
        private void ExchangeTreeEdges(List<EdgeData> treeEdges, EdgeData e, EdgeData f)
        {
            f.TreeIndex = e.TreeIndex;
            treeEdges[f.TreeIndex] = f;
            e.TreeIndex = -1;

            e.Tail.TreeOutEdges.Remove(e);
            e.Head.TreeInEdges.Remove(e);
            f.Tail.TreeOutEdges.Add(f);
            f.Head.TreeInEdges.Add(f);
        }
        private EdgeData LeaveEdge(List<EdgeData> treeEdges, ref int S_i)
        {
            EdgeData f = null, rv = null;
            int j = S_i, cnt = 0;

            while (S_i < treeEdges.Count)
            {
                f = treeEdges[S_i];

                if (f.CutValue < 0)
                {
                    if (rv != null)
                    {
                        if (rv.CutValue > f.CutValue)
                            rv = f;
                    }
                    else
                        rv = treeEdges[S_i];
                    if (++cnt >= this.Parameters.SearchSize)
                        return rv;
                }
                S_i++;
            }
            if (j > 0)
            {
                S_i = 0;
                while (S_i < j)
                {
                    f = treeEdges[S_i];
                    if (f.CutValue < 0)
                    {
                        if (rv != null)
                        {
                            if (rv.CutValue > f.CutValue)
                                rv = f;
                        }
                        else
                            rv = treeEdges[S_i];
                        if (++cnt >= this.Parameters.SearchSize)
                            return rv;
                    }
                    S_i++;
                }
            }
            return rv;
        }
        private void DFSEnterOutEdge(VertexData v, ref EdgeData Enter, ref int Slack, ref int Low, ref int Lim)
        {
            int slack = 0;

            foreach (var e in v.FastOutEdges)
            {
                if (e.TreeIndex < 0)
                {
                    if (!(Low <= e.Head.Lim && e.Head.Lim <= Lim))
                    {
                        slack = e.Slack;
                        if ((slack < Slack) || (Enter == null))
                        {
                            Enter = e;
                            Slack = slack;
                        }
                    }
                }
                else if (e.Head.Lim < v.Lim)
                {
                    this.DFSEnterOutEdge(e.Head, ref Enter, ref Slack, ref Low, ref Lim);
                }
            }


            for (int i = 0; i < v.TreeInEdges.Count && Slack > 0; i++)
            {
                var e = v.TreeInEdges[i];

                if (e.Tail.Lim <= v.Lim)
                {
                    this.DFSEnterOutEdge(e.Tail, ref Enter, ref Slack, ref Low, ref Lim);
                }
            }
        }
        private void DFSEnterInEdge(VertexData v, ref EdgeData Enter, ref int Slack, ref int Low, ref int Lim)
        {
            int slack = 0;

            foreach (var e in v.FastInEdges)
            {
                if (e.TreeIndex < 0)
                {
                    if (!(Low <= e.Tail.Lim && e.Tail.Lim <= Lim))
                    {
                        slack = e.Slack;
                        if ((slack < Slack) || (Enter == null))
                        {
                            Enter = e;
                            Slack = slack;
                        }
                    }
                }
                else if (e.Tail.Lim < v.Lim)
                {
                    this.DFSEnterInEdge(e.Tail, ref Enter, ref Slack, ref Low, ref Lim);
                }
            }
            for (int i = 0; i < v.TreeOutEdges.Count && Slack > 0; i++)
            {
                var e = v.TreeOutEdges[i];

                if (e.Head.Lim < v.Lim)
                {
                    this.DFSEnterInEdge(e.Head, ref Enter, ref Slack, ref Low, ref Lim);
                }
            }
        }
        private EdgeData EnterEdge(EdgeData e)
        {
            VertexData v = null;
            bool outsearch = false;

            /* v is the down node */
            if (e.Tail.Lim < e.Head.Lim)
            {
                v = e.Tail;
                outsearch = false;
            }
            else
            {
                v = e.Head;
                outsearch = true;
            }
            EdgeData Enter = null;
            int Slack = int.MaxValue;
            int Low = v.Low;
            int Lim = v.Lim;
            if (outsearch)
                this.DFSEnterOutEdge(v, ref Enter, ref Slack, ref Low, ref Lim);
            else
                this.DFSEnterInEdge(v, ref Enter, ref Slack, ref Low, ref Lim);
            return Enter;
        }
        private void InitCutValues(List<VertexData> component)
        {
            this.DFSRange(component.FirstOrDefault(), null, 1);
            this.DFSCutValue(component.FirstOrDefault(), null);
        }

        private class SubTree
        {
            public VertexData rep = null;            /* some node in the tree */
            public int size =0;            /* total tight tree size */
            public int heap_index =0;      /* required to find non-min elts when merged */
            public SubTree par = null;  /* union find */
        }

        /* find initial tight subtrees */
        private int TightSubTreeSearch(List<VertexData> treeNodes, List<EdgeData> treeEdges,VertexData v, SubTree st)
        {
            int rv = 1;
            v.SubTree = st;
            foreach(var e in v.FastInEdges)
            {
                if (e.TreeIndex>=0) continue;
                if ((e.Tail.SubTree == null) && (e.Slack == 0))
                {
                    this.AddTreeEdge(treeNodes,treeEdges,e);
                    rv += this.TightSubTreeSearch(treeNodes, treeEdges, e.Tail, st);
                }
            }
            foreach(var e in v.FastOutEdges)
            {
                if (e.TreeIndex>=0) continue;
                if (e.Head.SubTree==null && (e.Slack == 0))
                {
                    this.AddTreeEdge(treeNodes, treeEdges, e);
                    rv += this.TightSubTreeSearch(treeNodes, treeEdges, e.Head, st);
                }
            }
            return rv;
        }
        private SubTree FindTightSubTree(List<VertexData> treeNodes, List<EdgeData> treeEdges, VertexData n)
        {
            SubTree rv = new SubTree();
            rv.rep = n;
            rv.size = this.TightSubTreeSearch(treeNodes, treeEdges, n, rv);
            rv.par = rv;
            return rv;
        }
        private class SubTreeHeap
        {
            public List<SubTree> Elements = new List<SubTree>();
        }
        private SubTree SubTreeSetFind(VertexData n0)
        {
            SubTree s0 = n0.SubTree;
            while (s0.par!=null && (s0.par != s0))
            {
                if (s0.par.par!=null)
                {
                    s0.par = s0.par.par;
                }  /* path compression for the code weary */
                s0 = s0.par;
            }
            return s0;
        }
        private SubTree SubTreeSetUnion(SubTree s0, SubTree s1)
        {
            SubTree r0, r1, r;

            for (r0 = s0; r0.par!=null && (r0.par != r0); r0 = r0.par) ;
            for (r1 = s1; r1.par!=null && (r1.par != r1); r1 = r1.par) ;
            if (r0 == r1)
            {
                return r0;  /* safety code but shouldn't happen */
            }

            if (r1.heap_index == -1) r = r0;
            else if (r0.heap_index == -1) r = r1;
            else if (r1.size < r0.size) r = r0;
            else r = r1;

            r0.par = r1.par = r;
            r.size = r0.size + r1.size;

            return r;
        }
        /* find tightest edge to another tree incident on the given tree */
        private EdgeData InterTreeEdgeSearch(VertexData v, VertexData from, EdgeData best)
        {
            SubTree ts = SubTreeSetFind(v);
            if (best != null && (best.Slack) == 0)
            {
                return best;
            }
            foreach(var e in v.FastOutEdges)
            {
                if (e.TreeIndex>=0)
                {
                    if (e.Head== from) continue;  // do not search back in tree
                    best = InterTreeEdgeSearch(e.Head, v, best); // search forward in tree
                }
                else
                {
                    if (SubTreeSetFind(e.Head) != ts)
                    {   // encountered candidate edge
                        if ((best == null) || (e.Slack < best.Slack)) best = e;
                    }
                    /* else ignore non-tree edge between nodes in the same tree */
                }
            }
            /* the following code must mirror the above, but for in-edges */
            foreach(var e in v.FastInEdges)
            {
                if (e.TreeIndex>=0)
                {
                    if (e.Tail == from) continue;
                    best = InterTreeEdgeSearch(e.Tail, v, best);
                }
                else
                {
                    if (SubTreeSetFind(e.Tail) != ts)
                    {
                        if ((best == null) || (e.Slack <best.Slack)) best = e;
                    }
                }
            }
            return best;
        }
        private EdgeData InterTreeEdge(SubTree tree)
        {
            return InterTreeEdgeSearch(tree.rep, null, null);
        }
        private void SubTreeHeapify(SubTreeHeap heap, int i)
        {
            int left, right, smallest;
            var elt = heap.Elements;
            do
            {
                left = 2 * (i + 1) - 1;
                right = 2 * (i + 1);
                if ((left < elt.Count) && (elt[left].size < elt[i].size)) smallest = left;
                else smallest = i;
                if ((right <elt.Count) && (elt[right].size < elt[smallest].size)) smallest = right;
                else smallest = i;
                if (smallest != i)
                {
                    SubTree temp= elt[i];
                    elt[i] = elt[smallest];
                    elt[smallest] = temp;
                    elt[i].heap_index = i;
                    elt[smallest].heap_index = smallest;
                    i = smallest;
                }
                else break;
            } while (i < elt.Count);
        }
        private SubTreeHeap SubTreeBuildHeap(List<SubTree> elt)
        {
            SubTreeHeap heap = new SubTreeHeap
            {
                Elements = elt
            };
            for (int i = 0; i < heap.Elements.Count; i++)
                heap.Elements[i].heap_index = i;
            for (int i = heap.Elements.Count / 2; i >= 0; i--)
                SubTreeHeapify(heap, i);
            return heap;
        }
        private SubTree SubTreeExtractMin(SubTreeHeap heap)
        {
            SubTree rv = heap.Elements[0];
            heap.Elements[0] = heap.Elements.Last();
            heap.Elements[0].heap_index = 0;
            heap.Elements.RemoveAt(heap.Elements.Count-1);
            SubTreeHeapify(heap, 0);
            return rv;
        }
        private void TreeAdjust(VertexData v, VertexData from, int delta)
        { 
            v.Rank+= + delta;
            foreach(var e in v.TreeInEdges)
            {
                var w = e.Tail;
                if (w != from)
                    TreeAdjust(w, v, delta);
            }
            foreach(var e in v.TreeOutEdges)
            {
                var w = e.Head;
                if (w != from)
                    TreeAdjust(w, v, delta);
            }
        }
        private SubTree MergeTrees(List<VertexData> treeNodes,List<EdgeData> treeEdges, EdgeData e)   /* entering tree edge */
        {
            int delta;
            SubTree t0, t1, rv;

            t0 = SubTreeSetFind(e.Tail);
            t1 = SubTreeSetFind(e.Head);

            if (t0.heap_index == -1)
            {   // move t0
                delta = e.Slack;
                TreeAdjust(t0.rep, null, delta);
            }
            else
            {  // move t1
                delta = -e.Slack;
                TreeAdjust(t1.rep, null, delta);
            }
            this.AddTreeEdge(treeNodes,treeEdges,e);
            rv = SubTreeSetUnion(t0, t1);

            return rv;
        }

        /* Construct initial tight tree. Graph must be connected, feasible.
         * Adjust v.Rank as needed.  add_tree_edge() on tight tree edges.
         * trees are basically lists of nodes stored in nodequeues.
         * Return 1 if input graph is not connected; 0 on success.
         */
        private bool HasFeasibleTree(List<VertexData> treeNodes,List<EdgeData> treeEdges, List<VertexData> component)
        {
            bool error = false;

            /* initialization */
            component.ForEach(c => c.SubTree = null);

            var subTrees = new List<SubTree>();
            /* given init_rank, find all tight subtrees */
            foreach(var n in component)
            {
                if (n.SubTree  == null)
                {
                    subTrees.Add(this.FindTightSubTree(treeNodes,treeEdges,n));
                }
            }

            /* incrementally merge subtrees */
            var heap = SubTreeBuildHeap(subTrees);
            while (heap.Elements.Count > 1)
            {
                var tree0 = this.SubTreeExtractMin(heap);
                var treeEdge = this.InterTreeEdge(tree0);
                if (treeEdge == null)
                {
                    error = true;
                    break;
                }
                var tree1 = this.MergeTrees(treeNodes,treeEdges,treeEdge);

                SubTreeHeapify(heap, tree1.heap_index);
            }

            this.InitCutValues(component);
            return error;
        }

        /* walk up from v to LCA(v,w), setting new cutvalues. */
        private VertexData TreeUpdate(VertexData v, VertexData w, int cutvalue, bool dir)
        {
            bool d = false;

            while (!(v.Low<=w.Lim && w.Lim <=v.Lim))
            {
                var e = v.Par;
                if (v == e.Tail)
                    d = dir;
                else
                    d = !dir;
                if (d)
                    e.CutValue += cutvalue;
                else
                    e.CutValue -= cutvalue;
                if (e.Tail.Lim > e.Head.Lim)
                    v = e.Tail;
                else
                    v = e.Head;
            }
            return v;
        }

        private void Rerank(VertexData v, int delta)
        {
            v.Rank-= delta;
            foreach(var e in v.TreeOutEdges)
                if (e != v.Par)
                    Rerank(e.Head, delta);
            foreach(var e in v.TreeInEdges)
                if (e != v.Par)
                    Rerank(e.Tail, delta);
        }

        /* e is the tree edge that is leaving and f is the nontree edge that
         * is entering.  compute new cut values, ranks, and exchange e and f.
         */
        private void Update(List<EdgeData> treeEdges,EdgeData e, EdgeData f)
        {
            int cutvalue, delta = f.Slack; 
            VertexData lca = null;

            /* "for (v = in nodes in tail side of e) do v.Rank -= delta;" */
            if (delta > 0)
            {
                int s = e.Tail.TreeInEdges.Count + e.Tail.TreeOutEdges.Count;
                if (s == 1)
                    this.Rerank(e.Tail, delta);
                else
                {
                    s = e.Head.TreeInEdges.Count +e.Head.TreeOutEdges.Count;
                    if (s == 1)
                        this.Rerank(e.Head, -delta);
                    else
                    {
                        if (e.Tail.Lim < e.Head.Lim)
                            this.Rerank(e.Tail, delta);
                        else
                            this.Rerank(e.Head, -delta);
                    }
                }
            }

            cutvalue = e.CutValue;
            lca = this.TreeUpdate(f.Tail,f.Head, cutvalue, true);
            if (this.TreeUpdate(f.Head,f.Tail, cutvalue, false) != lca)
            {
                //longjmp(jbuf, 1);
            }
            f.CutValue = -cutvalue;
            e.CutValue = 0;
            this.ExchangeTreeEdges(treeEdges,e, f);

            this.DFSRange(lca, lca.Par, lca.Low);
        }
        private void FreeTreeList(List<VertexData> component)
        {
            foreach(var n in component)
            {
                n.TreeInEdges.Clear();
                n.TreeOutEdges.Clear();
                n.Mark = 0;
            }
        }
        private enum BalanceMode : int
        {
            None = 0,
            TopBottom = 1,
            LeftRight = 2,
        }
        private enum RankResult : int
        {
            OK = 0,
            Feasible = 1,
            Error = 2,
            MinIterators = 3,
            MaxIterators = 4,
        }
        private RankResult DoRank(GraphData g, List<VertexData> component, BalanceMode Balance, int MaxIterators)
        {
            var result = RankResult.OK;
            if (MaxIterators <= 0)
            {
                result = RankResult.MaxIterators;
            }
            else
            {
                var treeNodes = new List<VertexData>();
                var treeEdges = new List<EdgeData>();

                bool feasible = this.InitGraph(component);

                if (!feasible)
                {
                    this.InitRank(component);
                }

                if (this.HasFeasibleTree(treeNodes, treeEdges, component))
                {
                    result = RankResult.Feasible;
                }
                else
                {
                    int iter = 0;
                    int S_i = 0;
                    EdgeData e = null;
                    while ((e = this.LeaveEdge(treeEdges, ref S_i)) != null)
                    {
                        var f = this.EnterEdge(e);

                        this.Update(treeEdges, e, f);

                        if (++iter >= MaxIterators)
                        {
                            result = RankResult.MaxIterators;
                            break;
                        }
                    }
                    switch (Balance)
                    {
                        case BalanceMode.TopBottom:
                            this.DoTopBottomBalance(g, component);
                            break;
                        case BalanceMode.LeftRight:
                            this.DoLeftRightBalance(treeEdges);
                            FreeTreeList(component);
                            break;
                        default:
                            this.ScanRankAndNormalize(g, component);
                            break;
                    }
                    component.ForEach(c => c.Mark = 0);
                } 
            }
            return result;
        }

        /* set cut value of f, assuming values of edges on one side were already set */
        private void SetXCutValue(EdgeData f)
        {
            VertexData v = null;
            int dir = 1;

            /* set v to the node on the side of the edge already searched */
            if (f.Tail.Par == f)
            {
                v = f.Tail;
                dir = 1;
            }
            else
            {
                v = f.Head;
                dir = -1;
            }

            f.CutValue = v.FastOutEdges.Sum(e => GetXValue(e, v, dir)) + v.FastInEdges.Sum(e => GetXValue(e, v, dir));
        }

        private int GetXValue(EdgeData e, VertexData v, int dir)
        {
            int d = 0, rv = 0;
            bool f = false;

            VertexData other = null;

            if (e.Tail == v)
                other = e.Head;
            else
                other = e.Tail;
            if (!(v.Low <= other.Lim && other.Lim <= v.Lim))
            {
                f = true;
                rv = e.Weight;
            }
            else
            {
                f = false;
                if (e.TreeIndex >= 0)
                    rv = e.CutValue;
                else
                    rv = 0;
                rv -= e.Weight;
            }
            if (dir > 0)
            {
                if (e.Head == v)
                    d = 1;
                else
                    d = -1;
            }
            else
            {
                if (e.Tail == v)
                    d = 1;
                else
                    d = -1;
            }
            if (f)
                d = -d;
            if (d < 0)
                rv = -rv;
            return rv;
        }

        private void DFSCutValue(VertexData v, EdgeData par)
        {
            foreach (var e in v.TreeOutEdges)
            {
                if (e != par)
                {
                    this.DFSCutValue(e.Head, e);
                }
            }
            foreach (var e in v.TreeInEdges)
            {
                if (e != par)
                {
                    this.DFSCutValue(e.Tail, e);
                }
            }
            if (par != null)
            {
                this.SetXCutValue(par);
            }
        }

        private int DFSRange(VertexData v, EdgeData par, int low)
        {
            int lim = 0;

            lim = low;
            v.Par = par;
            v.Low = low;
            foreach (var e in v.TreeOutEdges)
            {
                if (e != par)
                {
                    lim = this.DFSRange(e.Head, e, lim);
                    //Debug.WriteLine("e={0}", e);
                }
            }
            foreach (var e in v.TreeInEdges)
            {
                if (e != par)
                {
                    lim = this.DFSRange(e.Tail, e, lim);
                }
            }
            v.Lim = lim;
            return lim + 1;
        }
    }
}
