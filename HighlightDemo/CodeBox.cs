using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using Org.Jinou.HighlightEngine;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Globalization;
using System.Windows.Input;

namespace Org.Jinou.HighlightDemo
{
    public delegate void CodeBoxScrollChanged(CodeBox sender, int firstLine, int firstCharColumn);

    public class CodeBox : UserControl
    {
        public const double TextFontSize = 12.0;
        private static Typeface Typeface = new Typeface("Consolas");
        private static FormattedText LineHeightText = CodeBox.GetFormatText(new string(' ', 120), Brushes.Black);

        private ScrollBar rightScrollBar;
        private ScrollBar bottomScrollBar;
        private CodeView codeView;
        private LineBar lineBar;

        public event CodeBoxScrollChanged ScrollChanged;

        public int LineCount { get; private set; }
        public int FirstLine { get; private set; }
        public int FirstCharColumn { get; private set; }

        public CodeBox()
        {
            InitComponents();

            this.rightScrollBar.Scroll += new ScrollEventHandler(rightScrollBar_Scroll);
            this.bottomScrollBar.Scroll += new ScrollEventHandler(bottomScrollBar_Scroll);

            this.ScrollChanged += this.lineBar.CodeBoxScrollChanged;
            this.ScrollChanged += this.codeView.CodeBoxScrollChanged;

            this.MouseWheel += new System.Windows.Input.MouseWheelEventHandler(CodeBox_MouseWheel);
            this.KeyDown += new System.Windows.Input.KeyEventHandler(CodeBox_KeyDown);

            this.Focusable = true;
        }

        public static double GetLineHeight()
        {
            return LineHeightText.Height;
        }

        public static FormattedText GetFormatText(string text, Brush brush)
        {
            return new FormattedText(
                text,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                Typeface,
                TextFontSize,
                brush);
        }

        public void UpdateLines(TextLineInfo[] lines)
        {
            this.rightScrollBar.Minimum = 0;
            this.rightScrollBar.Maximum = lines.Length;
            this.rightScrollBar.Value = 0;

            double totalHeight = lines.Length * GetLineHeight();
            SetThumbLength(this.rightScrollBar, totalHeight);

            this.bottomScrollBar.Minimum = 0;
            this.bottomScrollBar.Maximum = lines.Max(l => l.TextLine.Length);
            this.bottomScrollBar.Value = 0;

            double totalWidth = this.bottomScrollBar.Maximum * LineHeightText.WidthIncludingTrailingWhitespace / 120;
            SetThumbLength(this.bottomScrollBar, totalWidth);

            this.codeView.LineInfos = lines;
            this.lineBar.Width = GetLineBarWidth(lines.Length);

            ResetFirstLine();

            InvalidateVisual();
        }

        #region InitComponents

        private void InitComponents()
        {
            rightScrollBar = new ScrollBar()
            {
                VerticalAlignment = VerticalAlignment.Stretch,
                Minimum = 0,
                Maximum = 0
            };
            bottomScrollBar = new ScrollBar()
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Minimum = 0,
                Maximum = 0
            };
            codeView = new CodeView()
            {
                Background = Brushes.Cyan,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),
            };
            lineBar = new LineBar() { Width = 20, Background = Brushes.Chocolate };

            Grid grid = new Grid
            {
                Margin = new Thickness(2),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

            AddControlToGrid(grid, lineBar, 0, 0);
            AddControlToGrid(grid, codeView, 1, 0);
            AddControlToGrid(grid, rightScrollBar, 2, 0);
            AddControlToGrid(grid, bottomScrollBar, 1, 1);

            this.Content = grid;
            this.BorderBrush = Brushes.Gray;
            this.BorderThickness = new Thickness(0.5);
        }

        private void AddControlToGrid(Grid grid, Control c, int col, int row)
        {
            grid.Children.Add(c);
            Grid.SetColumn(c, col);
            Grid.SetRow(c, row);
        }

        #endregion

        public static void SetThumbLength(ScrollBar s, double totalLength)
        {
            double actualLength = s.Orientation == Orientation.Vertical ? s.ActualHeight : s.ActualWidth;

            if (totalLength < 0)
            {
                s.ViewportSize = 0;
            }
            else if (actualLength <= totalLength)
            {
                s.ViewportSize = actualLength * (s.Maximum - s.Minimum) / (totalLength - actualLength);
            }
            else
            {
                s.ViewportSize = double.MaxValue;
            }
        }

        private void rightScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            FirstLine = (int)e.NewValue;
            OnScrollChanged();
        }

        private void bottomScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            FirstCharColumn = (int)e.NewValue;
            OnScrollChanged();
        }

        private void CodeBox_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            SetFirstLine(FirstLine - e.Delta);
        }

        private void CodeBox_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;

            switch (e.Key)
            {
                case Key.Down:
                    SetFirstLine(FirstLine + 1);
                    break;
                case Key.Up:
                    SetFirstLine(FirstLine - 1);
                    break;
                case Key.PageDown:
                    SetFirstLine(FirstLine + LineCount);
                    break;
                case Key.PageUp:
                    SetFirstLine(FirstLine - LineCount);
                    break;
                default:
                    e.Handled = false;
                    break;
            }
        }

        private void OnScrollChanged()
        {
            if (ScrollChanged != null)
            {
                ScrollChanged(this, FirstLine, FirstCharColumn);
            }
        }
        private void SetFirstLine(int line)
        {
            FirstLine = Math.Min(line, (int)this.rightScrollBar.Maximum);
            FirstLine = Math.Max(0, FirstLine);

            this.rightScrollBar.Value = FirstLine;
            LineCount = (int)(this.ActualHeight / GetLineHeight());

            OnScrollChanged();
        }
        private void ResetFirstLine()
        {
            this.FirstLine = 0;
            this.FirstCharColumn = 0;
            this.rightScrollBar.Value = 0;

            OnScrollChanged();
        }

        private double GetLineBarWidth(int maxLine)
        {
            string format = string.Format("{{0,{0}}} ", Math.Round(Math.Log10(maxLine), MidpointRounding.AwayFromZero));
            var text = CodeBox.GetFormatText(string.Format(format, 0), Brushes.Gray);
            return text.WidthIncludingTrailingWhitespace;
        }
    }
}
