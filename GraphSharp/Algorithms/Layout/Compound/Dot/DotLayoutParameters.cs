namespace GraphSharp.Algorithms.Layout.Compound.Dot
{
    public enum DotLayoutDirection
    {
        LeftToRight,
        RightToLeft,
        TopToBottom,
        BottomToTop,
    }
    public class DotLayoutParameters : LayoutParametersBase
    {
        public bool UseEdgeLabels = false;
        public double RankSep = 1.0;
        public int MaxIterators = int.MaxValue;
        public int SearchSize = 30;
        protected DotLayoutDirection direction = DotLayoutDirection.LeftToRight;
        public DotLayoutDirection Direction
        {
            get => this.direction;
            set
            {
                this.direction = value;
                this.NotifyPropertyChanged(nameof(Direction));
            }
        }
    }
}
