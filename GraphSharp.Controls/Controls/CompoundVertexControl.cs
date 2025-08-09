using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using GraphSharp.Algorithms.Layout.Compound;
using WPFExtensions.AttachedBehaviours;
//using WPFExtensions.AttachedBehaviours;

namespace GraphSharp.Controls
{
    [TemplatePart(Name = CompoundVertexControl.PartInnerCanvas, Type = typeof(FrameworkElement))]
    public class CompoundVertexControl : VertexControl, ICompoundVertexControl
    {
        //Constants for PARTs
        protected const string PartInnerCanvas = "PART_InnerCanvas";

        //PARTs
        protected FrameworkElement _innerCanvas = null;

        protected DragableExpander expander = null;

        public bool IsSubContainer { get; set; } = false;

        public override bool IsClipping => false;
        public override Rect Rect
        {
            get => base.Rect;
            set
            {
                base.Rect = value;
                if (this.IsSubContainer)
                {
                    this.RelocateForSubEdges();
                }
            }
        }
        /// <summary>
        /// Gets the control of the inner canvas.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            //get the control of the inner canvas
            this._innerCanvas = this.Template.FindName(PartInnerCanvas,this) as FrameworkElement ?? this;

            this.expander = this.GetTemplateChild("Expander") as DragableExpander;
        }
        #region Dependency Properties
        public ObservableCollection<VertexControl> Vertices
        {
            get { return (ObservableCollection<VertexControl>)GetValue(VerticesProperty); }
            protected set { SetValue(VerticesPropertyKey, value); }
        }

        public static readonly DependencyProperty VerticesProperty;
        protected static readonly DependencyPropertyKey VerticesPropertyKey =
            DependencyProperty.RegisterReadOnly("Vertices", typeof(ObservableCollection<VertexControl>), typeof(CompoundVertexControl), new UIPropertyMetadata(null));



        public CompoundVertexInnerLayoutType LayoutMode
        {
            get { return (CompoundVertexInnerLayoutType)GetValue(LayoutModeProperty); }
            set { SetValue(LayoutModeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LayoutMode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LayoutModeProperty =
            DependencyProperty.Register("LayoutMode", typeof(CompoundVertexInnerLayoutType), typeof(CompoundVertexControl), new UIPropertyMetadata(CompoundVertexInnerLayoutType.Automatic));

        public bool IsExpanded
        {
            get { return (bool)GetValue(IsExpandedProperty); }
            set { SetValue(IsExpandedProperty, value); }
        }

        public static readonly DependencyProperty IsExpandedProperty =
            DependencyProperty.Register("IsExpanded", typeof(bool), typeof(CompoundVertexControl), new UIPropertyMetadata(true, IsExpanded_PropertyChanged));

        private static void IsExpanded_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var compoundVertexControl =  d as CompoundVertexControl;
            if (d != null)
            {
                if ((bool)e.NewValue)
                {
                    compoundVertexControl.RaiseEvent(new RoutedEventArgs(ExpandedEvent, compoundVertexControl));
                }
                else
                {
                    compoundVertexControl.RaiseEvent(new RoutedEventArgs(CollapsedEvent, compoundVertexControl));
                }
            }

        }

        public Point InnerCanvasOrigo
        {
            get { return (Point)GetValue(InnerCanvasOrigoProperty); }
            set { SetValue(InnerCanvasOrigoProperty, value); }
        }

        // Using a DependencyProperty as the backing store for InnerCanvasOrigo.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty InnerCanvasOrigoProperty =
            DependencyProperty.Register("InnerCanvasOrigo", typeof(Point), typeof(CompoundVertexControl), new UIPropertyMetadata(new Point()));


        static CompoundVertexControl()
        {
            //readonly DPs
            VerticesProperty = VerticesPropertyKey.DependencyProperty;

            //override the StyleKey Property
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CompoundVertexControl), new FrameworkPropertyMetadata(typeof(CompoundVertexControl)));

            //register a class handler for the GraphCanvas.PositionChanged routed event
            //EventManager.RegisterClassHandler(typeof(CompoundVertexControl), GraphCanvas.PositionChangedEvent, new PositionChangedEventHandler(OnPositionChanged));
        }

        protected override void OnPositionChanged(PositionChangedEventArgs args)
        {
            base.OnPositionChanged(args);

            if(this.IsSubContainer && args.Source == this&& !this._activePositionChangeReaction)
            {
                this._activePositionChangeReaction = true;

                this.RelocateForSubEdges();

                this._activePositionChangeReaction = false;
            }
        }
        protected void RelocateForSubEdges()
        {
            var pos = new Point(GraphCanvas.GetX(this) - this.ActualWidth * 0.5, GraphCanvas.GetY(this) - this.ActualHeight * 0.5);
            foreach(VertexControl sp in this.Vertices)
            {
                var op = sp.TranslatePoint(new Point(0.0,0.0), this);
                var np = sp.TranslatePoint(new Point(sp.ActualWidth * 0.5, sp.ActualHeight * 0.5), this);
                if(!this.IsExpanded)
                {
                    np.Y -= (op.Y - this.ActualHeight*0.5);
                }

                sp.Rect = new Rect(pos.X + np.X, pos.Y + np.Y, sp.ActualWidth, sp.ActualHeight);
            }
        }
        public CompoundVertexControl()
        {
            Vertices = new ObservableCollection<VertexControl>();

            this.Expanded += CompoundVertexControl_Expanded;
            this.Collapsed += CompoundVertexControl_Collapsed;
        }

        protected virtual void CompoundVertexControl_Collapsed(object sender, RoutedEventArgs e)
        {

        }

        protected virtual void CompoundVertexControl_Expanded(object sender, RoutedEventArgs e)
        {

        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            if (this.IsSubContainer && !this._activePositionChangeReaction)
            {
                this._activePositionChangeReaction = true;

                this.Rect = new Rect(this.Rect.Location, sizeInfo.NewSize);
                this.RelocateForSubEdges();

                this._activePositionChangeReaction = false;
            }
        }

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (this.expander!=null && this.expander.IsInsideTitle(e.GetPosition(this.expander)))
            {
                this.CaptureMouse();
                DragBehaviour.SetIsDragging(this, true);
            }
            base.OnPreviewMouseLeftButtonDown(e);

        }
        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);
        }
        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonUp(e);
            if(DragBehaviour.GetIsDragging(this))
            {
                DragBehaviour.SetIsDragging(this, false);
                this.ReleaseMouseCapture();
            }
        }
        #endregion

        #region Routed Events
        public static readonly RoutedEvent ExpandedEvent =
            EventManager.RegisterRoutedEvent("Expanded", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(CompoundVertexControl));

        public event RoutedEventHandler Expanded
        {
            add { AddHandler(ExpandedEvent, value); }
            remove { RemoveHandler(ExpandedEvent, value); }
        }

        public static readonly RoutedEvent CollapsedEvent =
            EventManager.RegisterRoutedEvent("Collapsed", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(CompoundVertexControl));

        public event RoutedEventHandler Collapsed
        {
            add { AddHandler(CollapsedEvent, value); }
            remove { RemoveHandler(CollapsedEvent, value); }
        }
        #endregion

        #region ICompoundVertexControl Members

        /// <summary>
        /// Gets the size of the inner canvas control.
        /// </summary>
        public Size InnerCanvasSize
        {
            get
            {
                if (_innerCanvas == null)
                    return new Size();

                return new Size(_innerCanvas.ActualWidth, _innerCanvas.ActualHeight);
            }
        }

        /// <summary>
        /// Gets the 'borderthickness' of the control around the inner canvas.
        /// </summary>
        public Thickness VertexBorderThickness
        {
            get
            {
                var thickness = new Thickness();
                if (_innerCanvas == null)
                    return thickness;

                var innerCanvasPosition = _innerCanvas.TranslatePoint(new Point(), this);
                var innerCanvasSize = InnerCanvasSize;
                var size = new Size(ActualWidth, ActualHeight);

                //calculate the thickness
                thickness.Left = innerCanvasPosition.X;
                thickness.Top = innerCanvasPosition.Y;
                thickness.Right = size.Width - (innerCanvasPosition.X + innerCanvasSize.Width);
                thickness.Bottom = size.Height - (innerCanvasPosition.Y + innerCanvasSize.Height);

                return thickness;
            }
        }

        #endregion
    }
}
