using SParser;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace SEdit
{
    /// <summary>
    /// SPEdit.xaml 
    /// </summary>
    public partial class SPEdit : UserControl
    {
        public interface SPTextElement
        {
            bool IsEditable { get; }
            IResultElement Result { get; }
            SPTextElement TextParent { get;}
            string SPText { get; }
            List<SPTextElement> Children { get; }
            SPTextElement AddChild(SPTextElement child);
            SPTextElement RemoveChild(SPTextElement child);
        }
        public class SPTextBlock :TextBlock, SPTextElement
        {
            public virtual IResultElement Result { get; set; }
            public virtual SPTextElement TextParent { get; protected set; }
            public virtual List<SPTextElement> Children { get; } = new List<SPTextElement>();

            public virtual string SPText { get => this.Display.Text; set => this.Display.Text = value; }

            public virtual TextBox Input { get; } = null;
            public virtual TextBlock Display { get; } = null;
            protected bool isEditing = false;
            public virtual bool IsEditable { get; set; }
            public virtual bool IsEditing
            {
                get => this.isEditing;
                set
                {
                    if (this.isEditing = value)
                    {
                        this.Display.Visibility = Visibility.Hidden;
                        this.Input.Visibility = Visibility.Visible;
                        this.Input.Focus();
                        this.Input.SelectAll();
                    }
                    else
                    {
                        this.Display.Visibility = Visibility.Visible;
                        this.Input.Visibility = Visibility.Hidden;
                    }
                }
            }
            public SPTextBlock(SPTextElement parent = null)
            {
                this.TextParent = parent;
                this.Input = new TextBox() { Visibility= Visibility.Hidden };
                this.Display = new TextBlock() { Visibility = Visibility.Visible } ;
            }

            public virtual SPTextElement AddChild(SPTextElement child)
            {
                if (child != null)
                {
                    if(child is Inline ci)
                    {
                        this.Display.Inlines.Add(ci);
                    }
                    this.Children.Add(child);
                }
                return this;
            }
            public virtual SPTextElement RemoveChild(SPTextElement child)
            {
                if (child != null)
                {
                    if (child is Inline ci)
                    {
                        this.Display.Inlines.Remove(ci);
                    }
                    this.Children.Remove(child);
                }
                return this;
            }
        }
        public class SPSpan : Span, SPTextElement
        {
            public virtual bool IsEditable => false;

            public virtual IResultElement Result { get; set; }
            public virtual SPTextElement TextParent { get; protected set; }
            public virtual List<SPTextElement> Children { get; } = new List<SPTextElement>();
            public virtual string SPText => string.Empty;

            public SPSpan(SPTextElement parent,SPTextElement child = null,TextPointer pointer = null)
                :base(child as Inline,pointer)
            {
                this.TextParent = parent;
                this.AddChild(child);
            }
            public virtual SPTextElement AddChild(SPTextElement child)
            {
                if (child != null)
                {
                    if (child is Inline inline)
                    {
                        this.Inlines.Add(inline);
                    }
                    this.Children.Add(child);
                }
                return this;
            }
            public virtual SPTextElement RemoveChild(SPTextElement child)
            {
                if (child != null)
                {
                    if (child is Inline inline)
                    {
                        this.Inlines.Remove(inline);
                    }
                    this.Children.Remove(child);
                }
                return this;
            }

        }
        public class SPRun : Run, SPTextElement
        {
            public virtual bool IsEditable => false;
            public virtual IResultElement Result { get; set; }
            public virtual SPTextElement TextParent { get; protected set; }

            public virtual List<SPTextElement> Children => null;
            public virtual string SPText { get => this.Text; set => this.Text = value; }

            public SPRun(SPTextElement parent, string text = null, TextPointer pointer = null)
                :base(text??string.Empty,pointer)
            {
                this.TextParent = parent;
            }
            public virtual SPTextElement AddChild(SPTextElement child) => this;
            public virtual SPTextElement RemoveChild(SPTextElement child) => this;
           
        }
        public class SPLineBreak : LineBreak, SPTextElement
        {
            public virtual bool IsEditable => false;
            public virtual IResultElement Result { get; set; }
            public virtual List<SPTextElement> Children => null;
            public virtual SPTextElement TextParent { get; protected set; }
            public virtual string SPText { get; set; } = string.Empty;
            public SPLineBreak(SPTextElement parent)
            {
                this.TextParent = parent;
            }
            public SPLineBreak(SPTextElement parent,TextPointer textPointer)
                :base(textPointer)
            {
                this.TextParent = parent;
            }

            public virtual SPTextElement AddChild(SPTextElement child) => this;
            public virtual SPTextElement RemoveChild(SPTextElement child) => this;
        }

        protected Grid board = null;
        protected SPTextBlock textBlock = null;

        public virtual string NewLine { get; set; } = Environment.NewLine;

        public delegate int SPVisibiltySelector(IResultElement parent);

        public SPEdit()
        {
            InitializeComponent();
            this.AddChild(this.board = new Grid());
            this.board.Children.Add(this.textBlock = new SPTextBlock());
        }

        public virtual void Load(ResultCluster rc, SPTextElement parent = null, SPVisibiltySelector selector = null)
        {
            parent ??= this.textBlock;
            int s = selector != null ? selector(rc) : 0;
            for(int i = 0;i<rc.ResultPatterns.Count;i++)
            {
                var childspan = new SPTextBlock(parent);
                parent?.AddChild(childspan);
                this.Load(rc.ResultPatterns[i], childspan, selector);
                if (i != s)
                {
                    childspan.Visibility = Visibility.Hidden;
                }
            }
        }
        public virtual void Load(ResultPattern rp, SPTextElement parent = null, SPVisibiltySelector selector = null)
        {
            parent ??= this.textBlock;
            for (int i = 0; i < rp.ResultNodes.Count; i++)
            {
                var childspan = new SPTextBlock(parent);
                parent?.AddChild(childspan);
                this.Load(rp.ResultNodes[i], childspan, selector);
            }
        }

        public virtual void Load(ResultNode rn, SPTextElement parent = null, SPVisibiltySelector selector = null)
        {
            parent ??= this.textBlock;
            if (rn.IsPrimitive)
            {
                var text = rn.Text;
                if (!string.IsNullOrEmpty(text))
                {
                    var parts = this.Split(text,this.NewLine);
                    var full = new SPTextBlock(parent) { Result = rn }; 
                    for (int i =0;i<parts.Length;i++)
                    {
                        full.AddChild(new SPRun(full,parts[i]));

                        if (i < parts.Length - 1)
                        {
                            full.AddChild(new SPLineBreak(full));
                        }
                    }
                    if (full.Children.Count > 0)
                    {
                        parent?.AddChild(full);
                    }
                }
            }
            else //
            {
                int s = selector!=null ? selector(rn): 0;
                for(int i = 0;i<rn.ResultClusters.Count;i++)
                {
                    var childspan = new SPTextBlock(parent);
                    parent.AddChild(childspan);
                    this.Load(rn.ResultClusters[i], childspan, selector);
                    if (s != i) //only show the selected
                    {
                        childspan.Visibility = Visibility.Hidden;
                    }
                }
            }
        }
        protected virtual string[] Split(string text, string newline = null)
        {
            string[] parts = null;
            if (!string.IsNullOrEmpty(text))
            {
                parts = text.Split(new string[] { newline ?? this.NewLine }, StringSplitOptions.None);
            }

            return parts ?? new string[0];
        }

    }
}
