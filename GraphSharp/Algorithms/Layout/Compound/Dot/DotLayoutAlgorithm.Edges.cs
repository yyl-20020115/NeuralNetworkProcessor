using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace GraphSharp.Algorithms.Layout.Compound.Dot
{
    public partial class DotLayoutAlgorithm<TVertex, TEdge, TGraph>
    {
        private const int FUDGE = 2;
        private const int MINW = 16;
        private const int HALFMINW = MINW >> 1;
        private const int W_DEGREE = 5;
        private const double MILLIPOINT = 0.001;
        private static int[,] pair_a = new int[8, 8]  {	    //array of possible node point pairs
	            {11,12,13,14,15,16,17,18},
                {21,22,23,24,25,26,27,28},
                {31,32,33,34,35,36,37,38},
                {41,42,43,44,45,46,47,48},
                {51,52,53,54,55,56,57,58},
                {61,62,63,64,65,66,67,68},
                {71,72,73,74,75,76,77,78},
                {81,82,83,84,85,86,87,88}
            };
        private const int INIT_DELTA = 10;
        private const int LOOP_TRIES = 15;  /* number of times to try to limiting boxes to regain space, using smaller divisions */
        private Dictionary<TVertex, Rect> _verticesRects = null;
        private bool DynamicRoutingProtecting = false;

        //copy old to new and reverse direction
        private EdgeData MakeForwardEdge(EdgeData old) =>
            new EdgeData(old)
            {
                Tail = old.Head,
                Head = old.Tail,
                TailIndex = old.HeadIndex,
                HeadIndex = old.TailIndex,
                EdgeType = EdgeTypes.Virtual,
                Original = old
            };

        private bool SplineMerge(VertexData n)
        {
            return ((n.NodeType == NodeTypes.Virtual)
                && ((n.FastInEdges.Count > 1) || (n.FastOutEdges.Count > 1)));
        }
        private bool SwapEnds(EdgeData e)
        {
            while (e.Original != null && e.Original != e)
                e = e.Original;
            if (e.Head.Rank > e.Tail.Rank)
                return false;
            if (e.Head.Rank < e.Tail.Rank)
                return true;
            if (e.Head.Order >= e.Tail.Order)
                return false;
            return true;
        }
        private int GetStraightLength(VertexData n)
        {
            int cnt = 0;
            var v = n;

            while (true)
            {
                v = v.FastOutEdges[0].Head;
                if (v.NodeType != NodeTypes.Virtual)
                    break;
                if ((v.FastOutEdges.Count != 1) || (v.FastInEdges.Count != 1))
                    break;
                if (v.Location.X != n.Location.X)
                    break;
                cnt++;
            }
            return cnt;
        }
        private Box RankBox(SplineInfoData sp, GraphData g, int r)
        {
            var b = sp.RankBoxes[r];
            if (b.LL.X == b.UR.X)
            {
                var left0 = g.Ranks[r].v[0];
                var left1 = g.Ranks[r + 1].v[0];
                b.LL.X = sp.LeftBound;
                b.LL.Y = (left1.Location).Y + g.Ranks[r + 1].ht2;
                b.UR.X = sp.RightBound;
                b.UR.Y = (left0.Location).Y - g.Ranks[r].ht1;
               
            }
            return b.Clone();
        }

        private EdgeSplineType SetEdgeSplineType(EdgeData e, EdgeSplineType hint1, EdgeSplineType hint2, EdgeSplineType f3)
        {
            EdgeSplineType f1 = EdgeSplineType.NONE, f2 = EdgeSplineType.NONE;
            if (hint1 != 0)
                f1 = hint1;
            else
            {
                if (e.Tail == e.Head)
                    if (e.TailIndex >= 0 || e.HeadIndex >= 0)
                        f1 = EdgeSplineType.SELFWPEDGE;
                    else
                        f1 = EdgeSplineType.SELFNPEDGE;
                else if (e.Tail.Rank == e.Head.Rank)
                    f1 = EdgeSplineType.FLATEDGE;
                else
                    f1 = EdgeSplineType.REGULAREDGE;
            }
            if (hint2 != 0)
                f2 = hint2;
            else
            {
                if (f1 == EdgeSplineType.REGULAREDGE)
                    f2 = (e.Tail.Rank < e.Head.Rank) ? EdgeSplineType.FWDEDGE : EdgeSplineType.BWDEDGE;
                else if (f1 == EdgeSplineType.FLATEDGE)
                    f2 = (e.Tail.Order < e.Head.Order) ? EdgeSplineType.FWDEDGE : EdgeSplineType.BWDEDGE;
                else            /* f1 == SELF*EDGE */
                    f2 = EdgeSplineType.FWDEDGE;
            }
            return e.EdgeSplineType = (f1 | f2 | f3);
        }
        private void NormalizeEdge(EdgeData e)
        {
            if (this.SwapEnds(e) && e.spl != null)
            {
                e.spl.line.list.Reverse();
            }
        }
        private EdgeData GetMainEdge(EdgeData e)
        {
            var le = e;
            while (le.Virtual != null && le != le.Virtual)
                le = le.Virtual;
            while (le.Original != null && le != le.Original)
                le = le.Original;
            return le;
        }
        public override void PostLayoutProcess(IDictionary<TVertex, Rect> dict)
        {
            this._verticesRects = new Dictionary<TVertex, Rect>(dict);

            foreach (var v in dict.Keys)
            {
                if (this._allVertexDatas.TryGetValue(v, out var vd))
                {
                    var vr = dict[v];
                    {
                        vd.Location = this.RotatePoint(vr.Location, this.Parameters.Direction, true);
                        vd.Size = this.RotateSize(vr.Size, this.Parameters.Direction, true);
                        vd.ht = vr.Height;
                        vd.lw = vd.rw = vr.Width / 2.0;
                    }
                }
            }
            this._rootGraph.sd.Splinesep = this._rootGraph.NodeSep / 4;
            this._rootGraph.sd.Multisep = this._rootGraph.NodeSep;
            this._rootGraph.sd.LeftBound = this._rootGraph.sd.RightBound = 0;
            this.MarkLowClusters(this._rootGraph);
            foreach (var _rank in this._rootGraph.Ranks)
            {
                var rank = _rank;
                double ht = 0.0;
                var v = rank.v.FirstOrDefault();

                if (v != null)
                {
                    this._rootGraph.sd.LeftBound = (int)Math.Min(this._rootGraph.sd.LeftBound, (v.Location.X - v.lw));
                }
                v = rank.v.LastOrDefault();
                if (v != null)
                {
                    this._rootGraph.sd.RightBound = (int)Math.Max(this._rootGraph.sd.RightBound, (v.Location.X + v.lw));
                }
                this._rootGraph.sd.LeftBound -= MINW;
                this._rootGraph.sd.RightBound += MINW;
                this._rootGraph.sd.RankBoxes.Clear();
                for (int i = 0; i < _rootGraph.MaxRank - _rootGraph.MinRank + 1; i++)
                {
                    _rootGraph.sd.RankBoxes.Add(new Box());
                }
                foreach (var n in rank.v)
                {
                    ht = Math.Max(n.ht, ht);
                    if (n.NodeType != NodeTypes.Normal && !SplineMerge(n))
                        continue;
                    foreach (var edge in n.FastOutEdges)
                    {
                        if (edge.EdgeType == EdgeTypes.FlatOrder || edge.EdgeType == EdgeTypes.Ignored)
                        {
                            continue;
                        }
                        this.SetEdgeSplineType(edge, EdgeSplineType.REGULAREDGE, EdgeSplineType.FWDEDGE, EdgeSplineType.MAINGRAPH);

                    }
                    foreach (var edge in n.FlatOutEdges)
                    {
                        this.SetEdgeSplineType(edge, EdgeSplineType.FLATEDGE, EdgeSplineType.NONE, EdgeSplineType.AUXGRAPH);
                    }
                    if (n.NodeType == NodeTypes.Normal)
                    {
                        var t = n.rw;
                        n.rw = n.Mval;
                        n.Mval = t;
                    }
                    if (n.OtherEdges.Count > 0)
                    {
                        foreach (var edge in n.OtherEdges)
                        {
                            this.SetEdgeSplineType(edge, EdgeSplineType.NONE, EdgeSplineType.NONE, EdgeSplineType.AUXGRAPH);
                        }
                    }
                }
                rank.ht2 = rank.ht1 = ht / 2.0;
            }
        }
        public override Point[] RouteEdge(TEdge edge, Rect tailRect, Rect headRect)
        {
            if (!this.DynamicRoutingProtecting && this.DynamicRouting && this._verticesRects != null && this._verticesRects.Count > 0)
            {
                this.DynamicRoutingProtecting = true;
                var e0 = edge != null ? this._allEdgeDatas.FirstOrDefault(a => object.ReferenceEquals(a.Edge, edge)) : null;
                if (e0 != null && e0.Tail != null && e0.Head != null)
                {
                    var tail = e0.Tail;
                    var head = e0.Head;


                    this._rootGraph.sd.WorkingBoxes.Clear();
                    //headRect = this.RotateRect(headRect, this.Parameters.Direction, true);
                    //tailRect = this.RotateRect(tailRect, this.Parameters.Direction, true);
                    var P = new Path();

                    //this is a patch for error input
                    //when tail!=head but tailRect.Location==headRect.Location
                    if (tail != head && tailRect.Location == headRect.Location)
                    {
                    }
                    else
                    {
                        tail.Location = tailRect.Location;
                        tail.Size = tailRect.Size;
                        tail.ht = tailRect.Height;
                        tail.lw = tail.rw = tailRect.Width / 2.0;

                        head.Location = headRect.Location;
                        head.Size = headRect.Size;
                        head.ht = headRect.Height;
                        head.lw = head.rw = headRect.Width / 2.0;
                    }

                    e0.HeadPoint = headRect.Location;// new Point(headRect.Left - headRect.Width / 2.0, headRect.Top + headRect.Height / 2.0);
                    e0.TailPoint = tailRect.Location;// new Point(tailRect.Left - tailRect.Width / 2.0, tailRect.Top + tailRect.Height / 2.0); 

                    var le0 = this.GetMainEdge(e0);

                    var ea = e0.HeadIndex >= 0 || e0.TailIndex >= 0 ? e0 : le0;

                    if (e0.Tail == e0.Head)
                    {
                        int sizey;
                        var n = e0.Tail;
                        var r = n.Rank;
                        if (r == _rootGraph.MaxRank)
                        {
                            if (r > 0)
                                sizey = (int)(_rootGraph.Ranks[r - 1].v[0].Location.Y - n.Location.Y);
                            else
                                sizey = (int)n.ht;
                        }
                        else if (r == _rootGraph.MinRank)
                        {
                            sizey = (int)(n.Location.Y - _rootGraph.Ranks[r + 1].v[0].Location.Y);
                        }
                        else
                        {
                            int upy = (int)(_rootGraph.Ranks[r - 1].v[0].Location.Y - n.Location.Y);
                            int dwny = (int)(n.Location.Y - _rootGraph.Ranks[r + 1].v[0].Location.Y);
                            sizey = Math.Min(upy, dwny);
                        }
                        this.MakeSelfEdge(_rootGraph, e0, _rootGraph.NodeSep, sizey / 2);
                    }
                    else if (e0.Tail.Rank == e0.Head.Rank)
                    {
                        //never called
                        this.MakeFlatEdge(this._rootGraph, e0, P);
                    }
                    else 
                    {
                        this.MakeRegularEdge(this._rootGraph, e0, P);
                    }

                    this.NormalizeEdge(e0);

                    this.DynamicRoutingProtecting = false;

                    if (e0.spl != null && e0.spl.line != null)
                    {
                        //this.RotatePoints(e0.spl.line.list, this.Parameters.Direction,true);

                        return e0.spl.line.list.ToArray();
                    }
                }
            }
            return new Point[0];
        }

        private EdgeData StraightPath(EdgeData e, int cnt, List<Point> plist, ref int np)
        {
            int n = 0;
            EdgeData f = e;

            while (cnt-- != 0 && f != null)
            {
                f = f.Head.FastOutEdges.FirstOrDefault();
            }

            plist[np++] = plist[n - 1];
            plist[np++] = plist[n - 1];
            plist[np] = f.Tail.Location; /* will be overwritten by next spline */

            return f;
        }
        private void RecoverSlack(EdgeData e, Path p)
        {
            int b = 0;
            /* skip first rank box */
            for (var vn = e.Head;
                vn.NodeType == NodeTypes.Virtual && !this.SplineMerge(vn);
                vn = vn.FastOutEdges[0].Head)
            {
                while ((b < p.boxes.Count) && (p.boxes[b].LL.Y > vn.Location.Y))
                    b++;
                if (b >= p.boxes.Count)
                    break;
                if (p.boxes[b].UR.Y < vn.Location.Y)
                    continue;

                this.ResizeVirtualNode(vn, (int)p.boxes[b].LL.X, (int)((p.boxes[b].LL.X +
                    p.boxes[b].UR.X) / 2.0),
                    (int)p.boxes[b].UR.X);
            }
        }
        private void ResizeVirtualNode(VertexData vn, int lx, int cx, int rx)
        {
            vn.Location.X = cx;
            vn.lw = cx - lx;
            vn.rw = rx - cx;
        }

        private void MakeRegularEdge(GraphData g, EdgeData e, Path P)
        {
            var ps = new List<Point>();
            var tend = new PathEnd();
            var hend = new PathEnd();
            var sl = 0;
            var si = 0;
            var smode = false;
            var hackflag = false;
            var pointfs = new List<Point>();
            var boxes = g.sd.WorkingBoxes;
            var ForwardEdgeAPair = new EdgeDataPair() { InEdge = new EdgeData(), OutEdge = new EdgeData() };
            var ForwardEdgeBPair = new EdgeDataPair() { InEdge = new EdgeData(), OutEdge = new EdgeData() };
            var ForwardEdgePair = new EdgeDataPair() { InEdge = new EdgeData(), OutEdge = new EdgeData() };

            if (Math.Abs(e.Tail.Rank - e.Head.Rank) > 1)
            {
                ForwardEdgeAPair.OutEdge = e;
                ForwardEdgeAPair.InEdge = this.MakeForwardEdge(e);

                if ((e.EdgeSplineType & EdgeSplineType.BWDEDGE) != EdgeSplineType.NONE)
                {
                    ForwardEdgeBPair.OutEdge = this.MakeForwardEdge(e);
                    ForwardEdgeBPair.OutEdge.Tail = e.Head;
                    ForwardEdgeBPair.OutEdge.TailIndex = e.TailIndex;
                    ForwardEdgeBPair.OutEdge.TailPoint = e.TailPoint;
                    ForwardEdgeBPair.OutEdge.TailPortBox = e.TailPortBox;

                    ForwardEdgeBPair.OutEdge.TailPortSide = e.TailPortSide;
                    ForwardEdgeBPair.OutEdge.TailPortClip = e.TailPortClip;
                }
                else
                {
                    ForwardEdgeBPair.OutEdge = e;
                    ForwardEdgeAPair.OutEdge.Tail = e.Tail;
                    ForwardEdgeBPair.InEdge = this.MakeForwardEdge(e);
                }

                var le = this.GetMainEdge(e);
                while (le.Virtual != null)
                    le = le.Virtual;
                ForwardEdgeAPair.OutEdge.Head = le.Head;
                ForwardEdgeAPair.OutEdge.HeadIndex = -1;
                ForwardEdgeAPair.OutEdge.EdgeType = EdgeTypes.Virtual;
                ForwardEdgeAPair.OutEdge.HeadPortBox.LL = new Point();
                ForwardEdgeAPair.OutEdge.Original = e;
                e = ForwardEdgeAPair.OutEdge;

                hackflag = true;
            }
            else if ((e.EdgeSplineType & EdgeSplineType.BWDEDGE) != EdgeSplineType.NONE)
            {
                ForwardEdgeAPair.OutEdge = this.MakeForwardEdge(e);
            }
            var fe = e;

            /* compute the spline points for the edge */

            var segfirst = e;
            var tn = e.Tail;
            var hn = e.Head;
            
            tend.nb = this.MaximalBoundingBox(g, g.sd, tn, null, e);
            var b = tend.nb.Clone();

            this.BeginPath(g, P, e, EdgeSplineType.REGULAREDGE, tend, SplineMerge(tn));
            b.UR.Y = tend.boxes[tend.boxes.Count - 1].UR.Y;
            b.LL.Y = tend.boxes[tend.boxes.Count - 1].LL.Y;
            b = this.MakeRegularEnd(b, PortSides.BOTTOM, (tn.Location).Y - g.Ranks[tn.Rank].ht1);
            if (b.LL.X < b.UR.X && b.LL.Y < b.UR.Y)
                tend.boxes.Add(b);
            smode = false;
            si = -1;
            while (hn.NodeType == NodeTypes.Virtual && !SplineMerge(hn))
            {
                //longedge = true;
                boxes.Add(RankBox(g.sd, g, tn.Rank));
                if (!smode
                    && ((sl = GetStraightLength(hn)) >= 3))
                {
                    smode = true;
                    si = 1;
                    sl -= 2;
                }
                if (!smode || si > 0)
                {
                    si--;
                    boxes.Add(this.MaximalBoundingBox(g, g.sd, hn, e, hn.FastOutEdges.FirstOrDefault()));
                    e = hn.FastOutEdges.FirstOrDefault();
                    tn = e.Tail;
                    hn = e.Head;
                    continue;
                }
                hend.nb = this.MaximalBoundingBox(g, g.sd, hn, e, hn.FastOutEdges.FirstOrDefault());
                this.EndPath(g, P, e, EdgeSplineType.REGULAREDGE, hend, SplineMerge(e.Head));
                b = MakeRegularEnd(hend.boxes[hend.boxes.Count - 1], PortSides.TOP,
                    (hn.Location).Y + g.Ranks[hn.Rank].ht2);
                if (b.LL.X < b.UR.X && b.LL.Y < b.UR.Y)
                    hend.boxes.Add(b);
                P.endTheta = Math.PI / 2;
                P.endConstrained = true;

                this.CompleteRegularPath(P, segfirst, e, tend, hend, boxes);

                ps = this.RouteSplines(P);

                if (ps.Count == 0)
                    return;
                pointfs.AddRange(ps);
                int pointn = 0;
                e = StraightPath(hn.FastOutEdges.FirstOrDefault(), sl, pointfs, ref pointn);
                RecoverSlack(segfirst, P);
                segfirst = e;
                tn = e.Tail;
                hn = e.Head;
                tend.nb = MaximalBoundingBox(g, g.sd, tn, tn.FastInEdges.FirstOrDefault(), e);
                BeginPath(g, P, e, EdgeSplineType.REGULAREDGE, tend, SplineMerge(tn));
                b = MakeRegularEnd(tend.boxes[tend.boxes.Count - 1], PortSides.BOTTOM,
                    (tn.Location).Y - g.Ranks[tn.Rank].ht1);
                if (b.LL.X < b.UR.X && b.LL.Y < b.UR.Y)
                    tend.boxes.Add(b);

                P.startTheta = -Math.PI / 2;
                P.startConstrained = true;
                smode = false;
            }
            boxes.Add(RankBox(g.sd, g, tn.Rank));
            b = hend.nb = MaximalBoundingBox(g, g.sd, hn, e, null);
            EndPath(g, P, hackflag ? ForwardEdgeBPair.OutEdge : e, EdgeSplineType.REGULAREDGE, hend, SplineMerge(e.Head));
            b.UR.Y = hend.boxes[hend.boxes.Count - 1].UR.Y;
            b.LL.Y = hend.boxes[hend.boxes.Count - 1].LL.Y;
            b = MakeRegularEnd(b, PortSides.TOP,
                (hn.Location).Y + g.Ranks[hn.Rank].ht2);
            if (b.LL.X < b.UR.X && b.LL.Y < b.UR.Y)
                hend.boxes.Add(b);
            this.CompleteRegularPath(P, segfirst, e, tend, hend, boxes);

            ps = RouteSplines(P);

            if (ps == null || ps.Count == 0)
                return;
            pointfs.AddRange(ps);
            this.RecoverSlack(segfirst, P);
            hn = hackflag ? ForwardEdgeBPair.OutEdge.Head : e.Head;

            this.ClipAndInstall(g, fe, hn, pointfs);
        }
        /* beginpath:
         * Set up boxes near the tail node.
         * For regular nodes, the result should be a list of contiguous rectangles 
         * such that the last one has the smallest LL.Y and its LL.Y is above
         * the bottom of the rank (rank.ht1).
         * 
         * For flat edges, we assume endp.sidemask has been set. For regular
         * edges, we set this, but it doesn't appear to be needed any more.
         * 
         * In many cases, we tweak the x or y coordinate of P.start.p by 1.
         * This is because of a problem in the path routing code. If the starting
         * point actually lies on the polygon, in some cases, the router gets
         * confused and routes the path outside the polygon. So, the offset ensures
         * the starting point is in the polygon.
         *
         * FIX: Creating the initial boxes only really works for rankdir=TB and
         * rankdir=LR. For the others, we rely on compassPort flipping the side
         * and then assume that the node shape has top-bottom symmetry. Since we
         * at present only put compass points on the bounding box, this works.
         * If we attempt to implement compass points on actual node perimeters,
         * something major will probably be necessary. Doing the coordinate
         * flip (postprocess) before spline routing will be too disruptive. The
         * correct solution is probably to have beginpath/endpath create the
         * boxes assuming an inverted node. Note that compassPort already does
         * some flipping. Even better would be to allow the *_path function
         * to provide a polygon.
         *
         * The extra space provided by FUDGE-2 prevents the edge from getting
         * too close the side of the node.
         *
         */
        private double HT2(VertexData n) => (n.ht / 2);


        private void BeginPath(GraphData g, Path P, EdgeData e, EdgeSplineType et, PathEnd endp, bool merge)
        {
            PortSides side = PortSides.NONE;
            //int(*pboxfn)(node_t *, port *, int, Box *, int *);

            var n = e.Tail;

            P.startPoint = AddPoint(n.Location, e.TailPoint);
            if (merge)
            {
                /*P.start.theta = - M_PI / 2; */
                P.startTheta = ConcentrateSlope(e.Tail);
                P.startConstrained = true;
            }
            else
            {
                P.startConstrained = false;
            }
            P.data = e;
            endp.np = P.startPoint;
            if ((et == EdgeSplineType.REGULAREDGE) && (n.NodeType == NodeTypes.Normal)
                && ((side = e.TailPortSide) != PortSides.NONE))
            {
                EdgeData orig;
                Box b0 = new Box(), b = endp.nb;
                if ((side & PortSides.TOP) != PortSides.NONE)
                {
                    endp.sidemask = PortSides.TOP;
                    if (P.startPoint.X < n.Location.X)
                    { /* go left */
                        b0.LL.X = b.LL.X - 1;
                        /* b0.LL.Y = n.Coordinate.Y + HT2(n); */
                        b0.LL.Y = P.startPoint.Y;
                        b0.UR.X = b.UR.X;
                        b0.UR.Y = n.Location.Y + HT2(n) + g.RankSep / 2;
                        b.UR.X = n.Location.X - n.lw - (FUDGE - 2);
                        b.UR.Y = b0.LL.Y;
                        b.LL.Y = n.Location.Y - HT2(n);
                        b.LL.X -= 1;
                        endp.boxes.Add(b0);
                        endp.boxes.Add(b);
                    }
                    else
                    {
                        b0.LL.X = b.LL.X;
                        b0.LL.Y = P.startPoint.Y;
                        /* b0.LL.Y = n.Coordinate.Y + HT2(n); */
                        b0.UR.X = b.UR.X + 1;
                        b0.UR.Y = n.Location.Y + HT2(n) + g.RankSep / 2;
                        b.LL.X = n.Location.X + n.rw + (FUDGE - 2);
                        b.UR.Y = b0.LL.Y;
                        b.LL.Y = n.Location.Y - HT2(n);
                        b.UR.X += 1;
                        endp.boxes.Add(b0);
                        endp.boxes.Add(b);
                    }
                    P.startPoint.Y += 1;
                }
                else if ((side & PortSides.BOTTOM) != PortSides.NONE)
                {
                    endp.sidemask = PortSides.BOTTOM;
                    b.UR.Y = Math.Max(b.UR.Y, P.startPoint.Y);
                    endp.boxes.Add(b);
                    P.startPoint.Y -= 1;
                }
                else if ((side & PortSides.LEFT) != PortSides.NONE)
                {
                    endp.sidemask = PortSides.LEFT;
                    b.UR.X = P.startPoint.X;
                    b.LL.Y = n.Location.Y - HT2(n);
                    b.UR.Y = P.startPoint.Y;
                    endp.boxes.Add(b);
                    P.startPoint.X -= 1;
                }
                else
                {
                    endp.sidemask = PortSides.RIGHT;
                    b.LL.X = P.startPoint.X;
                    b.LL.Y = n.Location.Y - HT2(n);
                    b.UR.Y = P.startPoint.Y;
                    endp.boxes.Add(b);
                    P.startPoint.X += 1;
                }
                for (orig = e; orig.EdgeType != EdgeTypes.Normal; orig = orig.Original) ;
                if (n == orig.Tail)
                    orig.TailPortClip = false;
                else
                    orig.HeadPortClip = false;
                return;
            }
            if ((et == EdgeSplineType.FLATEDGE) && ((side = e.TailPortSide) != PortSides.NONE))
            {
                Box b0 = new Box(), b = endp.nb;
                EdgeData orig;
                if ((side & PortSides.TOP) != PortSides.NONE)
                {
                    b.LL.Y = Math.Min(b.LL.Y, P.startPoint.Y);
                    endp.boxes.Add(b);
                    P.startPoint.Y += 1;
                }
                else if ((side & PortSides.BOTTOM) != PortSides.NONE)
                {
                    if (endp.sidemask == PortSides.TOP)
                    {
                        b0.UR.Y = n.Location.Y - HT2(n);
                        b0.UR.X = b.UR.X + 1;
                        b0.LL.X = P.startPoint.X;
                        b0.LL.Y = b0.UR.Y - g.RankSep / 2;
                        b.LL.X = n.Location.X + n.rw + (FUDGE - 2);
                        b.LL.Y = b0.UR.Y;
                        b.UR.Y = n.Location.Y + HT2(n);
                        b.UR.X += 1;
                        endp.boxes.Add(b0);
                        endp.boxes.Add(b);
                    }
                    else
                    {
                        b.UR.Y = Math.Max(b.UR.Y, P.startPoint.Y);
                        endp.boxes.Add(b);
                    }
                    P.startPoint.Y -= 1;
                }
                else if ((side & PortSides.LEFT) != PortSides.NONE)
                {
                    b.UR.X = P.startPoint.X + 1;
                    if (endp.sidemask == PortSides.TOP)
                    {
                        b.UR.Y = n.Location.Y + HT2(n);
                        b.LL.Y = P.startPoint.Y - 1;
                    }
                    else
                    {
                        b.LL.Y = n.Location.Y - HT2(n);
                        b.UR.Y = P.startPoint.Y + 1;
                    }
                    endp.boxes.Add(b);
                    P.startPoint.X -= 1;
                }
                else
                {
                    b.LL.X = P.startPoint.X;
                    if (endp.sidemask == PortSides.TOP)
                    {
                        b.UR.Y = n.Location.Y + HT2(n);
                        b.LL.Y = P.startPoint.Y;
                    }
                    else
                    {
                        b.LL.Y = n.Location.Y - HT2(n);
                        b.UR.Y = P.startPoint.Y + 1;
                    }
                    endp.boxes.Add(b);
                    P.startPoint.X += 1;
                }
                for (orig = e; orig.EdgeType != EdgeTypes.Normal; orig = orig.Original) ;
                if (n == orig.Tail)
                    orig.TailPortClip = false;
                else
                    orig.HeadPortClip = false;
                endp.sidemask = side;
                return;
            }

            if (et == EdgeSplineType.REGULAREDGE) side = PortSides.BOTTOM;
            else side = endp.sidemask;  /* for flat edges */
            //if (pboxfn
            //&& (mask = (*pboxfn)(n, &ED_tail_port(e), side, endp.boxes[0], endp.boxn)))
            //    endp.sidemask = mask;
            //else
            {
                endp.boxes.Add(endp.nb);

                switch (et)
                {
                    case EdgeSplineType.SELFEDGE:
                        /* moving the box UR.Y by + 1 avoids colinearity between
                           port point and box that confuses Proutespline().  it's
                           a bug in Proutespline() but this is the easiest fix. */
                        //assert(0);  /* at present, we don't use beginpath for selfedges */
                        endp.boxes[0].UR.Y = P.startPoint.Y - 1;
                        endp.sidemask = PortSides.BOTTOM;
                        break;
                    case EdgeSplineType.FLATEDGE:
                        if (endp.sidemask == PortSides.TOP)
                            endp.boxes[0].LL.Y = P.startPoint.Y;
                        else
                            endp.boxes[0].UR.Y = P.startPoint.Y;
                        break;
                    case EdgeSplineType.REGULAREDGE:
                        endp.boxes[0].UR.Y = P.startPoint.Y;
                        endp.sidemask = PortSides.BOTTOM;
                        P.startPoint.Y -= 1;
                        break;
                }
            }
        }

        private void EndPath(GraphData g, Path P, EdgeData e, EdgeSplineType et, PathEnd endp, bool merge)
        {
            PortSides side = PortSides.NONE;
            //int(*pboxfn)(node_t * n, port *, int, Box *, int *);

            var n = e.Head;

            P.endPoint = AddPoint(n.Location, e.HeadPoint);
            if (merge)
            {
                /*P.end.theta = M_PI / 2; */
                P.endTheta = ConcentrateSlope(e.Head) + Math.PI;
                //assert(P.end.theta < 2 * M_PI);
                P.endConstrained = true;
            }
            else
            {
                P.endConstrained = false;
            }
            endp.np = P.endPoint;
            if ((et == EdgeSplineType.REGULAREDGE) && (n.NodeType == NodeTypes.Normal) && ((side = e.HeadPortSide) != PortSides.NONE))
            {
                EdgeData orig;
                Box b0 = new Box(), b = endp.nb;
                if ((side & PortSides.TOP) != PortSides.NONE)
                {
                    endp.sidemask = PortSides.TOP;
                    b.LL.Y = Math.Min(b.LL.Y, P.endPoint.Y);
                    endp.boxes.Add(b);
                    P.endPoint.Y += 1;
                }
                else if ((side & PortSides.BOTTOM) != PortSides.NONE)
                {
                    endp.sidemask = PortSides.BOTTOM;
                    if (P.endPoint.X < n.Location.X)
                    { /* go left */
                        b0.LL.X = b.LL.X - 1;
                        /* b0.UR.Y = n.Coordinate.Y - HT2(n); */
                        b0.UR.Y = P.endPoint.Y;
                        b0.UR.X = b.UR.X;
                        b0.LL.Y = n.Location.Y - HT2(n) - g.RankSep / 2;
                        b.UR.X = n.Location.X - n.lw - (FUDGE - 2);
                        b.LL.Y = b0.UR.Y;
                        b.UR.Y = n.Location.Y + HT2(n);
                        b.LL.X -= 1;
                        endp.boxes.Add(b0);
                        endp.boxes.Add(b);
                    }
                    else
                    {
                        b0.LL.X = b.LL.X;
                        b0.UR.Y = P.endPoint.Y;
                        /* b0.UR.Y = n.Coordinate.Y - HT2(n); */
                        b0.UR.X = b.UR.X + 1;
                        b0.LL.Y = n.Location.Y - HT2(n) - g.RankSep / 2;
                        b.LL.X = n.Location.X + n.rw + (FUDGE - 2);
                        b.LL.Y = b0.UR.Y;
                        b.UR.Y = n.Location.Y + HT2(n);
                        b.UR.X += 1;
                        endp.boxes.Add(b0);
                        endp.boxes.Add(b);
                    }
                    P.endPoint.Y -= 1;
                }
                else if ((side & PortSides.LEFT) != PortSides.NONE)
                {
                    endp.sidemask = PortSides.LEFT;
                    b.UR.X = P.endPoint.X;
                    b.UR.Y = n.Location.Y + HT2(n);
                    b.LL.Y = P.endPoint.Y;
                    endp.boxes.Add(b);
                    P.endPoint.X -= 1;
                }
                else
                {
                    endp.sidemask = PortSides.RIGHT;
                    b.LL.X = P.endPoint.X;
                    b.UR.Y = n.Location.Y + HT2(n);
                    b.LL.Y = P.endPoint.Y;
                    endp.boxes.Add(b);
                    P.endPoint.X += 1;
                }
                for (orig = e; orig.EdgeType != EdgeTypes.Normal; orig = orig.Original) ;
                if (n == orig.Head)
                    orig.HeadPortClip = false;
                else
                    orig.TailPortClip = false;
                endp.sidemask = side;
                return;
            }

            if ((et == EdgeSplineType.FLATEDGE) && ((side = e.HeadPortSide) != PortSides.NONE))
            {
                Box b0 = new Box(), b = endp.nb;
                EdgeData orig;
                if ((side & PortSides.TOP) != PortSides.NONE)
                {
                    b.LL.Y = Math.Min(b.LL.Y, P.endPoint.Y);
                    endp.boxes.Add(b);
                    P.endPoint.Y += 1;
                }
                else if ((side & PortSides.BOTTOM) != PortSides.NONE)
                {
                    if (endp.sidemask == PortSides.TOP)
                    {
                        b0.LL.X = b.LL.X - 1;
                        b0.UR.Y = n.Location.Y - HT2(n);
                        b0.UR.X = P.endPoint.X;
                        b0.LL.Y = b0.UR.Y - g.RankSep / 2;
                        b.UR.X = n.Location.X - n.lw - 2;
                        b.LL.Y = b0.UR.Y;
                        b.UR.Y = n.Location.Y + HT2(n);
                        b.LL.X -= 1;
                        endp.boxes.Add(b0);
                        endp.boxes.Add(b);
                    }
                    else
                    {
                        b.UR.Y = Math.Max(b.UR.Y, P.startPoint.Y);
                        endp.boxes.Add(b);
                    }
                    P.endPoint.Y -= 1;
                }
                else if ((side & PortSides.LEFT) != PortSides.NONE)
                {
                    b.UR.X = P.endPoint.X + 1;
                    if (endp.sidemask == PortSides.TOP)
                    {
                        b.UR.Y = n.Location.Y + HT2(n);
                        b.LL.Y = P.endPoint.Y - 1;
                    }
                    else
                    {
                        b.LL.Y = n.Location.Y - HT2(n);
                        b.UR.Y = P.endPoint.Y + 1;
                    }
                    endp.boxes.Add(b);
                    P.endPoint.X -= 1;
                }
                else
                {
                    b.LL.X = P.endPoint.X - 1;
                    if (endp.sidemask == PortSides.TOP)
                    {
                        b.UR.Y = n.Location.Y + HT2(n);
                        b.LL.Y = P.endPoint.Y - 1;
                    }
                    else
                    {
                        b.LL.Y = n.Location.Y - HT2(n);
                        b.UR.Y = P.endPoint.Y;
                    }
                    endp.boxes.Add(b);
                    P.endPoint.X += 1;
                }
                for (orig = e; orig.EdgeType != EdgeTypes.Normal; orig = orig.Original) ;
                if (n == orig.Head)
                    orig.HeadPortClip = false;
                else
                    orig.TailPortClip = false;
                endp.sidemask = side;
                return;
            }

            if (et == EdgeSplineType.REGULAREDGE) side = PortSides.TOP;
            else side = endp.sidemask;  /* for flat edges */
            //TODO: check
            //if (pboxfn
            //&& (mask = (*pboxfn)(n, &ED_head_port(e), side, &endp.boxes[0],&endp.boxn)))
            //    endp.sidemask = mask;
            //else
            {
                endp.boxes.Add(endp.nb);

                switch (et)
                {
                    case EdgeSplineType.SELFEDGE:
                        /* offset of -1 is symmetric w.r.t. beginpath() 
                         * FIXME: is any of this right?  what if self-edge 
                         * doesn't connect from BOTTOM to TOP??? */
                        //assert(0);  /* at present, we don't use endpath for selfedges */
                        endp.boxes[0].LL.Y = P.endPoint.Y + 1;
                        endp.sidemask = PortSides.TOP;
                        break;
                    case EdgeSplineType.FLATEDGE:
                        if (endp.sidemask == PortSides.TOP)
                            endp.boxes[0].LL.Y = P.endPoint.Y;
                        else
                            endp.boxes[0].UR.Y = P.endPoint.Y;
                        break;
                    case EdgeSplineType.REGULAREDGE:
                        endp.boxes[0].LL.Y = P.endPoint.Y;
                        endp.sidemask = PortSides.TOP;
                        P.endPoint.Y += 1;
                        break;
                }
            }
        }
        private const double LB_FUDGE = 0.0001;
        private void limitBoxes(List<Box> boxes, List<Point> pps, int delta)
        {
            int bi, si, splinepi;
            double t;
            Point[] sp = new Point[4];
            int num_div = delta * boxes.Count;

            for (splinepi = 0; splinepi + 3 < pps.Count; splinepi += 3)
            {
                for (si = 0; si <= num_div; si++)
                {
                    t = si / (double)num_div;
                    sp[0] = pps[splinepi];
                    sp[1] = pps[splinepi + 1];
                    sp[2] = pps[splinepi + 2];
                    sp[3] = pps[splinepi + 3];
                    sp[0].X = sp[0].X + t * (sp[1].X - sp[0].X);
                    sp[0].Y = sp[0].Y + t * (sp[1].Y - sp[0].Y);
                    sp[1].X = sp[1].X + t * (sp[2].X - sp[1].X);
                    sp[1].Y = sp[1].Y + t * (sp[2].Y - sp[1].Y);
                    sp[2].X = sp[2].X + t * (sp[3].X - sp[2].X);
                    sp[2].Y = sp[2].Y + t * (sp[3].Y - sp[2].Y);
                    sp[0].X = sp[0].X + t * (sp[1].X - sp[0].X);
                    sp[0].Y = sp[0].Y + t * (sp[1].Y - sp[0].Y);
                    sp[1].X = sp[1].X + t * (sp[2].X - sp[1].X);
                    sp[1].Y = sp[1].Y + t * (sp[2].Y - sp[1].Y);
                    sp[0].X = sp[0].X + t * (sp[1].X - sp[0].X);
                    sp[0].Y = sp[0].Y + t * (sp[1].Y - sp[0].Y);
                    for (bi = 0; bi < boxes.Count; bi++)
                    {
                        /* this tested ok on 64bit machines, but on 32bit we need this FUDGE
                         *     or graphs/directed/records.gv fails */

                        if (sp[0].Y <= boxes[bi].UR.Y + LB_FUDGE && sp[0].Y >= boxes[bi].LL.Y - LB_FUDGE)
                        {
                            if (boxes[bi].LL.X > sp[0].X)
                                boxes[bi].LL.X = sp[0].X;
                            if (boxes[bi].UR.X < sp[0].X)
                                boxes[bi].UR.X = sp[0].X;
                        }
                    }
                }
            }
        }
        private int GetOverlapValue(int i0, int i1, int j0, int j1)
        {
            /* i'll bet there's an elegant way to do this */
            if (i1 <= j0)
                return 0;
            if (i0 >= j1)
                return 0;
            if ((j0 <= i0) && (i0 <= j1))
                return (j1 - i0);
            if ((j0 <= i1) && (i1 <= j1))
                return (i1 - j0);
            return Math.Min(i1 - i0, j1 - j0);
        }

        /*
         * repairs minor errors in the boxpath, such as boxes not joining
         * or slightly intersecting.  it's sort of the equivalent of the
         * audit process in the 5E control program - if you've given up on
         * fixing all the bugs, at least try to engineer around them!
         * in postmodern CS, we could call this "self-healing code."
         *
         * Return 1 on failure; 0 on success.
         */
        private bool CheckPath(List<Box> boxes, Path thepath)
        {
            int bi = 0, i = 0, errs = 0, l = 0, r = 0, d = 0, u = 0;

            /* remove degenerate boxes. */

            if (boxes.Count == 0) return false;

            for (bi = 0; bi < boxes.Count; bi++)
            {
                if (Math.Abs(boxes[bi].LL.Y - boxes[bi].UR.Y) < .01)
                    continue;
                if (Math.Abs(boxes[bi].LL.X - boxes[bi].UR.X) < .01)
                    continue;
                if (i != bi)
                    boxes[i] = boxes[bi];
                i++;
            }
            var boxn = i;


            var _ba = boxes[0];
            if (_ba.LL.X > _ba.UR.X || _ba.LL.Y > _ba.UR.Y)
            {
                return true;
            }
            for (bi = 0; bi < boxn - 1; bi++)
            {
                var ba = boxes[bi];
                var bb = boxes[bi + 1];
                if (bb.LL.X > bb.UR.X || bb.LL.Y > bb.UR.Y)
                {

                    return true;
                }
                l = (ba.UR.X < bb.LL.X) ? 1 : 0;
                r = (ba.LL.X > bb.UR.X) ? 1 : 0;
                d = (ba.UR.Y < bb.LL.Y) ? 1 : 0;
                u = (ba.LL.Y > bb.UR.Y) ? 1 : 0;
                errs = l + r + d + u;

                if (errs > 0)
                {
                    int xy = 0;

                    if (l == 1)
                    {
                        xy = (int)ba.UR.X;
                        ba.UR.X = bb.LL.X;
                        bb.LL.X = xy;
                        l = 0;
                    }
                    else if (r == 1)
                    {
                        xy = (int)ba.LL.X;
                        ba.LL.X = bb.UR.X;
                        bb.UR.X = xy;
                        r = 0;
                    }
                    else if (d == 1)
                    {
                        xy = (int)ba.UR.Y;
                        ba.UR.Y = bb.LL.Y;
                        bb.LL.Y = xy;
                        d = 0;
                    }
                    else if (u == 1)
                    {
                        xy = (int)ba.LL.Y;
                        ba.LL.Y = bb.UR.Y;
                        bb.UR.Y = xy;
                        u = 0;
                    }
                    for (i = 0; i < errs - 1; i++)
                    {
                        if (l == 1)
                        {
                            xy = (int)((ba.UR.X + bb.LL.X) / 2.0 + 0.5);
                            ba.UR.X =
                                bb.LL.X = xy;
                            l = 0;
                        }
                        else if (r == 1)
                        {
                            xy = (int)((ba.LL.X + bb.UR.X) / 2.0 + 0.5);
                            ba.LL.X =
                               bb.UR.X = xy;
                            r = 0;
                        }
                        else if (d == 1)
                        {
                            xy = (int)((ba.UR.Y + bb.LL.Y) / 2.0 + 0.5);
                            ba.UR.Y =
                                bb.LL.Y = xy;
                            d = 0;
                        }
                        else if (u == 1)
                        {
                            xy = (int)((ba.LL.Y + bb.UR.Y) / 2.0 + 0.5);
                            ba.LL.Y =
                            bb.UR.Y = xy;
                            u = 0;
                        }
                    }
                }

                /* check for overlapping boxes */
                int xoverlap = GetOverlapValue((int)ba.LL.X, (int)ba.UR.X, (int)bb.LL.X, (int)bb.UR.X);
                int yoverlap = GetOverlapValue((int)ba.LL.Y, (int)ba.UR.Y, (int)bb.LL.Y, (int)bb.UR.Y);
                if (xoverlap != 0 && yoverlap != 0)
                {
                    if (xoverlap < yoverlap)
                    {
                        if (ba.UR.X - ba.LL.X > bb.UR.X - bb.LL.X)
                        {
                            /* take space from ba */
                            if (ba.UR.X < bb.UR.X)
                                ba.UR.X = bb.LL.X;
                            else
                                ba.LL.X = bb.UR.X;
                        }
                        else
                        {
                            /* take space from bb */
                            if (ba.UR.X < bb.UR.X)
                                bb.LL.X = ba.UR.X;
                            else
                                bb.UR.X = ba.LL.X;
                        }
                    }
                    else
                    {       /* symmetric for y coords */
                        if (ba.UR.Y - ba.LL.Y > bb.UR.Y - bb.LL.Y)
                        {
                            /* take space from ba */
                            if (ba.UR.Y < bb.UR.Y)
                                ba.UR.Y = bb.LL.Y;
                            else
                                ba.LL.Y = bb.UR.Y;
                        }
                        else
                        {
                            /* take space from bb */
                            if (ba.UR.Y < bb.UR.Y)
                                bb.LL.Y = ba.UR.Y;
                            else
                                bb.UR.Y = ba.LL.Y;
                        }
                    }
                }
            }

            if (thepath.startPoint.X < boxes[0].LL.X
                || thepath.startPoint.X > boxes[0].UR.X
                || thepath.startPoint.Y < boxes[0].LL.Y
                || thepath.startPoint.Y > boxes[0].UR.Y)
            {

                if (thepath.startPoint.X < boxes[0].LL.X)
                    thepath.startPoint.X = boxes[0].LL.X;
                if (thepath.startPoint.X > boxes[0].UR.X)
                    thepath.startPoint.X = boxes[0].UR.X;
                if (thepath.startPoint.Y < boxes[0].LL.Y)
                    thepath.startPoint.Y = boxes[0].LL.Y;
                if (thepath.startPoint.Y > boxes[0].UR.Y)
                    thepath.startPoint.Y = boxes[0].UR.Y;

            }
            if (thepath.endPoint.X < boxes[boxn - 1].LL.X
                || thepath.endPoint.X > boxes[boxn - 1].UR.X
                || thepath.endPoint.Y < boxes[boxn - 1].LL.Y
                || thepath.endPoint.Y > boxes[boxn - 1].UR.Y)
            {

                if (thepath.endPoint.X < boxes[boxn - 1].LL.X)
                    thepath.endPoint.X = boxes[boxn - 1].LL.X;
                if (thepath.endPoint.X > boxes[boxn - 1].UR.X)
                    thepath.endPoint.X = boxes[boxn - 1].UR.X;
                if (thepath.endPoint.Y < boxes[boxn - 1].LL.Y)
                    thepath.endPoint.Y = boxes[boxn - 1].LL.Y;
                if (thepath.endPoint.Y > boxes[boxn - 1].UR.Y)
                    thepath.endPoint.Y = boxes[boxn - 1].UR.Y;

            }
            return false;
        }

        /* routesplines:
         * Route a path using the path info in pp. This includes start and end points
         * plus a collection of contiguous boxes contain the terminal points. The boxes
         * are converted into a containing polygon. A shortest path is constructed within
         * the polygon from between the terminal points. If polyline is true, this path
         * is converted to a spline representation. Otherwise, we call the path planner to
         * convert the polyline into a smooth spline staying within the polygon. In both
         * cases, the function returns an array of the computed control points. The number
         * of these points is given in npoints.
         *
         * Note that the returned points are stored in a single array, so the points must be
         * used before another call to this function.
         *
         * During cleanup, the function determines the x-extent of the spline in the box, so
         * the box can be shrunk to the minimum width. The extra space can then be used by other
         * edges. 
         *
         * If a catastrophic error, return null and npoints is 0.
         */
        private List<Point> RouteSplines(Path pp, bool polyline = false)
        {
            var ps = new List<Point>();
            Poly poly = new Poly();
            Poly pl = new Poly(), spl = new Poly();
            int splinepi;
            Point[] eps = new Point[2];
            Point[] evs = new Point[2];
            int edgei, prev, next;
            int pi, bi;
            EdgeData realedge;
            int loopcnt, delta = INIT_DELTA;
            bool unbounded, flip = false;
            List<PEdge> edges = new List<PEdge>();
            for (realedge = pp.data as EdgeData; realedge != realedge.Original && realedge != null && (realedge.EdgeType) != EdgeTypes.Normal; realedge = (realedge.Original)) ;
            if (realedge == null)
            {
                return null;
            }

            var boxes = pp.boxes;

            if (CheckPath(boxes, pp))
                return null;

            var boxn = boxes.Count;


            var polypoints = new Point[boxn * 8];

            if ((boxn > 1) && (boxes[0].LL.Y > boxes[1].LL.Y))
            {
                flip = true;
                for (bi = 0; bi < boxn; bi++)
                {
                    double v = boxes[bi].UR.Y;
                    boxes[bi].UR.Y = -1 * boxes[bi].LL.Y;
                    boxes[bi].LL.Y = -v;
                }
            }
            //else flip = false;

            if ((realedge.Tail) != (realedge.Head))
            {
                /* I assume that the path goes either down only or
                   up - right - down */
                for (bi = 0, pi = 0; bi < boxn; bi++)
                {
                    next = prev = 0;
                    if (bi > 0)
                        prev = (boxes[bi].LL.Y > boxes[bi - 1].LL.Y) ? -1 : 1;
                    if (bi < boxn - 1)
                        next = (boxes[bi + 1].LL.Y > boxes[bi].LL.Y) ? 1 : -1;
                    if (prev != next)
                    {
                        if (next == -1 || prev == 1)
                        {
                            polypoints[pi].X = boxes[bi].LL.X;
                            polypoints[pi++].Y = boxes[bi].UR.Y;
                            polypoints[pi].X = boxes[bi].LL.X;
                            polypoints[pi++].Y = boxes[bi].LL.Y;
                        }
                        else
                        {
                            polypoints[pi].X = boxes[bi].UR.X;
                            polypoints[pi++].Y = boxes[bi].LL.Y;
                            polypoints[pi].X = boxes[bi].UR.X;
                            polypoints[pi++].Y = boxes[bi].UR.Y;
                        }
                    }
                    else if (prev == 0)
                    { /* single box */
                        polypoints[pi].X = boxes[bi].LL.X;
                        polypoints[pi++].Y = boxes[bi].UR.Y;
                        polypoints[pi].X = boxes[bi].LL.X;
                        polypoints[pi++].Y = boxes[bi].LL.Y;
                    }
                    else
                    {
                        if (!(prev == -1 && next == -1))
                        {
                            return null;
                        }
                    }
                }
                for (bi = boxn - 1; bi >= 0; bi--)
                {
                    next = prev = 0;
                    if (bi < boxn - 1)
                        prev = (boxes[bi].LL.Y > boxes[bi + 1].LL.Y) ? -1 : 1;
                    if (bi > 0)
                        next = (boxes[bi - 1].LL.Y > boxes[bi].LL.Y) ? 1 : -1;
                    if (prev != next)
                    {
                        if (next == -1 || prev == 1)
                        {
                            polypoints[pi].X = boxes[bi].LL.X;
                            polypoints[pi++].Y = boxes[bi].UR.Y;
                            polypoints[pi].X = boxes[bi].LL.X;
                            polypoints[pi++].Y = boxes[bi].LL.Y;
                        }
                        else
                        {
                            polypoints[pi].X = boxes[bi].UR.X;
                            polypoints[pi++].Y = boxes[bi].LL.Y;
                            polypoints[pi].X = boxes[bi].UR.X;
                            polypoints[pi++].Y = boxes[bi].UR.Y;
                        }
                    }
                    else if (prev == 0)
                    { /* single box */
                        polypoints[pi].X = boxes[bi].UR.X;
                        polypoints[pi++].Y = boxes[bi].LL.Y;
                        polypoints[pi].X = boxes[bi].UR.X;
                        polypoints[pi++].Y = boxes[bi].UR.Y;
                    }
                    else
                    {
                        if (!(prev == -1 && next == -1))
                        {
                            /* it went badly, e.g. degenerate box in boxlist */
                            return null; /* for correctness sake, it's best to just stop */
                        }
                        polypoints[pi].X = boxes[bi].UR.X;
                        polypoints[pi++].Y = boxes[bi].LL.Y;
                        polypoints[pi].X = boxes[bi].UR.X;
                        polypoints[pi++].Y = boxes[bi].UR.Y;
                        polypoints[pi].X = boxes[bi].LL.X;
                        polypoints[pi++].Y = boxes[bi].UR.Y;
                        polypoints[pi].X = boxes[bi].LL.X;
                        polypoints[pi++].Y = boxes[bi].LL.Y;
                    }
                }
            }
            else
            {
                return null;
            }
            if (flip)
            {
                int i;
                for (bi = 0; bi < boxn; bi++)
                {
                    double v = boxes[bi].UR.Y;
                    boxes[bi].UR.Y = -1 * boxes[bi].LL.Y;
                    boxes[bi].LL.Y = -v;
                }
                for (i = 0; i < pi; i++)
                    polypoints[i].Y *= -1;
            }
            for (bi = 0; bi < boxn; bi++)
            {
                boxes[bi].LL.X = int.MaxValue;
                boxes[bi].UR.X = int.MinValue;
            }
            eps[0].X = pp.startPoint.X;
            eps[0].Y = pp.startPoint.Y;
            eps[1].X = pp.endPoint.X;
            eps[1].Y = pp.endPoint.Y;

            //if (DebugStop)
            //{
            //    DebugStop = false;
            //    Point[] ptx = new Point[]
            //    {
            //        new Point(123.000000,117.000000),
            //        new Point(123.000000,90.000000),
            //        new Point(123.000000,90.000000),
            //        new Point(123.000000,54.000000),
            //        new Point(123.000000,54.000000),
            //        new Point(123.000000,27.000000),
            //        new Point(-64.000000,27.000000),
            //        new Point(-64.000000,54.000000),
            //        new Point(-64.000000,54.000000),
            //        new Point(-64.000000,90.000000),
            //        new Point(-64.000000,90.000000),
            //        new Point(-64.000000,117.000000),
            //    };
            //    for(int i = 0; i < ptx.Length; i++)
            //    {
            //        polypoints[i] = ptx[i];
            //    }
            //    eps[0].X = 23;
            //    eps[0].Y = 116;
            //    eps[1].X = 23;
            //    eps[1].Y = 28;
            //}

            for (int i = 0; i < pi; i++)
            {
                poly.ps.Add(polypoints[i]);
            }

            if (Pshortestpath(poly, eps, pl) < 0)
            {
                return null;
            }

            if (polyline)
            {
                spl = MakePolyLine(pl);
            }
            else
            {

                edges = new List<PEdge>();
                for (edgei = 0; edgei < poly.ps.Count; edgei++)
                {
                    PEdge pe = new PEdge
                    {
                        a = polypoints[edgei],
                        b = polypoints[(edgei + 1) % poly.ps.Count]
                    };
                    edges.Add(pe);
                }
                if (pp.startConstrained)
                {
                    evs[0].X = Math.Cos(pp.startTheta);
                    evs[0].Y = Math.Sin(pp.startTheta);
                }
                else
                    evs[0].X = evs[0].Y = 0;
                if (pp.endConstrained)
                {
                    evs[1].X = -Math.Cos(pp.endTheta);
                    evs[1].Y = -Math.Sin(pp.endTheta);
                }
                else
                    evs[1].X = evs[1].Y = 0;

                if (Proutespline(edges, pl, evs, spl) < 0)
                {
                    return null;
                }

            }

            for (bi = 0; bi < boxn; bi++)
            {
                boxes[bi].LL.X = int.MaxValue;
                boxes[bi].UR.X = int.MinValue;
            }
            unbounded = true;
            for (splinepi = 0; splinepi < spl.ps.Count; splinepi++)
            {
                ps.Add(spl.ps[splinepi]);
            }

            for (loopcnt = 0; unbounded && (loopcnt < LOOP_TRIES); loopcnt++)
            {
                limitBoxes(boxes, ps, delta);

                /* The following check is necessary because if a box is not very 
                 * high, it is possible that the sampling above might miss it.
                 * Therefore, we make the sample finer until all boxes have
                 * valid values. cf. bug 456. Would making sp[] pointfs help?
                 */
                for (bi = 0; bi < boxn; bi++)
                {
                    /* these fp equality tests are used only to detect if the
                     * values have been changed since initialization - ok */
                    if ((boxes[bi].LL.X == int.MaxValue) || (boxes[bi].UR.X == int.MinValue))
                    {
                        delta *= 2; /* try again with a finer interval */
                        if (delta > int.MaxValue / boxn) /* in limitBoxes, boxn*delta must fit in an int, so give up */
                            loopcnt = LOOP_TRIES;
                        break;
                    }
                }
                if (bi == boxn)
                    unbounded = false;
            }
            if (unbounded)
            {
                /* Either an extremely short, even degenerate, box, or some failure with the path
                     * planner causing the spline to miss some boxes. In any case, use the shortest path 
                 * to bound the boxes. This will probably mean a bad edge, but we avoid an infinite
                 * loop and we can see the bad edge, and even use the showboxes scaffolding.
                 */
                Poly polyspl = MakePolyLine(pl);
                limitBoxes(boxes, polyspl.ps, INIT_DELTA);
            }

            return ps;
        }

        private Poly MakePolyLine(Poly line)
        {
            Poly sline = new Poly();
            int i, j;
            int npts = 4 + 3 * (line.ps.Count - 2);

            var ispline = new Point[npts];

            j = i = 0;
            ispline[j + 1] = ispline[j] = line.ps[i];
            j += 2;
            i++;
            for (; i < line.ps.Count - 1; i++)
            {
                ispline[j + 2] = ispline[j + 1] = ispline[j] = line.ps[i];
                j += 3;
            }
            ispline[j + 1] = ispline[j] = line.ps[i];

            sline.ps.AddRange(ispline);
            return sline;
        }
        /* makeregularend:
          * Add box to fill between node and interrank space. Needed because
          * nodes in a given rank can differ in height.
          * for now, regular edges always go from top to bottom
          */
        private Box MakeRegularEnd(Box b, PortSides side, double y)
        {
            Box newb = new Box();
            switch (side)
            {
                case PortSides.BOTTOM:
                    newb = CreateBox(b.LL.X, y, b.UR.X, b.LL.Y);
                    break;
                case PortSides.TOP:
                    newb = CreateBox(b.LL.X, b.UR.Y, b.UR.X, y);
                    break;
            }
            return newb;
        }
        private Splines GetSplinePoints(EdgeData e)
        {
            Splines sp = null;

            if (e != null)
            {
                for (var le = e; null == (sp = le.spl) && le.EdgeType != EdgeTypes.Normal;
                    le = le.Original) ;
            }
            return sp;
        }
        /* side > 0 means right. side < 0 means left */
        private EdgeData TopBound(EdgeData e, int side)
        {
            EdgeData ans = null;

            foreach (var f in e.Tail.FastOutEdges)
            {

                if (side * ((f.Head.Order) - (e.Head.Order)) <= 0)
                    continue;
                if ((f.spl == null)
                    && ((f.Original == null) || (f.Original.spl == null)))
                    continue;
                if ((ans == null)
                    || (side * (ans.Head.Order - f.Head.Order) > 0))
                    ans = f;
            }
            return ans;
        }

        private EdgeData BottomBound(EdgeData e, int side)
        {
            EdgeData ans = null;

            foreach (var f in e.Head.FastInEdges)
            {
                if (side * (f.Tail.Order - e.Tail.Order) <= 0)
                    continue;
                if ((f.spl == null)
                    && ((f.Original == null) || ((f.Original.spl) == null)))
                    continue;
                if ((ans == null)
                    || (side * (ans.Tail.Order - f.Tail.Order) > 0))
                    ans = f;
            }
            return ans;
        }
        private void CompleteRegularPath(Path P, EdgeData first, EdgeData last, PathEnd tendp, PathEnd hendp, List<Box> boxes)
        {
            EdgeData uleft, uright, lleft, lright;
            int i, fb, lb;
            Splines spl = null;
            List<Point> pp = null;

            int pn = 0;

            fb = lb = -1;
            uleft = uright = null;
            uleft = this.TopBound(first, -1);
            uright = this.TopBound(first, 1);
            if (uleft != null)
            {
                if (null == (spl = this.GetSplinePoints(uleft))) return;
                pp = spl.line.list;
                pn = spl.line.list.Count;
            }
            if (uright != null)
            {
                if (null == (spl = this.GetSplinePoints(uright))) return;
                pp = spl.line.list;
                pn = spl.line.list.Count;
            }
            lleft = lright = null;
            lleft = this.BottomBound(last, -1);
            lright = this.BottomBound(last, 1);
            if (lleft != null)
            {
                if (null == (spl = GetSplinePoints(lleft))) return;
                pp = spl.line.list;
                pn = spl.line.list.Count;
            }
            if (lright != null)
            {
                if (null == (spl = GetSplinePoints(lright))) return;
                pp = spl.line.list;
                pn = spl.line.list.Count;
            }
            for (i = 0; i < tendp.boxes.Count; i++)
                this.AddBox(P, tendp.boxes[i]);
            fb = P.boxes.Count + 1;
            lb = fb + boxes.Count - 3;
            for (i = 0; i < boxes.Count; i++)
                this.AddBox(P, boxes[i]);
            for (i = hendp.boxes.Count - 1; i >= 0; i--)
                this.AddBox(P, hendp.boxes[i]);
            this.AdjustRegularPath(P, fb, lb);
        }

        /* adjustregularpath:
         * make sure the path is wide enough.
         * the % 2 was so that in rank boxes would only be grown if
         * they were == 0 while inter-rank boxes could be stretched to a min
         * width.
         * The list of boxes has three parts: tail boxes, path boxes, and head
         * boxes. (Note that because of back edges, the tail boxes might actually
         * belong to the head node, and vice versa.) fb is the index of the
         * first interrank path box and lb is the last interrank path box.
         * If fb > lb, there are none.
         *
         * The second for loop was added by ek long ago, and apparently is intended
         * to guarantee an overlap between adjacent boxes of at least MINW.
         * It doesn't do this, and the ifdef'ed part has the potential of moving
         * a box within a node for more complex paths.
         */
        private void AdjustRegularPath(Path P, int fb, int lb)
        {
            for (int i = fb - 1; i < lb + 1; i++)
            {
                var bp1 = P.boxes[i];
                if ((i - fb) % 2 == 0)
                {
                    if (bp1.LL.X >= bp1.UR.X)
                    {
                        double x = ((bp1.LL.X + bp1.UR.X) / 2);
                        bp1.LL.X = x - HALFMINW;
                        bp1.UR.X = x + HALFMINW;
                    }
                }
                else
                {
                    if (bp1.LL.X + MINW > bp1.UR.X)
                    {
                        double x = ((bp1.LL.X + bp1.UR.X) / 2);
                        bp1.LL.X = x - HALFMINW;
                        bp1.UR.X = x + HALFMINW;
                    }
                }
            }
            for (int i = 0; i < P.boxes.Count - 1; i++)
            {
                var bp1 = P.boxes[i];
                var bp2 = P.boxes[i + 1];
                if (i >= fb && i <= lb && (i - fb) % 2 == 0)
                {
                    if (bp1.LL.X + MINW > bp2.UR.X)
                        bp2.UR.X = bp1.LL.X + MINW;
                    if (bp1.UR.X - MINW < bp2.LL.X)
                        bp2.LL.X = bp1.UR.X - MINW;
                }
                else if (i + 1 >= fb && i < lb && (i + 1 - fb) % 2 == 0)
                {
                    if (bp1.LL.X + MINW > bp2.UR.X)
                        bp1.LL.X = bp2.UR.X - MINW;
                    if (bp1.UR.X - MINW < bp2.LL.X)
                        bp1.UR.X = bp2.LL.X + MINW;
                }
            }
        }
        private Box CreateBox(double x1, double y1, double x2, double y2)
        {

            return new Box() { LL = new Point(x1, y1), UR = new Point(x2, y2) };
        }
        private void MakeFlatBottomEnd(GraphData g, SplineInfoData sp, Path P, VertexData n, EdgeData e, PathEnd endp, bool isBegin)
        {
            var b = endp.nb = this.MaximalBoundingBox(g, sp, n, null, e);
            endp.sidemask = PortSides.BOTTOM;
            if (isBegin)
                this.BeginPath(g, P, e, EdgeSplineType.FLATEDGE, endp, false);
            else
                this.EndPath(g, P, e, EdgeSplineType.FLATEDGE, endp, false);
            b.UR.Y = endp.boxes[endp.boxes.Count - 1].UR.Y;
            b.LL.Y = endp.boxes[endp.boxes.Count - 1].LL.Y;

            b = this.MakeRegularEnd(b, PortSides.BOTTOM, n.Location.Y - g.Ranks[n.Rank].ht2);
            if (b.LL.X < b.UR.X && b.LL.Y < b.UR.Y)
                endp.boxes.Add(b);
        }
        /* make_flat_bottom_edges:
         */
        private void MakeFlatBottomEdge(GraphData g, SplineInfoData sp, Path P, EdgeData e, List<Box> boxes)
        {
            double stepx, stepy, vspace;
            RankData nextr;
            var ps = new List<Point>();
            PathEnd tend = new PathEnd(), hend = new PathEnd();

            var tn = e.Tail;
            var hn = e.Head;
            var r = tn.Rank;
            if (r < g.MaxRank)
            {
                nextr = g.Ranks[(r + 1)];
                vspace = (tn.Location).Y - g.Ranks[r].pht1 -
                    ((nextr.v[0].Location).Y + nextr.pht2);
            }
            else
            {
                vspace = g.RankSep;
            }
            stepx = ((double)(sp.Multisep)) / 2;
            stepy = vspace / 2;

            this.MakeFlatBottomEnd(g, sp, P, tn, e, tend, true);
            this.MakeFlatBottomEnd(g, sp, P, hn, e, hend, false);

            var b = tend.boxes[tend.boxes.Count - 1];
            var nb = new Box();

            nb.LL.X = b.LL.X;
            nb.UR.Y = b.LL.Y;
            nb.UR.X = b.UR.X + (+1) * stepx;
            nb.LL.Y = b.LL.Y - (+1) * stepy;
            boxes.Add(nb);
            var mb = new Box();
            mb.LL.X = tend.boxes[tend.boxes.Count - 1].LL.X;
            mb.UR.Y = nb.LL.Y;
            mb.UR.X = hend.boxes[hend.boxes.Count - 1].UR.X;
            mb.LL.Y = mb.UR.Y - stepy;
            boxes.Add(mb);

            b = hend.boxes[hend.boxes.Count - 1];
            var pb = new Box();
            pb.UR.X = b.UR.X;
            pb.UR.Y = b.LL.Y;
            pb.LL.X = b.LL.X - (+1) * stepx;
            pb.LL.Y = mb.UR.Y;

            boxes.Add(pb);

            for (int j = 0; j < tend.boxes.Count; j++) AddBox(P, tend.boxes[j]);
            for (int j = 0; j < boxes.Count; j++) AddBox(P, boxes[j]);
            for (int j = hend.boxes.Count - 1; j >= 0; j--) AddBox(P, hend.boxes[j]);

            ps = this.RouteSplines(P);

            if (ps != null && ps.Count > 0)
            {
                this.ClipAndInstall(g, e, e.Head, ps);
                P.boxes.Clear();
            }
        }
        /* makeFlatEnd;*/
        private void MakeFlatEnd(GraphData g, SplineInfoData sp, Path P, VertexData n, EdgeData e, PathEnd endp, bool isBegin)
        {
            var b = endp.nb = this.MaximalBoundingBox(g, sp, n, null, e);

            endp.sidemask = PortSides.TOP;

            if (isBegin)
                this.BeginPath(g, P, e, EdgeSplineType.FLATEDGE, endp, false);
            else
                this.EndPath(g, P, e, EdgeSplineType.FLATEDGE, endp, false);

            b.UR.Y = endp.boxes[endp.boxes.Count - 1].UR.Y;
            b.LL.Y = endp.boxes[endp.boxes.Count - 1].LL.Y;

            b = MakeRegularEnd(b, PortSides.TOP, n.Location.Y + g.Ranks[n.Rank].ht2);
            if (b.LL.X < b.UR.X && b.LL.Y < b.UR.Y)
                endp.boxes.Add(b);
        }
        private void MakeFlatEdge(GraphData g, EdgeData e, Path P)
        {
            double stepx, stepy, vspace;
            PortSides tside, hside;
            var ps = new List<Point>();
            PathEnd tend = new PathEnd(), hend = new PathEnd();
            var boxes = g.sd.WorkingBoxes;

            /* Get sample edge; normalize to go from left to right */

            if ((e.EdgeSplineType & EdgeSplineType.BWDEDGE) != EdgeSplineType.NONE)
            {
                e = this.MakeForwardEdge(e);
            }

            tside = e.TailPortSide;
            hside = e.HeadPortSide;
            if (((tside == PortSides.BOTTOM) && (hside != PortSides.TOP)) ||
                ((hside == PortSides.BOTTOM) && (tside != PortSides.TOP)))
            {
                this.MakeFlatBottomEdge(g, g.sd, P, e, boxes);
                return;
            }

            var tn = e.Tail;
            var hn = e.Head;
            var r = tn.Rank;
            if (r > 0)
            {
                var prevr = g.Ranks[r - 1];
                vspace = prevr.v[0].Location.Y - prevr.ht1 - (tn.Location).Y - g.Ranks[r].ht2;
            }
            else
            {
                vspace = g.RankSep;
            }
            stepx = ((double)g.NodeSep) / 2;
            stepy = vspace / 2;

            this.MakeFlatEnd(g, g.sd, P, tn, e, tend, true);
            this.MakeFlatEnd(g, g.sd, P, hn, e, hend, false);

            var b = tend.boxes[tend.boxes.Count - 1];
            var nb = new Box();
            nb.LL.X = b.LL.X;
            nb.LL.Y = b.UR.Y;
            nb.UR.X = b.UR.X + (+1) * stepx;
            nb.UR.Y = b.UR.Y + (+1) * stepy;
            boxes.Add(nb);
            var mb = new Box();

            mb.LL.X = tend.boxes[tend.boxes.Count - 1].LL.X;
            mb.LL.Y = nb.UR.Y;
            mb.UR.X = hend.boxes[hend.boxes.Count - 1].UR.X;
            mb.UR.Y = nb.LL.Y + stepy;

            boxes.Add(mb);
            b = hend.boxes[hend.boxes.Count - 1];
            var pb = new Box();
            pb.UR.X = b.UR.X;
            pb.LL.Y = b.UR.Y;
            pb.LL.X = b.LL.X - (+1) * stepx;
            pb.UR.Y = mb.LL.Y;
            boxes.Add(pb);

            for (int j = 0; j < tend.boxes.Count; j++) AddBox(P, tend.boxes[j]);
            for (int j = 0; j < boxes.Count; j++) AddBox(P, boxes[j]);
            for (int j = hend.boxes.Count - 1; j >= 0; j--) AddBox(P, hend.boxes[j]);

            ps = RouteSplines(P);

            if (ps != null && ps.Count > 0)
            {
                this.ClipAndInstall(g, e, e.Head, ps);
                P.boxes.Clear();
            }
        }
        private int ConvertSidesToPoints(PortSides tail_side, PortSides head_side)
        {
            int[] vertices = new int[] { 12, 4, 6, 2, 3, 1, 9, 8 };
            //the cumulative side value of each node point
            int i, tail_i, head_i;
            tail_i = head_i = -1;
            for (i = 0; i < 8; i++)
            {
                if (head_side == (PortSides)vertices[i])
                {
                    head_i = i;
                    break;
                }
            }
            for (i = 0; i < 8; i++)
            {
                if (tail_side == (PortSides)vertices[i])
                {
                    tail_i = i;
                    break;
                }
            }

            if (tail_i < 0 || head_i < 0)
                return 0;
            else
                return pair_a[tail_i, head_i];
        }
        private Point CreatePointOf(double x, double y) => new Point(x, y);
        private void MakeSelfRightEdge(GraphData g, EdgeData e, double stepx, double sizey)
        {
            int sgn, point_pair;
            double hx, tx, stepy, dx, dy;
            Point tp, hp, np;
            var n = e.Tail;
            var points = new List<Point>();

            stepy = (sizey / 2.0);
            stepy = Math.Max(stepy, 2.0);
            np = n.Location;
            tp = e.TailPoint;
            hp = e.HeadPoint;

            switch (this.parameters.Direction)
            {
                case DotLayoutDirection.LeftToRight:
                case DotLayoutDirection.RightToLeft:
                    hp.Y -= sizey / 2.0;
                    tp.Y += sizey / 2.0;
                    break;
                case DotLayoutDirection.BottomToTop:
                case DotLayoutDirection.TopToBottom:
                    hp.X -= stepx / 2.0;
                    tp.X += stepx / 2.0;
                    break;
            }
            tp.X += np.X;
            tp.Y += np.Y;

            hp.X += np.X;
            hp.Y += np.Y;
            if (tp.Y >= hp.Y) sgn = 1;
            else sgn = -1;
            dx = n.Size.Width;
            dy = 0;
            // certain adjustments are required for some point_pairs in order to improve the 
            // display of the edge path between them
            point_pair = ConvertSidesToPoints(e.TailPortSide, e.HeadPortSide);
            switch (point_pair)
            {
                case 32:
                case 65:
                    if (tp.Y == hp.Y)
                        sgn = -sgn;
                    break;
                default:
                    break;
            }
            tx = Math.Min(dx, 3 * (np.X + dx - tp.X));
            hx = Math.Min(dx, 3 * (np.X + dx - hp.X));
            dx += stepx;
            tx += stepx;
            hx += stepx;
            dy += sgn * stepy;
            points.Add(tp);
            points.Add(CreatePointOf(tp.X + tx / 3, tp.Y + dy));
            points.Add(CreatePointOf(np.X + dx, tp.Y + dy));
            points.Add(CreatePointOf(np.X + dx + tx / 3, (tp.Y + hp.Y) / 2));
            points.Add(CreatePointOf(np.X + dx, hp.Y - dy));
            points.Add(CreatePointOf(hp.X + hx / 3, hp.Y - dy));
            points.Add(hp);
            this.ClipAndInstall(g, e, e.Head, points);
        }
        private void MakeSelfLeftEdge(GraphData g, EdgeData e, double stepx, double sizey)
        {
            int sgn, point_pair;
            double hx, tx, stepy, dx, dy;
            Point tp, hp, np;
            var n = e.Tail;
            var points = new List<Point>();

            stepy = (sizey / 2.0);
            stepy = Math.Max(stepy, 2.0);
            np = n.Location;
            tp = e.TailPoint;
            tp.X += np.X;
            tp.Y += np.Y;
            hp = e.HeadPoint;
            hp.X += np.X;
            hp.Y += np.Y;
            if (tp.Y >= hp.Y) sgn = 1;
            else sgn = -1;
            dx = n.Size.Width;
            dy = 0;
            // certain adjustments are required for some point_pairs in order to improve the 
            // display of the edge path between them
            point_pair = ConvertSidesToPoints(e.TailPortSide, e.HeadPortSide);
            switch (point_pair)
            {
                case 12:
                case 67:
                    if (tp.Y == hp.Y)
                        sgn = -sgn;
                    break;
                default:
                    break;
            }
            tx = Math.Min(dx, 3 * (np.X + dx - tp.X));
            hx = Math.Min(dx, 3 * (np.X + dx - hp.X));

            dx += stepx;
            tx += stepx;
            hx += stepx;
            dy += sgn * stepy;
            points.Add(tp);
            points.Add(CreatePointOf(tp.X - tx / 3, tp.Y + dy));
            points.Add(CreatePointOf(np.X - dx, tp.Y + dy));
            points.Add(CreatePointOf(np.X - dx - tx / 3, (tp.Y + hp.Y) / 2));
            points.Add(CreatePointOf(np.X - dx, hp.Y - dy));
            points.Add(CreatePointOf(hp.X - hx / 3, hp.Y - dy));
            points.Add(hp);

            this.ClipAndInstall(g, e, e.Head, points);
        }
        private void MakeSelfTopEdge(GraphData g, EdgeData e, double sizex, double stepy)
        {
            int sgn, point_pair;
            double hy, ty, stepx, dx, dy;
            Point tp, hp, np;
            var n = e.Tail;
            var points = new List<Point>();
            stepx = (sizex / 2.0);
            stepx = Math.Max(stepx, 2.0);
            np = n.Location;
            tp = e.TailPoint;
            tp.X += np.X;
            tp.Y += np.Y;
            hp = e.HeadPoint;
            hp.X += np.X;
            hp.Y += np.Y;
            if (tp.X >= hp.X) sgn = 1;
            else sgn = -1;
            dy = n.ht / 2.0;
            dx = 0;
            point_pair = ConvertSidesToPoints(e.TailPortSide, e.HeadPortSide);
            switch (point_pair)
            {
                case 15:
                    dx = sgn * (n.rw - (hp.X - np.X) + stepx);
                    break;

                case 38:
                    dx = sgn * (n.lw - (np.X - hp.X) + stepx);
                    break;
                case 41:
                    dx = sgn * (n.rw - (tp.X - np.X) + stepx);
                    break;
                case 48:
                    dx = sgn * (n.rw - (tp.X - np.X) + stepx);
                    break;

                case 14:
                case 37:
                case 47:
                case 51:
                case 57:
                case 58:
                    dx = sgn * ((((n.lw - (np.X - tp.X)) + (n.rw - (hp.X - np.X))) / 3.0));
                    break;
                case 73:
                    dx = sgn * (n.lw - (np.X - tp.X) + stepx);
                    break;
                case 83:
                    dx = sgn * (n.lw - (np.X - tp.X));
                    break;
                case 84:
                    dx = sgn * ((((n.lw - (np.X - tp.X)) + (n.rw - (hp.X - np.X))) / 2.0) + stepx);
                    break;
                case 74:
                case 75:
                case 85:
                    dx = sgn * ((((n.lw - (np.X - tp.X)) + (n.rw - (hp.X - np.X))) / 2.0) + 2 * stepx);
                    break;
                default:
                    break;
            }
            ty = Math.Min(dy, 3 * (np.Y + dy - tp.Y));
            hy = Math.Min(dy, 3 * (np.Y + dy - hp.Y));
            dy += stepy;
            ty += stepy;
            hy += stepy;
            dx += sgn * stepx;
            points.Add(tp);
            points.Add(CreatePointOf(tp.X + dx, tp.Y + ty / 3));
            points.Add(CreatePointOf(tp.X + dx, np.Y + dy));
            points.Add(CreatePointOf((tp.X + hp.X) / 2, np.Y + dy));
            points.Add(CreatePointOf(hp.X - dx, np.Y + dy));
            points.Add(CreatePointOf(hp.X - dx, hp.Y + hy / 3));
            points.Add(hp);
            this.ClipAndInstall(g, e, e.Head, points);

        }
        private void MakeSelfBottomEdge(GraphData g, EdgeData e, double sizex, double stepy)
        {
            int sgn, point_pair;
            double hy, ty, stepx, dx, dy;
            Point tp, hp, np;
            var n = e.Tail;
            var points = new List<Point>();
            stepx = (sizex / 2.0);
            stepx = Math.Max(stepx, 2.0);
            np = n.Location;
            tp = e.TailPoint;
            tp.X += np.X;
            tp.Y += np.Y;
            hp = e.HeadPoint;
            hp.X += np.X;
            hp.Y += np.Y;
            if (tp.X >= hp.X) sgn = 1;
            else sgn = -1;
            dy = n.ht / 2.0;
            dx = 0;
            point_pair = ConvertSidesToPoints(e.TailPortSide, e.HeadPortSide);
            switch (point_pair)
            {
                case 67:
                    sgn = -sgn;
                    break;
                default:
                    break;

            }
            ty = Math.Min(dy, 3 * (np.Y + dy - tp.Y));
            hy = Math.Min(dy, 3 * (np.Y + dy - hp.Y));
            dy += stepy;
            ty += stepy;
            hy += stepy;
            dx += sgn * stepx;
            points.Add(tp);
            points.Add(CreatePointOf(tp.X + dx, tp.Y - ty / 3));
            points.Add(CreatePointOf(tp.X + dx, np.Y - dy));
            points.Add(CreatePointOf((tp.X + hp.X) / 2, np.Y - dy));
            points.Add(CreatePointOf(hp.X - dx, np.Y - dy));
            points.Add(CreatePointOf(hp.X - dx, hp.Y - hy / 3));
            points.Add(hp);
            this.ClipAndInstall(g, e, e.Head, points);

        }
        private void MakeSelfEdge(GraphData g, EdgeData e, double sizex, double sizey)
        {
            if (((e.TailIndex < 0) && (e.HeadIndex < 0)) ||
                    ((e.TailPortSide & PortSides.LEFT) == PortSides.NONE &&
                        (e.HeadPortSide & PortSides.LEFT) == PortSides.NONE &&
                        ((e.TailPortSide != e.HeadPortSide) ||
                        ((e.TailPortSide & (PortSides.TOP | PortSides.BOTTOM)) == PortSides.NONE))))
            {
                this.MakeSelfRightEdge(g, e, sizex, sizey);
            }
            /* self edge with port on left side */
            else if ((e.TailPortSide & PortSides.LEFT) != PortSides.NONE || (e.HeadPortSide & PortSides.LEFT) != PortSides.NONE)
            {
                /* handle L-R specially */
                if ((e.TailPortSide & PortSides.RIGHT) != PortSides.NONE || (e.HeadPortSide & PortSides.RIGHT) != PortSides.NONE)
                {
                    this.MakeSelfTopEdge(g, e, sizex, sizey);
                }
                else
                {
                    this.MakeSelfLeftEdge(g, e, sizex, sizey);
                }
            }

            /* self edge with both ports on top side */
            else if ((e.TailPortSide & PortSides.TOP) != PortSides.NONE)
            {
                this.MakeSelfTopEdge(g, e, sizex, sizey);
            }
            else if ((e.TailPortSide & PortSides.BOTTOM) != PortSides.NONE)
            {
                this.MakeSelfBottomEdge(g, e, sizex, sizey);
            }
        }
        private class Inside
        {
            public VertexData n =null;
            public Box bp = null;
        }
        private bool APPROXEQPT(Point a, Point b, double c)
            => (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y) < c * c;
        /* new_spline:
         * Create and attach a new bezier of size sz to the edge d
         */
        private Bezier NewSpline(EdgeData e)
        {
            var rv = new Bezier();
            while (e.EdgeType != EdgeTypes.Normal &&e!=e.Original)
                e = e.Original;
            if (e.spl == null)
                e.spl = new Splines();
            return e.spl.line = rv;
        }
        private Bezier ClipAndInstall(GraphData g, EdgeData fe, VertexData hn, List<Point> ps, bool ignoreSwap = false)
        {
            var tn = fe.Tail;
            int start, end, i;
            int pn = ps.Count;
            bool clipTail, clipHead;
            EdgeData orig = null;
            Box tbox = new Box(), hbox = new Box();
            Inside inside_context = new Inside();

            var newspl = NewSpline(fe);

            for (orig = fe; orig.EdgeType != EdgeTypes.Normal && orig!=orig.Original; orig = orig.Original) ;

            /* may be a reversed flat edge */
            if (tn.Rank == hn.Rank && (tn.Order > hn.Order))
            {
                var t = hn;
                hn = tn;
                tn = t;
            }
            if (tn == orig.Tail)
            {
                clipTail = orig.TailPortClip;
                clipHead = orig.HeadPortClip;
                tbox = orig.TailPortBox;
                hbox = orig.HeadPortBox;
            }
            else
            { /* fe and orig are reversed */
                clipHead = orig.TailPortClip;
                clipTail = orig.HeadPortClip;
                hbox = orig.TailPortBox;
                tbox = orig.HeadPortBox;
            }

            ///* spline may be interior to node */
            //if (clipTail && tn.Shape != null)
            //{
            //    inside_context.n = tn;
            //    inside_context.bp = tbox;
            //    for (start = 0; start < pn - 4; start += 3)
            //    {
            //        p2.X = ps[start + 3].X - (tn.Location).X;
            //        p2.Y = ps[start + 3].Y - (tn.Location).Y;
            //        if (!tn.Shape.Inside(inside_context, p2))
            //            break;
            //    }
            //    this.ShapeClip0(inside_context, tn, ps, start, true);
            //}
            //else
            {
                start = 0;
            }
            //if (clipHead && hn.Shape != null)
            //{
            //    inside_context.n = hn;
            //    inside_context.bp = hbox;
            //    for (end = pn - 4; end > 0; end -= 3)
            //    {
            //        p2.X = ps[end].X - hn.Location.X;
            //        p2.Y = ps[end].Y - hn.Location.Y;
            //        if (!hn.Shape.Inside(inside_context, p2))
            //            break;
            //    }
            //    this.ShapeClip0(inside_context, hn, ps, end, false);
            //}
            //else
            {
                end = pn - 4;
            }
            for (; start < pn - 4; start += 3)
                if (!APPROXEQPT(ps[start], ps[start + 3], MILLIPOINT))
                    break;
            for (; end > 0; end -= 3)
                if (!APPROXEQPT(ps[end], ps[end + 3], MILLIPOINT))
                    break;
            //arrow_clip(fe, hn, ps, &start, &end, newspl, info);
            if (end == 0) end = start;
            for (i = start; i < end + 4;)
            {
                newspl.list.Add(ps[i++]);
            }
            return newspl;
        }

        /*
		 *  Bezier :
		 *	Evaluate a Bezier curve at a particular parameter value
		 *      Fill in control points for resulting sub-curves if "Left" and
		 *	"Right" are non-null.
		 *
		 */
        private Point CreateBezier(Point[] V, int degree, double t, Point[] Left, Point[] Right)
        {
            int i, j;           /* Index variables      */
            Point[,] Vtemp = new Point[W_DEGREE + 1, W_DEGREE + 1];//[W_DEGREE + 1][W_DEGREE + 1];

            /* Copy control points  */
            for (j = 0; j <= degree; j++)
            {
                Vtemp[0, j] = V[j];
            }

            /* Triangle computation */
            for (i = 1; i <= degree; i++)
            {
                for (j = 0; j <= degree - i; j++)
                {
                    Vtemp[i, j].X =
                    (1.0 - t) * Vtemp[i - 1, j].X + t * Vtemp[i - 1, j + 1].X;
                    Vtemp[i, j].Y =
                    (1.0 - t) * Vtemp[i - 1, j].Y + t * Vtemp[i - 1, j + 1].Y;
                }
            }

            if (Left != null)
                for (j = 0; j <= degree; j++)
                    Left[j] = Vtemp[j, 0];
            if (Right != null)
                for (j = 0; j <= degree; j++)
                    Right[j] = Vtemp[degree - j, j];

            return (Vtemp[degree, 0]);
        }
        private delegate bool InsideDelegate(Inside inside_context, Point p);
        private void BezierClip(Inside inside_context,
                 InsideDelegate inside,
                  Point[] sp, bool left_inside)
        {
            Point[] seg = new Point[4];
            Point[] best = new Point[4];
            Point pt, opt;
            Point[] left, right;
            double low, high, t;
            int id = -1, od = -1;
            bool found;
            int i;

            if (left_inside)
            {
                left = null;
                right = seg;
                pt = sp[0];
                id = 1; //1:low
                od = 2; //2:high
            }
            else
            {
                left = seg;
                right = null;
                pt = sp[3];
                id = 2;
                od = 1;
            }
            found = false;
            low = 0.0;
            high = 1.0;
            int c = 0;
            do
            {
                opt = pt;
                t = (high + low) / 2.0;
                pt = CreateBezier(sp, 3, t, left, right);
                if (inside(inside_context, pt))
                {
                    switch (id)
                    {
                        case 1:
                            low = t;
                            break;
                        case 2:
                            high = t;
                            break;
                    }
                }
                else
                {
                    for (i = 0; i < 4; i++)
                        best[i] = seg[i];
                    found = true;
                    switch (od)
                    {
                        case 1:
                            low = t;
                            break;
                        case 2:
                            high = t;
                            break;
                    }
                }
            } while (c++ < 10 && (Math.Abs(opt.X - pt.X) > 0.5 || Math.Abs(opt.Y - pt.Y) > 0.5));
            if (found)
                for (i = 0; i < 4; i++)
                    sp[i] = best[i];
            else
                for (i = 0; i < 4; i++)
                    sp[i] = seg[i];
        }

        /* shape_clip0:
         * Clip Bezier to node shape using binary search.
         * left_inside specifies that curve[0] is inside the node, else
         * curve[3] is taken as inside.
         * Assumes ND_shape(n) and ND_shape(n).fns.insidefn are non-null.
         * See note on shape_clip.
         */
        private void ShapeClip0(Inside inside_context, VertexData n, List<Point> curve, int start, bool left_inside)
        {
            double save_real_size;
            Point[] c = new Point[4];

            save_real_size = n.rw;
            for (int i = start; i < start + 4; i++)
            {
                c[i - start].X = curve[i].X - n.Location.X;
                c[i - start].Y = curve[i].Y - n.Location.Y;
            }

            this.BezierClip(inside_context, n.Shape.Inside, c, left_inside);

            for (int i = start; i < start + 4; i++)
            {
                curve[i] = new Point(c[i - start].X + n.Location.X, c[i - start].Y + n.Location.Y);
            }
            n.rw = save_real_size;
        }
        private double ConcentrateSlope(VertexData n)
        {
            double s_in, s_out, m_in, m_out;
            int cnt_in, cnt_out;
            Point p = new Point();
            EdgeData e = null;

            s_in = s_out = 0.0;
            for (cnt_in = 0; (e = n.FlatInEdges[cnt_in]) != null; cnt_in++)
                s_in += (e.Tail.Location).X;
            for (cnt_out = 0; (e = n.FlatOutEdges[cnt_out]) != null; cnt_out++)
                s_out += (e.Head.Location).X;
            p.X = n.Location.X - (s_in / cnt_in);
            p.Y = n.Location.Y - ((n.FlatInEdges[0].Tail.Location)).Y;
            m_in = Math.Atan2(p.Y, p.X);
            p.X = (s_out / cnt_out) - n.Location.X;
            p.Y = ((n.FlatOutEdges[0].Head.Location)).Y - n.Location.Y;
            m_out = Math.Atan2(p.Y, p.X);
            return ((m_in + m_out) / 2.0);
        }

        private void AddBox(Path P, Box b)
        {
            if (b.LL.X < b.UR.X && b.LL.Y < b.UR.Y)
            {
                P.boxes.Add(b);
            }
            else
            {

            }
        }

        private bool Between(double a, double b, double c) => (((a) <= (b)) && ((b) <= (c)));
        private bool ClusterVirtualNodeInSide(GraphData cl, VertexData n)
        {
            return (Between((cl.bb).LL.X, (double)(n.Location.X), (cl.bb).UR.X) &&
                Between((cl.bb).LL.Y, (double)(n.Location.Y), (cl.bb).UR.Y));
        }

        /* All nodes belong to some cluster, which may be the root graph.
         * For the following, we only want a cluster if it is a real cluster
         * It is not clear this will handle all potential problems. It seems one
         * could have hcl and tcl contained in cl, which would also cause problems.
         */
        private GraphData GetRealCluster(VertexData n, GraphData g) => (n.ClusterGraph == g ? null : n.ClusterGraph);

        /* returns the cluster of (adj) that interferes with n,
         */
        private GraphData ClusterBound(GraphData g, VertexData n, VertexData adj)
        {
            GraphData rv, cl, tcl, hcl;
            EdgeData orig;

            rv = null;
            if (n.NodeType == NodeTypes.Normal)
                tcl = hcl = n.ClusterGraph;
            else
            {
                orig = (n.FlatOutEdges[0].Original);
                tcl = (orig.Tail.ClusterGraph);
                hcl = (orig.Head.ClusterGraph);
            }
            if ((adj.NodeType) == NodeTypes.Normal)
            {
                cl = GetRealCluster(adj, g);
                if (cl != null && (cl != tcl) && (cl != hcl))
                    rv = cl;
            }
            else
            {
                orig = adj.FastOutEdges[0].Original;
                cl = GetRealCluster(orig.Tail, g);
                if (cl != null && (cl != tcl) && (cl != hcl) && ClusterVirtualNodeInSide(cl, adj))
                    rv = cl;
                else
                {
                    cl = GetRealCluster(orig.Head, g);
                    if (cl != null && (cl != tcl) && (cl != hcl) && ClusterVirtualNodeInSide(cl, adj))
                        rv = cl;
                }
            }
            return rv;
        }

        /* maximal_bbox:
         * Return an initial bounding box to be used for building the
         * beginning or ending of the path of boxes.
         * Height reflects height of tallest node on rank.
         * The extra space provided by FUDGE allows begin/endpath to create a box
         * FUDGE-2 away from the node, so the routing can avoid the node and the
         * box is at least 2 wide.
         */

        private Box MaximalBoundingBox(GraphData g, SplineInfoData sp, VertexData vn, EdgeData ie, EdgeData oe)
        {
            double b, nb;
            GraphData left_cl, right_cl;
            VertexData left, right;
            Box rv = new Box();

            left_cl = right_cl = null;

            /* give this node all the available space up to its neighbors */
            b = vn.Location.X - vn.lw - FUDGE;
            if ((left = Neighbor(g, vn, ie, oe, -1)) != null)
            {
                if ((left_cl = ClusterBound(g, vn, left)) != null)
                    nb = (left_cl.bb).UR.X + sp.Splinesep;
                else
                {
                    nb = left.Location.X + left.Mval;
                    if (left.NodeType == NodeTypes.Normal)
                        nb += g.NodeSep / 2.0;
                    else
                        nb += sp.Splinesep;
                }
                if (nb < b)
                    b = nb;
                rv.LL.X = Math.Round(b);
            }
            else
                rv.LL.X = Math.Min(Math.Round(b), sp.LeftBound);


            b = (vn.Location).X + (vn.rw) + FUDGE;
            if ((right = Neighbor(g, vn, ie, oe, 1)) != null)
            {
                if ((right_cl = ClusterBound(g, vn, right)) != null)
                    nb = (right_cl.bb).LL.X - (double)(sp.Splinesep);
                else
                {
                    nb = (right.Location).X - (right.lw);
                    if ((right.NodeType) == NodeTypes.Normal)
                        nb -= g.NodeSep / 2.0;
                    else
                        nb -= (double)(sp.Splinesep);
                }
                if (nb > b)
                    b = nb;
                rv.UR.X = Math.Round(b);
            }
            else
                rv.UR.X = Math.Max(Math.Round(b), sp.RightBound);


            rv.LL.Y = (vn.Location).Y - g.Ranks[(vn.Rank)].ht1;
            rv.UR.Y = (vn.Location).Y + g.Ranks[(vn.Rank)].ht2;
            return rv;
        }

        private VertexData Neighbor(GraphData g, VertexData vn, EdgeData ie, EdgeData oe, int dir)
        {
            VertexData rv = null;
            var rank = g.Ranks[vn.Rank];

            for (int i = vn.Order + dir; ((i >= 0) && (i < rank.v.Count)); i += dir)
            {
                var n = rank.v[i];

                if (n.NodeType == NodeTypes.Normal)
                {
                    rv = n;
                    break;
                }
                if (PathCross(n, vn, ie, oe) == false)
                {
                    rv = n;
                    break;
                }
            }
            return rv;
        }

        private bool PathCross(VertexData n0, VertexData n1, EdgeData ie1, EdgeData oe1)
        {
            VertexData na, nb;

            var order = (n0.Order > n1.Order);
            if ((n0.FastOutEdges.Count != 1) && (n1.FastOutEdges.Count != 1))
                return false;
            var e1 = oe1;
            if (n0.FastOutEdges.Count == 1 && e1 != null)
            {
                var e0 = n0.FastOutEdges[0];
                for (int cnt = 0; cnt < 2; cnt++)
                {
                    if ((na = e0.Head) == (nb = e1.Head))
                        break;
                    if (order != (na.Order > nb.Order))
                        return true;
                    if ((na.FastOutEdges.Count != 1) || (na.NodeType == NodeTypes.Normal))
                        break;
                    e0 = na.FastOutEdges[0];
                    if ((nb.FastOutEdges.Count != 1) || (nb.NodeType == NodeTypes.Normal))
                        break;
                    e1 = nb.FastOutEdges[0];
                }
            }
            e1 = ie1;
            if (n0.FastInEdges.Count == 1 && e1 != null)
            {
                var e0 = n0.FastInEdges[0];
                for (int cnt = 0; cnt < 2; cnt++)
                {
                    if ((na = e0.Tail) == (nb = e1.Tail))
                        break;
                    if (order != (na.Order > nb.Order))
                        return true;
                    if ((na.FastInEdges.Count != 1) || (na.NodeType == NodeTypes.Normal))
                        break;
                    e0 = na.FastInEdges[0];
                    if ((nb.FastInEdges.Count != 1) || (nb.NodeType == NodeTypes.Normal))
                        break;
                    e1 = nb.FastInEdges[0];
                }
            }
            return false;
        }
    }
}
