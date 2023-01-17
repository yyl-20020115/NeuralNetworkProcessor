using NeuralNetworkProcessor.Core;
using NeuralNetworkProcessor.ZRF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Utilities;

namespace NNPlatform
{
    public class Caret
    {
        public TextBlock Main { get; protected set; }
        public TextBlock Scope { 
            get 
            {
                var m = this.Main.Parent as FrameworkElement;
                while (m != null)
                {
                    m = m.Parent as FrameworkElement;
                    if (m is TextBlock block) return block;
                }
                return null;
            } 
        }
        public DualDictionary<UIElement, Extraction> Dict { get; protected set; }
        public HashSet<Symbol> Tops { get; } = new();
        public HashSet<Symbol> Atoms { get; } = new();
        public Caret(DualDictionary<UIElement, Extraction> dict = null)
        {
            this.Dict = dict ?? new();
        }
        public Caret Bind(TextBlock Main)
        {
            this.Main = Main;
            return this;
        }
        public Caret MoveTo(TextBlock Main)
        {
            if (this.Main != null)
            {
                this.Main.Background = Brushes.White;
            }
            if (this.Scope is TextBlock Scope)
            {
                Scope.Background = Brushes.White;
            }

            if((this.Main = Main) != null)
            {
                this.Main.Background = Brushes.Yellow;
            }
            if (this.Scope is TextBlock NewScope)
            {
                NewScope.Background = Brushes.DarkGray;
            }
            return this;
        }
        public Caret BindWithScope(TextBlock Scope)
        {
            this.MoveTo(Scope);
            this.MoveToLogicalChild(true);
            return this;
        }
        public Caret MoveToLogicalParent()
        {

            return this;
        }
        public Caret MoveToLogicalChild(bool firstOrLast = true)
        {
            
            return this;
        }
        public Caret MoveToLogicalNext(bool loop = false)
        {
            if (this.Main !=null && this.Scope!=null)
            {
                var list = this.Scope.Inlines.Select(
                    il=>(il as InlineUIContainer).Child).Cast<TextBlock>().ToList();
                var i = list.IndexOf(this.Main);
                if(loop && i == list.Count - 1)
                {
                    //this.Move(this.Scope, list[0]);
                }
                else
                {
                    //if (i >= 0 && i < list.Count - 1) 
                        //this.Move(this.Scope, list[i + 1]);
                }
            }
            return this;
        }
        public Caret MoveToLogicalPrevious(bool loop = false)
        {
            if (this.Main != null && this.Scope != null)
            {
                var list = this.Scope.Inlines.Select(il => (il as InlineUIContainer).Child).Cast<TextBlock>().ToList();
                var i = list.IndexOf(this.Main);
                if (loop && i == 0)
                {
                    //this.Move(this.Scope, list[^1]);
                }
                else
                {
                    //if (i >= 1 && i < list.Count)
                    //    this.Move(this.Scope, list[i - 1]);
                }
            }
            return this;
        }

        public Caret MoveToPreviousLine()
        {
            //TODO:
            return this;
        }
        public Caret MoveToNextLine()
        {
            //TODO:
            return this;
        }
        public Caret MoveToLeftSibling()
        {
            //TODO:
            return this;
        }
        public Caret MoveToRightSibling()
        {
            //TODO:
            return this;
        }
    }

    [Flags]
    public enum CaretMovingDirection : int
    {
        Still = 0,
        Previous = 1,
        Next = 2,
        Parent = 3,
        Child = 4,
        Up = 5,
        Down = 6,
        Left = 7,
        Right = 8,
        FirstFlag = 0x0,
        FirstOrLoop = 0x10
    }

    public class CaretManager
    {
        public List<TextBlock> Trace { get; protected set; } = new();
        public int Index { get; protected set; } = -1;
        public Caret Caret { get; protected set; } = new();
        public CaretManager(Caret Caret = null)
        {
            this.Bind(Caret);
        }
        public CaretManager Bind(Caret Caret)
        {
            this.Caret = Caret ?? new Caret();
            if (this.Caret.Main != null)
            {
                this.Trace.Add(this.Caret.Main);
                this.Index++;
            }
            return this;
        }
        public CaretManager Move(CaretMovingDirection direction)
        {
            var FirstOrLoop = (direction & CaretMovingDirection.FirstOrLoop) == CaretMovingDirection.FirstOrLoop;
            direction &= ~CaretMovingDirection.FirstOrLoop;
            var p = this.Caret.Main;
            switch (direction)
            {
                case CaretMovingDirection.Parent:
                    this.Caret.MoveToLogicalParent();
                    break;
                case CaretMovingDirection.Child:
                    this.Caret.MoveToLogicalChild();
                    break;
                case CaretMovingDirection.Previous:
                    this.Caret.MoveToLogicalPrevious(FirstOrLoop);
                    break;
                case CaretMovingDirection.Next:
                    this.Caret.MoveToLogicalNext(FirstOrLoop);
                    break;
                case CaretMovingDirection.Up:
                    this.Caret.MoveToPreviousLine();
                    break;
                case CaretMovingDirection.Down:
                    this.Caret.MoveToNextLine();
                    break;
                case CaretMovingDirection.Left:
                    this.Caret.MoveToLeftSibling();
                    break;
                case CaretMovingDirection.Right:
                    this.Caret.MoveToRightSibling();
                    break;
            }
            var d = this.Caret.Main;
            if (d != p)
            {
                if (this.Index < this.Trace.Count - 1)
                {
                    this.Trace = this.Trace.ToArray()[0..(this.Index + 1)].ToList();
                }
                this.Trace.Add(d);
            }
            return this;
        }
        public CaretManager NavigateBackward()
        {
            if (this.Index > 0)
            {
                this.Index--;
                this.Caret.Bind(this.Trace[this.Index]);
            }
            return this;
        }
        public CaretManager NavigateForeward()
        {
            if (this.Index < this.Trace.Count - 1)
            {
                this.Index++;
                this.Caret.Bind(this.Trace[this.Index]);
            }
            return this;
        }

        public CaretManager Rebind(FastParser parser)
        {
            this.Caret.Tops.Clear();
            this.Caret.Atoms.Clear();
            this.Caret.Tops.UnionWith(parser.Tops.Select(t => t.Definition).Cast<Symbol>());
            this.Caret.Atoms.UnionWith(parser.Atoms.Select(a => a.Definition).Cast<Symbol>());
            return this;
        }
    }
}
