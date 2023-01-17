using System.Windows;
using System.Windows.Controls;
using GraphSharp.Helpers;
using System;
using System.Windows.Input;

namespace GraphSharp.Controls
{
    /// <summary>
    /// Logical representation of a vertex.
    /// </summary>
    public class VertexControl : Control, IPoolObject, IDisposable
    {
        protected static int _LastIndex = 0;

        public readonly int _Index = _LastIndex++;

        public static Rect CompleteInvalidRect = new Rect(double.NaN, double.NaN, double.NaN, double.NaN);
        public static bool IsCompleteInvalidRect(Rect rect) => double.IsNaN(rect.Left) && double.IsNaN(rect.Top) && double.IsNaN(rect.Width) && double.IsNaN(rect.Height);

        public static bool IsValidRect(object o) => o is Rect rect && IsValidRect(rect);
        public static bool IsValidRect(Rect rect) => !double.IsNaN(rect.Left) && !double.IsNaN(rect.Top) && !double.IsNaN(rect.Width) && !double.IsNaN(rect.Height);

        protected bool _activePositionChangeReaction = false;

        public virtual bool IsClipping => this.CompoundParent == null;

        public object Vertex
        {
            get { return GetValue(VertexProperty); }
            set { SetValue(VertexProperty, value); }
        }

        public static readonly DependencyProperty VertexProperty =
            DependencyProperty.Register("Vertex", typeof(object), typeof(VertexControl), new UIPropertyMetadata(null));

        public GraphCanvas RootCanvas
        {
            get { return (GraphCanvas)GetValue(RootCanvasProperty); }
            set { SetValue(RootCanvasProperty, value); }
        }

        public CompoundVertexControl CompoundParent { get; set; }
        public static readonly DependencyProperty RootCanvasProperty =
            DependencyProperty.Register("RootCanvas", typeof(GraphCanvas), typeof(VertexControl), new UIPropertyMetadata(null));

        public static readonly DependencyProperty RectProperty =
            DependencyProperty.Register("Rect", typeof(Rect), typeof(VertexControl), new UIPropertyMetadata(CompleteInvalidRect));

        public virtual Rect Rect
        {
            get { return (Rect)GetValue(RectProperty); }
            set
            {
                SetValue(RectProperty, value);
                GraphCanvas.SetX(this, value.Left);
                GraphCanvas.SetY(this, value.Top);
            }
        }

        static VertexControl()
        {
            //override the StyleKey Property
            DefaultStyleKeyProperty.OverrideMetadata(typeof(VertexControl), new FrameworkPropertyMetadata(typeof(VertexControl)));
            EventManager.RegisterClassHandler(typeof(VertexControl), GraphCanvas.PositionChangedEvent, new PositionChangedEventHandler(OnPositionChanged));
        }
        #region Other Properties
        #endregion


        #region IPoolObject Members

        public void Reset()
        {
            Vertex = null;
        }

        public void Terminate()
        {
            //nothing to do, there are no unmanaged resources
        }

        public event DisposingHandler Disposing;

        public void Dispose()
        {
            if (Disposing != null)
                Disposing(this);
        }
        #endregion

        public override string ToString() => string.Format("{0}", this.Vertex);

        protected virtual void OnPositionChanged(PositionChangedEventArgs args)
        {
            //NOTICE:the rect maybe partial valid
            if (!IsCompleteInvalidRect(this.Rect) && !this._activePositionChangeReaction)
            {
                this._activePositionChangeReaction = true;

                this.Rect = new Rect(GraphCanvas.GetX(this), GraphCanvas.GetY(this), this.ActualWidth, this.ActualHeight);

                this._activePositionChangeReaction = false;
            }
        }
        private static void OnPositionChanged(object sender, PositionChangedEventArgs args)
        {
            if(sender is VertexControl c)
            {
                c.OnPositionChanged(args);
            }
        }
    }
}