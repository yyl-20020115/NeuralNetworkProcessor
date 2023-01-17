using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using QuickGraph;

namespace GraphSharp.Algorithms.Layout.Compound.Dot
{
    public partial class DotLayoutAlgorithm<TVertex, TEdge, TGraph>
    {
        private double FontSize = 12.0;
        private string FontFamily = "Default";

        //private IEnumerable<string> InstalledFontFamilies => new System.Drawing.Text.InstalledFontCollection()
        //    .Families.Select(f => f.Name);
        /// <summary>
        /// <list type="ul">
        /// <listheader>
        /// Initializes the algorithm, and the following things:
        /// </listheader>
        /// <item>the nodes sizes (of the compound vertices)</item>
        /// <item>the thresholds for the convergence</item>
        /// <item>random initial positions (if the position is not null)</item>
        /// <item>remove the 'tree-nodes' from the root graph (level 0)</item>
        /// </list>
        /// </summary>
        /// <param name="vertexSizes">The dictionary of the inner canvas sizes 
        /// of the compound vertices.</param>
        /// <param name="vertexBorders">The dictionary of the border thickness of
        /// the compound vertices.</param>
        /// <param name="layoutTypes">The dictionary of the layout types of 
        /// the compound vertices.</param>
        private void Init(IDictionary<TVertex, Size> vertexSizes, IDictionary<TVertex, Thickness> vertexBorders)
        {
            this.InitSimpleVertices();
            this.InitCompoundVertices();
            this.InitEdges();
        }
        private Size MeasureText(string text, double fontSize, string fontFamily)
        {
            var formattedText = new FormattedText(
                text,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(fontFamily.ToString()),
                fontSize,Brushes.Black,
                1.0);//Aussme 96DPI
            return new Size(formattedText.WidthIncludingTrailingWhitespace,formattedText.Height);
        }
        /// <summary>
        /// Initializes the data of the simple vertices.
        /// </summary>
        /// <param name="vertexSizes">Dictionary of the vertex sizes.</param>
        private void InitSimpleVertices()
        {
            if(_compoundGraph is ISubVertexListGraph<TVertex,TEdge> g)
            {
                foreach (var vertex in g.TopVertices)
                {
                    //create the information container for this simple vertex
                    var dataContainer = new SimpleVertexData(vertex)
                    {
                        Graph = _rootGraph,
                        Parent = _rootCompoundVertex,
                        IsTop = true,
                        Size = this.MeasureText(vertex.ToString(),this.FontSize,this.FontFamily)
                    };
                    _simpleVertexDatas[vertex] = dataContainer;
                    _allVertexDatas[vertex] = dataContainer;
                    _topVertexDatas[vertex] = dataContainer;
                    _rootCompoundVertex.Children.Add(dataContainer);

                    var subs = g.GetSubVertices(vertex);
                    if (subs != null)
                    {
                        foreach(var sub in subs)
                        {
                            var subContainer = new SimpleVertexData(sub)
                            {
                                Graph = _rootGraph,
                                Parent = dataContainer,
                                IsTop = false,
                                Size = this.MeasureText(sub.ToString(),this.FontSize,this.FontFamily)
                            };
                            Size cs = subContainer.Size;
                            Size fs = dataContainer.Size;
                            if (this.Parameters.Direction == DotLayoutDirection.LeftToRight
                                  || this.Parameters.Direction == DotLayoutDirection.RightToLeft)
                            {
                                fs.Width += cs.Width;
                                fs.Height = Math.Max(fs.Height, cs.Height);
                            }
                            else if (this.Parameters.Direction == DotLayoutDirection.TopToBottom
                                || this.Parameters.Direction == DotLayoutDirection.BottomToTop)
                            {
                                fs.Height += cs.Height;
                                fs.Width = Math.Max(fs.Width, cs.Width);
                            }
                            dataContainer.Size = fs;
                            dataContainer.Subs.Add(subContainer);
                            _allVertexDatas[sub] = subContainer;
                        }
                    }
                }
            }
            else
            {
                foreach (var vertex in _compoundGraph.SimpleVertices)
                {
                    //create the information container for this simple vertex
                    var dataContainer = new SimpleVertexData(vertex)
                    {
                        Graph = _rootGraph,
                        Parent = _rootCompoundVertex,
                        IsTop = true,
                        Size = this.MeasureText(vertex.ToString(),this.FontSize,this.FontFamily)
                    };
                    _simpleVertexDatas[vertex] = dataContainer;
                    _allVertexDatas[vertex] = dataContainer;
                    _topVertexDatas[vertex] = dataContainer;
                    _rootCompoundVertex.Children.Add(dataContainer);
                }
            }
        }
        /// <summary>
        /// Initializes the data of the compound vertices.
        /// </summary>
        /// <param name="vertexBorders">Dictionary of the border thicknesses.</param>
        /// <param name="vertexSizes">Dictionary of the vertex sizes.</param>
        /// <param name="layoutTypes">Dictionary of the layout types.</param>
        /// <param name="movableParentUpdateQueue">The compound vertices with fixed layout
        /// should be added to this queue.</param>
        private void InitCompoundVertices()
        {
            //compound vertices have no subs
            foreach (var vertex in _compoundGraph.CompoundVertices)
            {
                //create the information container for this compound vertex
                var dataContainer = new CompoundVertexData(vertex)
                {
                    Graph = _rootGraph,
                    IsTop = true
                };

                var parent = this._compoundGraph.GetParent(vertex);

                if (parent == null)
                {
                    dataContainer.Parent = _rootCompoundVertex;
                    _rootCompoundVertex.Children.Add(dataContainer);
                }
                _compoundVertexDatas[vertex] = dataContainer;
                _allVertexDatas[vertex] = dataContainer;
                _topVertexDatas[vertex] = dataContainer;
            }
            foreach(var kv in this._compoundVertexDatas)
            {
                //add the datas of the childrens
                var children = _compoundGraph.GetChildrenVertices(kv.Key);
                var childrenData = children.Select(v => _allVertexDatas[v]);

                Size fs = new Size();
                foreach (var child in childrenData)
                {
                    child.Parent = kv.Value;
                    Size cs = this.MeasureText(child.Vertex.ToString(), this.FontSize, this.FontFamily);

                    if(this.Parameters.Direction == DotLayoutDirection.LeftToRight
                        || this.Parameters.Direction == DotLayoutDirection.RightToLeft)
                    {
                        fs.Width += cs.Width;
                        fs.Height = Math.Max(fs.Height, cs.Height);
                    }
                    else if(this.Parameters.Direction == DotLayoutDirection.TopToBottom
                        || this.Parameters.Direction == DotLayoutDirection.BottomToTop)
                    {
                        fs.Height += cs.Height;
                        fs.Width = Math.Max(fs.Width, cs.Width);
                    }
                }
                kv.Value.Size = fs;
                kv.Value.Children.Clear();
                kv.Value.Children.AddRange(childrenData);
            }
        }
        private void InitEdges()
        {
            foreach (var edge in _compoundGraph.Edges)
            {
                var e = new EdgeData(edge);

                this._allEdgeDatas.Add(e);

                if (this._allVertexDatas.TryGetValue(edge.Source, out var vo))
                {
                    vo.RealOutEdges.Add(e);
                    e.Tail = vo;
                }
                else
                {

                }
                if (this._allVertexDatas.TryGetValue(edge.Target, out var vi))
                {
                    vi.RealInEdges.Add(e);
                    e.Head = vi;
                }
                else
                {

                }
            }
            if (_compoundGraph is ISubVertexListGraph<TVertex, TEdge> g)
            {
                foreach (var edge in g.TopEdges)
                {
                    var real = g.GetRealEdge(edge);

                    TVertex realSource = real.Source;
                    TVertex realTarget = real.Target;

                    var ns = g.TopVertices.Where(v => v == edge.Source).FirstOrDefault();
                    var nt = g.TopVertices.Where(v => v == edge.Target).FirstOrDefault();

                    var e = new EdgeData(edge);

                    this._topEdgeDatas.Add(e);

                    if (this._allVertexDatas.TryGetValue(ns, out var vo))
                    {
                        int i = 0;
                        var nss = g.GetSubVertices(ns);
                        if(nss!=null && nss.Count > 0)
                        {
                            i = nss.IndexOf(realSource) + 1;
                        }
                        vo.TopOutEdges.Add(e);
                        e.Tail = vo;
                        e.TailIndex = i;
                    }

                    if (this._allVertexDatas.TryGetValue(nt, out var vi))
                    {
                        int i = 0;
                        var nts = g.GetSubVertices(nt);
                        if (nts != null && nts.Count > 0)
                        {
                            i = nts.IndexOf(realTarget) + 1;
                        }
                        vi.TopInEdges.Add(e);
                        e.Head = vi;
                        e.HeadIndex = i;
                    }
                }
            }

        }
    }
}