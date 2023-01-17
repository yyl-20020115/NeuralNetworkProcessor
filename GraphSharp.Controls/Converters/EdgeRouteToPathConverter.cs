using System;
using System.Windows.Data;
using System.Windows;
using System.Windows.Media;
using GraphSharp.Controls;
using System.Collections.Generic;
using System.Linq;

namespace GraphSharp.Converters
{
    /// <summary>
    /// Converts the position and sizes of the source and target points, and the route informations
    /// of an edge to a path.
    /// The edge can bend, or it can be straight line.
    /// </summary>
    public class EdgeRouteToPathConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values != null && values.Length == 4 && VertexControl.IsValidRect(values[0]) && VertexControl.IsValidRect(values[1]))
            {
                var sourceRect = (Rect)values[0];
                var targetRect = (Rect)values[1];

                var routePoints = values[2] as Point[];

                var useSpline = false;

                if (values[3] is EdgeControl SelfControl && SelfControl.EnableSplineRouting)
                {
                    var route = SelfControl.GetRoute(sourceRect, targetRect);
                    if (route != null && route.Length > 0)
                    {
                        routePoints = route;
                        useSpline = true;
                    }
                }
                //
                // Create the path
                //
                var p1 = new Point(sourceRect.Location.X, sourceRect.Location.Y);
                var p2 = new Point(targetRect.Location.X, targetRect.Location.Y);

                if (useSpline)
                {
                    bool hasRouteInfo = routePoints != null && routePoints.Length > 0;
                    //append route points
                    var pLast = (hasRouteInfo ? routePoints[routePoints.Length - 1] : p1);
                    var v = pLast - p2;
                    if (v.Length != 0)
                    {
                        v = v / v.Length * 5;

                        var n = new Vector(-v.Y, v.X) * 0.3;

                        return new PathFigureCollection(2)
                        {
                            this.CreateBezierLine(routePoints) , //arrow
                            new PathFigure(p2,
                              new PathSegment[] {
                                  new LineSegment(p2 + v - n, true),
                                  new LineSegment(p2 + v + n, true)
                              },
                              true
                            )
                        };
                    }
                }
                else
                {
                    Vector v = p1 - p2;
                    if (v.X == 0)
                    {
                        v.X = 1;
                    }
                    if (v.Y == 0)
                    {
                        v.Y = 1;
                    }
                    v = v / v.Length * 5;
                    Vector n = new Vector(-v.Y, v.X) * 0.3;

                    var segments = new PathSegment[] { new LineSegment(p2 + v, true) };

                    PathFigureCollection pfc = new PathFigureCollection(2)
                    {
                        new PathFigure(p1, segments, false),
                        new PathFigure(p2,
                        new PathSegment[] {
                                    new LineSegment(p2 + v - n, true),
                                    new LineSegment(p2 + v + n, true)}, true)
                    };

                    return pfc;
                }

            }

            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public PathFigure CreateBezierLine(IList<Point> list)
        {
            PathFigure pf = new PathFigure
            {
                StartPoint = list[0]
            };

            var controls = new List<Point>();

            for (int i = 0; i < list.Count; i++)
            {
                controls.AddRange(CalculateControlPoints(list, i));
            }

            for (int i = 1; i < list.Count; i++)
            {
                BezierSegment bs = new BezierSegment(controls[i * 2 - 1], controls[i * 2], list[i], true);

                bs.IsSmoothJoin = true;

                pf.Segments.Add(bs);
            }

            return pf;
        }

        public List<Point> CalculateControlPoints(IList<Point> list, int n)
        {
            var point = new List<Point>
            {
                new Point(),
                new Point()
            };

            if (n == 0)
            {
                point[0] = list[0];
            }
            else
            {
                point[0] = Average(list[n - 1], list[n]);
            }

            if (n == list.Count - 1)
            {
                point[1] = list[list.Count - 1];
            }
            else
            {
                point[1] = Average(list[n], list[n + 1]);
            }

            Point ave = Average(point[0], point[1]);

            Point sh = Sub(list[n], ave);

            point[0] = Mul(Add(point[0], sh), list[n], 0.6);

            point[1] = Mul(Add(point[1], sh), list[n], 0.6);

            return point;
        }

        public Point Average(Point x, Point y) => new Point((x.X + y.X) / 2, (x.Y + y.Y) / 2);

        public Point Add(Point x, Point y) => new Point(x.X + y.X, x.Y + y.Y);

        public Point Sub(Point x, Point y) => new Point(x.X - y.X, x.Y - y.Y);

        public Point Mul(Point x, Point y, double d)
        {

            Point t = Sub(x, y);

            t = new Point(t.X * d, t.Y * d);

            t = Add(y, t);

            return t;

        }
    }
}