using System;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace QuickGraph
{
    /// <summary>
    /// A tagged edge as value type.
    /// </summary>
    /// <typeparam name="TVertex">type of the vertices</typeparam>
    /// <typeparam name="TTag"></typeparam>
#if !SILVERLIGHT
    [Serializable]
#endif
    [StructLayout(LayoutKind.Auto)]
    [DebuggerDisplay("{Source}->{Target}:{Tag}")]
    public struct STaggedEdge<TVertex, TTag>
        : IEdge<TVertex>
        , ITagged<TTag>
    {
        TVertex source;
        TVertex target;
        TTag tag;

        public STaggedEdge(TVertex source, TVertex target, TTag tag)
        {
            Contract.Requires(source != null);
            Contract.Requires(target != null);

            this.source = source;
            this.target = target;
            this.tag = tag;
            this.TagChanged = null;
        }

        public TVertex Source
        {
            get { return this.source; }
            set { this.source = value; }
        }

        public TVertex Target
        {
            get { return this.target; }
            set { this.target = value; }
        }

        public event EventHandler TagChanged;

        void OnTagChanged(EventArgs e)
        {
            var eh = this.TagChanged;
            if (eh != null)
                eh(this, e);
        }

        public TTag Tag
        {
            get { return this.tag; }
            set
            {
                if (!object.Equals(this.tag, value))
                {
                    this.tag = value;
                    this.OnTagChanged(EventArgs.Empty);
                }
            }
        }

        TVertex IEdge<TVertex>.Source { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        TVertex IEdge<TVertex>.Target { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        /// <summary>
        /// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </returns>
        public override string ToString()
        {
            return String.Format("{0}->{1}:{2}", this.Source, this.Target, this.Tag);
        }

        IEdge<TVertex> IEdge<TVertex>.Clone() => new STaggedEdge<TVertex, TTag>(this.source, this.target, this.tag);
    }
}
