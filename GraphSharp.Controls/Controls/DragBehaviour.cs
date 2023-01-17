using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace GraphSharp.Controls
{
    public class DragBehaviour
    {
        public static Dictionary<Control, bool> Dragging = new();
        public static bool GetIsDragging(Control ctrl)
        {
            return Dragging[ctrl];
        }

        public static void SetIsDragging(Control ctrl, bool v)
        {
            Dragging[ctrl] = v;
        }
    }
}