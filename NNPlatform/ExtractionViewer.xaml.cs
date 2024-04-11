using NNP.Core;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Utilities;

namespace NNPlatform
{
    public partial class ExtractionEdit : UserControl
    {
        public DualDictionary<DependencyObject, Extraction> Dict { get; } = new();
        public CaretManager CaretManager { get; } = new CaretManager();
        public ExtractionEdit() => InitializeComponent();
        public void Rebind(FastParser parser) => this.CaretManager.Rebind(parser);
        public void SetResults(IEnumerable<Results> list)
        {
            this.Canvas.Inlines.Clear();
            foreach (var rs in list)
            {
                DocumentBuilder.Build(this.Canvas, rs, this.Dict);
            }
            this.CaretManager.Caret.BindWithScope(this.Canvas);
        }
        protected override void OnInitialized(EventArgs e) 
            => base.OnInitialized(e);
        protected override void OnKeyDown(KeyEventArgs e)
        {
            var ctrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            //Ctrl+Up/Down/Left/Right:Parent/Child/Previous/Next

            switch (e.Key)
            {
                case Key.Space:
                    this.CaretManager.Move(
                        CaretMovingDirection.Child);
                    this.Focus();
                    break;
                case Key.Escape:
                    this.CaretManager.Move(
                        CaretMovingDirection.Parent);
                    this.Focus();
                    break;
                //case Key.Up:
                //    this.CaretManager.Move(
                //        CaretMovingDirection.Up);
                //    break;
                //case Key.Down:
                //    this.CaretManager.Move(
                //        CaretMovingDirection.Down);
                //    break;
                case Key.Left:
                    this.CaretManager.Move(
                        CaretMovingDirection.Previous
                        );
                    this.Focus();
                    break;
                case Key.Right:
                    this.CaretManager.Move(
                        CaretMovingDirection.Next);
                    this.Focus();
                    break;
            }
            base.OnKeyDown(e);
        }

        private void Canvas_MouseEnter(object sender, MouseEventArgs e)
        {

        }

        private void Canvas_MouseLeave(object sender, MouseEventArgs e)
        {
            
        }
    }
}
