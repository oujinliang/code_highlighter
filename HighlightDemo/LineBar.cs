using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;

namespace Microsoft.Internals.Tools.Ding.HighlightDemo
{
    public class LineBar : Control
    {
        private static SolidColorBrush LineNumberBackground = Brushes.Beige;
        private static SolidColorBrush LineNumberForeground = Brushes.DarkGray;
        private static SolidColorBrush DefaultForeground = Brushes.Black;

        private int firstLine;

        public void CodeBoxScrollChanged(CodeBox sender, int firstLine, int firstCharColumn)
        {
            if (this.firstLine != firstLine)
            {
                this.firstLine = firstLine;
                InvalidateVisual();
            }
        }

        protected override void OnRender(DrawingContext ctx)
        {
            ctx.PushClip(new RectangleGeometry(new Rect(0, 0, this.ActualWidth, this.ActualHeight)));//restrict drawing to textbox
            ctx.DrawRectangle(LineNumberBackground, null, new Rect(0, 0, this.ActualWidth, this.ActualHeight));//Draw Background

            int count = (int)(this.ActualHeight / CodeBox.GetLineHeight());

            string lineText = string.Join(
                Environment.NewLine,
                Enumerable.Range(firstLine + 1, count).Select(l => l.ToString()).ToArray());
            var text = CodeBox.GetFormatText(lineText, LineNumberForeground);

            ctx.DrawText(text, new Point(0, 0));
        }
    }
}
