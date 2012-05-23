//---------------------------------------------------------------------
// <copyright file="TextLineInfo.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
// Use of this source code is subject to the terms of the Microsoft 
// end-user license agreement (EULA) under which you licensed this
// SOFTWARE PRODUCT. If you did not accept the terms of the EULA, you 
// are not authorized to use this source code. For a copy of the EULA, 
// please see the LICENSE.RTF on your install media.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.Internals.Tools.Ding.HighlightEngine
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
