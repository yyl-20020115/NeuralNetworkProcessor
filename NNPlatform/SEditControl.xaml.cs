using SParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SEdit
{
    /// <summary>
    /// SEditControl.xaml
    /// </summary>
    public partial class SEditControl : UserControl
    {
        public class TextBlockPanel : StackPanel
        {
            public TextBlock Block => this.Children[0] as TextBlock;
            public TextBox Box => this.Children[1] as TextBox;
            public bool IsInEditMode { get; protected set; } = false;
            public bool IsPlaceHolder { get; set; } = false;
            public new Brush Background { get=>base.Background; set { base.Background = this.Block.Background=this.Box.Background = value; } }
            public TextBlockPanel(TextBlock block, TextBox box)
            {
                this.Orientation = Orientation.Horizontal;
                this.Children.Add(block);
                this.Children.Add(box);
                this.LeaveEditMode();
            }
           
            public void EnterEditMode()
            {
                this.Block.Visibility = Visibility.Collapsed;
                this.Box.Visibility = Visibility.Visible;
                this.Box.Focus();
                this.Box.SelectAll();
                this.IsInEditMode = true;
            }

            public void LeaveEditMode(bool Confirmed = false)
            {
                if (Confirmed)
                {
                    this.Block.Text = this.Box.Text;
                }
                else
                {
                    this.Box.Text = this.Block.Text;
                }
                this.Block.Visibility = Visibility.Visible;
                this.Box.Visibility = Visibility.Collapsed;
                this.IsInEditMode = false;
            }
        }
        public class CursorPosition
        {
            public StackPanel VPanel = null;
            public StackPanel HPanel = null;
            public TextBlockPanel TPanel = null;
            public Brush SelectionBackBrush = null;
            public int CharInsertIndex = 0;
            public ResultNode RN => (ResultNode)(this.TPanel?.Tag ?? default(ResultNode));
            public virtual bool IsValidContainer => this.VPanel != null && this.HPanel != null;
            public virtual bool IsValidTextPanel => this.VPanel != null && this.HPanel != null && this.TPanel != null;
            public CursorPosition Select(TextBlockPanel tbp, int ci = 0)
            {
                if (tbp != null)
                {
                    this.TPanel = tbp;
                    this.HPanel = this.TPanel?.Parent as StackPanel;
                    this.VPanel = this.HPanel?.Parent as StackPanel;
                    this.CharInsertIndex = ci;
                    this.TPanel.Background = this.SelectionBackBrush;
                }

                return this;
            }
            public CursorPosition Unselect()
            {
                if (this.TPanel != null)
                {
                    if (this.TPanel.IsInEditMode)
                    {
                        this.TPanel.LeaveEditMode(false);
                    }
                    this.TPanel.Background = this.HPanel.Background;
                }
                return this;
            }
            public CursorPosition GoToUpLevel()
            {
                if (this.TPanel != null)
                {
                    if(this.RN is ResultNode rn)
                    {
                        if(rn.Tag is ResultPattern rp)
                        {
                            if(rp.Tag is ResultCluster rc)
                            {
                                if(rc.Tag is ResultNode un)
                                {
                                    if(un.Tag is ResultPattern up)
                                    {
                                        if(up.Tag is ResultCluster uc)
                                        {
                                            if(uc.Tag is ResultNode xn)
                                            {
                                                if(xn.Tag is ResultPattern xp)
                                                {
                                                    foreach (var np in xp.ResultNodes)
                                                    {
                                                        var mp = np.ResultClusters[0].ResultPatterns[0].ResultNodes[0].ResultClusters[0].ResultPatterns[0].ResultNodes[0];
                                                        if (mp.OtherTags.FirstOrDefault() is TextBlockPanel sp)
                                                        {
                                                            sp.Background = this.SelectionBackBrush;
                                                            sp.Opacity = 0.5;
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                    }
                                }
                            }
                        }
                    }
                }
                return this;
            }
            public CursorPosition GoToDownLevel()
            {
                return this;
            }
            public CursorPosition GoToPrevious()
            {
                return this;
            }
            public CursorPosition GoToNext()
            {
                return this;
            }
        }
        public string CrLf = "\r\n";
        public string Return = "\r";
        public string NewLine = "\n";
        public string PlaceHolder = "\uFEFF"; //BOM
        public char TabChar = '\t';
        public char SpaceChar = ' ';
        public HashSet<string> TextNames { get; } = new HashSet<string>();
        public Dictionary<string, (Brush, Brush)> BrushesDict = new Dictionary<string, (Brush, Brush)>();
        public Brush DefaultForeBrush { get; set; } = Brushes.Black;
        public Brush DefaultBackBrush { get; set; } = Brushes.White;
        public Brush SelectionBackBrush { get; set; } = Brushes.SkyBlue;
        public virtual ResultCluster Root { get; set; } = default;
        public int TabsCount { get; set; } = 4;

        public virtual CursorPosition Current { get; protected set; } = new CursorPosition();

        public bool OverwriteMode { get; set; } = false;
        public SEditControl()
        {
            InitializeComponent();
        }
        protected void UserControl_Initialized(object sender, EventArgs e)
        {
            this.Current.SelectionBackBrush = this.SelectionBackBrush;
            this.Canvas.Children.Add(this.Current.VPanel = this.CreatePanel(null,true));
        }
        protected virtual ref ResultPattern FilterPatterns(RefList<ResultPattern> resultPatterns) 
            => ref resultPatterns.RefFirstOrDefault();
        protected virtual ref ResultCluster FilterCluster(RefList<ResultCluster> resultClusters) 
            => ref resultClusters.RefFirstOrDefault();
        public void Load(ResultCluster root)
        {
            this.Current.VPanel.Tag = this.Root = root;

            this.LoadPanel(root);
        }
        protected virtual StackPanel CreatePanel(IResultElement e = null, bool vertical = false)
        {
            var sp = new StackPanel()
            {
                Orientation = vertical 
                    ? Orientation.Vertical 
                    : Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 0),
            };
            
            e?.OtherTags.Add(sp);

            return sp;
        }
        protected override void OnPreviewKeyUp(KeyEventArgs e)
        {
            if (e != null && this.Current.IsValidTextPanel)
            {
                if(e.Key == Key.Up && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    this.Current.GoToUpLevel();
                }
                else if (e.Key == Key.Escape)
                {
                    this.Current.Unselect();
                }
                else if (e.Key == Key.Enter)
                {
                    this.Current.Unselect();
                }
            }

            base.OnPreviewKeyUp(e);
        }

        protected virtual TextBlockPanel CreateTextBlock(IResultElement e, (Brush,Brush) brushes, string text = null, bool placeholder = false)
        {
            var g = new TextBlockPanel(
                new TextBlock { 
                    Text = text ?? e.Text, 
                    Foreground = brushes.Item1 ?? this.DefaultForeBrush, 
                    Background = brushes.Item2 ?? this.DefaultBackBrush,
                    Margin = new Thickness(1, 1, 1, 1)
                },
                new TextBox { 
                    Text = text ?? e.Text, 
                    //Foreground = brushes.Item1 ?? this.DefaultForeBrush, 
                    //Background = brushes.Item2 ?? this.DefaultBackBrush 
                }) { Tag = e, IsPlaceHolder = placeholder };

            e.OtherTags.Add(g);
            
            g.MouseLeftButtonDown += T_MouseLeftButtonDown;
            g.MouseRightButtonUp += T_MouseRightButtonUp;
           
            return g;
        }

        protected virtual void T_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if(sender is TextBlockPanel f)
            {
                if (this.Current.TPanel != null)
                {
                    this.Current.Unselect();
                }

                if (this.Current.TPanel != f)
                {
                    this.Current.Select(f);
                }
                else
                {
                    this.Current.TPanel.EnterEditMode();
                }
            }
        }

        protected virtual void T_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {

        }

        protected virtual string TryExtractInfo(ResultNode n, ref (Brush,Brush) brushes)
        {
            if (n.IsPrimitive)
            {
                return n.Text;
            }
            else if(n.ResultClusters.Count==1 && this.TextNames.Contains(n.ResultClusters[0].Text))
            {
                if(!this.BrushesDict.TryGetValue(n.ResultClusters[0].Text, out brushes))
                {
                    brushes.Item1 = DefaultForeBrush;
                    brushes.Item2 = DefaultBackBrush;
                }
                return n.ResultClusters[0].Flatten();
            }
            return null;
        }

        protected virtual void AppendTextBlock(ResultNode node, (Brush,Brush) brushes, string text = null)
        {
            var VPanel = this.Current.VPanel;
            if (VPanel != null)
            {
                var HPanel = this.Current.HPanel;
                if (HPanel == null)
                {
                    var count = VPanel.Children.Count;
                    if (count > 0)
                    {
                        this.Current.HPanel = HPanel = VPanel.Children[count - 1] as StackPanel;
                    }
                    else
                    {
                        VPanel.Children.Add(this.Current.HPanel = HPanel = this.CreatePanel(node));
                    }
                }
                
                if ((text = text ?? node.Text) == NewLine)
                {
                    if(HPanel!=null && HPanel.Children.Count == 0)
                    {
                        var ph = this.CreateTextBlock(node, brushes, this.PlaceHolder, true);
                        var i = HPanel.Children.IndexOf(this.Current.TPanel);
                        if (i >= 0)
                        {
                            if (this.OverwriteMode)
                            {
                                HPanel.Children.RemoveAt(i);
                            }
                            HPanel.Children.Insert(i, ph);
                        }
                        else
                        {
                            HPanel.Children.Add(ph);
                        }
                        this.Current.TPanel = ph;
                    }
                    
                    VPanel.Children.Add(this.Current.HPanel= this.CreatePanel(node));

                }
                else if(HPanel!=null)
                {
                    var tb = this.CreateTextBlock(node, brushes, text);

                    var i = HPanel.Children.IndexOf(this.Current.TPanel);

                    if (i >= 0)
                    {
                        if (this.OverwriteMode)
                        {
                            HPanel.Children.RemoveAt(i);
                        }
                        HPanel.Children.Insert(i, tb);
                    }
                    else
                    {
                        HPanel.Children.Add(tb);
                    }
                }
            }
        }
        protected virtual void LoadPanel(ResultCluster cluster)
        {
            ref var pattern = ref this.FilterPatterns(cluster.ResultPatterns);

            pattern.Tag = cluster;
            
            for(int i = 0;i<pattern.ResultNodes.Count;i++)
            {
                ref var node = ref pattern.ResultNodes[i];
                node.Tag = pattern;
                node.OtherTags.Clear();
                var brushes = (this.DefaultForeBrush, this.DefaultBackBrush)
;               var text = this.TryExtractInfo(node, ref brushes);
                if (text!=null)
                {
                    var nps = this.TranslateNewLine(text).Split(
                        new string[] { this.NewLine }, StringSplitOptions.None);
                    if (nps.Length >= 1)
                    {
                        for (int k = 0; k < nps.Length; k++)
                        {
                            var npt = nps[k];
                            var tsb = new StringBuilder();
                            for (int t = 0; t < npt.Length; t++)
                            {
                                if (npt[t] == TabChar)
                                {
                                    if (tsb.Length > 0)
                                    {
                                        this.AppendTextBlock(node, brushes, tsb.ToString());
                                        tsb.Clear();
                                    }
                                    this.AppendTextBlock(node, brushes, new string(this.SpaceChar, this.TabsCount));
                                }
                                else
                                {
                                    tsb.Append(npt[t]);
                                }
                            }
                            if (tsb.Length > 0)
                            {
                                this.AppendTextBlock(node, brushes, tsb.ToString());
                                tsb.Clear();
                            }
                            if (k < nps.Length - 1)
                            {
                                this.AppendTextBlock(node, brushes, NewLine);
                            }
                        }
                    }
                    else
                    {
                        this.AppendTextBlock(node, brushes);
                    }
                }
                else
                {
                    node.OtherTags.Add(this.Current.HPanel);
                    for(int j = 0;j<node.ResultClusters.Count;j++)
                    {
                        ref var nc = ref node.ResultClusters[j];
                        nc.Tag = node;
                        this.LoadPanel(nc);
                    }
                }
            }
        }

        protected virtual string TranslateNewLine(string text) 
            => text.Replace(this.CrLf, this.NewLine).Replace(Return, this.NewLine);
    }
}
