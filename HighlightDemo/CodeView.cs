using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using Org.Jinou.HighlightEngine;
using System.Windows.Media;
using System.Windows;
using System.Windows.Input;

namespace Org.Jinou.HighlightDemo
{
    public class CodeView : Control
    {
        private int firstLine;
        private int firstCharCol;

        private TextLineInfo[] lineInfos;
        public TextLineInfo[] LineInfos
        {
            get
            {
                return lineInfos ?? new TextLineInfo[0];
            }
            set
            {
                this.lineInfos = value;
            }
        }

        public CodeView()
        {
            this.MouseDown += new MouseButtonEventHandler(CodeView_MouseDown);
        }

        void CodeView_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.GetTextPointFromPoint(e.GetPosition(this));
        }


        public void CodeBoxScrollChanged(CodeBox sender, int firstLine, int firstCharColumn)
        {
            if (this.firstLine != firstLine || this.firstCharCol != firstCharColumn)
            {
                this.firstLine = firstLine;
                this.firstCharCol = firstCharColumn;

                InvalidateVisual();
            }
        }

        protected override void OnRender(System.Windows.Media.DrawingContext ctx)
        {
            double x = 0;
            double y = 0;

            ctx.PushClip(new RectangleGeometry(new Rect(0, 0, this.ActualWidth, this.ActualHeight)));//restrict drawing to textbox
            ctx.DrawRectangle(Brushes.White, null, new Rect(0, 0, this.ActualWidth, this.ActualHeight));//Draw Background

            int startLine = this.firstLine;
            int count = (int)(this.ActualHeight / CodeBox.GetLineHeight());

            int lastLine = Math.Min(startLine + count, this.LineInfos.Length);
            string[] lines = this.LineInfos.Skip(startLine).Take(count).Select(f => f.TextLine).ToArray();
            FormattedText text = CodeBox.GetFormatText(string.Join(Environment.NewLine, lines), Brushes.White);

            int lineIndex = 0;
            for (int i = startLine; i < lastLine; ++i)
            {
                var lineInfo = this.LineInfos[i];
                int index = 0;

                double h = text.Height;// +text.LineHeight;
                // Draw for each segment.
                foreach (var seg in lineInfo.Segments)
                {
                    if (seg.StartIndex != index)
                    {
                        text.SetForegroundBrush(Brushes.Black, index + lineIndex, seg.StartIndex - index);
                    }
                    text.SetForegroundBrush(seg.Foreground, seg.StartIndex + lineIndex, seg.Length);

                    index = seg.StartIndex + seg.Length;
                }

                if (lineInfo.TextLine.Length != index)
                {
                    text.SetForegroundBrush(Brushes.Black, index + lineIndex, lineInfo.TextLine.Length - index);
                }

                lineIndex += lineInfo.TextLine.Length + Environment.NewLine.Length;
            }

            FormattedText hide = CodeBox.GetFormatText(text.Text.Substring(0, firstCharCol), Brushes.White);

            ctx.DrawText(text, new Point(x - hide.WidthIncludingTrailingWhitespace, y));
        }

        private Point GetTextPointFromPoint(Point point)
        {
            int line = (int)(point.Y / CodeBox.GetLineHeight());
            //line = Math.Min(line, this.LineInfos.Length - 1);
            //TextLineInfo info = this.LineInfos[line];

            this.Cursor = Cursors.IBeam;

            return new Point(0, line);
        }
    }
}
