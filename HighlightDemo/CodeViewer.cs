using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HightlightEngine;

namespace HighlightDemo
{
    /// <summary>
    /// 
    /// </summary>
    internal class CodeViewer : UserControl
    {
        private const int TextFontSize = 12;
        private const int LineHeight = TextFontSize + 3;

        private static SolidColorBrush LineNumberBackground = Brushes.Beige;
        private static SolidColorBrush LineNumberForeground = Brushes.DarkGray;
        private static SolidColorBrush DefaultForeground = Brushes.Black;
        private static Typeface Typeface = new Typeface("Consolas");

        // text lines for drawing.
        private TextLineInfo[] lineInfos = new TextLineInfo[0];

        /// <summary>
        /// Load a file and display.
        /// </summary>
        /// <param name="filePath"></param>
        public void LoadFile(string filePath)
        {
            string extension = new FileInfo(filePath).Extension;
            HighlightProfile profile = HighlightProfileFactory.GetProfileByExtension(extension);

            string[] lines = File.ReadAllLines(filePath);
            this.lineInfos = new HighlightParser(profile).Parse(lines, 0);

            this.Height = lines.Length * LineHeight;
        }

        protected override void OnRender(DrawingContext ctx)
        {
            double y = 0;

            foreach (var lineInfo in this.lineInfos)
            {
                double x = 0;
                int index = 0;
                FormattedText text = null;

                // Draw line number.
                string format = string.Format("{{0,{0}}} ", Math.Round(Math.Log10(lineInfos.Length) + 1, MidpointRounding.AwayFromZero));
                text = GetFormatText(string.Format(format, lineInfo.LineNumber + 1), LineNumberForeground);
                double w = text.WidthIncludingTrailingWhitespace;
                ctx.DrawRectangle(LineNumberBackground, null, new Rect(new Point(x, y), new Size(w, LineHeight)));
                ctx.DrawText(text, new Point(x, y));
                x += w;

                // Draw for each segment.
                foreach (var seg in lineInfo.Segments)
                {
                    if (seg.StartIndex != index)
                    {
                        text = GetFormatText(lineInfo.TextLine.Substring(index, seg.StartIndex - index), DefaultForeground);
                        ctx.DrawText(text, new Point(x, y));
                        x += text.WidthIncludingTrailingWhitespace;
                    }

                    text = GetFormatText(lineInfo.TextLine.Substring(seg.StartIndex, seg.Length), seg.Foreground);
                    ctx.DrawText(text, new Point(x, y));
                    x += text.WidthIncludingTrailingWhitespace;
                    index = seg.StartIndex + seg.Length;
                }

                if (lineInfo.TextLine.Length != index)
                {
                    text = GetFormatText(lineInfo.TextLine.Substring(index, lineInfo.TextLine.Length - index), DefaultForeground);
                    ctx.DrawText(text, new Point(x, y));
                }

                y += LineHeight;
            }
        }

        private static FormattedText GetFormatText(string text, Brush brush)
        {
            return new FormattedText(
                text,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                Typeface,
                TextFontSize,
                brush);
        }
    }
}
