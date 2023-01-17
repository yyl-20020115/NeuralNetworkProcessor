using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickGraph
{
    public abstract class SubVertexListGraph<TVertex, TEdge> 
        : ISubVertexListGraph<TVertex,TEdge>
        where TEdge : IEdge<TVertex>
    {
        private IDictionary<TEdge, TEdge> _edgeMap = new Dictionary<TEdge, TEdge>();

        private IDictionary<TVertex, IList<TVertex>> _subsRegistry =
            new Dictionary<TVertex, IList<TVertex>>();
        public IEnumerable<TVertex> TopVertices => this.Vertices.Where(v => this.IsTopVertex(v));
        public IEnumerable<TVertex> SubVertices => this._subsRegistry.Values.SelectMany(v => v);

        public abstract IEnumerable<TVertex> Vertices { get; }

        protected List<TEdge> normalEdges = new List<TEdge>();
        protected List<TEdge> sourceSubEdges = new List<TEdge>();
        protected List<TEdge> targetSubEdges = new List<TEdge>();
        protected List<TEdge> bothSubEdges = new List<TEdge>();
        protected List<TEdge> topEdges = new List<TEdge>();
        public IList<TEdge> NormalEdges => this.normalEdges;
        public IList<TEdge> SourceSubEdges => this.sourceSubEdges;
        public IList<TEdge> TargetSubEdges => this.targetSubEdges;
        public IList<TEdge> BothSubEdges => this.bothSubEdges;
        public IList<TEdge> TopEdges => this.topEdges;

        private IList<TVertex> GetSubsList(TVertex vertex, bool createIfNotExists)
        {
            IList<TVertex> subList = null;
            if (_subsRegistry.TryGetValue(vertex, out subList) || !createIfNotExists)
                return subList;

            subList = new List<TVertex>();
            _subsRegistry[vertex] = subList;
            return subList;
        }
        public abstract bool ContainsVertex(TVertex v);

        public bool AddSubVertex(TVertex container, TVertex sub)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            if (sub == null) throw new ArgumentNullException(nameof(sub));

            if (this.ContainsVertex(container))
            {
                var subs = this.GetSubsList(container, true);
                if (subs != null)
                {
                    subs.Add(sub);
                }
                return true;
            }
            return false;
        }

        public int AddSubVertexRange(TVertex container, IList<TVertex> subs)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            if (subs == null) throw new ArgumentNullException(nameof(subs));
            if (this.ContainsVertex(container))
            {
                var subsList = this.GetSubsList(container, true);
                if (subsList != null)
                {
                    foreach (var s in subs)
                        subsList.Add(s);
                    return subs.Count;
                }
            }
            return 0;
        }

        public TVertex GetContainer(TVertex sub)
        {
            if (sub == null) throw new ArgumentNullException(nameof(sub));
            foreach (var v in this.Vertices)
            {
                var subs = this.GetSubsList(v, false);
                if (subs != null && subs.Contains(sub))
                {
                    return v;
                }
            }
            return default(TVertex);
        }

        public bool IsSubVertex(TVertex vertex)
        {
            if (vertex == null) throw new ArgumentNullException(nameof(vertex));
            foreach (var v in this.Vertices)
            {
                var subs = this.GetSubsList(v, false);
                if (subs != null && subs.Contains(vertex))
                {
                    return true;
                }
            }
            return false;
        }
        public bool IsTopVertex(TVertex vertex)
        {
            return !this.IsSubVertex(vertex);
        }
        public IList<TVertex> GetSubVertices(TVertex container)
        {
            return this.GetSubsList(container, false);
        }

        public int GetSubsCount(TVertex container)
        {
            var subs = this.GetSubsList(container, false);
            return subs != null ? subs.Count : 0;
        }

        public SubEdgeTypes GetEdgeType(TEdge edge)
        {
            SubEdgeTypes type = SubEdgeTypes.NormalEdge;

            if (edge == null) throw new ArgumentNullException(nameof(edge));

            if (edge.Source != null && this.IsSubVertex(edge.Source))
            {
                type |= SubEdgeTypes.SourceSubEdge;
            }
            if (edge.Target != null && this.IsSubVertex(edge.Target))
            {
                type |= SubEdgeTypes.TargetSubEdge;
            }

            return type;
        }
        public virtual bool CreateEdgeWithReflection { get; set; } = false;
        protected virtual TEdge CreateTEdge(TVertex source, TVertex target)
        {
            var t = typeof(TEdge);
            var c = t.GetConstructor(System.Type.EmptyTypes);
            if (c == null)
            {
                throw new InvalidOperationException("Can not find default constructor for type: " + t.FullName);
            }
            var o = c.Invoke(null);
            if (c == null)
            {
                throw new InvalidOperationException("Failed to initialize object for type: " + t.FullName);
            }
            var ps = t.GetProperty("Source");
            if (ps == null)
            {
                throw new InvalidOperationException("Failed to get Source property object for type: " + t.FullName);
            }
            var pt = t.GetProperty("Target");
            if (ps == null)
            {
                throw new InvalidOperationException("Failed to get Target property object for type: " + t.FullName);
            }
            ps.SetValue(o, source);
            pt.SetValue(o, target);
            return (TEdge)o;
        }

        protected virtual void PreAddEdge(TEdge e)
        {
            var type = this.GetEdgeType(e);
            var topEdge = e;
            var Source = e.Source;
            var Target = e.Target;
            switch (type)
            {
                case SubEdgeTypes.NormalEdge:
                    this.normalEdges.Add(e);
                    topEdge = e;
                    break;
                case SubEdgeTypes.SourceSubEdge:
                    this.sourceSubEdges.Add(e);
                    Source = this.GetContainer(e.Source);
                    break;
                case SubEdgeTypes.TargetSubEdge:
                    this.targetSubEdges.Add(e);
                    Target = this.GetContainer(e.Target);
                    break;
                case SubEdgeTypes.BothSubEdge:
                    Source = this.GetContainer(e.Source);
                    Target = this.GetContainer(e.Target);
                    this.bothSubEdges.Add(e);
                    break;
                default:
                    break;
            }

            if(type!= SubEdgeTypes.NormalEdge)
            {
                if (this.CreateEdgeWithReflection)
                {
                    topEdge = this.CreateTEdge(Source, Target);
                }
                else
                {
                    topEdge = (TEdge)e.Clone();
                    topEdge.Source = Source;
                    topEdge.Target = Target;
                }
            }
            this.topEdges.Add(topEdge);

            this._edgeMap[topEdge] = e;
        }

        public virtual TEdge GetRealEdge(TEdge edge)
        {
            return this._edgeMap[edge];
        }
    }
}
