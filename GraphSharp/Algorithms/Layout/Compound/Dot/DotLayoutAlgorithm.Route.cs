using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace GraphSharp.Algorithms.Layout.Compound.Dot
{
    public partial class DotLayoutAlgorithm<TVertex, TEdge, TGraph>
    {
        private const double EPS = 1E-7;
        private const double EPSILON1 = 1E-3;
        private const double EPSILON2 = 1E-6;
        private class TNA
        {
            public double t =  0;
            public Point[] a = new Point[2];
        }
        private class PEdge
        {
            public Point a;
            public Point b;
        }

        /* Proutespline:
         * Given a set of edgen line segments edges as obstacles, a template
         * path input, and endpoint vectors evs, construct a spline fitting the
         * input and endpoing vectors, and return in output.
         * Return 0 on success and -1 on failure, including no memory.
         */
        private int Proutespline(List<PEdge> edges, Poly input,Point[] evs, Poly poly)
        {
            var inps = input.ps.ToArray();
            if (inps.Length == 0) return -1;
            /* generate the splines */
            evs[0] = this.NormalizeVectorPoint(evs[0]);
            evs[1] = this.NormalizeVectorPoint(evs[1]);
            poly.ps.Add(inps[0]);
            var tnas = new TNA[inps.Length];
            for(int i = 0; i < inps.Length; i++)
            {
                tnas[i] = new TNA();
            }

            if (_RouteSpline(tnas, poly.ps, edges, inps, 0,inps.Length, evs[0], evs[1]) == -1)
                return -1;

            return 0;
        }

        private int _RouteSpline(TNA[] tnas, List<Point> ops, List<PEdge> edges, Point[] inps,int inpm, int inpn, Point ev0, Point ev1)
        {
            Point p1 = new Point(), p2 = new Point(), cp1 = new Point(), cp2 = new Point(), p = new Point();
            Point v1 = new Point(), v2 = new Point(), splitv = new Point(), splitv1 = new Point(), splitv2 = new Point();
            double maxd, d, t;
            int maxi, i, spliti;

            tnas[0].t = 0;
            for (i = inpm + 1; i < inpn; i++)
                tnas[i].t = tnas[i - 1].t + GetDistance(inps[i], inps[i - 1]);
            for (i = inpm + 1; i < inpn; i++)
                tnas[i].t /= tnas[inpn - 1].t;
            for (i = inpm + 0; i < inpn; i++)
            {
                tnas[i].a[0] = ScalePoint(ev0, B1(tnas[i].t));
                tnas[i].a[1] = ScalePoint(ev1, B2(tnas[i].t));
            }
            if (MakeSpline(inps, inpm,inpn,tnas, ev0, ev1, ref p1, ref v1, ref p2, ref v2) == -1)
                return -1;
            if (SplineFits(ops, edges, p1, v1, p2, v2, inps,inpm,inpn))
                return 0;
            cp1 = AddPoint(p1, ScalePoint(v1, 1 / 3.0));
            cp2 = SubPoint(p2, ScalePoint(v2, 1 / 3.0));
            for (maxd = -1, maxi = -1, i = inpm + 1; i < inpn - 1; i++)
            {
                t = tnas[i].t;
                p.X = B0(t) * p1.X + B1(t) * cp1.X + B2(t) * cp2.X + B3(t) * p2.X;
                p.Y = B0(t) * p1.Y + B1(t) * cp1.Y + B2(t) * cp2.Y + B3(t) * p2.Y;
                if ((d = GetDistance(p, inps[i])) > maxd)
                {
                    maxd = d;
                    maxi = i;
                }
            }
            if (maxi >= 0)
            {
                spliti = maxi;
                splitv1 = NormalizeVectorPoint(SubPoint(inps[spliti], inps[spliti - 1]));
                splitv2 = NormalizeVectorPoint(SubPoint(inps[spliti + 1], inps[spliti]));
                splitv = NormalizeVectorPoint(AddPoint(splitv1, splitv2));
                this._RouteSpline(tnas, ops, edges, inps, 0, spliti + 1, ev0, splitv);
                this._RouteSpline(tnas, ops, edges, inps, spliti, inpn - spliti, splitv, ev1);

            }
            return 0;
        }

        private int MakeSpline(Point[] inps, int inpm,int inpn,TNA[] tnas, Point ev0,
            Point ev1, ref Point sp0, ref Point sv0,
            ref Point sp1, ref Point sv1)
        {
            Point pt;
            double[,] c = new double[2, 2];
            double[] x = new double[2];
            double det01, det0X, detX1;
            double d01, scale0, scale3;
            int i = 0 ;

            scale0 = scale3 = 0.0;
            c[0, 0] = c[0, 1] = c[1, 0] = c[1, 1] = 0.0;
            x[0] = x[1] = 0.0;
            for (i = inpm; i < inpn; i++) {
                c[0, 0] += DotProduct(tnas[i].a[0], tnas[i].a[0]);
                c[0, 1] += DotProduct(tnas[i].a[0], tnas[i].a[1]);
                c[1, 0] = c[0, 1];
                c[1, 1] += DotProduct(tnas[i].a[1], tnas[i].a[1]);
                pt = SubPoint(inps[i], AddPoint(ScalePoint(inps[inpm], B01(tnas[i].t)),
                    ScalePoint(inps[inpn - 1], B23(tnas[i].t))));
                x[0] += DotProduct(tnas[i].a[0], pt);
                x[1] += DotProduct(tnas[i].a[1], pt);
            }
            det01 = c[0, 0] * c[1, 1] - c[1, 0] * c[0, 1];
            det0X = c[0, 0] * x[1] - c[0, 1] * x[0];
            detX1 = x[0] * c[1, 1] - x[1] * c[0, 1];
            if (Math.Abs(det01) >= 1e-6) {
                scale0 = detX1 / det01;
                scale3 = det0X / det01;
            }
            if (Math.Abs(det01) < 1e-6 || scale0 <= 0.0 || scale3 <= 0.0)
            {
                d01 = GetDistance(inps[inpm], inps[inpn - 1]) / 3.0;
                scale0 = d01;
                scale3 = d01;
            }

            sp0 = inps[inpm];

            sv0 = ScalePoint(ev0, scale0);

            sp1 = inps[inpn - 1];

            sv1 = ScalePoint(ev1, scale3);
            return 0;
        }

        private double GetTotalDistance(Point[] p)
        {
            double rv = 0.0;
            for (int i = 1; i < p.Length; i++)
            {
                rv += Math.Sqrt((p[i].X - p[i - 1].X) * (p[i].X - p[i - 1].X) + (p[i].Y - p[i - 1].Y) * (p[i].Y - p[i - 1].Y));
            }
            return rv;
        }
        private bool SplineFits(List<Point> ops,List<PEdge> edges,  Point pa, Point va, Point pb, Point vb,Point[] inps,int inpm,int inpn )
        {
            Point[] sps = new Point[4];
            double a, b;
            int edgen = edges.Count;
            int pi;
            bool forceflag;
            bool first = true;

            forceflag = inpn == 2;

            a = b = 4;

            for (; ; )
            {
                sps[0].X = pa.X;
                sps[0].Y = pa.Y;
                sps[1].X = pa.X + a * va.X / 3.0;
                sps[1].Y = pa.Y + a * va.Y / 3.0;
                sps[2].X = pb.X - b * vb.X / 3.0;
                sps[2].Y = pb.Y - b * vb.Y / 3.0;
                sps[3].X = pb.X;
                sps[3].Y = pb.Y;

                /* shortcuts (paths shorter than the shortest path) not allowed -
                 * they must be outside the constraint polygon.  this can happen
                 * if the candidate spline intersects the constraint polygon exactly
                 * on sides or vertices.  maybe this could be more elegant, but
                 * it solves the immediate problem. we could also try jittering the
                 * constraint polygon, or computing the candidate spline more carefully,
                 * for example using the path. SCN */

                if (first && (GetTotalDistance(sps) < (GetTotalDistance(inps) - EPSILON1)))
                    return false;
                first = false;

                if (SplineIsInside(edges, sps))
                {
                    for (pi = 1; pi < 4; pi++) {
                        ops[ops.Count - 1] = new Point(sps[pi].X,ops[ops.Count-1].Y);
                        ops.Add(ops[ops.Count - 1]);
                        ops[ops.Count - 1] = new Point(ops[ops.Count - 1].X,sps[pi].Y);
                    }
                    return true;
                }
                if (a == 0 && b == 0)
                {
                    if (forceflag)
                    {
                        for (pi = 1; pi < 4; pi++) {
                            ops[ops.Count - 1] = new Point(sps[pi].X, ops[ops.Count - 1].Y);
                            ops.Add(ops[ops.Count - 1]);
                            ops[ops.Count - 1] = new Point(ops[ops.Count - 1].X, sps[pi].Y);
                        }
                        return true;
                    }
                    break;
                }
                if (a > .01) {
                    a /= 2;
                    b /= 2;
                }
                else
                    a = b = 0;
            }

            return false;
        }
        private double GetDistanceSquare(Point a, Point b) => ((((a).X - (b).X) * ((a).X - (b).X)) + (((a).Y - (b).Y) * ((a).Y - (b).Y)) );
        private bool SplineIsInside(List<PEdge> edges,Point[] sps)
        {
            double[] roots = new double[4];
            int rooti, rootn;
            int ei;
            Point[] lps = new Point[2];
            Point ip = new Point();
            double t, ta, tb, tc, td;
            int edgen = edges.Count;
            for (ei = 0; ei < edgen; ei++)
            {
                lps[0] = edges[ei].a;
                lps[1] = edges[ei].b;
                /* if ((rootn = splineintersectsline (sps, lps, roots)) == 4)
                   return 1; */
                if ((rootn = SplineIntersectsLine(sps, lps, roots)) == 4)
                    continue;
                for (rooti = 0; rooti < rootn; rooti++)
                {
                    if (roots[rooti] < EPSILON2 || roots[rooti] > 1 - EPSILON2)
                        continue;
                    t = roots[rooti];
                    td = t * t * t;
                    tc = 3 * t * t * (1 - t);
                    tb = 3 * t * (1 - t) * (1 - t);
                    ta = (1 - t) * (1 - t) * (1 - t);
                    ip.X = ta * sps[0].X + tb * sps[1].X +
                        tc * sps[2].X + td * sps[3].X;
                    ip.Y = ta * sps[0].Y + tb * sps[1].Y +
                        tc * sps[2].Y + td * sps[3].Y;
                    if (GetDistanceSquare(ip, lps[0]) < EPSILON1 ||
                        GetDistanceSquare(ip, lps[1]) < EPSILON1)
                        continue;
                    return false;
                }
            }
            return true;
        }
        private int SplineIntersectsLine(Point[] sps, Point[] lps,double[] roots)
        {
            double tv, sv, rat;

            double[] scoeff = new double[4];
            double[] xcoeff = new double[2];
            double[] ycoeff = new double[2];
            double[] xroots = new double[3];
            double[] yroots = new double[3];

            int rootn, xrootn, yrootn, i, j;

            xcoeff[0] = lps[0].X;
            xcoeff[1] = lps[1].X - lps[0].X;
            ycoeff[0] = lps[0].Y;
            ycoeff[1] = lps[1].Y - lps[0].Y;
            rootn = 0;
            if (xcoeff[1] == 0)
            {
                if (ycoeff[1] == 0)
                {
                    PointsToCoefficients(sps[0].X, sps[1].X, sps[2].X, sps[3].X, scoeff);
                    scoeff[0] -= xcoeff[0];
                    xrootn = Solve3(scoeff, xroots);
                    PointsToCoefficients(sps[0].Y, sps[1].Y, sps[2].Y, sps[3].Y, scoeff);
                    scoeff[0] -= ycoeff[0];
                    yrootn = Solve3(scoeff, yroots);
                    if (xrootn == 4)
                        if (yrootn == 4)
                            return 4;
                        else
                            for (j = 0; j < yrootn; j++)
                                AddRoot(yroots[j], roots, ref rootn);
                    else if (yrootn == 4)
                        for (i = 0; i < xrootn; i++)
                            AddRoot(xroots[i], roots, ref rootn);
                    else
                        for (i = 0; i < xrootn; i++)
                            for (j = 0; j < yrootn; j++)
                                if (xroots[i] == yroots[j])
                                    AddRoot(xroots[i], roots, ref rootn);
                    return rootn;
                }
                else
                {
                    PointsToCoefficients(sps[0].X, sps[1].X, sps[2].X, sps[3].X, scoeff);
                    scoeff[0] -= xcoeff[0];
                    xrootn = Solve3(scoeff, xroots);
                    if (xrootn == 4)
                        return 4;
                    for (i = 0; i < xrootn; i++)
                    {
                        tv = xroots[i];
                        if (tv >= 0 && tv <= 1)
                        {
                            PointsToCoefficients(sps[0].Y, sps[1].Y, sps[2].Y, sps[3].Y,
                                scoeff);
                            sv = scoeff[0] + tv * (scoeff[1] + tv *
                                (scoeff[2] + tv * scoeff[3]));
                            sv = (sv - ycoeff[0]) / ycoeff[1];
                            if ((0 <= sv) && (sv <= 1))
                                AddRoot(tv, roots, ref rootn);
                        }
                    }
                    return rootn;
                }
            }
            else
            {
                rat = ycoeff[1] / xcoeff[1];
                PointsToCoefficients(sps[0].Y - rat * sps[0].X, sps[1].Y - rat * sps[1].X,
                    sps[2].Y - rat * sps[2].X, sps[3].Y - rat * sps[3].X,
                    scoeff);
                scoeff[0] += rat * xcoeff[0] - ycoeff[0];
                xrootn = Solve3(scoeff, xroots);
                if (xrootn == 4)
                    return 4;
                for (i = 0; i < xrootn; i++)
                {
                    tv = xroots[i];
                    if (tv >= 0 && tv <= 1)
                    {
                        PointsToCoefficients(sps[0].X, sps[1].X, sps[2].X, sps[3].X,
                            scoeff);
                        sv = scoeff[0] + tv * (scoeff[1] +
                            tv * (scoeff[2] + tv * scoeff[3]));
                        sv = (sv - xcoeff[0]) / xcoeff[1];
                        if ((0 <= sv) && (sv <= 1))
                            AddRoot(tv, roots, ref rootn);
                    }
                }
                return rootn;
            }
        }
        private void PointsToCoefficients(double v0, double v1, double v2, double v3, double[] coeff)
        {
            coeff[3] = v3 + 3 * v1 - (v0 + 3 * v2);
            coeff[2] = 3 * v0 + 3 * v2 - 6 * v1;
            coeff[1] = 3 * (v1 - v0);
            coeff[0] = v0;
        }
        private void AddRoot(double root, double[] roots, ref int rootnp)
        {
            if (root >= 0 && root <= 1) {
                roots[rootnp] = root;
                rootnp++;
            }
        }
        private Point NormalizeVectorPoint(Point v)
        {
            double d = v.X * v.X + v.Y * v.Y;
            if (d > 1e-6)
            {
                d = Math.Sqrt(d);
                v.X /= d;
                v.Y /= d;
            }
            return v;
        }
        private Point AddPoint(Point p1, Point p2)
        {
            p1.X += p2.X;
            p1.Y += p2.Y;
            return p1;
        }
        private Point SubPoint(Point p1, Point p2)
        {
            p1.X -= p2.X; p1.Y -= p2.Y;
            return p1;
        }
        private double GetDistance(Point p1, Point p2)
        {
            double dx, dy;

            dx = p2.X - p1.X;
            dy = p2.Y - p1.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
        private Point ScalePoint(Point p, double c)
        {
            p.X *= c;
            p.Y *= c;
            return p;
        }
        private double DotProduct(Point p1, Point p2)
        {
            return p1.X * p2.X + p1.Y * p2.Y;
        }
        private double B0(double t)
        {
            double tmp = 1.0 - t;
            return tmp * tmp * tmp;
        }
        private double B1(double t)
        {
            double tmp = 1.0 - t;
            return 3 * t * tmp * tmp;
        }
        private double B2(double t)
        {
            double tmp = 1.0 - t;
            return 3 * t * t * tmp;
        }
        private double B3(double t)
        {
            return t * t * t;
        }
        private double B01(double t)
        {
            double tmp = 1.0 - t;
            return tmp * tmp * (tmp + 3 * t);
        }
        private double B23(double t)
        {
            double tmp = 1.0 - t;
            return t * t * (3 * tmp + t);
        }
        private bool Aeq0(double x) => (((x) < EPS) && ((x) > -EPS));
        private double Cbrt(double x)=> ((x< 0) ? (-1*Math.Pow(-x, 1.0/3.0)) : Math.Pow(x, 1.0/3.0));
        private int Solve3(double[] coeff, double[] roots)
        {
            double a, b, c, d;
            int rootn, i;
            double p, q, disc, b_over_3a, c_over_a, d_over_a;
            double r, theta, temp, alpha, beta;

            a = coeff[3];
            b = coeff[2];
            c = coeff[1];
            d = coeff[0];
            if (Aeq0(a))
                return Solve2(coeff, roots);
            b_over_3a = b / (3 * a);
            c_over_a = c / a;
            d_over_a = d / a;

            p = b_over_3a * b_over_3a;
            q = 2 * b_over_3a * p - b_over_3a * c_over_a + d_over_a;
            p = c_over_a / 3 - p;
            disc = q * q + 4 * p * p * p;

            if (disc < 0)
            {
                r = .5 * Math.Sqrt(-disc + q * q);
                theta = Math.Atan2(Math.Sqrt(-disc), -q);
                temp = 2 * Cbrt(r);
                roots[0] = temp * Math.Cos(theta / 3);
                roots[1] = temp * Math.Cos((theta + Math.PI + Math.PI) / 3);
                roots[2] = temp * Math.Cos((theta - Math.PI - Math.PI) / 3);
                rootn = 3;
            }
            else
            {
                alpha = .5 * (Math.Sqrt(disc) - q);
                beta = -q - alpha;
                roots[0] = Cbrt(alpha) + Cbrt(beta);
                if (disc > 0)
                    rootn = 1;
                else {
                    roots[1] = roots[2] = -.5 * roots[0];
                    rootn = 3;
                }
            }

            for (i = 0; i < rootn; i++)
                roots[i] -= b_over_3a;

            return rootn;
        }
        private int Solve2(double[] coeff, double[] roots)
        {
            double a, b, c;
            double disc, b_over_2a, c_over_a;

            a = coeff[2];
            b = coeff[1];
            c = coeff[0];
            if (Aeq0(a))
                return Solve1(coeff, roots);
            b_over_2a = b / (2 * a);
            c_over_a = c / a;

            disc = b_over_2a * b_over_2a - c_over_a;
            if (disc < 0)
                return 0;
            else if (disc == 0)
            {
                roots[0] = -b_over_2a;
                return 1;
            }
            else
            {
                roots[0] = -b_over_2a + Math.Sqrt(disc);
                roots[1] = -2 * b_over_2a - roots[0];
                return 2;
            }
        }
        private int Solve1(double[] coeff, double[] roots)
        {
            double a, b;

            a = coeff[1];
            b = coeff[0];
            if (Aeq0(a))
            {
                if (Aeq0(b))
                    return 4;
                else
                    return 0;
            }
            roots[0] = -b / a;
            return 1;
        }
    }
}
