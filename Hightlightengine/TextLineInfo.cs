/* Copyright (C) 2012  Jinliang Ou */

namespace Org.Jinou.HighlightEngine
{
    using System.Collections.Generic;
    using System.Windows.Media;

    public class TextLineInfo
    {
        /// <summary>
        /// Text segment
        /// </summary>
        public struct TextSegment
        {
            public TextSegment(int startIndex, int length, Brush foreground)
                : this()
            {
                this.StartIndex = startIndex;
                this.Length = length;
                this.Foreground = foreground;
            }

            /// <summary>
            /// Start
            /// </summary>
            public int StartIndex { get; private set; }

            /// <summary>
            /// End
            /// </summary>
            public int Length { get; private set; }

            /// <summary>
            /// Foreground color.
            /// </summary>
            public Brush Foreground { get; private set; }
        }

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="lineNumber"></param>
        public TextLineInfo(string line, int lineNumber)
        {
            this.TextLine = line;
            this.LineNumber = lineNumber;
            this.Segments = new List<TextSegment>();
        }
        /// <summary>
        /// The text line.
        /// </summary>
        public string TextLine { get; internal set; }

        /// <summary>
        /// Line number
        /// </summary>
        public int LineNumber { get; internal set; }

        /// <summary>
        /// Text segments
        /// </summary>
        public IList<TextSegment> Segments { get; internal set; }
    }
}
