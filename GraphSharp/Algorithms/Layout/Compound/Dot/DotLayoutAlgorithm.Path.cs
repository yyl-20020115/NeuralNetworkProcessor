using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace GraphSharp.Algorithms.Layout.Compound.Dot
{
    public partial class DotLayoutAlgorithm<TVertex, TEdge, TGraph>
    {
        private const int ISCCW = 1;
        private const int ISCW = 2;
        private const int ISON = 3;

        private const int DQ_FRONT = 1;
        private const int DQ_BACK = 2;
        private const double HUGE_VAL = double.MaxValue;

        private class Poly
        {
            public List<Point> ps = new List<Point>();
        }
        private class PointNLink
        {
            public Point pp = new Point(); 
            public PointNLink link = null;
            public override string ToString()
            {
                return "PNL:" + pp.ToString();
            }
        }
        private class XTEdge
        {
            public PointNLink pnl0p = null;
            public PointNLink pnl1p = null;
            public Triangle ltp = null;
            public Triangle rtp = null;
        }
        private class Triangle
        {
            public int mark = 0;
            public XTEdge[] e = new XTEdge[] { new XTEdge(), new XTEdge(), new XTEdge() };
        }
        private class Deque
        {
            public PointNLink[] pnlps;
            public int fpnlpi = 0;
            public int lpnlpi = 0;
            public int apex = 0;
        }

        /* Pshortestpath:
         * Find a shortest path contained in the polygon polyp going between the
         * points supplied in eps. The resulting polyline is stored in output.
         * Return 0 on success, -1 on bad input, -2 on memory allocation problem. 
         */
        private int Pshortestpath(Poly polyp, Point[] eps, Poly output)
        {
            int pi, minpi;
            double minx;
            Point p1, p2, p3;
            int trii, trij, ftrii, ltrii;
            int ei;

            PointNLink[] epnls = new PointNLink[2] { new PointNLink(), new PointNLink() };

            PointNLink lpnlp = null, rpnlp = null, pnlp = null;
            Triangle trip;
            int splitindex;
            //pointnlink_t pnls;
            var pnls =new PointNLink[polyp.ps.Count];
            var pnlps = new PointNLink[polyp.ps.Count];
            int pnll = 0;

            var tris = new List<Triangle>();
            int tril = 0, trin = 0;

            var dq = new Deque() { pnlps = new PointNLink[polyp.ps.Count * 2] };

            var ops = new List<Point>();
            /* make space */

            dq.fpnlpi = dq.pnlps.Length  / 2;
            dq.lpnlpi = dq.fpnlpi - 1;

            /* make sure polygon is CCW and load pnls array */
            for (pi = 0, minx = HUGE_VAL, minpi = -1; pi < polyp.ps.Count; pi++)
            {
                if (minx > polyp.ps[pi].X)
                {
                    minx = polyp.ps[pi].X;
                    minpi = pi;
                }
                   
            }

            if (minpi < 0) return -1;
            p2 = polyp.ps[minpi];
            p1 = polyp.ps[((minpi == 0) ? polyp.ps.Count - 1 : minpi - 1)];
            p3 = polyp.ps[((minpi == polyp.ps.Count - 1) ? 0 : minpi + 1)];
            if (((p1.X == p2.X && p2.X == p3.X) && (p3.Y > p2.Y)) ||
            ConterClockwise(p1, p2, p3) != ISCCW)
            {
                for (pi = polyp.ps.Count - 1; pi >= 0; pi--)
                {
                    if (pi < polyp.ps.Count - 1
                    && polyp.ps[pi].X == polyp.ps[pi + 1].X
                    && polyp.ps[pi].Y == polyp.ps[pi + 1].Y)
                        continue;
                    pnls[pnll] = new PointNLink
                    {
                        pp = polyp.ps[pi],
                        link = pnls[pnll % polyp.ps.Count]
                    };
                    pnlps[pnll] = pnls[pnll];
                    pnll++;
                }
            }
            else
            {
                for (pi = 0; pi < polyp.ps.Count; pi++)
                {
                    if (pi > 0 && polyp.ps[pi].X == polyp.ps[pi - 1].X &&
                    polyp.ps[pi].Y == polyp.ps[pi - 1].Y)
                        continue;
                    pnls[pnll] = new PointNLink
                    {
                        pp = polyp.ps[pi],
                        link = pnls[pnll % polyp.ps.Count]
                    };
                    pnlps[pnll] = pnls[pnll];
                    pnll++;
                }
            }


            /* generate list of triangles */
            Triangulate(tris,ref tril, ref trin, pnlps, pnll);

            /* connect all pairs of triangles that share an edge */
            for (trii = 0; trii < tril; trii++)
                for (trij = trii + 1; trij < tril; trij++)
                    ConnectTriangles(tris,trii, trij);

            /* find first and last triangles */
            for (trii = 0; trii < tril; trii++)
                if (PointInTriangle(tris, trii, eps[0]))
                    break;
            if (trii == tril)
            {
                return -1;
            }
            ftrii = trii;
            for (trii = 0; trii < tril; trii++)
                if (PointInTriangle(tris, trii, eps[1]))
                    break;
            if (trii == tril)
            {
                return -1;
            }
            ltrii = trii;

            /* mark the strip of triangles from eps[0] to eps[1] */
            if (!MarkTrianglePath(tris, ftrii, ltrii))
            {
                /* a straight line is better than failing */
                ops.Add(eps[0]);
                ops.Add(eps[1]);

                output.ps.AddRange(ops);

                return 0;
            }

            /* if endpoints in same triangle, use a single line */
            if (ftrii == ltrii)
            {
                ops.Add(eps[0]);
                ops.Add(eps[1]);

                output.ps.AddRange(ops);
                return 0;
            }

            /* build funnel and shortest path linked list (in add2dq) */
            epnls[0].pp = eps[0]; epnls[0].link = null;
            epnls[1].pp = eps[1]; epnls[1].link = null;
            AddToDq(dq,DQ_FRONT, epnls[0]);
            dq.apex = dq.fpnlpi;
            trii = ftrii;
            while (trii != -1)
            {
                trip = tris[trii];
                trip.mark = 2;

                /* find the left and right points of the exiting edge */
                for (ei = 0; ei < 3; ei++)
                    if (trip.e[ei].rtp != null && trip.e[ei].rtp.mark == 1)
                        break;
                if (ei == 3)
                {       /* in last triangle */
                    if (ConterClockwise(eps[1], dq.pnlps[dq.fpnlpi].pp,
                        dq.pnlps[dq.lpnlpi].pp) == ISCCW) {
                        rpnlp = epnls[1];
                    }
                    else {
                        lpnlp = epnls[1];
                        rpnlp = dq.pnlps[dq.lpnlpi];
                    }
                }
                else
                {
                    pnlp = trip.e[(ei + 1) % 3].pnl1p;
                    if (ConterClockwise(trip.e[ei].pnl0p.pp, pnlp.pp,
                        trip.e[ei].pnl1p.pp) == ISCCW)
                    {
                        lpnlp = trip.e[ei].pnl1p;
                        rpnlp = trip.e[ei].pnl0p;
                    }
                    else {
                        lpnlp = trip.e[ei].pnl0p;
                        rpnlp = trip.e[ei].pnl1p;
                    }
                }

                /* update deque */
                if (trii == ftrii)
                {
                    AddToDq(dq,DQ_BACK, lpnlp);
                    AddToDq(dq, DQ_FRONT, rpnlp);
                }
                else
                {
                    if (dq.pnlps[dq.fpnlpi] != rpnlp
                    && dq.pnlps[dq.lpnlpi] != rpnlp)
                    {
                        /* add right point to deque */
                        splitindex = FindDqSplit(dq, rpnlp);
                        SplitDq(dq, DQ_BACK, splitindex);
                        AddToDq(dq, DQ_FRONT, rpnlp);
                        /* if the split is behind the apex, then reset apex */
                        if (splitindex > dq.apex)
                            dq.apex = splitindex;
                    }
                    else
                    {
                        /* add left point to deque */
                        splitindex = FindDqSplit(dq, lpnlp);
                        SplitDq(dq, DQ_FRONT, splitindex);
                        AddToDq(dq, DQ_BACK, lpnlp);
                        /* if the split is in front of the apex, then reset apex */
                        if (splitindex < dq.apex)
                            dq.apex = splitindex;
                    }
                }
                trii = -1;
                for (ei = 0; ei < 3; ei++)
                    if (trip.e[ei].rtp!=null && trip.e[ei].rtp.mark == 1)
                    {
                        trii = tris.IndexOf(trip.e[ei].rtp);
                        break;
                    }
            }

            for (pi = pi - 1, pnlp = epnls[1]; pnlp!=null; pi--, pnlp = pnlp.link)
                ops.Add(pnlp.pp);
            ops.Reverse();
            output.ps.AddRange(ops);

            return 0;
        }
        /* triangulate polygon */
        private void Triangulate(List<Triangle> tris, ref int tril, ref int trin, PointNLink[] _pnlps, int _pnln)
        {
            int pnli, pnlip1, pnlip2;
            if (_pnln > 3)
            {
                for (pnli = 0; pnli < _pnln; pnli++)
                {
                    pnlip1 = (pnli + 1) % _pnln;
                    pnlip2 = (pnli + 2) % _pnln;
                    if (IsDiagonal(pnli, pnlip2, _pnlps, _pnln))
                    {
                        LoadTriangle(tris, ref tril,ref trin,_pnlps[pnli], _pnlps[pnlip1], _pnlps[pnlip2]);
                        for (pnli = pnlip1; pnli < _pnln - 1; pnli++)
                            _pnlps[pnli] = _pnlps[pnli + 1];
                        Triangulate(tris, ref tril, ref trin, _pnlps, _pnln - 1);
                        return;
                    }
                }
            }
            else
            {
                LoadTriangle(tris, ref tril, ref trin, _pnlps[0], _pnlps[1], _pnlps[2]);
            }
        }
        /* check if (i, i + 2) is a diagonal */
        private bool IsDiagonal(int pnli, int pnlip2, PointNLink[] _pnlps, int _pnln)
        {
            int pnlip1, pnlim1, pnlj, pnljp1;
            bool res;
            /* neighborhood test */
            pnlip1 = (pnli + 1) % _pnln;
            pnlim1 = (pnli + _pnln - 1) % _pnln;
            /* If P[pnli] is a convex vertex [ pnli+1 left of (pnli-1,pnli) ]. */
            if (ConterClockwise(_pnlps[pnlim1].pp, _pnlps[pnli].pp, _pnlps[pnlip1].pp) == ISCCW)
                res =
                    (ConterClockwise(_pnlps[pnli].pp, _pnlps[pnlip2].pp, _pnlps[pnlim1].pp) ==  ISCCW)
                    && (ConterClockwise(_pnlps[pnlip2].pp, _pnlps[pnli].pp, _pnlps[pnlip1].pp) == ISCCW);
            /* Assume (pnli - 1, pnli, pnli + 1) not collinear. */
            else
                res = (ConterClockwise(_pnlps[pnli].pp, _pnlps[pnlip2].pp,
                       _pnlps[pnlip1].pp) == ISCW);
            if (!res)
                return false;

            /* check against all other edges */
            for (pnlj = 0; pnlj < _pnln; pnlj++)
            {
                pnljp1 = (pnlj + 1) % _pnln;
                if (!((pnlj == pnli) || (pnljp1 == pnli) ||
                      (pnlj == pnlip2) || (pnljp1 == pnlip2)))
                    if (Intersects(_pnlps[pnli].pp, _pnlps[pnlip2].pp,
                           _pnlps[pnlj].pp, _pnlps[pnljp1].pp))
                        return false;
            }
            return true;
        }

        private void LoadTriangle(List<Triangle> tris,ref int tril,ref int trin,PointNLink pnlap, PointNLink pnlbp,PointNLink pnlcp)
        {
            /* make space */
            if (tril >= trin)
            {
                for(int i = 0; i < 20; i++)
                {
                    tris.Add(new Triangle());
                }
            }

            var trip = tris[tril++];
            trip.mark = 0;
            trip.e[0].pnl0p = pnlap;
            trip.e[0].pnl1p = pnlbp;
            trip.e[0].rtp = null;
            trip.e[1].pnl0p = pnlbp;
            trip.e[1].pnl1p = pnlcp;
            trip.e[1].rtp = null;
            trip.e[2].pnl0p = pnlcp;
            trip.e[2].pnl1p = pnlap;
            trip.e[2].rtp = null;
            for (int ei = 0; ei < 3; ei++)
                trip.e[ei].ltp = trip;
        }

        /* connect a pair of triangles at their common edge (if any) */
        private void ConnectTriangles(List<Triangle> tris, int tri1, int tri2)
        {
            Triangle tri1p;
            Triangle tri2p;
            int ei, ej;

            for (ei = 0; ei < 3; ei++)
            {
                for (ej = 0; ej < 3; ej++)
                {
                    tri1p = tris[tri1];
                    tri2p = tris[tri2];
                    if ((tri1p.e[ei].pnl0p.pp == tri2p.e[ej].pnl0p.pp &&
                     tri1p.e[ei].pnl1p.pp == tri2p.e[ej].pnl1p.pp) ||
                    (tri1p.e[ei].pnl0p.pp == tri2p.e[ej].pnl1p.pp &&
                     tri1p.e[ei].pnl1p.pp == tri2p.e[ej].pnl0p.pp)) {
                        tri1p.e[ei].rtp = tri2p;
                        tri2p.e[ej].rtp = tri1p;
                    }
                }
            }
        }
        /* find and mark path from trii, to trij */
        private bool MarkTrianglePath(List<Triangle> tris,int trii, int trij)
        {
            if (tris[trii].mark!=0)
                return false;
            tris[trii].mark = 1;
            if (trii == trij)
                return true;
            for (int ei = 0; ei < 3; ei++)
                if (tris[trii].e[ei].rtp !=null&&
                    MarkTrianglePath(tris,tris.IndexOf(tris[trii].e[ei].rtp), trij))
                    return true;
            tris[trii].mark = 0;
            return false;
        }
        /* add a new point to the deque, either front or back */
        private void AddToDq(Deque dq, int side, PointNLink pnlp)
        {
            if (side == DQ_FRONT)
            {
                if (dq.lpnlpi - dq.fpnlpi >= 0)
                    pnlp.link = dq.pnlps[dq.fpnlpi];   /* shortest path links */
                dq.fpnlpi--;
                dq.pnlps[dq.fpnlpi] = pnlp;
            }
            else
            {
                if (dq.lpnlpi - dq.fpnlpi >= 0)
                    pnlp.link = dq.pnlps[dq.lpnlpi];   /* shortest path links */
                dq.lpnlpi++;
                dq.pnlps[dq.lpnlpi] = pnlp;
            }
        }
        private void SplitDq(Deque dq, int side, int index)
        {
            if (side == DQ_FRONT)
                dq.lpnlpi = index;
            else
                dq.fpnlpi = index;
        }
        private int FindDqSplit(Deque dq,PointNLink pnlp)
        {
            int index;

            for (index = dq.fpnlpi; index < dq.apex; index++)
                if (ConterClockwise(dq.pnlps[index + 1].pp, dq.pnlps[index].pp, pnlp.pp) ==
                    ISCCW)
                    return index;
            for (index = dq.lpnlpi; index > dq.apex; index--)
                if (ConterClockwise(dq.pnlps[index - 1].pp, dq.pnlps[index].pp, pnlp.pp) ==
                    ISCW)
                    return index;
            return dq.apex;
        }
        /* ccw test: CCW, CW, or co-linear */
        private int ConterClockwise(Point p1p, Point p2p, Point p3p)
        {
            double d;

            d = ((p1p.Y - p2p.Y) * (p3p.X - p2p.X)) -
            ((p3p.Y - p2p.Y) * (p1p.X - p2p.X));
            return (d > 0) ? ISCCW : ((d < 0) ? ISCW : ISON);
        }
        /* line to line intersection */
        private bool Intersects(Point pap, Point pbp,Point pcp, Point pdp)
        {
            int ccw1, ccw2, ccw3, ccw4;

            if (ConterClockwise(pap, pbp, pcp) == ISON || ConterClockwise(pap, pbp, pdp) == ISON ||
            ConterClockwise(pcp, pdp, pap) == ISON || ConterClockwise(pcp, pdp, pbp) == ISON)
            {
                if (Between(pap, pbp, pcp) || Between(pap, pbp, pdp) ||
                    Between(pcp, pdp, pap) || Between(pcp, pdp, pbp))
                    return true;
            }
            else
            {
                ccw1 = (ConterClockwise(pap, pbp, pcp) == ISCCW) ? 1 : 0;
                ccw2 = (ConterClockwise(pap, pbp, pdp) == ISCCW) ? 1 : 0;
                ccw3 = (ConterClockwise(pcp, pdp, pap) == ISCCW) ? 1 : 0;
                ccw4 = (ConterClockwise(pcp, pdp, pbp) == ISCCW) ? 1 : 0;
                return (ccw1 ^ ccw2)!=0 && (ccw3 ^ ccw4) != 0;
            }
            return false;
        }
        /* is pbp between pap and pcp */
        private bool Between(Point pap, Point pbp, Point pcp)
        {
            Point p1 = new Point();
            Point p2 = new Point();

            p1.X = pbp.X - pap.X; p1.Y = pbp.Y - pap.Y;
            p2.X = pcp.X - pap.X; p2.Y = pcp.Y - pap.Y;
            if (ConterClockwise(pap, pbp, pcp) != ISON)
                return false;
            return (p2.X * p1.X + p2.Y * p1.Y >= 0) &&
                (p2.X * p2.X + p2.Y * p2.Y <= p1.X * p1.X + p1.Y * p1.Y);
        }
        private bool PointInTriangle(List<Triangle> tris, int trii, Point pp)
        {
            int ei, sum;

            for (ei = 0, sum = 0; ei < 3; ei++)
                if (ConterClockwise(tris[trii].e[ei].pnl0p.pp,
                    tris[trii].e[ei].pnl1p.pp, pp) != ISCW)
                    sum++;
            return (sum == 3 || sum == 0);
        }
    }
}
