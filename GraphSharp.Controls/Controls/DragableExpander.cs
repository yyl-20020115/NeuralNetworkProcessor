using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace GraphSharp.Controls
{
    public class DragableExpander :Expander
    {
        private const string ExpanderToggleButtonTemplateName = "HeaderSite";

        private ToggleButton headerSite = null;

        public DragableExpander()
        {

        }
        public virtual bool IsInsideTitle(Point p)
        {
            if (this.headerSite != null)
            {
                var r = new Rect(0.0, 0.0, this.ActualWidth, this.headerSite.ActualHeight);
                return (r.Contains(p) && !this.IsInsideToggleRect(p));
            }
            else
            {
                return new Rect(0.0, 0.0, this.ActualWidth, this.ActualHeight).Contains(p);
            }
        }
        public virtual bool IsInsideToggleRect(Point p)
        {
            if (this.headerSite != null)
            {
                var o = this.headerSite.TranslatePoint(new Point(0.0,0.0), this);
                var h = this.headerSite.ActualHeight;
                var r = new Rect(o.X, o.Y, h, h);

                if (r.Contains(p))
                {
                    return true;
                }
            }
            return false;
        }
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if((this.headerSite = this.GetTemplateChild(ExpanderToggleButtonTemplateName) as ToggleButton)!=null)
            {
                

            }
        }
    }
}
