using System.Windows.Controls;

namespace GraphSharp.Controls
{
    public class GSItemsControl : ItemsControl
    {
        public GSItemsControl()
        {

        }
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is VertexControl ? false : base.IsItemItsOwnContainerOverride(item);
        }
    }
}
