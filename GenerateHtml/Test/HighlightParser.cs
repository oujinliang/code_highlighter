//---------------------------------------------------------------------
// <copyright file="HighlightParser.cs" company="Microsoft">
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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// A class to parse text lines.
    /// </summary>
    public class HighlightParser
    {
        private HighlightProfile profile;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="profile"></param>
        public HighlightParser(HighlightProfile profile)
        {
            this.profile = profile;
        }

        /// <summary>
        /// Parse the text lines;
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public TextLineInfo[] Parse(string[] lines, int startLine)
        {
            Expect.ArgumentNotNull(lines, "lines");
            Expect.ArgumentCheck(startLine >= 0, "startLine should >= 0");

            MultiLinesBlock inBlock = null;
            TextLineInfo[] lineInfos = new TextLineInfo[lines.Length];
            for (int i = 0; i < lines.Length; ++i)
            {
                lineInfos[i] = new TextLineInfo(lines[i], startLine + i);
                if (this.profile != null)
                {
                    inBlock = ParseLine(lineInfos[i], inBlock);
                }
            }

            return lineInfos;
        }

        #region Private Methods

        // Parse single line.
        private MultiLinesBlock ParseLine(TextLineInfo info, MultiLinesBlock blockForLineStart)
        {
            var seg = info.Segments;
            string line = info.TextLine;

            int index = 0;
            while (index < line.Length)
            {
                int endIndex;
                // check multiline block
                bool lineStartInBlock = blockForLineStart != null;
                if (blockForLineStart != null
                    || (blockForLineStart = MatchLineBlockStart(this.profile.MultiLinesBlocks, line, index)) != null)
                {
                    endIndex = GetBlockEndIndex(line, index, blockForLineStart, lineStartInBlock);
                    // try next line.
                    if (endIndex < 0)
                    {
                        AddSegments(seg, index, line.Length - index, blockForLineStart, !lineStartInBlock, false);
                        return blockForLineStart;
                    }

                    AddSegments(seg, index, endIndex - index, blockForLineStart, !lineStartInBlock, true);
                    blockForLineStart = null;
                    index = endIndex;
                    continue;
                }

                // check single line block
                SingleLineBlock singleLineBlock = MatchLineBlockStart(this.profile.SingleLineBlocks, line, index);
                if (singleLineBlock != null)
                {
                    endIndex = GetBlockEndIndex(line, index, singleLineBlock, false);
                    endIndex = endIndex < 0 ? line.Length : endIndex;

                    AddSegments(seg, index, endIndex - index, singleLineBlock, true, true);
                    index = endIndex;
                    continue;
                }

                // check tokens
                Token token;
                var match = MatchToken(line, index, out token);
                if (match != null)
                {
                    AddSegments(seg, token, match);
                    index += match.Length;
                    continue;
                }

                // check keywords
                endIndex = GetNextTokenEndIndex(line, index);
                if (endIndex > index)
                {
                    int length = endIndex - index;
                    var keywords = MatchKeyword(line, index, length);
                    if (keywords != null)
                    {
                        seg.Add(new TextLineInfo.TextSegment(index, length, keywords.Foreground));
                    }
                    index = endIndex + 1;
                    continue;
                }

                ++index;
            }

            return blockForLineStart;
        }

        // Check if given line can match token using regular expression.
        private Match MatchToken(string line, int index, out Token token)
        {
            foreach (var t in this.profile.Tokens)
            {
                Match match = t.Pattern.Match(line, index);
                if (match.Success && match.Index == index)
                {
                    token = t;
                    return match;
                }
            }

            token = null;
            return null;
        }

        // Add segments to TextLineInfo if there is token matches.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, Token token, Match match)
        {
            if (token.Groups == null || token.Groups.Length == 0)
            {
                seg.Add(new TextLineInfo.TextSegment(match.Index, match.Length, token.Foreground));
                return;
            }

            int index = match.Index;
            var groups = token.Groups
                .Select(g => new { Foreground = g.Foreground, Captrue = match.Groups[g.Name] })
                .OrderBy(g => g.Captrue.Index);

            foreach (var g in groups)
            {
                if (index != g.Captrue.Index)
                {
                    seg.Add(new TextLineInfo.TextSegment(index, g.Captrue.Index - index, token.Foreground));
                }

                seg.Add(new TextLineInfo.TextSegment(g.Captrue.Index, g.Captrue.Length, g.Foreground));
                index = g.Captrue.Index + g.Captrue.Length;
            }

            if (index != match.Index + match.Length)
            {
                seg.Add(new TextLineInfo.TextSegment(index, match.Index + match.Length - index, token.Foreground));
            }
        }

        // Add segments to TextLineInfo if there is code block found.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, int index, int length, CodeBlock block, bool hasStart, bool hasEnd)
        {
            if (block.WrapperForeground == null)
            {
                seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));
                return;
            }

            if (hasStart)
            {
                seg.Add(new TextLineInfo.TextSegment(index, block.Start.Length, block.WrapperForeground));
                index += block.Start.Length;
                length -= block.Start.Length;
            }

            length = hasEnd ? length - block.End.Length : length;
            seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));

            if (hasEnd)
            {
                seg.Add(new TextLineInfo.TextSegment(index + length, block.End.Length, block.WrapperForeground));
            }
        }

        // Check if line starting from index matches a code block.
        private T MatchLineBlockStart<T>(IEnumerable<T> blocks, string line, int index) where T : CodeBlock
        {
            return blocks.FirstOrDefault(block => StartsWith(line, index, block.Start));
        }

        // Find the block end index.
        private int GetBlockEndIndex(string line, int index, CodeBlock block, bool ignoreBlockStart)
        {
            if (!ignoreBlockStart)
            {
                index += block.Start.Length;
            }

            if (block is SingleLineBlock && string.IsNullOrEmpty(block.End))
            {
                return line.Length;
            }

            for (int i = index; i < line.Length; ++i)
            {
                var escape = block.Escape;
                if (escape != null)
                {
                    string escapeString = block.Escape.EscapeString;
                    if (!string.IsNullOrEmpty(escapeString) && StartsWith(line, i, escapeString))
                    {
                        i += escapeString.Length; // skip next one;
                        continue;
                    }

                    string[] escapeItems = block.Escape.Items ?? (new string[0]);
                    string found = escapeItems.FirstOrDefault(item => StartsWith(line, i, item));
                    if (found != null)
                    {
                        i += found.Length - 1;
                        continue;
                    }
                }

                if (StartsWith(line, i, block.End))
                {
                    return i + block.End.Length;
                }
            }

            // not found.
            return -1;
        }

        // Get next index of end of the token.
        private int GetNextTokenEndIndex(string line, int startIndex)
        {
            for (int i = startIndex; i < line.Length; ++i)
            {
                // Matches delimeter.
                if (Array.BinarySearch(this.profile.Delimiter, line[i]) >= 0)
                {
                    return i;
                }

                if (Array.BinarySearch(this.profile.BackDelimiter, line[i]) >= 0)
                {
                    return i - 1;
                }
            }

            return line.Length;
        }

        // Check if the line starting from index matches a keyword.
        private KeywordCollection MatchKeyword(string line, int index, int length)
        {
            Expect.ArgumentNotNull(line, "line");
            Expect.ArgumentCheck(line.Length >= index + length, "Check line length");

            foreach (var keywords in this.profile.KeywordCollecions)
            {
                int result = keywords.Keywords.BinarySearch(k => CompareKeyword(k, line, index, length));
                if (result >= 0)
                {
                    return keywords;
                }
            }

            return null;
        }

        // Check if the line starting from index starts another string.
        private bool StartsWith(string line, int index, string another)
        {
            if (line.Length < another.Length + index)
            {
                return false;
            }

            for (int i = 0; i < another.Length; ++i)
            {
                if (GetChar(line[index + i]) != GetChar(another[i]))
                {
                    return false;
                }
            }

            return true;
        }

        // Compare a keyword.
        private int CompareKeyword(string keyword, string line, int index, int length)
        {
            for (int i = 0; i < Math.Min(keyword.Length, length); ++i)
            {
                int result = GetChar(keyword[i]) - GetChar(line[index + i]);
                if (result != 0)
                {
                    return result;
                }
            }

            return keyword.Length - length;
        }

        // Convert a char.
        private char GetChar(char c)
        {
            return this.profile.IgnoreCase ? char.ToUpper(c) : c;
        }

        #endregion
    }
}
//---------------------------------------------------------------------
// <copyright file="HighlightParser.cs" company="Microsoft">
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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// A class to parse text lines.
    /// </summary>
    public class HighlightParser
    {
        private HighlightProfile profile;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="profile"></param>
        public HighlightParser(HighlightProfile profile)
        {
            this.profile = profile;
        }

        /// <summary>
        /// Parse the text lines;
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public TextLineInfo[] Parse(string[] lines, int startLine)
        {
            Expect.ArgumentNotNull(lines, "lines");
            Expect.ArgumentCheck(startLine >= 0, "startLine should >= 0");

            MultiLinesBlock inBlock = null;
            TextLineInfo[] lineInfos = new TextLineInfo[lines.Length];
            for (int i = 0; i < lines.Length; ++i)
            {
                lineInfos[i] = new TextLineInfo(lines[i], startLine + i);
                if (this.profile != null)
                {
                    inBlock = ParseLine(lineInfos[i], inBlock);
                }
            }

            return lineInfos;
        }

        #region Private Methods

        // Parse single line.
        private MultiLinesBlock ParseLine(TextLineInfo info, MultiLinesBlock blockForLineStart)
        {
            var seg = info.Segments;
            string line = info.TextLine;

            int index = 0;
            while (index < line.Length)
            {
                int endIndex;
                // check multiline block
                bool lineStartInBlock = blockForLineStart != null;
                if (blockForLineStart != null
                    || (blockForLineStart = MatchLineBlockStart(this.profile.MultiLinesBlocks, line, index)) != null)
                {
                    endIndex = GetBlockEndIndex(line, index, blockForLineStart, lineStartInBlock);
                    // try next line.
                    if (endIndex < 0)
                    {
                        AddSegments(seg, index, line.Length - index, blockForLineStart, !lineStartInBlock, false);
                        return blockForLineStart;
                    }

                    AddSegments(seg, index, endIndex - index, blockForLineStart, !lineStartInBlock, true);
                    blockForLineStart = null;
                    index = endIndex;
                    continue;
                }

                // check single line block
                SingleLineBlock singleLineBlock = MatchLineBlockStart(this.profile.SingleLineBlocks, line, index);
                if (singleLineBlock != null)
                {
                    endIndex = GetBlockEndIndex(line, index, singleLineBlock, false);
                    endIndex = endIndex < 0 ? line.Length : endIndex;

                    AddSegments(seg, index, endIndex - index, singleLineBlock, true, true);
                    index = endIndex;
                    continue;
                }

                // check tokens
                Token token;
                var match = MatchToken(line, index, out token);
                if (match != null)
                {
                    AddSegments(seg, token, match);
                    index += match.Length;
                    continue;
                }

                // check keywords
                endIndex = GetNextTokenEndIndex(line, index);
                if (endIndex > index)
                {
                    int length = endIndex - index;
                    var keywords = MatchKeyword(line, index, length);
                    if (keywords != null)
                    {
                        seg.Add(new TextLineInfo.TextSegment(index, length, keywords.Foreground));
                    }
                    index = endIndex + 1;
                    continue;
                }

                ++index;
            }

            return blockForLineStart;
        }

        // Check if given line can match token using regular expression.
        private Match MatchToken(string line, int index, out Token token)
        {
            foreach (var t in this.profile.Tokens)
            {
                Match match = t.Pattern.Match(line, index);
                if (match.Success && match.Index == index)
                {
                    token = t;
                    return match;
                }
            }

            token = null;
            return null;
        }

        // Add segments to TextLineInfo if there is token matches.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, Token token, Match match)
        {
            if (token.Groups == null || token.Groups.Length == 0)
            {
                seg.Add(new TextLineInfo.TextSegment(match.Index, match.Length, token.Foreground));
                return;
            }

            int index = match.Index;
            var groups = token.Groups
                .Select(g => new { Foreground = g.Foreground, Captrue = match.Groups[g.Name] })
                .OrderBy(g => g.Captrue.Index);

            foreach (var g in groups)
            {
                if (index != g.Captrue.Index)
                {
                    seg.Add(new TextLineInfo.TextSegment(index, g.Captrue.Index - index, token.Foreground));
                }

                seg.Add(new TextLineInfo.TextSegment(g.Captrue.Index, g.Captrue.Length, g.Foreground));
                index = g.Captrue.Index + g.Captrue.Length;
            }

            if (index != match.Index + match.Length)
            {
                seg.Add(new TextLineInfo.TextSegment(index, match.Index + match.Length - index, token.Foreground));
            }
        }

        // Add segments to TextLineInfo if there is code block found.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, int index, int length, CodeBlock block, bool hasStart, bool hasEnd)
        {
            if (block.WrapperForeground == null)
            {
                seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));
                return;
            }

            if (hasStart)
            {
                seg.Add(new TextLineInfo.TextSegment(index, block.Start.Length, block.WrapperForeground));
                index += block.Start.Length;
                length -= block.Start.Length;
            }

            length = hasEnd ? length - block.End.Length : length;
            seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));

            if (hasEnd)
            {
                seg.Add(new TextLineInfo.TextSegment(index + length, block.End.Length, block.WrapperForeground));
            }
        }

        // Check if line starting from index matches a code block.
        private T MatchLineBlockStart<T>(IEnumerable<T> blocks, string line, int index) where T : CodeBlock
        {
            return blocks.FirstOrDefault(block => StartsWith(line, index, block.Start));
        }

        // Find the block end index.
        private int GetBlockEndIndex(string line, int index, CodeBlock block, bool ignoreBlockStart)
        {
            if (!ignoreBlockStart)
            {
                index += block.Start.Length;
            }

            if (block is SingleLineBlock && string.IsNullOrEmpty(block.End))
            {
                return line.Length;
            }

            for (int i = index; i < line.Length; ++i)
            {
                var escape = block.Escape;
                if (escape != null)
                {
                    string escapeString = block.Escape.EscapeString;
                    if (!string.IsNullOrEmpty(escapeString) && StartsWith(line, i, escapeString))
                    {
                        i += escapeString.Length; // skip next one;
                        continue;
                    }

                    string[] escapeItems = block.Escape.Items ?? (new string[0]);
                    string found = escapeItems.FirstOrDefault(item => StartsWith(line, i, item));
                    if (found != null)
                    {
                        i += found.Length - 1;
                        continue;
                    }
                }

                if (StartsWith(line, i, block.End))
                {
                    return i + block.End.Length;
                }
            }

            // not found.
            return -1;
        }

        // Get next index of end of the token.
        private int GetNextTokenEndIndex(string line, int startIndex)
        {
            for (int i = startIndex; i < line.Length; ++i)
            {
                // Matches delimeter.
                if (Array.BinarySearch(this.profile.Delimiter, line[i]) >= 0)
                {
                    return i;
                }

                if (Array.BinarySearch(this.profile.BackDelimiter, line[i]) >= 0)
                {
                    return i - 1;
                }
            }

            return line.Length;
        }

        // Check if the line starting from index matches a keyword.
        private KeywordCollection MatchKeyword(string line, int index, int length)
        {
            Expect.ArgumentNotNull(line, "line");
            Expect.ArgumentCheck(line.Length >= index + length, "Check line length");

            foreach (var keywords in this.profile.KeywordCollecions)
            {
                int result = keywords.Keywords.BinarySearch(k => CompareKeyword(k, line, index, length));
                if (result >= 0)
                {
                    return keywords;
                }
            }

            return null;
        }

        // Check if the line starting from index starts another string.
        private bool StartsWith(string line, int index, string another)
        {
            if (line.Length < another.Length + index)
            {
                return false;
            }

            for (int i = 0; i < another.Length; ++i)
            {
                if (GetChar(line[index + i]) != GetChar(another[i]))
                {
                    return false;
                }
            }

            return true;
        }

        // Compare a keyword.
        private int CompareKeyword(string keyword, string line, int index, int length)
        {
            for (int i = 0; i < Math.Min(keyword.Length, length); ++i)
            {
                int result = GetChar(keyword[i]) - GetChar(line[index + i]);
                if (result != 0)
                {
                    return result;
                }
            }

            return keyword.Length - length;
        }

        // Convert a char.
        private char GetChar(char c)
        {
            return this.profile.IgnoreCase ? char.ToUpper(c) : c;
        }

        #endregion
    }
}
//---------------------------------------------------------------------
// <copyright file="HighlightParser.cs" company="Microsoft">
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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// A class to parse text lines.
    /// </summary>
    public class HighlightParser
    {
        private HighlightProfile profile;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="profile"></param>
        public HighlightParser(HighlightProfile profile)
        {
            this.profile = profile;
        }

        /// <summary>
        /// Parse the text lines;
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public TextLineInfo[] Parse(string[] lines, int startLine)
        {
            Expect.ArgumentNotNull(lines, "lines");
            Expect.ArgumentCheck(startLine >= 0, "startLine should >= 0");

            MultiLinesBlock inBlock = null;
            TextLineInfo[] lineInfos = new TextLineInfo[lines.Length];
            for (int i = 0; i < lines.Length; ++i)
            {
                lineInfos[i] = new TextLineInfo(lines[i], startLine + i);
                if (this.profile != null)
                {
                    inBlock = ParseLine(lineInfos[i], inBlock);
                }
            }

            return lineInfos;
        }

        #region Private Methods

        // Parse single line.
        private MultiLinesBlock ParseLine(TextLineInfo info, MultiLinesBlock blockForLineStart)
        {
            var seg = info.Segments;
            string line = info.TextLine;

            int index = 0;
            while (index < line.Length)
            {
                int endIndex;
                // check multiline block
                bool lineStartInBlock = blockForLineStart != null;
                if (blockForLineStart != null
                    || (blockForLineStart = MatchLineBlockStart(this.profile.MultiLinesBlocks, line, index)) != null)
                {
                    endIndex = GetBlockEndIndex(line, index, blockForLineStart, lineStartInBlock);
                    // try next line.
                    if (endIndex < 0)
                    {
                        AddSegments(seg, index, line.Length - index, blockForLineStart, !lineStartInBlock, false);
                        return blockForLineStart;
                    }

                    AddSegments(seg, index, endIndex - index, blockForLineStart, !lineStartInBlock, true);
                    blockForLineStart = null;
                    index = endIndex;
                    continue;
                }

                // check single line block
                SingleLineBlock singleLineBlock = MatchLineBlockStart(this.profile.SingleLineBlocks, line, index);
                if (singleLineBlock != null)
                {
                    endIndex = GetBlockEndIndex(line, index, singleLineBlock, false);
                    endIndex = endIndex < 0 ? line.Length : endIndex;

                    AddSegments(seg, index, endIndex - index, singleLineBlock, true, true);
                    index = endIndex;
                    continue;
                }

                // check tokens
                Token token;
                var match = MatchToken(line, index, out token);
                if (match != null)
                {
                    AddSegments(seg, token, match);
                    index += match.Length;
                    continue;
                }

                // check keywords
                endIndex = GetNextTokenEndIndex(line, index);
                if (endIndex > index)
                {
                    int length = endIndex - index;
                    var keywords = MatchKeyword(line, index, length);
                    if (keywords != null)
                    {
                        seg.Add(new TextLineInfo.TextSegment(index, length, keywords.Foreground));
                    }
                    index = endIndex + 1;
                    continue;
                }

                ++index;
            }

            return blockForLineStart;
        }

        // Check if given line can match token using regular expression.
        private Match MatchToken(string line, int index, out Token token)
        {
            foreach (var t in this.profile.Tokens)
            {
                Match match = t.Pattern.Match(line, index);
                if (match.Success && match.Index == index)
                {
                    token = t;
                    return match;
                }
            }

            token = null;
            return null;
        }

        // Add segments to TextLineInfo if there is token matches.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, Token token, Match match)
        {
            if (token.Groups == null || token.Groups.Length == 0)
            {
                seg.Add(new TextLineInfo.TextSegment(match.Index, match.Length, token.Foreground));
                return;
            }

            int index = match.Index;
            var groups = token.Groups
                .Select(g => new { Foreground = g.Foreground, Captrue = match.Groups[g.Name] })
                .OrderBy(g => g.Captrue.Index);

            foreach (var g in groups)
            {
                if (index != g.Captrue.Index)
                {
                    seg.Add(new TextLineInfo.TextSegment(index, g.Captrue.Index - index, token.Foreground));
                }

                seg.Add(new TextLineInfo.TextSegment(g.Captrue.Index, g.Captrue.Length, g.Foreground));
                index = g.Captrue.Index + g.Captrue.Length;
            }

            if (index != match.Index + match.Length)
            {
                seg.Add(new TextLineInfo.TextSegment(index, match.Index + match.Length - index, token.Foreground));
            }
        }

        // Add segments to TextLineInfo if there is code block found.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, int index, int length, CodeBlock block, bool hasStart, bool hasEnd)
        {
            if (block.WrapperForeground == null)
            {
                seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));
                return;
            }

            if (hasStart)
            {
                seg.Add(new TextLineInfo.TextSegment(index, block.Start.Length, block.WrapperForeground));
                index += block.Start.Length;
                length -= block.Start.Length;
            }

            length = hasEnd ? length - block.End.Length : length;
            seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));

            if (hasEnd)
            {
                seg.Add(new TextLineInfo.TextSegment(index + length, block.End.Length, block.WrapperForeground));
            }
        }

        // Check if line starting from index matches a code block.
        private T MatchLineBlockStart<T>(IEnumerable<T> blocks, string line, int index) where T : CodeBlock
        {
            return blocks.FirstOrDefault(block => StartsWith(line, index, block.Start));
        }

        // Find the block end index.
        private int GetBlockEndIndex(string line, int index, CodeBlock block, bool ignoreBlockStart)
        {
            if (!ignoreBlockStart)
            {
                index += block.Start.Length;
            }

            if (block is SingleLineBlock && string.IsNullOrEmpty(block.End))
            {
                return line.Length;
            }

            for (int i = index; i < line.Length; ++i)
            {
                var escape = block.Escape;
                if (escape != null)
                {
                    string escapeString = block.Escape.EscapeString;
                    if (!string.IsNullOrEmpty(escapeString) && StartsWith(line, i, escapeString))
                    {
                        i += escapeString.Length; // skip next one;
                        continue;
                    }

                    string[] escapeItems = block.Escape.Items ?? (new string[0]);
                    string found = escapeItems.FirstOrDefault(item => StartsWith(line, i, item));
                    if (found != null)
                    {
                        i += found.Length - 1;
                        continue;
                    }
                }

                if (StartsWith(line, i, block.End))
                {
                    return i + block.End.Length;
                }
            }

            // not found.
            return -1;
        }

        // Get next index of end of the token.
        private int GetNextTokenEndIndex(string line, int startIndex)
        {
            for (int i = startIndex; i < line.Length; ++i)
            {
                // Matches delimeter.
                if (Array.BinarySearch(this.profile.Delimiter, line[i]) >= 0)
                {
                    return i;
                }

                if (Array.BinarySearch(this.profile.BackDelimiter, line[i]) >= 0)
                {
                    return i - 1;
                }
            }

            return line.Length;
        }

        // Check if the line starting from index matches a keyword.
        private KeywordCollection MatchKeyword(string line, int index, int length)
        {
            Expect.ArgumentNotNull(line, "line");
            Expect.ArgumentCheck(line.Length >= index + length, "Check line length");

            foreach (var keywords in this.profile.KeywordCollecions)
            {
                int result = keywords.Keywords.BinarySearch(k => CompareKeyword(k, line, index, length));
                if (result >= 0)
                {
                    return keywords;
                }
            }

            return null;
        }

        // Check if the line starting from index starts another string.
        private bool StartsWith(string line, int index, string another)
        {
            if (line.Length < another.Length + index)
            {
                return false;
            }

            for (int i = 0; i < another.Length; ++i)
            {
                if (GetChar(line[index + i]) != GetChar(another[i]))
                {
                    return false;
                }
            }

            return true;
        }

        // Compare a keyword.
        private int CompareKeyword(string keyword, string line, int index, int length)
        {
            for (int i = 0; i < Math.Min(keyword.Length, length); ++i)
            {
                int result = GetChar(keyword[i]) - GetChar(line[index + i]);
                if (result != 0)
                {
                    return result;
                }
            }

            return keyword.Length - length;
        }

        // Convert a char.
        private char GetChar(char c)
        {
            return this.profile.IgnoreCase ? char.ToUpper(c) : c;
        }

        #endregion
    }
}
//---------------------------------------------------------------------
// <copyright file="HighlightParser.cs" company="Microsoft">
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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// A class to parse text lines.
    /// </summary>
    public class HighlightParser
    {
        private HighlightProfile profile;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="profile"></param>
        public HighlightParser(HighlightProfile profile)
        {
            this.profile = profile;
        }

        /// <summary>
        /// Parse the text lines;
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public TextLineInfo[] Parse(string[] lines, int startLine)
        {
            Expect.ArgumentNotNull(lines, "lines");
            Expect.ArgumentCheck(startLine >= 0, "startLine should >= 0");

            MultiLinesBlock inBlock = null;
            TextLineInfo[] lineInfos = new TextLineInfo[lines.Length];
            for (int i = 0; i < lines.Length; ++i)
            {
                lineInfos[i] = new TextLineInfo(lines[i], startLine + i);
                if (this.profile != null)
                {
                    inBlock = ParseLine(lineInfos[i], inBlock);
                }
            }

            return lineInfos;
        }

        #region Private Methods

        // Parse single line.
        private MultiLinesBlock ParseLine(TextLineInfo info, MultiLinesBlock blockForLineStart)
        {
            var seg = info.Segments;
            string line = info.TextLine;

            int index = 0;
            while (index < line.Length)
            {
                int endIndex;
                // check multiline block
                bool lineStartInBlock = blockForLineStart != null;
                if (blockForLineStart != null
                    || (blockForLineStart = MatchLineBlockStart(this.profile.MultiLinesBlocks, line, index)) != null)
                {
                    endIndex = GetBlockEndIndex(line, index, blockForLineStart, lineStartInBlock);
                    // try next line.
                    if (endIndex < 0)
                    {
                        AddSegments(seg, index, line.Length - index, blockForLineStart, !lineStartInBlock, false);
                        return blockForLineStart;
                    }

                    AddSegments(seg, index, endIndex - index, blockForLineStart, !lineStartInBlock, true);
                    blockForLineStart = null;
                    index = endIndex;
                    continue;
                }

                // check single line block
                SingleLineBlock singleLineBlock = MatchLineBlockStart(this.profile.SingleLineBlocks, line, index);
                if (singleLineBlock != null)
                {
                    endIndex = GetBlockEndIndex(line, index, singleLineBlock, false);
                    endIndex = endIndex < 0 ? line.Length : endIndex;

                    AddSegments(seg, index, endIndex - index, singleLineBlock, true, true);
                    index = endIndex;
                    continue;
                }

                // check tokens
                Token token;
                var match = MatchToken(line, index, out token);
                if (match != null)
                {
                    AddSegments(seg, token, match);
                    index += match.Length;
                    continue;
                }

                // check keywords
                endIndex = GetNextTokenEndIndex(line, index);
                if (endIndex > index)
                {
                    int length = endIndex - index;
                    var keywords = MatchKeyword(line, index, length);
                    if (keywords != null)
                    {
                        seg.Add(new TextLineInfo.TextSegment(index, length, keywords.Foreground));
                    }
                    index = endIndex + 1;
                    continue;
                }

                ++index;
            }

            return blockForLineStart;
        }

        // Check if given line can match token using regular expression.
        private Match MatchToken(string line, int index, out Token token)
        {
            foreach (var t in this.profile.Tokens)
            {
                Match match = t.Pattern.Match(line, index);
                if (match.Success && match.Index == index)
                {
                    token = t;
                    return match;
                }
            }

            token = null;
            return null;
        }

        // Add segments to TextLineInfo if there is token matches.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, Token token, Match match)
        {
            if (token.Groups == null || token.Groups.Length == 0)
            {
                seg.Add(new TextLineInfo.TextSegment(match.Index, match.Length, token.Foreground));
                return;
            }

            int index = match.Index;
            var groups = token.Groups
                .Select(g => new { Foreground = g.Foreground, Captrue = match.Groups[g.Name] })
                .OrderBy(g => g.Captrue.Index);

            foreach (var g in groups)
            {
                if (index != g.Captrue.Index)
                {
                    seg.Add(new TextLineInfo.TextSegment(index, g.Captrue.Index - index, token.Foreground));
                }

                seg.Add(new TextLineInfo.TextSegment(g.Captrue.Index, g.Captrue.Length, g.Foreground));
                index = g.Captrue.Index + g.Captrue.Length;
            }

            if (index != match.Index + match.Length)
            {
                seg.Add(new TextLineInfo.TextSegment(index, match.Index + match.Length - index, token.Foreground));
            }
        }

        // Add segments to TextLineInfo if there is code block found.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, int index, int length, CodeBlock block, bool hasStart, bool hasEnd)
        {
            if (block.WrapperForeground == null)
            {
                seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));
                return;
            }

            if (hasStart)
            {
                seg.Add(new TextLineInfo.TextSegment(index, block.Start.Length, block.WrapperForeground));
                index += block.Start.Length;
                length -= block.Start.Length;
            }

            length = hasEnd ? length - block.End.Length : length;
            seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));

            if (hasEnd)
            {
                seg.Add(new TextLineInfo.TextSegment(index + length, block.End.Length, block.WrapperForeground));
            }
        }

        // Check if line starting from index matches a code block.
        private T MatchLineBlockStart<T>(IEnumerable<T> blocks, string line, int index) where T : CodeBlock
        {
            return blocks.FirstOrDefault(block => StartsWith(line, index, block.Start));
        }

        // Find the block end index.
        private int GetBlockEndIndex(string line, int index, CodeBlock block, bool ignoreBlockStart)
        {
            if (!ignoreBlockStart)
            {
                index += block.Start.Length;
            }

            if (block is SingleLineBlock && string.IsNullOrEmpty(block.End))
            {
                return line.Length;
            }

            for (int i = index; i < line.Length; ++i)
            {
                var escape = block.Escape;
                if (escape != null)
                {
                    string escapeString = block.Escape.EscapeString;
                    if (!string.IsNullOrEmpty(escapeString) && StartsWith(line, i, escapeString))
                    {
                        i += escapeString.Length; // skip next one;
                        continue;
                    }

                    string[] escapeItems = block.Escape.Items ?? (new string[0]);
                    string found = escapeItems.FirstOrDefault(item => StartsWith(line, i, item));
                    if (found != null)
                    {
                        i += found.Length - 1;
                        continue;
                    }
                }

                if (StartsWith(line, i, block.End))
                {
                    return i + block.End.Length;
                }
            }

            // not found.
            return -1;
        }

        // Get next index of end of the token.
        private int GetNextTokenEndIndex(string line, int startIndex)
        {
            for (int i = startIndex; i < line.Length; ++i)
            {
                // Matches delimeter.
                if (Array.BinarySearch(this.profile.Delimiter, line[i]) >= 0)
                {
                    return i;
                }

                if (Array.BinarySearch(this.profile.BackDelimiter, line[i]) >= 0)
                {
                    return i - 1;
                }
            }

            return line.Length;
        }

        // Check if the line starting from index matches a keyword.
        private KeywordCollection MatchKeyword(string line, int index, int length)
        {
            Expect.ArgumentNotNull(line, "line");
            Expect.ArgumentCheck(line.Length >= index + length, "Check line length");

            foreach (var keywords in this.profile.KeywordCollecions)
            {
                int result = keywords.Keywords.BinarySearch(k => CompareKeyword(k, line, index, length));
                if (result >= 0)
                {
                    return keywords;
                }
            }

            return null;
        }

        // Check if the line starting from index starts another string.
        private bool StartsWith(string line, int index, string another)
        {
            if (line.Length < another.Length + index)
            {
                return false;
            }

            for (int i = 0; i < another.Length; ++i)
            {
                if (GetChar(line[index + i]) != GetChar(another[i]))
                {
                    return false;
                }
            }

            return true;
        }

        // Compare a keyword.
        private int CompareKeyword(string keyword, string line, int index, int length)
        {
            for (int i = 0; i < Math.Min(keyword.Length, length); ++i)
            {
                int result = GetChar(keyword[i]) - GetChar(line[index + i]);
                if (result != 0)
                {
                    return result;
                }
            }

            return keyword.Length - length;
        }

        // Convert a char.
        private char GetChar(char c)
        {
            return this.profile.IgnoreCase ? char.ToUpper(c) : c;
        }

        #endregion
    }
}
//---------------------------------------------------------------------
// <copyright file="HighlightParser.cs" company="Microsoft">
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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// A class to parse text lines.
    /// </summary>
    public class HighlightParser
    {
        private HighlightProfile profile;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="profile"></param>
        public HighlightParser(HighlightProfile profile)
        {
            this.profile = profile;
        }

        /// <summary>
        /// Parse the text lines;
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public TextLineInfo[] Parse(string[] lines, int startLine)
        {
            Expect.ArgumentNotNull(lines, "lines");
            Expect.ArgumentCheck(startLine >= 0, "startLine should >= 0");

            MultiLinesBlock inBlock = null;
            TextLineInfo[] lineInfos = new TextLineInfo[lines.Length];
            for (int i = 0; i < lines.Length; ++i)
            {
                lineInfos[i] = new TextLineInfo(lines[i], startLine + i);
                if (this.profile != null)
                {
                    inBlock = ParseLine(lineInfos[i], inBlock);
                }
            }

            return lineInfos;
        }

        #region Private Methods

        // Parse single line.
        private MultiLinesBlock ParseLine(TextLineInfo info, MultiLinesBlock blockForLineStart)
        {
            var seg = info.Segments;
            string line = info.TextLine;

            int index = 0;
            while (index < line.Length)
            {
                int endIndex;
                // check multiline block
                bool lineStartInBlock = blockForLineStart != null;
                if (blockForLineStart != null
                    || (blockForLineStart = MatchLineBlockStart(this.profile.MultiLinesBlocks, line, index)) != null)
                {
                    endIndex = GetBlockEndIndex(line, index, blockForLineStart, lineStartInBlock);
                    // try next line.
                    if (endIndex < 0)
                    {
                        AddSegments(seg, index, line.Length - index, blockForLineStart, !lineStartInBlock, false);
                        return blockForLineStart;
                    }

                    AddSegments(seg, index, endIndex - index, blockForLineStart, !lineStartInBlock, true);
                    blockForLineStart = null;
                    index = endIndex;
                    continue;
                }

                // check single line block
                SingleLineBlock singleLineBlock = MatchLineBlockStart(this.profile.SingleLineBlocks, line, index);
                if (singleLineBlock != null)
                {
                    endIndex = GetBlockEndIndex(line, index, singleLineBlock, false);
                    endIndex = endIndex < 0 ? line.Length : endIndex;

                    AddSegments(seg, index, endIndex - index, singleLineBlock, true, true);
                    index = endIndex;
                    continue;
                }

                // check tokens
                Token token;
                var match = MatchToken(line, index, out token);
                if (match != null)
                {
                    AddSegments(seg, token, match);
                    index += match.Length;
                    continue;
                }

                // check keywords
                endIndex = GetNextTokenEndIndex(line, index);
                if (endIndex > index)
                {
                    int length = endIndex - index;
                    var keywords = MatchKeyword(line, index, length);
                    if (keywords != null)
                    {
                        seg.Add(new TextLineInfo.TextSegment(index, length, keywords.Foreground));
                    }
                    index = endIndex + 1;
                    continue;
                }

                ++index;
            }

            return blockForLineStart;
        }

        // Check if given line can match token using regular expression.
        private Match MatchToken(string line, int index, out Token token)
        {
            foreach (var t in this.profile.Tokens)
            {
                Match match = t.Pattern.Match(line, index);
                if (match.Success && match.Index == index)
                {
                    token = t;
                    return match;
                }
            }

            token = null;
            return null;
        }

        // Add segments to TextLineInfo if there is token matches.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, Token token, Match match)
        {
            if (token.Groups == null || token.Groups.Length == 0)
            {
                seg.Add(new TextLineInfo.TextSegment(match.Index, match.Length, token.Foreground));
                return;
            }

            int index = match.Index;
            var groups = token.Groups
                .Select(g => new { Foreground = g.Foreground, Captrue = match.Groups[g.Name] })
                .OrderBy(g => g.Captrue.Index);

            foreach (var g in groups)
            {
                if (index != g.Captrue.Index)
                {
                    seg.Add(new TextLineInfo.TextSegment(index, g.Captrue.Index - index, token.Foreground));
                }

                seg.Add(new TextLineInfo.TextSegment(g.Captrue.Index, g.Captrue.Length, g.Foreground));
                index = g.Captrue.Index + g.Captrue.Length;
            }

            if (index != match.Index + match.Length)
            {
                seg.Add(new TextLineInfo.TextSegment(index, match.Index + match.Length - index, token.Foreground));
            }
        }

        // Add segments to TextLineInfo if there is code block found.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, int index, int length, CodeBlock block, bool hasStart, bool hasEnd)
        {
            if (block.WrapperForeground == null)
            {
                seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));
                return;
            }

            if (hasStart)
            {
                seg.Add(new TextLineInfo.TextSegment(index, block.Start.Length, block.WrapperForeground));
                index += block.Start.Length;
                length -= block.Start.Length;
            }

            length = hasEnd ? length - block.End.Length : length;
            seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));

            if (hasEnd)
            {
                seg.Add(new TextLineInfo.TextSegment(index + length, block.End.Length, block.WrapperForeground));
            }
        }

        // Check if line starting from index matches a code block.
        private T MatchLineBlockStart<T>(IEnumerable<T> blocks, string line, int index) where T : CodeBlock
        {
            return blocks.FirstOrDefault(block => StartsWith(line, index, block.Start));
        }

        // Find the block end index.
        private int GetBlockEndIndex(string line, int index, CodeBlock block, bool ignoreBlockStart)
        {
            if (!ignoreBlockStart)
            {
                index += block.Start.Length;
            }

            if (block is SingleLineBlock && string.IsNullOrEmpty(block.End))
            {
                return line.Length;
            }

            for (int i = index; i < line.Length; ++i)
            {
                var escape = block.Escape;
                if (escape != null)
                {
                    string escapeString = block.Escape.EscapeString;
                    if (!string.IsNullOrEmpty(escapeString) && StartsWith(line, i, escapeString))
                    {
                        i += escapeString.Length; // skip next one;
                        continue;
                    }

                    string[] escapeItems = block.Escape.Items ?? (new string[0]);
                    string found = escapeItems.FirstOrDefault(item => StartsWith(line, i, item));
                    if (found != null)
                    {
                        i += found.Length - 1;
                        continue;
                    }
                }

                if (StartsWith(line, i, block.End))
                {
                    return i + block.End.Length;
                }
            }

            // not found.
            return -1;
        }

        // Get next index of end of the token.
        private int GetNextTokenEndIndex(string line, int startIndex)
        {
            for (int i = startIndex; i < line.Length; ++i)
            {
                // Matches delimeter.
                if (Array.BinarySearch(this.profile.Delimiter, line[i]) >= 0)
                {
                    return i;
                }

                if (Array.BinarySearch(this.profile.BackDelimiter, line[i]) >= 0)
                {
                    return i - 1;
                }
            }

            return line.Length;
        }

        // Check if the line starting from index matches a keyword.
        private KeywordCollection MatchKeyword(string line, int index, int length)
        {
            Expect.ArgumentNotNull(line, "line");
            Expect.ArgumentCheck(line.Length >= index + length, "Check line length");

            foreach (var keywords in this.profile.KeywordCollecions)
            {
                int result = keywords.Keywords.BinarySearch(k => CompareKeyword(k, line, index, length));
                if (result >= 0)
                {
                    return keywords;
                }
            }

            return null;
        }

        // Check if the line starting from index starts another string.
        private bool StartsWith(string line, int index, string another)
        {
            if (line.Length < another.Length + index)
            {
                return false;
            }

            for (int i = 0; i < another.Length; ++i)
            {
                if (GetChar(line[index + i]) != GetChar(another[i]))
                {
                    return false;
                }
            }

            return true;
        }

        // Compare a keyword.
        private int CompareKeyword(string keyword, string line, int index, int length)
        {
            for (int i = 0; i < Math.Min(keyword.Length, length); ++i)
            {
                int result = GetChar(keyword[i]) - GetChar(line[index + i]);
                if (result != 0)
                {
                    return result;
                }
            }

            return keyword.Length - length;
        }

        // Convert a char.
        private char GetChar(char c)
        {
            return this.profile.IgnoreCase ? char.ToUpper(c) : c;
        }

        #endregion
    }
}
//---------------------------------------------------------------------
// <copyright file="HighlightParser.cs" company="Microsoft">
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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// A class to parse text lines.
    /// </summary>
    public class HighlightParser
    {
        private HighlightProfile profile;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="profile"></param>
        public HighlightParser(HighlightProfile profile)
        {
            this.profile = profile;
        }

        /// <summary>
        /// Parse the text lines;
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public TextLineInfo[] Parse(string[] lines, int startLine)
        {
            Expect.ArgumentNotNull(lines, "lines");
            Expect.ArgumentCheck(startLine >= 0, "startLine should >= 0");

            MultiLinesBlock inBlock = null;
            TextLineInfo[] lineInfos = new TextLineInfo[lines.Length];
            for (int i = 0; i < lines.Length; ++i)
            {
                lineInfos[i] = new TextLineInfo(lines[i], startLine + i);
                if (this.profile != null)
                {
                    inBlock = ParseLine(lineInfos[i], inBlock);
                }
            }

            return lineInfos;
        }

        #region Private Methods

        // Parse single line.
        private MultiLinesBlock ParseLine(TextLineInfo info, MultiLinesBlock blockForLineStart)
        {
            var seg = info.Segments;
            string line = info.TextLine;

            int index = 0;
            while (index < line.Length)
            {
                int endIndex;
                // check multiline block
                bool lineStartInBlock = blockForLineStart != null;
                if (blockForLineStart != null
                    || (blockForLineStart = MatchLineBlockStart(this.profile.MultiLinesBlocks, line, index)) != null)
                {
                    endIndex = GetBlockEndIndex(line, index, blockForLineStart, lineStartInBlock);
                    // try next line.
                    if (endIndex < 0)
                    {
                        AddSegments(seg, index, line.Length - index, blockForLineStart, !lineStartInBlock, false);
                        return blockForLineStart;
                    }

                    AddSegments(seg, index, endIndex - index, blockForLineStart, !lineStartInBlock, true);
                    blockForLineStart = null;
                    index = endIndex;
                    continue;
                }

                // check single line block
                SingleLineBlock singleLineBlock = MatchLineBlockStart(this.profile.SingleLineBlocks, line, index);
                if (singleLineBlock != null)
                {
                    endIndex = GetBlockEndIndex(line, index, singleLineBlock, false);
                    endIndex = endIndex < 0 ? line.Length : endIndex;

                    AddSegments(seg, index, endIndex - index, singleLineBlock, true, true);
                    index = endIndex;
                    continue;
                }

                // check tokens
                Token token;
                var match = MatchToken(line, index, out token);
                if (match != null)
                {
                    AddSegments(seg, token, match);
                    index += match.Length;
                    continue;
                }

                // check keywords
                endIndex = GetNextTokenEndIndex(line, index);
                if (endIndex > index)
                {
                    int length = endIndex - index;
                    var keywords = MatchKeyword(line, index, length);
                    if (keywords != null)
                    {
                        seg.Add(new TextLineInfo.TextSegment(index, length, keywords.Foreground));
                    }
                    index = endIndex + 1;
                    continue;
                }

                ++index;
            }

            return blockForLineStart;
        }

        // Check if given line can match token using regular expression.
        private Match MatchToken(string line, int index, out Token token)
        {
            foreach (var t in this.profile.Tokens)
            {
                Match match = t.Pattern.Match(line, index);
                if (match.Success && match.Index == index)
                {
                    token = t;
                    return match;
                }
            }

            token = null;
            return null;
        }

        // Add segments to TextLineInfo if there is token matches.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, Token token, Match match)
        {
            if (token.Groups == null || token.Groups.Length == 0)
            {
                seg.Add(new TextLineInfo.TextSegment(match.Index, match.Length, token.Foreground));
                return;
            }

            int index = match.Index;
            var groups = token.Groups
                .Select(g => new { Foreground = g.Foreground, Captrue = match.Groups[g.Name] })
                .OrderBy(g => g.Captrue.Index);

            foreach (var g in groups)
            {
                if (index != g.Captrue.Index)
                {
                    seg.Add(new TextLineInfo.TextSegment(index, g.Captrue.Index - index, token.Foreground));
                }

                seg.Add(new TextLineInfo.TextSegment(g.Captrue.Index, g.Captrue.Length, g.Foreground));
                index = g.Captrue.Index + g.Captrue.Length;
            }

            if (index != match.Index + match.Length)
            {
                seg.Add(new TextLineInfo.TextSegment(index, match.Index + match.Length - index, token.Foreground));
            }
        }

        // Add segments to TextLineInfo if there is code block found.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, int index, int length, CodeBlock block, bool hasStart, bool hasEnd)
        {
            if (block.WrapperForeground == null)
            {
                seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));
                return;
            }

            if (hasStart)
            {
                seg.Add(new TextLineInfo.TextSegment(index, block.Start.Length, block.WrapperForeground));
                index += block.Start.Length;
                length -= block.Start.Length;
            }

            length = hasEnd ? length - block.End.Length : length;
            seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));

            if (hasEnd)
            {
                seg.Add(new TextLineInfo.TextSegment(index + length, block.End.Length, block.WrapperForeground));
            }
        }

        // Check if line starting from index matches a code block.
        private T MatchLineBlockStart<T>(IEnumerable<T> blocks, string line, int index) where T : CodeBlock
        {
            return blocks.FirstOrDefault(block => StartsWith(line, index, block.Start));
        }

        // Find the block end index.
        private int GetBlockEndIndex(string line, int index, CodeBlock block, bool ignoreBlockStart)
        {
            if (!ignoreBlockStart)
            {
                index += block.Start.Length;
            }

            if (block is SingleLineBlock && string.IsNullOrEmpty(block.End))
            {
                return line.Length;
            }

            for (int i = index; i < line.Length; ++i)
            {
                var escape = block.Escape;
                if (escape != null)
                {
                    string escapeString = block.Escape.EscapeString;
                    if (!string.IsNullOrEmpty(escapeString) && StartsWith(line, i, escapeString))
                    {
                        i += escapeString.Length; // skip next one;
                        continue;
                    }

                    string[] escapeItems = block.Escape.Items ?? (new string[0]);
                    string found = escapeItems.FirstOrDefault(item => StartsWith(line, i, item));
                    if (found != null)
                    {
                        i += found.Length - 1;
                        continue;
                    }
                }

                if (StartsWith(line, i, block.End))
                {
                    return i + block.End.Length;
                }
            }

            // not found.
            return -1;
        }

        // Get next index of end of the token.
        private int GetNextTokenEndIndex(string line, int startIndex)
        {
            for (int i = startIndex; i < line.Length; ++i)
            {
                // Matches delimeter.
                if (Array.BinarySearch(this.profile.Delimiter, line[i]) >= 0)
                {
                    return i;
                }

                if (Array.BinarySearch(this.profile.BackDelimiter, line[i]) >= 0)
                {
                    return i - 1;
                }
            }

            return line.Length;
        }

        // Check if the line starting from index matches a keyword.
        private KeywordCollection MatchKeyword(string line, int index, int length)
        {
            Expect.ArgumentNotNull(line, "line");
            Expect.ArgumentCheck(line.Length >= index + length, "Check line length");

            foreach (var keywords in this.profile.KeywordCollecions)
            {
                int result = keywords.Keywords.BinarySearch(k => CompareKeyword(k, line, index, length));
                if (result >= 0)
                {
                    return keywords;
                }
            }

            return null;
        }

        // Check if the line starting from index starts another string.
        private bool StartsWith(string line, int index, string another)
        {
            if (line.Length < another.Length + index)
            {
                return false;
            }

            for (int i = 0; i < another.Length; ++i)
            {
                if (GetChar(line[index + i]) != GetChar(another[i]))
                {
                    return false;
                }
            }

            return true;
        }

        // Compare a keyword.
        private int CompareKeyword(string keyword, string line, int index, int length)
        {
            for (int i = 0; i < Math.Min(keyword.Length, length); ++i)
            {
                int result = GetChar(keyword[i]) - GetChar(line[index + i]);
                if (result != 0)
                {
                    return result;
                }
            }

            return keyword.Length - length;
        }

        // Convert a char.
        private char GetChar(char c)
        {
            return this.profile.IgnoreCase ? char.ToUpper(c) : c;
        }

        #endregion
    }
}
//---------------------------------------------------------------------
// <copyright file="HighlightParser.cs" company="Microsoft">
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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// A class to parse text lines.
    /// </summary>
    public class HighlightParser
    {
        private HighlightProfile profile;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="profile"></param>
        public HighlightParser(HighlightProfile profile)
        {
            this.profile = profile;
        }

        /// <summary>
        /// Parse the text lines;
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public TextLineInfo[] Parse(string[] lines, int startLine)
        {
            Expect.ArgumentNotNull(lines, "lines");
            Expect.ArgumentCheck(startLine >= 0, "startLine should >= 0");

            MultiLinesBlock inBlock = null;
            TextLineInfo[] lineInfos = new TextLineInfo[lines.Length];
            for (int i = 0; i < lines.Length; ++i)
            {
                lineInfos[i] = new TextLineInfo(lines[i], startLine + i);
                if (this.profile != null)
                {
                    inBlock = ParseLine(lineInfos[i], inBlock);
                }
            }

            return lineInfos;
        }

        #region Private Methods

        // Parse single line.
        private MultiLinesBlock ParseLine(TextLineInfo info, MultiLinesBlock blockForLineStart)
        {
            var seg = info.Segments;
            string line = info.TextLine;

            int index = 0;
            while (index < line.Length)
            {
                int endIndex;
                // check multiline block
                bool lineStartInBlock = blockForLineStart != null;
                if (blockForLineStart != null
                    || (blockForLineStart = MatchLineBlockStart(this.profile.MultiLinesBlocks, line, index)) != null)
                {
                    endIndex = GetBlockEndIndex(line, index, blockForLineStart, lineStartInBlock);
                    // try next line.
                    if (endIndex < 0)
                    {
                        AddSegments(seg, index, line.Length - index, blockForLineStart, !lineStartInBlock, false);
                        return blockForLineStart;
                    }

                    AddSegments(seg, index, endIndex - index, blockForLineStart, !lineStartInBlock, true);
                    blockForLineStart = null;
                    index = endIndex;
                    continue;
                }

                // check single line block
                SingleLineBlock singleLineBlock = MatchLineBlockStart(this.profile.SingleLineBlocks, line, index);
                if (singleLineBlock != null)
                {
                    endIndex = GetBlockEndIndex(line, index, singleLineBlock, false);
                    endIndex = endIndex < 0 ? line.Length : endIndex;

                    AddSegments(seg, index, endIndex - index, singleLineBlock, true, true);
                    index = endIndex;
                    continue;
                }

                // check tokens
                Token token;
                var match = MatchToken(line, index, out token);
                if (match != null)
                {
                    AddSegments(seg, token, match);
                    index += match.Length;
                    continue;
                }

                // check keywords
                endIndex = GetNextTokenEndIndex(line, index);
                if (endIndex > index)
                {
                    int length = endIndex - index;
                    var keywords = MatchKeyword(line, index, length);
                    if (keywords != null)
                    {
                        seg.Add(new TextLineInfo.TextSegment(index, length, keywords.Foreground));
                    }
                    index = endIndex + 1;
                    continue;
                }

                ++index;
            }

            return blockForLineStart;
        }

        // Check if given line can match token using regular expression.
        private Match MatchToken(string line, int index, out Token token)
        {
            foreach (var t in this.profile.Tokens)
            {
                Match match = t.Pattern.Match(line, index);
                if (match.Success && match.Index == index)
                {
                    token = t;
                    return match;
                }
            }

            token = null;
            return null;
        }

        // Add segments to TextLineInfo if there is token matches.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, Token token, Match match)
        {
            if (token.Groups == null || token.Groups.Length == 0)
            {
                seg.Add(new TextLineInfo.TextSegment(match.Index, match.Length, token.Foreground));
                return;
            }

            int index = match.Index;
            var groups = token.Groups
                .Select(g => new { Foreground = g.Foreground, Captrue = match.Groups[g.Name] })
                .OrderBy(g => g.Captrue.Index);

            foreach (var g in groups)
            {
                if (index != g.Captrue.Index)
                {
                    seg.Add(new TextLineInfo.TextSegment(index, g.Captrue.Index - index, token.Foreground));
                }

                seg.Add(new TextLineInfo.TextSegment(g.Captrue.Index, g.Captrue.Length, g.Foreground));
                index = g.Captrue.Index + g.Captrue.Length;
            }

            if (index != match.Index + match.Length)
            {
                seg.Add(new TextLineInfo.TextSegment(index, match.Index + match.Length - index, token.Foreground));
            }
        }

        // Add segments to TextLineInfo if there is code block found.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, int index, int length, CodeBlock block, bool hasStart, bool hasEnd)
        {
            if (block.WrapperForeground == null)
            {
                seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));
                return;
            }

            if (hasStart)
            {
                seg.Add(new TextLineInfo.TextSegment(index, block.Start.Length, block.WrapperForeground));
                index += block.Start.Length;
                length -= block.Start.Length;
            }

            length = hasEnd ? length - block.End.Length : length;
            seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));

            if (hasEnd)
            {
                seg.Add(new TextLineInfo.TextSegment(index + length, block.End.Length, block.WrapperForeground));
            }
        }

        // Check if line starting from index matches a code block.
        private T MatchLineBlockStart<T>(IEnumerable<T> blocks, string line, int index) where T : CodeBlock
        {
            return blocks.FirstOrDefault(block => StartsWith(line, index, block.Start));
        }

        // Find the block end index.
        private int GetBlockEndIndex(string line, int index, CodeBlock block, bool ignoreBlockStart)
        {
            if (!ignoreBlockStart)
            {
                index += block.Start.Length;
            }

            if (block is SingleLineBlock && string.IsNullOrEmpty(block.End))
            {
                return line.Length;
            }

            for (int i = index; i < line.Length; ++i)
            {
                var escape = block.Escape;
                if (escape != null)
                {
                    string escapeString = block.Escape.EscapeString;
                    if (!string.IsNullOrEmpty(escapeString) && StartsWith(line, i, escapeString))
                    {
                        i += escapeString.Length; // skip next one;
                        continue;
                    }

                    string[] escapeItems = block.Escape.Items ?? (new string[0]);
                    string found = escapeItems.FirstOrDefault(item => StartsWith(line, i, item));
                    if (found != null)
                    {
                        i += found.Length - 1;
                        continue;
                    }
                }

                if (StartsWith(line, i, block.End))
                {
                    return i + block.End.Length;
                }
            }

            // not found.
            return -1;
        }

        // Get next index of end of the token.
        private int GetNextTokenEndIndex(string line, int startIndex)
        {
            for (int i = startIndex; i < line.Length; ++i)
            {
                // Matches delimeter.
                if (Array.BinarySearch(this.profile.Delimiter, line[i]) >= 0)
                {
                    return i;
                }

                if (Array.BinarySearch(this.profile.BackDelimiter, line[i]) >= 0)
                {
                    return i - 1;
                }
            }

            return line.Length;
        }

        // Check if the line starting from index matches a keyword.
        private KeywordCollection MatchKeyword(string line, int index, int length)
        {
            Expect.ArgumentNotNull(line, "line");
            Expect.ArgumentCheck(line.Length >= index + length, "Check line length");

            foreach (var keywords in this.profile.KeywordCollecions)
            {
                int result = keywords.Keywords.BinarySearch(k => CompareKeyword(k, line, index, length));
                if (result >= 0)
                {
                    return keywords;
                }
            }

            return null;
        }

        // Check if the line starting from index starts another string.
        private bool StartsWith(string line, int index, string another)
        {
            if (line.Length < another.Length + index)
            {
                return false;
            }

            for (int i = 0; i < another.Length; ++i)
            {
                if (GetChar(line[index + i]) != GetChar(another[i]))
                {
                    return false;
                }
            }

            return true;
        }

        // Compare a keyword.
        private int CompareKeyword(string keyword, string line, int index, int length)
        {
            for (int i = 0; i < Math.Min(keyword.Length, length); ++i)
            {
                int result = GetChar(keyword[i]) - GetChar(line[index + i]);
                if (result != 0)
                {
                    return result;
                }
            }

            return keyword.Length - length;
        }

        // Convert a char.
        private char GetChar(char c)
        {
            return this.profile.IgnoreCase ? char.ToUpper(c) : c;
        }

        #endregion
    }
}
//---------------------------------------------------------------------
// <copyright file="HighlightParser.cs" company="Microsoft">
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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// A class to parse text lines.
    /// </summary>
    public class HighlightParser
    {
        private HighlightProfile profile;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="profile"></param>
        public HighlightParser(HighlightProfile profile)
        {
            this.profile = profile;
        }

        /// <summary>
        /// Parse the text lines;
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public TextLineInfo[] Parse(string[] lines, int startLine)
        {
            Expect.ArgumentNotNull(lines, "lines");
            Expect.ArgumentCheck(startLine >= 0, "startLine should >= 0");

            MultiLinesBlock inBlock = null;
            TextLineInfo[] lineInfos = new TextLineInfo[lines.Length];
            for (int i = 0; i < lines.Length; ++i)
            {
                lineInfos[i] = new TextLineInfo(lines[i], startLine + i);
                if (this.profile != null)
                {
                    inBlock = ParseLine(lineInfos[i], inBlock);
                }
            }

            return lineInfos;
        }

        #region Private Methods

        // Parse single line.
        private MultiLinesBlock ParseLine(TextLineInfo info, MultiLinesBlock blockForLineStart)
        {
            var seg = info.Segments;
            string line = info.TextLine;

            int index = 0;
            while (index < line.Length)
            {
                int endIndex;
                // check multiline block
                bool lineStartInBlock = blockForLineStart != null;
                if (blockForLineStart != null
                    || (blockForLineStart = MatchLineBlockStart(this.profile.MultiLinesBlocks, line, index)) != null)
                {
                    endIndex = GetBlockEndIndex(line, index, blockForLineStart, lineStartInBlock);
                    // try next line.
                    if (endIndex < 0)
                    {
                        AddSegments(seg, index, line.Length - index, blockForLineStart, !lineStartInBlock, false);
                        return blockForLineStart;
                    }

                    AddSegments(seg, index, endIndex - index, blockForLineStart, !lineStartInBlock, true);
                    blockForLineStart = null;
                    index = endIndex;
                    continue;
                }

                // check single line block
                SingleLineBlock singleLineBlock = MatchLineBlockStart(this.profile.SingleLineBlocks, line, index);
                if (singleLineBlock != null)
                {
                    endIndex = GetBlockEndIndex(line, index, singleLineBlock, false);
                    endIndex = endIndex < 0 ? line.Length : endIndex;

                    AddSegments(seg, index, endIndex - index, singleLineBlock, true, true);
                    index = endIndex;
                    continue;
                }

                // check tokens
                Token token;
                var match = MatchToken(line, index, out token);
                if (match != null)
                {
                    AddSegments(seg, token, match);
                    index += match.Length;
                    continue;
                }

                // check keywords
                endIndex = GetNextTokenEndIndex(line, index);
                if (endIndex > index)
                {
                    int length = endIndex - index;
                    var keywords = MatchKeyword(line, index, length);
                    if (keywords != null)
                    {
                        seg.Add(new TextLineInfo.TextSegment(index, length, keywords.Foreground));
                    }
                    index = endIndex + 1;
                    continue;
                }

                ++index;
            }

            return blockForLineStart;
        }

        // Check if given line can match token using regular expression.
        private Match MatchToken(string line, int index, out Token token)
        {
            foreach (var t in this.profile.Tokens)
            {
                Match match = t.Pattern.Match(line, index);
                if (match.Success && match.Index == index)
                {
                    token = t;
                    return match;
                }
            }

            token = null;
            return null;
        }

        // Add segments to TextLineInfo if there is token matches.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, Token token, Match match)
        {
            if (token.Groups == null || token.Groups.Length == 0)
            {
                seg.Add(new TextLineInfo.TextSegment(match.Index, match.Length, token.Foreground));
                return;
            }

            int index = match.Index;
            var groups = token.Groups
                .Select(g => new { Foreground = g.Foreground, Captrue = match.Groups[g.Name] })
                .OrderBy(g => g.Captrue.Index);

            foreach (var g in groups)
            {
                if (index != g.Captrue.Index)
                {
                    seg.Add(new TextLineInfo.TextSegment(index, g.Captrue.Index - index, token.Foreground));
                }

                seg.Add(new TextLineInfo.TextSegment(g.Captrue.Index, g.Captrue.Length, g.Foreground));
                index = g.Captrue.Index + g.Captrue.Length;
            }

            if (index != match.Index + match.Length)
            {
                seg.Add(new TextLineInfo.TextSegment(index, match.Index + match.Length - index, token.Foreground));
            }
        }

        // Add segments to TextLineInfo if there is code block found.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, int index, int length, CodeBlock block, bool hasStart, bool hasEnd)
        {
            if (block.WrapperForeground == null)
            {
                seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));
                return;
            }

            if (hasStart)
            {
                seg.Add(new TextLineInfo.TextSegment(index, block.Start.Length, block.WrapperForeground));
                index += block.Start.Length;
                length -= block.Start.Length;
            }

            length = hasEnd ? length - block.End.Length : length;
            seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));

            if (hasEnd)
            {
                seg.Add(new TextLineInfo.TextSegment(index + length, block.End.Length, block.WrapperForeground));
            }
        }

        // Check if line starting from index matches a code block.
        private T MatchLineBlockStart<T>(IEnumerable<T> blocks, string line, int index) where T : CodeBlock
        {
            return blocks.FirstOrDefault(block => StartsWith(line, index, block.Start));
        }

        // Find the block end index.
        private int GetBlockEndIndex(string line, int index, CodeBlock block, bool ignoreBlockStart)
        {
            if (!ignoreBlockStart)
            {
                index += block.Start.Length;
            }

            if (block is SingleLineBlock && string.IsNullOrEmpty(block.End))
            {
                return line.Length;
            }

            for (int i = index; i < line.Length; ++i)
            {
                var escape = block.Escape;
                if (escape != null)
                {
                    string escapeString = block.Escape.EscapeString;
                    if (!string.IsNullOrEmpty(escapeString) && StartsWith(line, i, escapeString))
                    {
                        i += escapeString.Length; // skip next one;
                        continue;
                    }

                    string[] escapeItems = block.Escape.Items ?? (new string[0]);
                    string found = escapeItems.FirstOrDefault(item => StartsWith(line, i, item));
                    if (found != null)
                    {
                        i += found.Length - 1;
                        continue;
                    }
                }

                if (StartsWith(line, i, block.End))
                {
                    return i + block.End.Length;
                }
            }

            // not found.
            return -1;
        }

        // Get next index of end of the token.
        private int GetNextTokenEndIndex(string line, int startIndex)
        {
            for (int i = startIndex; i < line.Length; ++i)
            {
                // Matches delimeter.
                if (Array.BinarySearch(this.profile.Delimiter, line[i]) >= 0)
                {
                    return i;
                }

                if (Array.BinarySearch(this.profile.BackDelimiter, line[i]) >= 0)
                {
                    return i - 1;
                }
            }

            return line.Length;
        }

        // Check if the line starting from index matches a keyword.
        private KeywordCollection MatchKeyword(string line, int index, int length)
        {
            Expect.ArgumentNotNull(line, "line");
            Expect.ArgumentCheck(line.Length >= index + length, "Check line length");

            foreach (var keywords in this.profile.KeywordCollecions)
            {
                int result = keywords.Keywords.BinarySearch(k => CompareKeyword(k, line, index, length));
                if (result >= 0)
                {
                    return keywords;
                }
            }

            return null;
        }

        // Check if the line starting from index starts another string.
        private bool StartsWith(string line, int index, string another)
        {
            if (line.Length < another.Length + index)
            {
                return false;
            }

            for (int i = 0; i < another.Length; ++i)
            {
                if (GetChar(line[index + i]) != GetChar(another[i]))
                {
                    return false;
                }
            }

            return true;
        }

        // Compare a keyword.
        private int CompareKeyword(string keyword, string line, int index, int length)
        {
            for (int i = 0; i < Math.Min(keyword.Length, length); ++i)
            {
                int result = GetChar(keyword[i]) - GetChar(line[index + i]);
                if (result != 0)
                {
                    return result;
                }
            }

            return keyword.Length - length;
        }

        // Convert a char.
        private char GetChar(char c)
        {
            return this.profile.IgnoreCase ? char.ToUpper(c) : c;
        }

        #endregion
    }
}
//---------------------------------------------------------------------
// <copyright file="HighlightParser.cs" company="Microsoft">
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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// A class to parse text lines.
    /// </summary>
    public class HighlightParser
    {
        private HighlightProfile profile;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="profile"></param>
        public HighlightParser(HighlightProfile profile)
        {
            this.profile = profile;
        }

        /// <summary>
        /// Parse the text lines;
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public TextLineInfo[] Parse(string[] lines, int startLine)
        {
            Expect.ArgumentNotNull(lines, "lines");
            Expect.ArgumentCheck(startLine >= 0, "startLine should >= 0");

            MultiLinesBlock inBlock = null;
            TextLineInfo[] lineInfos = new TextLineInfo[lines.Length];
            for (int i = 0; i < lines.Length; ++i)
            {
                lineInfos[i] = new TextLineInfo(lines[i], startLine + i);
                if (this.profile != null)
                {
                    inBlock = ParseLine(lineInfos[i], inBlock);
                }
            }

            return lineInfos;
        }

        #region Private Methods

        // Parse single line.
        private MultiLinesBlock ParseLine(TextLineInfo info, MultiLinesBlock blockForLineStart)
        {
            var seg = info.Segments;
            string line = info.TextLine;

            int index = 0;
            while (index < line.Length)
            {
                int endIndex;
                // check multiline block
                bool lineStartInBlock = blockForLineStart != null;
                if (blockForLineStart != null
                    || (blockForLineStart = MatchLineBlockStart(this.profile.MultiLinesBlocks, line, index)) != null)
                {
                    endIndex = GetBlockEndIndex(line, index, blockForLineStart, lineStartInBlock);
                    // try next line.
                    if (endIndex < 0)
                    {
                        AddSegments(seg, index, line.Length - index, blockForLineStart, !lineStartInBlock, false);
                        return blockForLineStart;
                    }

                    AddSegments(seg, index, endIndex - index, blockForLineStart, !lineStartInBlock, true);
                    blockForLineStart = null;
                    index = endIndex;
                    continue;
                }

                // check single line block
                SingleLineBlock singleLineBlock = MatchLineBlockStart(this.profile.SingleLineBlocks, line, index);
                if (singleLineBlock != null)
                {
                    endIndex = GetBlockEndIndex(line, index, singleLineBlock, false);
                    endIndex = endIndex < 0 ? line.Length : endIndex;

                    AddSegments(seg, index, endIndex - index, singleLineBlock, true, true);
                    index = endIndex;
                    continue;
                }

                // check tokens
                Token token;
                var match = MatchToken(line, index, out token);
                if (match != null)
                {
                    AddSegments(seg, token, match);
                    index += match.Length;
                    continue;
                }

                // check keywords
                endIndex = GetNextTokenEndIndex(line, index);
                if (endIndex > index)
                {
                    int length = endIndex - index;
                    var keywords = MatchKeyword(line, index, length);
                    if (keywords != null)
                    {
                        seg.Add(new TextLineInfo.TextSegment(index, length, keywords.Foreground));
                    }
                    index = endIndex + 1;
                    continue;
                }

                ++index;
            }

            return blockForLineStart;
        }

        // Check if given line can match token using regular expression.
        private Match MatchToken(string line, int index, out Token token)
        {
            foreach (var t in this.profile.Tokens)
            {
                Match match = t.Pattern.Match(line, index);
                if (match.Success && match.Index == index)
                {
                    token = t;
                    return match;
                }
            }

            token = null;
            return null;
        }

        // Add segments to TextLineInfo if there is token matches.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, Token token, Match match)
        {
            if (token.Groups == null || token.Groups.Length == 0)
            {
                seg.Add(new TextLineInfo.TextSegment(match.Index, match.Length, token.Foreground));
                return;
            }

            int index = match.Index;
            var groups = token.Groups
                .Select(g => new { Foreground = g.Foreground, Captrue = match.Groups[g.Name] })
                .OrderBy(g => g.Captrue.Index);

            foreach (var g in groups)
            {
                if (index != g.Captrue.Index)
                {
                    seg.Add(new TextLineInfo.TextSegment(index, g.Captrue.Index - index, token.Foreground));
                }

                seg.Add(new TextLineInfo.TextSegment(g.Captrue.Index, g.Captrue.Length, g.Foreground));
                index = g.Captrue.Index + g.Captrue.Length;
            }

            if (index != match.Index + match.Length)
            {
                seg.Add(new TextLineInfo.TextSegment(index, match.Index + match.Length - index, token.Foreground));
            }
        }

        // Add segments to TextLineInfo if there is code block found.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, int index, int length, CodeBlock block, bool hasStart, bool hasEnd)
        {
            if (block.WrapperForeground == null)
            {
                seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));
                return;
            }

            if (hasStart)
            {
                seg.Add(new TextLineInfo.TextSegment(index, block.Start.Length, block.WrapperForeground));
                index += block.Start.Length;
                length -= block.Start.Length;
            }

            length = hasEnd ? length - block.End.Length : length;
            seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));

            if (hasEnd)
            {
                seg.Add(new TextLineInfo.TextSegment(index + length, block.End.Length, block.WrapperForeground));
            }
        }

        // Check if line starting from index matches a code block.
        private T MatchLineBlockStart<T>(IEnumerable<T> blocks, string line, int index) where T : CodeBlock
        {
            return blocks.FirstOrDefault(block => StartsWith(line, index, block.Start));
        }

        // Find the block end index.
        private int GetBlockEndIndex(string line, int index, CodeBlock block, bool ignoreBlockStart)
        {
            if (!ignoreBlockStart)
            {
                index += block.Start.Length;
            }

            if (block is SingleLineBlock && string.IsNullOrEmpty(block.End))
            {
                return line.Length;
            }

            for (int i = index; i < line.Length; ++i)
            {
                var escape = block.Escape;
                if (escape != null)
                {
                    string escapeString = block.Escape.EscapeString;
                    if (!string.IsNullOrEmpty(escapeString) && StartsWith(line, i, escapeString))
                    {
                        i += escapeString.Length; // skip next one;
                        continue;
                    }

                    string[] escapeItems = block.Escape.Items ?? (new string[0]);
                    string found = escapeItems.FirstOrDefault(item => StartsWith(line, i, item));
                    if (found != null)
                    {
                        i += found.Length - 1;
                        continue;
                    }
                }

                if (StartsWith(line, i, block.End))
                {
                    return i + block.End.Length;
                }
            }

            // not found.
            return -1;
        }

        // Get next index of end of the token.
        private int GetNextTokenEndIndex(string line, int startIndex)
        {
            for (int i = startIndex; i < line.Length; ++i)
            {
                // Matches delimeter.
                if (Array.BinarySearch(this.profile.Delimiter, line[i]) >= 0)
                {
                    return i;
                }

                if (Array.BinarySearch(this.profile.BackDelimiter, line[i]) >= 0)
                {
                    return i - 1;
                }
            }

            return line.Length;
        }

        // Check if the line starting from index matches a keyword.
        private KeywordCollection MatchKeyword(string line, int index, int length)
        {
            Expect.ArgumentNotNull(line, "line");
            Expect.ArgumentCheck(line.Length >= index + length, "Check line length");

            foreach (var keywords in this.profile.KeywordCollecions)
            {
                int result = keywords.Keywords.BinarySearch(k => CompareKeyword(k, line, index, length));
                if (result >= 0)
                {
                    return keywords;
                }
            }

            return null;
        }

        // Check if the line starting from index starts another string.
        private bool StartsWith(string line, int index, string another)
        {
            if (line.Length < another.Length + index)
            {
                return false;
            }

            for (int i = 0; i < another.Length; ++i)
            {
                if (GetChar(line[index + i]) != GetChar(another[i]))
                {
                    return false;
                }
            }

            return true;
        }

        // Compare a keyword.
        private int CompareKeyword(string keyword, string line, int index, int length)
        {
            for (int i = 0; i < Math.Min(keyword.Length, length); ++i)
            {
                int result = GetChar(keyword[i]) - GetChar(line[index + i]);
                if (result != 0)
                {
                    return result;
                }
            }

            return keyword.Length - length;
        }

        // Convert a char.
        private char GetChar(char c)
        {
            return this.profile.IgnoreCase ? char.ToUpper(c) : c;
        }

        #endregion
    }
}
//---------------------------------------------------------------------
// <copyright file="HighlightParser.cs" company="Microsoft">
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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// A class to parse text lines.
    /// </summary>
    public class HighlightParser
    {
        private HighlightProfile profile;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="profile"></param>
        public HighlightParser(HighlightProfile profile)
        {
            this.profile = profile;
        }

        /// <summary>
        /// Parse the text lines;
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public TextLineInfo[] Parse(string[] lines, int startLine)
        {
            Expect.ArgumentNotNull(lines, "lines");
            Expect.ArgumentCheck(startLine >= 0, "startLine should >= 0");

            MultiLinesBlock inBlock = null;
            TextLineInfo[] lineInfos = new TextLineInfo[lines.Length];
            for (int i = 0; i < lines.Length; ++i)
            {
                lineInfos[i] = new TextLineInfo(lines[i], startLine + i);
                if (this.profile != null)
                {
                    inBlock = ParseLine(lineInfos[i], inBlock);
                }
            }

            return lineInfos;
        }

        #region Private Methods

        // Parse single line.
        private MultiLinesBlock ParseLine(TextLineInfo info, MultiLinesBlock blockForLineStart)
        {
            var seg = info.Segments;
            string line = info.TextLine;

            int index = 0;
            while (index < line.Length)
            {
                int endIndex;
                // check multiline block
                bool lineStartInBlock = blockForLineStart != null;
                if (blockForLineStart != null
                    || (blockForLineStart = MatchLineBlockStart(this.profile.MultiLinesBlocks, line, index)) != null)
                {
                    endIndex = GetBlockEndIndex(line, index, blockForLineStart, lineStartInBlock);
                    // try next line.
                    if (endIndex < 0)
                    {
                        AddSegments(seg, index, line.Length - index, blockForLineStart, !lineStartInBlock, false);
                        return blockForLineStart;
                    }

                    AddSegments(seg, index, endIndex - index, blockForLineStart, !lineStartInBlock, true);
                    blockForLineStart = null;
                    index = endIndex;
                    continue;
                }

                // check single line block
                SingleLineBlock singleLineBlock = MatchLineBlockStart(this.profile.SingleLineBlocks, line, index);
                if (singleLineBlock != null)
                {
                    endIndex = GetBlockEndIndex(line, index, singleLineBlock, false);
                    endIndex = endIndex < 0 ? line.Length : endIndex;

                    AddSegments(seg, index, endIndex - index, singleLineBlock, true, true);
                    index = endIndex;
                    continue;
                }

                // check tokens
                Token token;
                var match = MatchToken(line, index, out token);
                if (match != null)
                {
                    AddSegments(seg, token, match);
                    index += match.Length;
                    continue;
                }

                // check keywords
                endIndex = GetNextTokenEndIndex(line, index);
                if (endIndex > index)
                {
                    int length = endIndex - index;
                    var keywords = MatchKeyword(line, index, length);
                    if (keywords != null)
                    {
                        seg.Add(new TextLineInfo.TextSegment(index, length, keywords.Foreground));
                    }
                    index = endIndex + 1;
                    continue;
                }

                ++index;
            }

            return blockForLineStart;
        }

        // Check if given line can match token using regular expression.
        private Match MatchToken(string line, int index, out Token token)
        {
            foreach (var t in this.profile.Tokens)
            {
                Match match = t.Pattern.Match(line, index);
                if (match.Success && match.Index == index)
                {
                    token = t;
                    return match;
                }
            }

            token = null;
            return null;
        }

        // Add segments to TextLineInfo if there is token matches.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, Token token, Match match)
        {
            if (token.Groups == null || token.Groups.Length == 0)
            {
                seg.Add(new TextLineInfo.TextSegment(match.Index, match.Length, token.Foreground));
                return;
            }

            int index = match.Index;
            var groups = token.Groups
                .Select(g => new { Foreground = g.Foreground, Captrue = match.Groups[g.Name] })
                .OrderBy(g => g.Captrue.Index);

            foreach (var g in groups)
            {
                if (index != g.Captrue.Index)
                {
                    seg.Add(new TextLineInfo.TextSegment(index, g.Captrue.Index - index, token.Foreground));
                }

                seg.Add(new TextLineInfo.TextSegment(g.Captrue.Index, g.Captrue.Length, g.Foreground));
                index = g.Captrue.Index + g.Captrue.Length;
            }

            if (index != match.Index + match.Length)
            {
                seg.Add(new TextLineInfo.TextSegment(index, match.Index + match.Length - index, token.Foreground));
            }
        }

        // Add segments to TextLineInfo if there is code block found.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, int index, int length, CodeBlock block, bool hasStart, bool hasEnd)
        {
            if (block.WrapperForeground == null)
            {
                seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));
                return;
            }

            if (hasStart)
            {
                seg.Add(new TextLineInfo.TextSegment(index, block.Start.Length, block.WrapperForeground));
                index += block.Start.Length;
                length -= block.Start.Length;
            }

            length = hasEnd ? length - block.End.Length : length;
            seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));

            if (hasEnd)
            {
                seg.Add(new TextLineInfo.TextSegment(index + length, block.End.Length, block.WrapperForeground));
            }
        }

        // Check if line starting from index matches a code block.
        private T MatchLineBlockStart<T>(IEnumerable<T> blocks, string line, int index) where T : CodeBlock
        {
            return blocks.FirstOrDefault(block => StartsWith(line, index, block.Start));
        }

        // Find the block end index.
        private int GetBlockEndIndex(string line, int index, CodeBlock block, bool ignoreBlockStart)
        {
            if (!ignoreBlockStart)
            {
                index += block.Start.Length;
            }

            if (block is SingleLineBlock && string.IsNullOrEmpty(block.End))
            {
                return line.Length;
            }

            for (int i = index; i < line.Length; ++i)
            {
                var escape = block.Escape;
                if (escape != null)
                {
                    string escapeString = block.Escape.EscapeString;
                    if (!string.IsNullOrEmpty(escapeString) && StartsWith(line, i, escapeString))
                    {
                        i += escapeString.Length; // skip next one;
                        continue;
                    }

                    string[] escapeItems = block.Escape.Items ?? (new string[0]);
                    string found = escapeItems.FirstOrDefault(item => StartsWith(line, i, item));
                    if (found != null)
                    {
                        i += found.Length - 1;
                        continue;
                    }
                }

                if (StartsWith(line, i, block.End))
                {
                    return i + block.End.Length;
                }
            }

            // not found.
            return -1;
        }

        // Get next index of end of the token.
        private int GetNextTokenEndIndex(string line, int startIndex)
        {
            for (int i = startIndex; i < line.Length; ++i)
            {
                // Matches delimeter.
                if (Array.BinarySearch(this.profile.Delimiter, line[i]) >= 0)
                {
                    return i;
                }

                if (Array.BinarySearch(this.profile.BackDelimiter, line[i]) >= 0)
                {
                    return i - 1;
                }
            }

            return line.Length;
        }

        // Check if the line starting from index matches a keyword.
        private KeywordCollection MatchKeyword(string line, int index, int length)
        {
            Expect.ArgumentNotNull(line, "line");
            Expect.ArgumentCheck(line.Length >= index + length, "Check line length");

            foreach (var keywords in this.profile.KeywordCollecions)
            {
                int result = keywords.Keywords.BinarySearch(k => CompareKeyword(k, line, index, length));
                if (result >= 0)
                {
                    return keywords;
                }
            }

            return null;
        }

        // Check if the line starting from index starts another string.
        private bool StartsWith(string line, int index, string another)
        {
            if (line.Length < another.Length + index)
            {
                return false;
            }

            for (int i = 0; i < another.Length; ++i)
            {
                if (GetChar(line[index + i]) != GetChar(another[i]))
                {
                    return false;
                }
            }

            return true;
        }

        // Compare a keyword.
        private int CompareKeyword(string keyword, string line, int index, int length)
        {
            for (int i = 0; i < Math.Min(keyword.Length, length); ++i)
            {
                int result = GetChar(keyword[i]) - GetChar(line[index + i]);
                if (result != 0)
                {
                    return result;
                }
            }

            return keyword.Length - length;
        }

        // Convert a char.
        private char GetChar(char c)
        {
            return this.profile.IgnoreCase ? char.ToUpper(c) : c;
        }

        #endregion
    }
}
//---------------------------------------------------------------------
// <copyright file="HighlightParser.cs" company="Microsoft">
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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// A class to parse text lines.
    /// </summary>
    public class HighlightParser
    {
        private HighlightProfile profile;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="profile"></param>
        public HighlightParser(HighlightProfile profile)
        {
            this.profile = profile;
        }

        /// <summary>
        /// Parse the text lines;
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public TextLineInfo[] Parse(string[] lines, int startLine)
        {
            Expect.ArgumentNotNull(lines, "lines");
            Expect.ArgumentCheck(startLine >= 0, "startLine should >= 0");

            MultiLinesBlock inBlock = null;
            TextLineInfo[] lineInfos = new TextLineInfo[lines.Length];
            for (int i = 0; i < lines.Length; ++i)
            {
                lineInfos[i] = new TextLineInfo(lines[i], startLine + i);
                if (this.profile != null)
                {
                    inBlock = ParseLine(lineInfos[i], inBlock);
                }
            }

            return lineInfos;
        }

        #region Private Methods

        // Parse single line.
        private MultiLinesBlock ParseLine(TextLineInfo info, MultiLinesBlock blockForLineStart)
        {
            var seg = info.Segments;
            string line = info.TextLine;

            int index = 0;
            while (index < line.Length)
            {
                int endIndex;
                // check multiline block
                bool lineStartInBlock = blockForLineStart != null;
                if (blockForLineStart != null
                    || (blockForLineStart = MatchLineBlockStart(this.profile.MultiLinesBlocks, line, index)) != null)
                {
                    endIndex = GetBlockEndIndex(line, index, blockForLineStart, lineStartInBlock);
                    // try next line.
                    if (endIndex < 0)
                    {
                        AddSegments(seg, index, line.Length - index, blockForLineStart, !lineStartInBlock, false);
                        return blockForLineStart;
                    }

                    AddSegments(seg, index, endIndex - index, blockForLineStart, !lineStartInBlock, true);
                    blockForLineStart = null;
                    index = endIndex;
                    continue;
                }

                // check single line block
                SingleLineBlock singleLineBlock = MatchLineBlockStart(this.profile.SingleLineBlocks, line, index);
                if (singleLineBlock != null)
                {
                    endIndex = GetBlockEndIndex(line, index, singleLineBlock, false);
                    endIndex = endIndex < 0 ? line.Length : endIndex;

                    AddSegments(seg, index, endIndex - index, singleLineBlock, true, true);
                    index = endIndex;
                    continue;
                }

                // check tokens
                Token token;
                var match = MatchToken(line, index, out token);
                if (match != null)
                {
                    AddSegments(seg, token, match);
                    index += match.Length;
                    continue;
                }

                // check keywords
                endIndex = GetNextTokenEndIndex(line, index);
                if (endIndex > index)
                {
                    int length = endIndex - index;
                    var keywords = MatchKeyword(line, index, length);
                    if (keywords != null)
                    {
                        seg.Add(new TextLineInfo.TextSegment(index, length, keywords.Foreground));
                    }
                    index = endIndex + 1;
                    continue;
                }

                ++index;
            }

            return blockForLineStart;
        }

        // Check if given line can match token using regular expression.
        private Match MatchToken(string line, int index, out Token token)
        {
            foreach (var t in this.profile.Tokens)
            {
                Match match = t.Pattern.Match(line, index);
                if (match.Success && match.Index == index)
                {
                    token = t;
                    return match;
                }
            }

            token = null;
            return null;
        }

        // Add segments to TextLineInfo if there is token matches.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, Token token, Match match)
        {
            if (token.Groups == null || token.Groups.Length == 0)
            {
                seg.Add(new TextLineInfo.TextSegment(match.Index, match.Length, token.Foreground));
                return;
            }

            int index = match.Index;
            var groups = token.Groups
                .Select(g => new { Foreground = g.Foreground, Captrue = match.Groups[g.Name] })
                .OrderBy(g => g.Captrue.Index);

            foreach (var g in groups)
            {
                if (index != g.Captrue.Index)
                {
                    seg.Add(new TextLineInfo.TextSegment(index, g.Captrue.Index - index, token.Foreground));
                }

                seg.Add(new TextLineInfo.TextSegment(g.Captrue.Index, g.Captrue.Length, g.Foreground));
                index = g.Captrue.Index + g.Captrue.Length;
            }

            if (index != match.Index + match.Length)
            {
                seg.Add(new TextLineInfo.TextSegment(index, match.Index + match.Length - index, token.Foreground));
            }
        }

        // Add segments to TextLineInfo if there is code block found.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, int index, int length, CodeBlock block, bool hasStart, bool hasEnd)
        {
            if (block.WrapperForeground == null)
            {
                seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));
                return;
            }

            if (hasStart)
            {
                seg.Add(new TextLineInfo.TextSegment(index, block.Start.Length, block.WrapperForeground));
                index += block.Start.Length;
                length -= block.Start.Length;
            }

            length = hasEnd ? length - block.End.Length : length;
            seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));

            if (hasEnd)
            {
                seg.Add(new TextLineInfo.TextSegment(index + length, block.End.Length, block.WrapperForeground));
            }
        }

        // Check if line starting from index matches a code block.
        private T MatchLineBlockStart<T>(IEnumerable<T> blocks, string line, int index) where T : CodeBlock
        {
            return blocks.FirstOrDefault(block => StartsWith(line, index, block.Start));
        }

        // Find the block end index.
        private int GetBlockEndIndex(string line, int index, CodeBlock block, bool ignoreBlockStart)
        {
            if (!ignoreBlockStart)
            {
                index += block.Start.Length;
            }

            if (block is SingleLineBlock && string.IsNullOrEmpty(block.End))
            {
                return line.Length;
            }

            for (int i = index; i < line.Length; ++i)
            {
                var escape = block.Escape;
                if (escape != null)
                {
                    string escapeString = block.Escape.EscapeString;
                    if (!string.IsNullOrEmpty(escapeString) && StartsWith(line, i, escapeString))
                    {
                        i += escapeString.Length; // skip next one;
                        continue;
                    }

                    string[] escapeItems = block.Escape.Items ?? (new string[0]);
                    string found = escapeItems.FirstOrDefault(item => StartsWith(line, i, item));
                    if (found != null)
                    {
                        i += found.Length - 1;
                        continue;
                    }
                }

                if (StartsWith(line, i, block.End))
                {
                    return i + block.End.Length;
                }
            }

            // not found.
            return -1;
        }

        // Get next index of end of the token.
        private int GetNextTokenEndIndex(string line, int startIndex)
        {
            for (int i = startIndex; i < line.Length; ++i)
            {
                // Matches delimeter.
                if (Array.BinarySearch(this.profile.Delimiter, line[i]) >= 0)
                {
                    return i;
                }

                if (Array.BinarySearch(this.profile.BackDelimiter, line[i]) >= 0)
                {
                    return i - 1;
                }
            }

            return line.Length;
        }

        // Check if the line starting from index matches a keyword.
        private KeywordCollection MatchKeyword(string line, int index, int length)
        {
            Expect.ArgumentNotNull(line, "line");
            Expect.ArgumentCheck(line.Length >= index + length, "Check line length");

            foreach (var keywords in this.profile.KeywordCollecions)
            {
                int result = keywords.Keywords.BinarySearch(k => CompareKeyword(k, line, index, length));
                if (result >= 0)
                {
                    return keywords;
                }
            }

            return null;
        }

        // Check if the line starting from index starts another string.
        private bool StartsWith(string line, int index, string another)
        {
            if (line.Length < another.Length + index)
            {
                return false;
            }

            for (int i = 0; i < another.Length; ++i)
            {
                if (GetChar(line[index + i]) != GetChar(another[i]))
                {
                    return false;
                }
            }

            return true;
        }

        // Compare a keyword.
        private int CompareKeyword(string keyword, string line, int index, int length)
        {
            for (int i = 0; i < Math.Min(keyword.Length, length); ++i)
            {
                int result = GetChar(keyword[i]) - GetChar(line[index + i]);
                if (result != 0)
                {
                    return result;
                }
            }

            return keyword.Length - length;
        }

        // Convert a char.
        private char GetChar(char c)
        {
            return this.profile.IgnoreCase ? char.ToUpper(c) : c;
        }

        #endregion
    }
}
//---------------------------------------------------------------------
// <copyright file="HighlightParser.cs" company="Microsoft">
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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// A class to parse text lines.
    /// </summary>
    public class HighlightParser
    {
        private HighlightProfile profile;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="profile"></param>
        public HighlightParser(HighlightProfile profile)
        {
            this.profile = profile;
        }

        /// <summary>
        /// Parse the text lines;
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public TextLineInfo[] Parse(string[] lines, int startLine)
        {
            Expect.ArgumentNotNull(lines, "lines");
            Expect.ArgumentCheck(startLine >= 0, "startLine should >= 0");

            MultiLinesBlock inBlock = null;
            TextLineInfo[] lineInfos = new TextLineInfo[lines.Length];
            for (int i = 0; i < lines.Length; ++i)
            {
                lineInfos[i] = new TextLineInfo(lines[i], startLine + i);
                if (this.profile != null)
                {
                    inBlock = ParseLine(lineInfos[i], inBlock);
                }
            }

            return lineInfos;
        }

        #region Private Methods

        // Parse single line.
        private MultiLinesBlock ParseLine(TextLineInfo info, MultiLinesBlock blockForLineStart)
        {
            var seg = info.Segments;
            string line = info.TextLine;

            int index = 0;
            while (index < line.Length)
            {
                int endIndex;
                // check multiline block
                bool lineStartInBlock = blockForLineStart != null;
                if (blockForLineStart != null
                    || (blockForLineStart = MatchLineBlockStart(this.profile.MultiLinesBlocks, line, index)) != null)
                {
                    endIndex = GetBlockEndIndex(line, index, blockForLineStart, lineStartInBlock);
                    // try next line.
                    if (endIndex < 0)
                    {
                        AddSegments(seg, index, line.Length - index, blockForLineStart, !lineStartInBlock, false);
                        return blockForLineStart;
                    }

                    AddSegments(seg, index, endIndex - index, blockForLineStart, !lineStartInBlock, true);
                    blockForLineStart = null;
                    index = endIndex;
                    continue;
                }

                // check single line block
                SingleLineBlock singleLineBlock = MatchLineBlockStart(this.profile.SingleLineBlocks, line, index);
                if (singleLineBlock != null)
                {
                    endIndex = GetBlockEndIndex(line, index, singleLineBlock, false);
                    endIndex = endIndex < 0 ? line.Length : endIndex;

                    AddSegments(seg, index, endIndex - index, singleLineBlock, true, true);
                    index = endIndex;
                    continue;
                }

                // check tokens
                Token token;
                var match = MatchToken(line, index, out token);
                if (match != null)
                {
                    AddSegments(seg, token, match);
                    index += match.Length;
                    continue;
                }

                // check keywords
                endIndex = GetNextTokenEndIndex(line, index);
                if (endIndex > index)
                {
                    int length = endIndex - index;
                    var keywords = MatchKeyword(line, index, length);
                    if (keywords != null)
                    {
                        seg.Add(new TextLineInfo.TextSegment(index, length, keywords.Foreground));
                    }
                    index = endIndex + 1;
                    continue;
                }

                ++index;
            }

            return blockForLineStart;
        }

        // Check if given line can match token using regular expression.
        private Match MatchToken(string line, int index, out Token token)
        {
            foreach (var t in this.profile.Tokens)
            {
                Match match = t.Pattern.Match(line, index);
                if (match.Success && match.Index == index)
                {
                    token = t;
                    return match;
                }
            }

            token = null;
            return null;
        }

        // Add segments to TextLineInfo if there is token matches.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, Token token, Match match)
        {
            if (token.Groups == null || token.Groups.Length == 0)
            {
                seg.Add(new TextLineInfo.TextSegment(match.Index, match.Length, token.Foreground));
                return;
            }

            int index = match.Index;
            var groups = token.Groups
                .Select(g => new { Foreground = g.Foreground, Captrue = match.Groups[g.Name] })
                .OrderBy(g => g.Captrue.Index);

            foreach (var g in groups)
            {
                if (index != g.Captrue.Index)
                {
                    seg.Add(new TextLineInfo.TextSegment(index, g.Captrue.Index - index, token.Foreground));
                }

                seg.Add(new TextLineInfo.TextSegment(g.Captrue.Index, g.Captrue.Length, g.Foreground));
                index = g.Captrue.Index + g.Captrue.Length;
            }

            if (index != match.Index + match.Length)
            {
                seg.Add(new TextLineInfo.TextSegment(index, match.Index + match.Length - index, token.Foreground));
            }
        }

        // Add segments to TextLineInfo if there is code block found.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, int index, int length, CodeBlock block, bool hasStart, bool hasEnd)
        {
            if (block.WrapperForeground == null)
            {
                seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));
                return;
            }

            if (hasStart)
            {
                seg.Add(new TextLineInfo.TextSegment(index, block.Start.Length, block.WrapperForeground));
                index += block.Start.Length;
                length -= block.Start.Length;
            }

            length = hasEnd ? length - block.End.Length : length;
            seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));

            if (hasEnd)
            {
                seg.Add(new TextLineInfo.TextSegment(index + length, block.End.Length, block.WrapperForeground));
            }
        }

        // Check if line starting from index matches a code block.
        private T MatchLineBlockStart<T>(IEnumerable<T> blocks, string line, int index) where T : CodeBlock
        {
            return blocks.FirstOrDefault(block => StartsWith(line, index, block.Start));
        }

        // Find the block end index.
        private int GetBlockEndIndex(string line, int index, CodeBlock block, bool ignoreBlockStart)
        {
            if (!ignoreBlockStart)
            {
                index += block.Start.Length;
            }

            if (block is SingleLineBlock && string.IsNullOrEmpty(block.End))
            {
                return line.Length;
            }

            for (int i = index; i < line.Length; ++i)
            {
                var escape = block.Escape;
                if (escape != null)
                {
                    string escapeString = block.Escape.EscapeString;
                    if (!string.IsNullOrEmpty(escapeString) && StartsWith(line, i, escapeString))
                    {
                        i += escapeString.Length; // skip next one;
                        continue;
                    }

                    string[] escapeItems = block.Escape.Items ?? (new string[0]);
                    string found = escapeItems.FirstOrDefault(item => StartsWith(line, i, item));
                    if (found != null)
                    {
                        i += found.Length - 1;
                        continue;
                    }
                }

                if (StartsWith(line, i, block.End))
                {
                    return i + block.End.Length;
                }
            }

            // not found.
            return -1;
        }

        // Get next index of end of the token.
        private int GetNextTokenEndIndex(string line, int startIndex)
        {
            for (int i = startIndex; i < line.Length; ++i)
            {
                // Matches delimeter.
                if (Array.BinarySearch(this.profile.Delimiter, line[i]) >= 0)
                {
                    return i;
                }

                if (Array.BinarySearch(this.profile.BackDelimiter, line[i]) >= 0)
                {
                    return i - 1;
                }
            }

            return line.Length;
        }

        // Check if the line starting from index matches a keyword.
        private KeywordCollection MatchKeyword(string line, int index, int length)
        {
            Expect.ArgumentNotNull(line, "line");
            Expect.ArgumentCheck(line.Length >= index + length, "Check line length");

            foreach (var keywords in this.profile.KeywordCollecions)
            {
                int result = keywords.Keywords.BinarySearch(k => CompareKeyword(k, line, index, length));
                if (result >= 0)
                {
                    return keywords;
                }
            }

            return null;
        }

        // Check if the line starting from index starts another string.
        private bool StartsWith(string line, int index, string another)
        {
            if (line.Length < another.Length + index)
            {
                return false;
            }

            for (int i = 0; i < another.Length; ++i)
            {
                if (GetChar(line[index + i]) != GetChar(another[i]))
                {
                    return false;
                }
            }

            return true;
        }

        // Compare a keyword.
        private int CompareKeyword(string keyword, string line, int index, int length)
        {
            for (int i = 0; i < Math.Min(keyword.Length, length); ++i)
            {
                int result = GetChar(keyword[i]) - GetChar(line[index + i]);
                if (result != 0)
                {
                    return result;
                }
            }

            return keyword.Length - length;
        }

        // Convert a char.
        private char GetChar(char c)
        {
            return this.profile.IgnoreCase ? char.ToUpper(c) : c;
        }

        #endregion
    }
}
//---------------------------------------------------------------------
// <copyright file="HighlightParser.cs" company="Microsoft">
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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// A class to parse text lines.
    /// </summary>
    public class HighlightParser
    {
        private HighlightProfile profile;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="profile"></param>
        public HighlightParser(HighlightProfile profile)
        {
            this.profile = profile;
        }

        /// <summary>
        /// Parse the text lines;
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public TextLineInfo[] Parse(string[] lines, int startLine)
        {
            Expect.ArgumentNotNull(lines, "lines");
            Expect.ArgumentCheck(startLine >= 0, "startLine should >= 0");

            MultiLinesBlock inBlock = null;
            TextLineInfo[] lineInfos = new TextLineInfo[lines.Length];
            for (int i = 0; i < lines.Length; ++i)
            {
                lineInfos[i] = new TextLineInfo(lines[i], startLine + i);
                if (this.profile != null)
                {
                    inBlock = ParseLine(lineInfos[i], inBlock);
                }
            }

            return lineInfos;
        }

        #region Private Methods

        // Parse single line.
        private MultiLinesBlock ParseLine(TextLineInfo info, MultiLinesBlock blockForLineStart)
        {
            var seg = info.Segments;
            string line = info.TextLine;

            int index = 0;
            while (index < line.Length)
            {
                int endIndex;
                // check multiline block
                bool lineStartInBlock = blockForLineStart != null;
                if (blockForLineStart != null
                    || (blockForLineStart = MatchLineBlockStart(this.profile.MultiLinesBlocks, line, index)) != null)
                {
                    endIndex = GetBlockEndIndex(line, index, blockForLineStart, lineStartInBlock);
                    // try next line.
                    if (endIndex < 0)
                    {
                        AddSegments(seg, index, line.Length - index, blockForLineStart, !lineStartInBlock, false);
                        return blockForLineStart;
                    }

                    AddSegments(seg, index, endIndex - index, blockForLineStart, !lineStartInBlock, true);
                    blockForLineStart = null;
                    index = endIndex;
                    continue;
                }

                // check single line block
                SingleLineBlock singleLineBlock = MatchLineBlockStart(this.profile.SingleLineBlocks, line, index);
                if (singleLineBlock != null)
                {
                    endIndex = GetBlockEndIndex(line, index, singleLineBlock, false);
                    endIndex = endIndex < 0 ? line.Length : endIndex;

                    AddSegments(seg, index, endIndex - index, singleLineBlock, true, true);
                    index = endIndex;
                    continue;
                }

                // check tokens
                Token token;
                var match = MatchToken(line, index, out token);
                if (match != null)
                {
                    AddSegments(seg, token, match);
                    index += match.Length;
                    continue;
                }

                // check keywords
                endIndex = GetNextTokenEndIndex(line, index);
                if (endIndex > index)
                {
                    int length = endIndex - index;
                    var keywords = MatchKeyword(line, index, length);
                    if (keywords != null)
                    {
                        seg.Add(new TextLineInfo.TextSegment(index, length, keywords.Foreground));
                    }
                    index = endIndex + 1;
                    continue;
                }

                ++index;
            }

            return blockForLineStart;
        }

        // Check if given line can match token using regular expression.
        private Match MatchToken(string line, int index, out Token token)
        {
            foreach (var t in this.profile.Tokens)
            {
                Match match = t.Pattern.Match(line, index);
                if (match.Success && match.Index == index)
                {
                    token = t;
                    return match;
                }
            }

            token = null;
            return null;
        }

        // Add segments to TextLineInfo if there is token matches.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, Token token, Match match)
        {
            if (token.Groups == null || token.Groups.Length == 0)
            {
                seg.Add(new TextLineInfo.TextSegment(match.Index, match.Length, token.Foreground));
                return;
            }

            int index = match.Index;
            var groups = token.Groups
                .Select(g => new { Foreground = g.Foreground, Captrue = match.Groups[g.Name] })
                .OrderBy(g => g.Captrue.Index);

            foreach (var g in groups)
            {
                if (index != g.Captrue.Index)
                {
                    seg.Add(new TextLineInfo.TextSegment(index, g.Captrue.Index - index, token.Foreground));
                }

                seg.Add(new TextLineInfo.TextSegment(g.Captrue.Index, g.Captrue.Length, g.Foreground));
                index = g.Captrue.Index + g.Captrue.Length;
            }

            if (index != match.Index + match.Length)
            {
                seg.Add(new TextLineInfo.TextSegment(index, match.Index + match.Length - index, token.Foreground));
            }
        }

        // Add segments to TextLineInfo if there is code block found.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, int index, int length, CodeBlock block, bool hasStart, bool hasEnd)
        {
            if (block.WrapperForeground == null)
            {
                seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));
                return;
            }

            if (hasStart)
            {
                seg.Add(new TextLineInfo.TextSegment(index, block.Start.Length, block.WrapperForeground));
                index += block.Start.Length;
                length -= block.Start.Length;
            }

            length = hasEnd ? length - block.End.Length : length;
            seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));

            if (hasEnd)
            {
                seg.Add(new TextLineInfo.TextSegment(index + length, block.End.Length, block.WrapperForeground));
            }
        }

        // Check if line starting from index matches a code block.
        private T MatchLineBlockStart<T>(IEnumerable<T> blocks, string line, int index) where T : CodeBlock
        {
            return blocks.FirstOrDefault(block => StartsWith(line, index, block.Start));
        }

        // Find the block end index.
        private int GetBlockEndIndex(string line, int index, CodeBlock block, bool ignoreBlockStart)
        {
            if (!ignoreBlockStart)
            {
                index += block.Start.Length;
            }

            if (block is SingleLineBlock && string.IsNullOrEmpty(block.End))
            {
                return line.Length;
            }

            for (int i = index; i < line.Length; ++i)
            {
                var escape = block.Escape;
                if (escape != null)
                {
                    string escapeString = block.Escape.EscapeString;
                    if (!string.IsNullOrEmpty(escapeString) && StartsWith(line, i, escapeString))
                    {
                        i += escapeString.Length; // skip next one;
                        continue;
                    }

                    string[] escapeItems = block.Escape.Items ?? (new string[0]);
                    string found = escapeItems.FirstOrDefault(item => StartsWith(line, i, item));
                    if (found != null)
                    {
                        i += found.Length - 1;
                        continue;
                    }
                }

                if (StartsWith(line, i, block.End))
                {
                    return i + block.End.Length;
                }
            }

            // not found.
            return -1;
        }

        // Get next index of end of the token.
        private int GetNextTokenEndIndex(string line, int startIndex)
        {
            for (int i = startIndex; i < line.Length; ++i)
            {
                // Matches delimeter.
                if (Array.BinarySearch(this.profile.Delimiter, line[i]) >= 0)
                {
                    return i;
                }

                if (Array.BinarySearch(this.profile.BackDelimiter, line[i]) >= 0)
                {
                    return i - 1;
                }
            }

            return line.Length;
        }

        // Check if the line starting from index matches a keyword.
        private KeywordCollection MatchKeyword(string line, int index, int length)
        {
            Expect.ArgumentNotNull(line, "line");
            Expect.ArgumentCheck(line.Length >= index + length, "Check line length");

            foreach (var keywords in this.profile.KeywordCollecions)
            {
                int result = keywords.Keywords.BinarySearch(k => CompareKeyword(k, line, index, length));
                if (result >= 0)
                {
                    return keywords;
                }
            }

            return null;
        }

        // Check if the line starting from index starts another string.
        private bool StartsWith(string line, int index, string another)
        {
            if (line.Length < another.Length + index)
            {
                return false;
            }

            for (int i = 0; i < another.Length; ++i)
            {
                if (GetChar(line[index + i]) != GetChar(another[i]))
                {
                    return false;
                }
            }

            return true;
        }

        // Compare a keyword.
        private int CompareKeyword(string keyword, string line, int index, int length)
        {
            for (int i = 0; i < Math.Min(keyword.Length, length); ++i)
            {
                int result = GetChar(keyword[i]) - GetChar(line[index + i]);
                if (result != 0)
                {
                    return result;
                }
            }

            return keyword.Length - length;
        }

        // Convert a char.
        private char GetChar(char c)
        {
            return this.profile.IgnoreCase ? char.ToUpper(c) : c;
        }

        #endregion
    }
}
//---------------------------------------------------------------------
// <copyright file="HighlightParser.cs" company="Microsoft">
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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// A class to parse text lines.
    /// </summary>
    public class HighlightParser
    {
        private HighlightProfile profile;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="profile"></param>
        public HighlightParser(HighlightProfile profile)
        {
            this.profile = profile;
        }

        /// <summary>
        /// Parse the text lines;
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public TextLineInfo[] Parse(string[] lines, int startLine)
        {
            Expect.ArgumentNotNull(lines, "lines");
            Expect.ArgumentCheck(startLine >= 0, "startLine should >= 0");

            MultiLinesBlock inBlock = null;
            TextLineInfo[] lineInfos = new TextLineInfo[lines.Length];
            for (int i = 0; i < lines.Length; ++i)
            {
                lineInfos[i] = new TextLineInfo(lines[i], startLine + i);
                if (this.profile != null)
                {
                    inBlock = ParseLine(lineInfos[i], inBlock);
                }
            }

            return lineInfos;
        }

        #region Private Methods

        // Parse single line.
        private MultiLinesBlock ParseLine(TextLineInfo info, MultiLinesBlock blockForLineStart)
        {
            var seg = info.Segments;
            string line = info.TextLine;

            int index = 0;
            while (index < line.Length)
            {
                int endIndex;
                // check multiline block
                bool lineStartInBlock = blockForLineStart != null;
                if (blockForLineStart != null
                    || (blockForLineStart = MatchLineBlockStart(this.profile.MultiLinesBlocks, line, index)) != null)
                {
                    endIndex = GetBlockEndIndex(line, index, blockForLineStart, lineStartInBlock);
                    // try next line.
                    if (endIndex < 0)
                    {
                        AddSegments(seg, index, line.Length - index, blockForLineStart, !lineStartInBlock, false);
                        return blockForLineStart;
                    }

                    AddSegments(seg, index, endIndex - index, blockForLineStart, !lineStartInBlock, true);
                    blockForLineStart = null;
                    index = endIndex;
                    continue;
                }

                // check single line block
                SingleLineBlock singleLineBlock = MatchLineBlockStart(this.profile.SingleLineBlocks, line, index);
                if (singleLineBlock != null)
                {
                    endIndex = GetBlockEndIndex(line, index, singleLineBlock, false);
                    endIndex = endIndex < 0 ? line.Length : endIndex;

                    AddSegments(seg, index, endIndex - index, singleLineBlock, true, true);
                    index = endIndex;
                    continue;
                }

                // check tokens
                Token token;
                var match = MatchToken(line, index, out token);
                if (match != null)
                {
                    AddSegments(seg, token, match);
                    index += match.Length;
                    continue;
                }

                // check keywords
                endIndex = GetNextTokenEndIndex(line, index);
                if (endIndex > index)
                {
                    int length = endIndex - index;
                    var keywords = MatchKeyword(line, index, length);
                    if (keywords != null)
                    {
                        seg.Add(new TextLineInfo.TextSegment(index, length, keywords.Foreground));
                    }
                    index = endIndex + 1;
                    continue;
                }

                ++index;
            }

            return blockForLineStart;
        }

        // Check if given line can match token using regular expression.
        private Match MatchToken(string line, int index, out Token token)
        {
            foreach (var t in this.profile.Tokens)
            {
                Match match = t.Pattern.Match(line, index);
                if (match.Success && match.Index == index)
                {
                    token = t;
                    return match;
                }
            }

            token = null;
            return null;
        }

        // Add segments to TextLineInfo if there is token matches.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, Token token, Match match)
        {
            if (token.Groups == null || token.Groups.Length == 0)
            {
                seg.Add(new TextLineInfo.TextSegment(match.Index, match.Length, token.Foreground));
                return;
            }

            int index = match.Index;
            var groups = token.Groups
                .Select(g => new { Foreground = g.Foreground, Captrue = match.Groups[g.Name] })
                .OrderBy(g => g.Captrue.Index);

            foreach (var g in groups)
            {
                if (index != g.Captrue.Index)
                {
                    seg.Add(new TextLineInfo.TextSegment(index, g.Captrue.Index - index, token.Foreground));
                }

                seg.Add(new TextLineInfo.TextSegment(g.Captrue.Index, g.Captrue.Length, g.Foreground));
                index = g.Captrue.Index + g.Captrue.Length;
            }

            if (index != match.Index + match.Length)
            {
                seg.Add(new TextLineInfo.TextSegment(index, match.Index + match.Length - index, token.Foreground));
            }
        }

        // Add segments to TextLineInfo if there is code block found.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, int index, int length, CodeBlock block, bool hasStart, bool hasEnd)
        {
            if (block.WrapperForeground == null)
            {
                seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));
                return;
            }

            if (hasStart)
            {
                seg.Add(new TextLineInfo.TextSegment(index, block.Start.Length, block.WrapperForeground));
                index += block.Start.Length;
                length -= block.Start.Length;
            }

            length = hasEnd ? length - block.End.Length : length;
            seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));

            if (hasEnd)
            {
                seg.Add(new TextLineInfo.TextSegment(index + length, block.End.Length, block.WrapperForeground));
            }
        }

        // Check if line starting from index matches a code block.
        private T MatchLineBlockStart<T>(IEnumerable<T> blocks, string line, int index) where T : CodeBlock
        {
            return blocks.FirstOrDefault(block => StartsWith(line, index, block.Start));
        }

        // Find the block end index.
        private int GetBlockEndIndex(string line, int index, CodeBlock block, bool ignoreBlockStart)
        {
            if (!ignoreBlockStart)
            {
                index += block.Start.Length;
            }

            if (block is SingleLineBlock && string.IsNullOrEmpty(block.End))
            {
                return line.Length;
            }

            for (int i = index; i < line.Length; ++i)
            {
                var escape = block.Escape;
                if (escape != null)
                {
                    string escapeString = block.Escape.EscapeString;
                    if (!string.IsNullOrEmpty(escapeString) && StartsWith(line, i, escapeString))
                    {
                        i += escapeString.Length; // skip next one;
                        continue;
                    }

                    string[] escapeItems = block.Escape.Items ?? (new string[0]);
                    string found = escapeItems.FirstOrDefault(item => StartsWith(line, i, item));
                    if (found != null)
                    {
                        i += found.Length - 1;
                        continue;
                    }
                }

                if (StartsWith(line, i, block.End))
                {
                    return i + block.End.Length;
                }
            }

            // not found.
            return -1;
        }

        // Get next index of end of the token.
        private int GetNextTokenEndIndex(string line, int startIndex)
        {
            for (int i = startIndex; i < line.Length; ++i)
            {
                // Matches delimeter.
                if (Array.BinarySearch(this.profile.Delimiter, line[i]) >= 0)
                {
                    return i;
                }

                if (Array.BinarySearch(this.profile.BackDelimiter, line[i]) >= 0)
                {
                    return i - 1;
                }
            }

            return line.Length;
        }

        // Check if the line starting from index matches a keyword.
        private KeywordCollection MatchKeyword(string line, int index, int length)
        {
            Expect.ArgumentNotNull(line, "line");
            Expect.ArgumentCheck(line.Length >= index + length, "Check line length");

            foreach (var keywords in this.profile.KeywordCollecions)
            {
                int result = keywords.Keywords.BinarySearch(k => CompareKeyword(k, line, index, length));
                if (result >= 0)
                {
                    return keywords;
                }
            }

            return null;
        }

        // Check if the line starting from index starts another string.
        private bool StartsWith(string line, int index, string another)
        {
            if (line.Length < another.Length + index)
            {
                return false;
            }

            for (int i = 0; i < another.Length; ++i)
            {
                if (GetChar(line[index + i]) != GetChar(another[i]))
                {
                    return false;
                }
            }

            return true;
        }

        // Compare a keyword.
        private int CompareKeyword(string keyword, string line, int index, int length)
        {
            for (int i = 0; i < Math.Min(keyword.Length, length); ++i)
            {
                int result = GetChar(keyword[i]) - GetChar(line[index + i]);
                if (result != 0)
                {
                    return result;
                }
            }

            return keyword.Length - length;
        }

        // Convert a char.
        private char GetChar(char c)
        {
            return this.profile.IgnoreCase ? char.ToUpper(c) : c;
        }

        #endregion
    }
}
//---------------------------------------------------------------------
// <copyright file="HighlightParser.cs" company="Microsoft">
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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// A class to parse text lines.
    /// </summary>
    public class HighlightParser
    {
        private HighlightProfile profile;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="profile"></param>
        public HighlightParser(HighlightProfile profile)
        {
            this.profile = profile;
        }

        /// <summary>
        /// Parse the text lines;
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public TextLineInfo[] Parse(string[] lines, int startLine)
        {
            Expect.ArgumentNotNull(lines, "lines");
            Expect.ArgumentCheck(startLine >= 0, "startLine should >= 0");

            MultiLinesBlock inBlock = null;
            TextLineInfo[] lineInfos = new TextLineInfo[lines.Length];
            for (int i = 0; i < lines.Length; ++i)
            {
                lineInfos[i] = new TextLineInfo(lines[i], startLine + i);
                if (this.profile != null)
                {
                    inBlock = ParseLine(lineInfos[i], inBlock);
                }
            }

            return lineInfos;
        }

        #region Private Methods

        // Parse single line.
        private MultiLinesBlock ParseLine(TextLineInfo info, MultiLinesBlock blockForLineStart)
        {
            var seg = info.Segments;
            string line = info.TextLine;

            int index = 0;
            while (index < line.Length)
            {
                int endIndex;
                // check multiline block
                bool lineStartInBlock = blockForLineStart != null;
                if (blockForLineStart != null
                    || (blockForLineStart = MatchLineBlockStart(this.profile.MultiLinesBlocks, line, index)) != null)
                {
                    endIndex = GetBlockEndIndex(line, index, blockForLineStart, lineStartInBlock);
                    // try next line.
                    if (endIndex < 0)
                    {
                        AddSegments(seg, index, line.Length - index, blockForLineStart, !lineStartInBlock, false);
                        return blockForLineStart;
                    }

                    AddSegments(seg, index, endIndex - index, blockForLineStart, !lineStartInBlock, true);
                    blockForLineStart = null;
                    index = endIndex;
                    continue;
                }

                // check single line block
                SingleLineBlock singleLineBlock = MatchLineBlockStart(this.profile.SingleLineBlocks, line, index);
                if (singleLineBlock != null)
                {
                    endIndex = GetBlockEndIndex(line, index, singleLineBlock, false);
                    endIndex = endIndex < 0 ? line.Length : endIndex;

                    AddSegments(seg, index, endIndex - index, singleLineBlock, true, true);
                    index = endIndex;
                    continue;
                }

                // check tokens
                Token token;
                var match = MatchToken(line, index, out token);
                if (match != null)
                {
                    AddSegments(seg, token, match);
                    index += match.Length;
                    continue;
                }

                // check keywords
                endIndex = GetNextTokenEndIndex(line, index);
                if (endIndex > index)
                {
                    int length = endIndex - index;
                    var keywords = MatchKeyword(line, index, length);
                    if (keywords != null)
                    {
                        seg.Add(new TextLineInfo.TextSegment(index, length, keywords.Foreground));
                    }
                    index = endIndex + 1;
                    continue;
                }

                ++index;
            }

            return blockForLineStart;
        }

        // Check if given line can match token using regular expression.
        private Match MatchToken(string line, int index, out Token token)
        {
            foreach (var t in this.profile.Tokens)
            {
                Match match = t.Pattern.Match(line, index);
                if (match.Success && match.Index == index)
                {
                    token = t;
                    return match;
                }
            }

            token = null;
            return null;
        }

        // Add segments to TextLineInfo if there is token matches.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, Token token, Match match)
        {
            if (token.Groups == null || token.Groups.Length == 0)
            {
                seg.Add(new TextLineInfo.TextSegment(match.Index, match.Length, token.Foreground));
                return;
            }

            int index = match.Index;
            var groups = token.Groups
                .Select(g => new { Foreground = g.Foreground, Captrue = match.Groups[g.Name] })
                .OrderBy(g => g.Captrue.Index);

            foreach (var g in groups)
            {
                if (index != g.Captrue.Index)
                {
                    seg.Add(new TextLineInfo.TextSegment(index, g.Captrue.Index - index, token.Foreground));
                }

                seg.Add(new TextLineInfo.TextSegment(g.Captrue.Index, g.Captrue.Length, g.Foreground));
                index = g.Captrue.Index + g.Captrue.Length;
            }

            if (index != match.Index + match.Length)
            {
                seg.Add(new TextLineInfo.TextSegment(index, match.Index + match.Length - index, token.Foreground));
            }
        }

        // Add segments to TextLineInfo if there is code block found.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, int index, int length, CodeBlock block, bool hasStart, bool hasEnd)
        {
            if (block.WrapperForeground == null)
            {
                seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));
                return;
            }

            if (hasStart)
            {
                seg.Add(new TextLineInfo.TextSegment(index, block.Start.Length, block.WrapperForeground));
                index += block.Start.Length;
                length -= block.Start.Length;
            }

            length = hasEnd ? length - block.End.Length : length;
            seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));

            if (hasEnd)
            {
                seg.Add(new TextLineInfo.TextSegment(index + length, block.End.Length, block.WrapperForeground));
            }
        }

        // Check if line starting from index matches a code block.
        private T MatchLineBlockStart<T>(IEnumerable<T> blocks, string line, int index) where T : CodeBlock
        {
            return blocks.FirstOrDefault(block => StartsWith(line, index, block.Start));
        }

        // Find the block end index.
        private int GetBlockEndIndex(string line, int index, CodeBlock block, bool ignoreBlockStart)
        {
            if (!ignoreBlockStart)
            {
                index += block.Start.Length;
            }

            if (block is SingleLineBlock && string.IsNullOrEmpty(block.End))
            {
                return line.Length;
            }

            for (int i = index; i < line.Length; ++i)
            {
                var escape = block.Escape;
                if (escape != null)
                {
                    string escapeString = block.Escape.EscapeString;
                    if (!string.IsNullOrEmpty(escapeString) && StartsWith(line, i, escapeString))
                    {
                        i += escapeString.Length; // skip next one;
                        continue;
                    }

                    string[] escapeItems = block.Escape.Items ?? (new string[0]);
                    string found = escapeItems.FirstOrDefault(item => StartsWith(line, i, item));
                    if (found != null)
                    {
                        i += found.Length - 1;
                        continue;
                    }
                }

                if (StartsWith(line, i, block.End))
                {
                    return i + block.End.Length;
                }
            }

            // not found.
            return -1;
        }

        // Get next index of end of the token.
        private int GetNextTokenEndIndex(string line, int startIndex)
        {
            for (int i = startIndex; i < line.Length; ++i)
            {
                // Matches delimeter.
                if (Array.BinarySearch(this.profile.Delimiter, line[i]) >= 0)
                {
                    return i;
                }

                if (Array.BinarySearch(this.profile.BackDelimiter, line[i]) >= 0)
                {
                    return i - 1;
                }
            }

            return line.Length;
        }

        // Check if the line starting from index matches a keyword.
        private KeywordCollection MatchKeyword(string line, int index, int length)
        {
            Expect.ArgumentNotNull(line, "line");
            Expect.ArgumentCheck(line.Length >= index + length, "Check line length");

            foreach (var keywords in this.profile.KeywordCollecions)
            {
                int result = keywords.Keywords.BinarySearch(k => CompareKeyword(k, line, index, length));
                if (result >= 0)
                {
                    return keywords;
                }
            }

            return null;
        }

        // Check if the line starting from index starts another string.
        private bool StartsWith(string line, int index, string another)
        {
            if (line.Length < another.Length + index)
            {
                return false;
            }

            for (int i = 0; i < another.Length; ++i)
            {
                if (GetChar(line[index + i]) != GetChar(another[i]))
                {
                    return false;
                }
            }

            return true;
        }

        // Compare a keyword.
        private int CompareKeyword(string keyword, string line, int index, int length)
        {
            for (int i = 0; i < Math.Min(keyword.Length, length); ++i)
            {
                int result = GetChar(keyword[i]) - GetChar(line[index + i]);
                if (result != 0)
                {
                    return result;
                }
            }

            return keyword.Length - length;
        }

        // Convert a char.
        private char GetChar(char c)
        {
            return this.profile.IgnoreCase ? char.ToUpper(c) : c;
        }

        #endregion
    }
}
//---------------------------------------------------------------------
// <copyright file="HighlightParser.cs" company="Microsoft">
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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// A class to parse text lines.
    /// </summary>
    public class HighlightParser
    {
        private HighlightProfile profile;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="profile"></param>
        public HighlightParser(HighlightProfile profile)
        {
            this.profile = profile;
        }

        /// <summary>
        /// Parse the text lines;
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public TextLineInfo[] Parse(string[] lines, int startLine)
        {
            Expect.ArgumentNotNull(lines, "lines");
            Expect.ArgumentCheck(startLine >= 0, "startLine should >= 0");

            MultiLinesBlock inBlock = null;
            TextLineInfo[] lineInfos = new TextLineInfo[lines.Length];
            for (int i = 0; i < lines.Length; ++i)
            {
                lineInfos[i] = new TextLineInfo(lines[i], startLine + i);
                if (this.profile != null)
                {
                    inBlock = ParseLine(lineInfos[i], inBlock);
                }
            }

            return lineInfos;
        }

        #region Private Methods

        // Parse single line.
        private MultiLinesBlock ParseLine(TextLineInfo info, MultiLinesBlock blockForLineStart)
        {
            var seg = info.Segments;
            string line = info.TextLine;

            int index = 0;
            while (index < line.Length)
            {
                int endIndex;
                // check multiline block
                bool lineStartInBlock = blockForLineStart != null;
                if (blockForLineStart != null
                    || (blockForLineStart = MatchLineBlockStart(this.profile.MultiLinesBlocks, line, index)) != null)
                {
                    endIndex = GetBlockEndIndex(line, index, blockForLineStart, lineStartInBlock);
                    // try next line.
                    if (endIndex < 0)
                    {
                        AddSegments(seg, index, line.Length - index, blockForLineStart, !lineStartInBlock, false);
                        return blockForLineStart;
                    }

                    AddSegments(seg, index, endIndex - index, blockForLineStart, !lineStartInBlock, true);
                    blockForLineStart = null;
                    index = endIndex;
                    continue;
                }

                // check single line block
                SingleLineBlock singleLineBlock = MatchLineBlockStart(this.profile.SingleLineBlocks, line, index);
                if (singleLineBlock != null)
                {
                    endIndex = GetBlockEndIndex(line, index, singleLineBlock, false);
                    endIndex = endIndex < 0 ? line.Length : endIndex;

                    AddSegments(seg, index, endIndex - index, singleLineBlock, true, true);
                    index = endIndex;
                    continue;
                }

                // check tokens
                Token token;
                var match = MatchToken(line, index, out token);
                if (match != null)
                {
                    AddSegments(seg, token, match);
                    index += match.Length;
                    continue;
                }

                // check keywords
                endIndex = GetNextTokenEndIndex(line, index);
                if (endIndex > index)
                {
                    int length = endIndex - index;
                    var keywords = MatchKeyword(line, index, length);
                    if (keywords != null)
                    {
                        seg.Add(new TextLineInfo.TextSegment(index, length, keywords.Foreground));
                    }
                    index = endIndex + 1;
                    continue;
                }

                ++index;
            }

            return blockForLineStart;
        }

        // Check if given line can match token using regular expression.
        private Match MatchToken(string line, int index, out Token token)
        {
            foreach (var t in this.profile.Tokens)
            {
                Match match = t.Pattern.Match(line, index);
                if (match.Success && match.Index == index)
                {
                    token = t;
                    return match;
                }
            }

            token = null;
            return null;
        }

        // Add segments to TextLineInfo if there is token matches.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, Token token, Match match)
        {
            if (token.Groups == null || token.Groups.Length == 0)
            {
                seg.Add(new TextLineInfo.TextSegment(match.Index, match.Length, token.Foreground));
                return;
            }

            int index = match.Index;
            var groups = token.Groups
                .Select(g => new { Foreground = g.Foreground, Captrue = match.Groups[g.Name] })
                .OrderBy(g => g.Captrue.Index);

            foreach (var g in groups)
            {
                if (index != g.Captrue.Index)
                {
                    seg.Add(new TextLineInfo.TextSegment(index, g.Captrue.Index - index, token.Foreground));
                }

                seg.Add(new TextLineInfo.TextSegment(g.Captrue.Index, g.Captrue.Length, g.Foreground));
                index = g.Captrue.Index + g.Captrue.Length;
            }

            if (index != match.Index + match.Length)
            {
                seg.Add(new TextLineInfo.TextSegment(index, match.Index + match.Length - index, token.Foreground));
            }
        }

        // Add segments to TextLineInfo if there is code block found.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, int index, int length, CodeBlock block, bool hasStart, bool hasEnd)
        {
            if (block.WrapperForeground == null)
            {
                seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));
                return;
            }

            if (hasStart)
            {
                seg.Add(new TextLineInfo.TextSegment(index, block.Start.Length, block.WrapperForeground));
                index += block.Start.Length;
                length -= block.Start.Length;
            }

            length = hasEnd ? length - block.End.Length : length;
            seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));

            if (hasEnd)
            {
                seg.Add(new TextLineInfo.TextSegment(index + length, block.End.Length, block.WrapperForeground));
            }
        }

        // Check if line starting from index matches a code block.
        private T MatchLineBlockStart<T>(IEnumerable<T> blocks, string line, int index) where T : CodeBlock
        {
            return blocks.FirstOrDefault(block => StartsWith(line, index, block.Start));
        }

        // Find the block end index.
        private int GetBlockEndIndex(string line, int index, CodeBlock block, bool ignoreBlockStart)
        {
            if (!ignoreBlockStart)
            {
                index += block.Start.Length;
            }

            if (block is SingleLineBlock && string.IsNullOrEmpty(block.End))
            {
                return line.Length;
            }

            for (int i = index; i < line.Length; ++i)
            {
                var escape = block.Escape;
                if (escape != null)
                {
                    string escapeString = block.Escape.EscapeString;
                    if (!string.IsNullOrEmpty(escapeString) && StartsWith(line, i, escapeString))
                    {
                        i += escapeString.Length; // skip next one;
                        continue;
                    }

                    string[] escapeItems = block.Escape.Items ?? (new string[0]);
                    string found = escapeItems.FirstOrDefault(item => StartsWith(line, i, item));
                    if (found != null)
                    {
                        i += found.Length - 1;
                        continue;
                    }
                }

                if (StartsWith(line, i, block.End))
                {
                    return i + block.End.Length;
                }
            }

            // not found.
            return -1;
        }

        // Get next index of end of the token.
        private int GetNextTokenEndIndex(string line, int startIndex)
        {
            for (int i = startIndex; i < line.Length; ++i)
            {
                // Matches delimeter.
                if (Array.BinarySearch(this.profile.Delimiter, line[i]) >= 0)
                {
                    return i;
                }

                if (Array.BinarySearch(this.profile.BackDelimiter, line[i]) >= 0)
                {
                    return i - 1;
                }
            }

            return line.Length;
        }

        // Check if the line starting from index matches a keyword.
        private KeywordCollection MatchKeyword(string line, int index, int length)
        {
            Expect.ArgumentNotNull(line, "line");
            Expect.ArgumentCheck(line.Length >= index + length, "Check line length");

            foreach (var keywords in this.profile.KeywordCollecions)
            {
                int result = keywords.Keywords.BinarySearch(k => CompareKeyword(k, line, index, length));
                if (result >= 0)
                {
                    return keywords;
                }
            }

            return null;
        }

        // Check if the line starting from index starts another string.
        private bool StartsWith(string line, int index, string another)
        {
            if (line.Length < another.Length + index)
            {
                return false;
            }

            for (int i = 0; i < another.Length; ++i)
            {
                if (GetChar(line[index + i]) != GetChar(another[i]))
                {
                    return false;
                }
            }

            return true;
        }

        // Compare a keyword.
        private int CompareKeyword(string keyword, string line, int index, int length)
        {
            for (int i = 0; i < Math.Min(keyword.Length, length); ++i)
            {
                int result = GetChar(keyword[i]) - GetChar(line[index + i]);
                if (result != 0)
                {
                    return result;
                }
            }

            return keyword.Length - length;
        }

        // Convert a char.
        private char GetChar(char c)
        {
            return this.profile.IgnoreCase ? char.ToUpper(c) : c;
        }

        #endregion
    }
}
//---------------------------------------------------------------------
// <copyright file="HighlightParser.cs" company="Microsoft">
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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// A class to parse text lines.
    /// </summary>
    public class HighlightParser
    {
        private HighlightProfile profile;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="profile"></param>
        public HighlightParser(HighlightProfile profile)
        {
            this.profile = profile;
        }

        /// <summary>
        /// Parse the text lines;
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public TextLineInfo[] Parse(string[] lines, int startLine)
        {
            Expect.ArgumentNotNull(lines, "lines");
            Expect.ArgumentCheck(startLine >= 0, "startLine should >= 0");

            MultiLinesBlock inBlock = null;
            TextLineInfo[] lineInfos = new TextLineInfo[lines.Length];
            for (int i = 0; i < lines.Length; ++i)
            {
                lineInfos[i] = new TextLineInfo(lines[i], startLine + i);
                if (this.profile != null)
                {
                    inBlock = ParseLine(lineInfos[i], inBlock);
                }
            }

            return lineInfos;
        }

        #region Private Methods

        // Parse single line.
        private MultiLinesBlock ParseLine(TextLineInfo info, MultiLinesBlock blockForLineStart)
        {
            var seg = info.Segments;
            string line = info.TextLine;

            int index = 0;
            while (index < line.Length)
            {
                int endIndex;
                // check multiline block
                bool lineStartInBlock = blockForLineStart != null;
                if (blockForLineStart != null
                    || (blockForLineStart = MatchLineBlockStart(this.profile.MultiLinesBlocks, line, index)) != null)
                {
                    endIndex = GetBlockEndIndex(line, index, blockForLineStart, lineStartInBlock);
                    // try next line.
                    if (endIndex < 0)
                    {
                        AddSegments(seg, index, line.Length - index, blockForLineStart, !lineStartInBlock, false);
                        return blockForLineStart;
                    }

                    AddSegments(seg, index, endIndex - index, blockForLineStart, !lineStartInBlock, true);
                    blockForLineStart = null;
                    index = endIndex;
                    continue;
                }

                // check single line block
                SingleLineBlock singleLineBlock = MatchLineBlockStart(this.profile.SingleLineBlocks, line, index);
                if (singleLineBlock != null)
                {
                    endIndex = GetBlockEndIndex(line, index, singleLineBlock, false);
                    endIndex = endIndex < 0 ? line.Length : endIndex;

                    AddSegments(seg, index, endIndex - index, singleLineBlock, true, true);
                    index = endIndex;
                    continue;
                }

                // check tokens
                Token token;
                var match = MatchToken(line, index, out token);
                if (match != null)
                {
                    AddSegments(seg, token, match);
                    index += match.Length;
                    continue;
                }

                // check keywords
                endIndex = GetNextTokenEndIndex(line, index);
                if (endIndex > index)
                {
                    int length = endIndex - index;
                    var keywords = MatchKeyword(line, index, length);
                    if (keywords != null)
                    {
                        seg.Add(new TextLineInfo.TextSegment(index, length, keywords.Foreground));
                    }
                    index = endIndex + 1;
                    continue;
                }

                ++index;
            }

            return blockForLineStart;
        }

        // Check if given line can match token using regular expression.
        private Match MatchToken(string line, int index, out Token token)
        {
            foreach (var t in this.profile.Tokens)
            {
                Match match = t.Pattern.Match(line, index);
                if (match.Success && match.Index == index)
                {
                    token = t;
                    return match;
                }
            }

            token = null;
            return null;
        }

        // Add segments to TextLineInfo if there is token matches.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, Token token, Match match)
        {
            if (token.Groups == null || token.Groups.Length == 0)
            {
                seg.Add(new TextLineInfo.TextSegment(match.Index, match.Length, token.Foreground));
                return;
            }

            int index = match.Index;
            var groups = token.Groups
                .Select(g => new { Foreground = g.Foreground, Captrue = match.Groups[g.Name] })
                .OrderBy(g => g.Captrue.Index);

            foreach (var g in groups)
            {
                if (index != g.Captrue.Index)
                {
                    seg.Add(new TextLineInfo.TextSegment(index, g.Captrue.Index - index, token.Foreground));
                }

                seg.Add(new TextLineInfo.TextSegment(g.Captrue.Index, g.Captrue.Length, g.Foreground));
                index = g.Captrue.Index + g.Captrue.Length;
            }

            if (index != match.Index + match.Length)
            {
                seg.Add(new TextLineInfo.TextSegment(index, match.Index + match.Length - index, token.Foreground));
            }
        }

        // Add segments to TextLineInfo if there is code block found.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, int index, int length, CodeBlock block, bool hasStart, bool hasEnd)
        {
            if (block.WrapperForeground == null)
            {
                seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));
                return;
            }

            if (hasStart)
            {
                seg.Add(new TextLineInfo.TextSegment(index, block.Start.Length, block.WrapperForeground));
                index += block.Start.Length;
                length -= block.Start.Length;
            }

            length = hasEnd ? length - block.End.Length : length;
            seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));

            if (hasEnd)
            {
                seg.Add(new TextLineInfo.TextSegment(index + length, block.End.Length, block.WrapperForeground));
            }
        }

        // Check if line starting from index matches a code block.
        private T MatchLineBlockStart<T>(IEnumerable<T> blocks, string line, int index) where T : CodeBlock
        {
            return blocks.FirstOrDefault(block => StartsWith(line, index, block.Start));
        }

        // Find the block end index.
        private int GetBlockEndIndex(string line, int index, CodeBlock block, bool ignoreBlockStart)
        {
            if (!ignoreBlockStart)
            {
                index += block.Start.Length;
            }

            if (block is SingleLineBlock && string.IsNullOrEmpty(block.End))
            {
                return line.Length;
            }

            for (int i = index; i < line.Length; ++i)
            {
                var escape = block.Escape;
                if (escape != null)
                {
                    string escapeString = block.Escape.EscapeString;
                    if (!string.IsNullOrEmpty(escapeString) && StartsWith(line, i, escapeString))
                    {
                        i += escapeString.Length; // skip next one;
                        continue;
                    }

                    string[] escapeItems = block.Escape.Items ?? (new string[0]);
                    string found = escapeItems.FirstOrDefault(item => StartsWith(line, i, item));
                    if (found != null)
                    {
                        i += found.Length - 1;
                        continue;
                    }
                }

                if (StartsWith(line, i, block.End))
                {
                    return i + block.End.Length;
                }
            }

            // not found.
            return -1;
        }

        // Get next index of end of the token.
        private int GetNextTokenEndIndex(string line, int startIndex)
        {
            for (int i = startIndex; i < line.Length; ++i)
            {
                // Matches delimeter.
                if (Array.BinarySearch(this.profile.Delimiter, line[i]) >= 0)
                {
                    return i;
                }

                if (Array.BinarySearch(this.profile.BackDelimiter, line[i]) >= 0)
                {
                    return i - 1;
                }
            }

            return line.Length;
        }

        // Check if the line starting from index matches a keyword.
        private KeywordCollection MatchKeyword(string line, int index, int length)
        {
            Expect.ArgumentNotNull(line, "line");
            Expect.ArgumentCheck(line.Length >= index + length, "Check line length");

            foreach (var keywords in this.profile.KeywordCollecions)
            {
                int result = keywords.Keywords.BinarySearch(k => CompareKeyword(k, line, index, length));
                if (result >= 0)
                {
                    return keywords;
                }
            }

            return null;
        }

        // Check if the line starting from index starts another string.
        private bool StartsWith(string line, int index, string another)
        {
            if (line.Length < another.Length + index)
            {
                return false;
            }

            for (int i = 0; i < another.Length; ++i)
            {
                if (GetChar(line[index + i]) != GetChar(another[i]))
                {
                    return false;
                }
            }

            return true;
        }

        // Compare a keyword.
        private int CompareKeyword(string keyword, string line, int index, int length)
        {
            for (int i = 0; i < Math.Min(keyword.Length, length); ++i)
            {
                int result = GetChar(keyword[i]) - GetChar(line[index + i]);
                if (result != 0)
                {
                    return result;
                }
            }

            return keyword.Length - length;
        }

        // Convert a char.
        private char GetChar(char c)
        {
            return this.profile.IgnoreCase ? char.ToUpper(c) : c;
        }

        #endregion
    }
}
//---------------------------------------------------------------------
// <copyright file="HighlightParser.cs" company="Microsoft">
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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// A class to parse text lines.
    /// </summary>
    public class HighlightParser
    {
        private HighlightProfile profile;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="profile"></param>
        public HighlightParser(HighlightProfile profile)
        {
            this.profile = profile;
        }

        /// <summary>
        /// Parse the text lines;
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public TextLineInfo[] Parse(string[] lines, int startLine)
        {
            Expect.ArgumentNotNull(lines, "lines");
            Expect.ArgumentCheck(startLine >= 0, "startLine should >= 0");

            MultiLinesBlock inBlock = null;
            TextLineInfo[] lineInfos = new TextLineInfo[lines.Length];
            for (int i = 0; i < lines.Length; ++i)
            {
                lineInfos[i] = new TextLineInfo(lines[i], startLine + i);
                if (this.profile != null)
                {
                    inBlock = ParseLine(lineInfos[i], inBlock);
                }
            }

            return lineInfos;
        }

        #region Private Methods

        // Parse single line.
        private MultiLinesBlock ParseLine(TextLineInfo info, MultiLinesBlock blockForLineStart)
        {
            var seg = info.Segments;
            string line = info.TextLine;

            int index = 0;
            while (index < line.Length)
            {
                int endIndex;
                // check multiline block
                bool lineStartInBlock = blockForLineStart != null;
                if (blockForLineStart != null
                    || (blockForLineStart = MatchLineBlockStart(this.profile.MultiLinesBlocks, line, index)) != null)
                {
                    endIndex = GetBlockEndIndex(line, index, blockForLineStart, lineStartInBlock);
                    // try next line.
                    if (endIndex < 0)
                    {
                        AddSegments(seg, index, line.Length - index, blockForLineStart, !lineStartInBlock, false);
                        return blockForLineStart;
                    }

                    AddSegments(seg, index, endIndex - index, blockForLineStart, !lineStartInBlock, true);
                    blockForLineStart = null;
                    index = endIndex;
                    continue;
                }

                // check single line block
                SingleLineBlock singleLineBlock = MatchLineBlockStart(this.profile.SingleLineBlocks, line, index);
                if (singleLineBlock != null)
                {
                    endIndex = GetBlockEndIndex(line, index, singleLineBlock, false);
                    endIndex = endIndex < 0 ? line.Length : endIndex;

                    AddSegments(seg, index, endIndex - index, singleLineBlock, true, true);
                    index = endIndex;
                    continue;
                }

                // check tokens
                Token token;
                var match = MatchToken(line, index, out token);
                if (match != null)
                {
                    AddSegments(seg, token, match);
                    index += match.Length;
                    continue;
                }

                // check keywords
                endIndex = GetNextTokenEndIndex(line, index);
                if (endIndex > index)
                {
                    int length = endIndex - index;
                    var keywords = MatchKeyword(line, index, length);
                    if (keywords != null)
                    {
                        seg.Add(new TextLineInfo.TextSegment(index, length, keywords.Foreground));
                    }
                    index = endIndex + 1;
                    continue;
                }

                ++index;
            }

            return blockForLineStart;
        }

        // Check if given line can match token using regular expression.
        private Match MatchToken(string line, int index, out Token token)
        {
            foreach (var t in this.profile.Tokens)
            {
                Match match = t.Pattern.Match(line, index);
                if (match.Success && match.Index == index)
                {
                    token = t;
                    return match;
                }
            }

            token = null;
            return null;
        }

        // Add segments to TextLineInfo if there is token matches.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, Token token, Match match)
        {
            if (token.Groups == null || token.Groups.Length == 0)
            {
                seg.Add(new TextLineInfo.TextSegment(match.Index, match.Length, token.Foreground));
                return;
            }

            int index = match.Index;
            var groups = token.Groups
                .Select(g => new { Foreground = g.Foreground, Captrue = match.Groups[g.Name] })
                .OrderBy(g => g.Captrue.Index);

            foreach (var g in groups)
            {
                if (index != g.Captrue.Index)
                {
                    seg.Add(new TextLineInfo.TextSegment(index, g.Captrue.Index - index, token.Foreground));
                }

                seg.Add(new TextLineInfo.TextSegment(g.Captrue.Index, g.Captrue.Length, g.Foreground));
                index = g.Captrue.Index + g.Captrue.Length;
            }

            if (index != match.Index + match.Length)
            {
                seg.Add(new TextLineInfo.TextSegment(index, match.Index + match.Length - index, token.Foreground));
            }
        }

        // Add segments to TextLineInfo if there is code block found.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, int index, int length, CodeBlock block, bool hasStart, bool hasEnd)
        {
            if (block.WrapperForeground == null)
            {
                seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));
                return;
            }

            if (hasStart)
            {
                seg.Add(new TextLineInfo.TextSegment(index, block.Start.Length, block.WrapperForeground));
                index += block.Start.Length;
                length -= block.Start.Length;
            }

            length = hasEnd ? length - block.End.Length : length;
            seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));

            if (hasEnd)
            {
                seg.Add(new TextLineInfo.TextSegment(index + length, block.End.Length, block.WrapperForeground));
            }
        }

        // Check if line starting from index matches a code block.
        private T MatchLineBlockStart<T>(IEnumerable<T> blocks, string line, int index) where T : CodeBlock
        {
            return blocks.FirstOrDefault(block => StartsWith(line, index, block.Start));
        }

        // Find the block end index.
        private int GetBlockEndIndex(string line, int index, CodeBlock block, bool ignoreBlockStart)
        {
            if (!ignoreBlockStart)
            {
                index += block.Start.Length;
            }

            if (block is SingleLineBlock && string.IsNullOrEmpty(block.End))
            {
                return line.Length;
            }

            for (int i = index; i < line.Length; ++i)
            {
                var escape = block.Escape;
                if (escape != null)
                {
                    string escapeString = block.Escape.EscapeString;
                    if (!string.IsNullOrEmpty(escapeString) && StartsWith(line, i, escapeString))
                    {
                        i += escapeString.Length; // skip next one;
                        continue;
                    }

                    string[] escapeItems = block.Escape.Items ?? (new string[0]);
                    string found = escapeItems.FirstOrDefault(item => StartsWith(line, i, item));
                    if (found != null)
                    {
                        i += found.Length - 1;
                        continue;
                    }
                }

                if (StartsWith(line, i, block.End))
                {
                    return i + block.End.Length;
                }
            }

            // not found.
            return -1;
        }

        // Get next index of end of the token.
        private int GetNextTokenEndIndex(string line, int startIndex)
        {
            for (int i = startIndex; i < line.Length; ++i)
            {
                // Matches delimeter.
                if (Array.BinarySearch(this.profile.Delimiter, line[i]) >= 0)
                {
                    return i;
                }

                if (Array.BinarySearch(this.profile.BackDelimiter, line[i]) >= 0)
                {
                    return i - 1;
                }
            }

            return line.Length;
        }

        // Check if the line starting from index matches a keyword.
        private KeywordCollection MatchKeyword(string line, int index, int length)
        {
            Expect.ArgumentNotNull(line, "line");
            Expect.ArgumentCheck(line.Length >= index + length, "Check line length");

            foreach (var keywords in this.profile.KeywordCollecions)
            {
                int result = keywords.Keywords.BinarySearch(k => CompareKeyword(k, line, index, length));
                if (result >= 0)
                {
                    return keywords;
                }
            }

            return null;
        }

        // Check if the line starting from index starts another string.
        private bool StartsWith(string line, int index, string another)
        {
            if (line.Length < another.Length + index)
            {
                return false;
            }

            for (int i = 0; i < another.Length; ++i)
            {
                if (GetChar(line[index + i]) != GetChar(another[i]))
                {
                    return false;
                }
            }

            return true;
        }

        // Compare a keyword.
        private int CompareKeyword(string keyword, string line, int index, int length)
        {
            for (int i = 0; i < Math.Min(keyword.Length, length); ++i)
            {
                int result = GetChar(keyword[i]) - GetChar(line[index + i]);
                if (result != 0)
                {
                    return result;
                }
            }

            return keyword.Length - length;
        }

        // Convert a char.
        private char GetChar(char c)
        {
            return this.profile.IgnoreCase ? char.ToUpper(c) : c;
        }

        #endregion
    }
}
//---------------------------------------------------------------------
// <copyright file="HighlightParser.cs" company="Microsoft">
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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// A class to parse text lines.
    /// </summary>
    public class HighlightParser
    {
        private HighlightProfile profile;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="profile"></param>
        public HighlightParser(HighlightProfile profile)
        {
            this.profile = profile;
        }

        /// <summary>
        /// Parse the text lines;
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public TextLineInfo[] Parse(string[] lines, int startLine)
        {
            Expect.ArgumentNotNull(lines, "lines");
            Expect.ArgumentCheck(startLine >= 0, "startLine should >= 0");

            MultiLinesBlock inBlock = null;
            TextLineInfo[] lineInfos = new TextLineInfo[lines.Length];
            for (int i = 0; i < lines.Length; ++i)
            {
                lineInfos[i] = new TextLineInfo(lines[i], startLine + i);
                if (this.profile != null)
                {
                    inBlock = ParseLine(lineInfos[i], inBlock);
                }
            }

            return lineInfos;
        }

        #region Private Methods

        // Parse single line.
        private MultiLinesBlock ParseLine(TextLineInfo info, MultiLinesBlock blockForLineStart)
        {
            var seg = info.Segments;
            string line = info.TextLine;

            int index = 0;
            while (index < line.Length)
            {
                int endIndex;
                // check multiline block
                bool lineStartInBlock = blockForLineStart != null;
                if (blockForLineStart != null
                    || (blockForLineStart = MatchLineBlockStart(this.profile.MultiLinesBlocks, line, index)) != null)
                {
                    endIndex = GetBlockEndIndex(line, index, blockForLineStart, lineStartInBlock);
                    // try next line.
                    if (endIndex < 0)
                    {
                        AddSegments(seg, index, line.Length - index, blockForLineStart, !lineStartInBlock, false);
                        return blockForLineStart;
                    }

                    AddSegments(seg, index, endIndex - index, blockForLineStart, !lineStartInBlock, true);
                    blockForLineStart = null;
                    index = endIndex;
                    continue;
                }

                // check single line block
                SingleLineBlock singleLineBlock = MatchLineBlockStart(this.profile.SingleLineBlocks, line, index);
                if (singleLineBlock != null)
                {
                    endIndex = GetBlockEndIndex(line, index, singleLineBlock, false);
                    endIndex = endIndex < 0 ? line.Length : endIndex;

                    AddSegments(seg, index, endIndex - index, singleLineBlock, true, true);
                    index = endIndex;
                    continue;
                }

                // check tokens
                Token token;
                var match = MatchToken(line, index, out token);
                if (match != null)
                {
                    AddSegments(seg, token, match);
                    index += match.Length;
                    continue;
                }

                // check keywords
                endIndex = GetNextTokenEndIndex(line, index);
                if (endIndex > index)
                {
                    int length = endIndex - index;
                    var keywords = MatchKeyword(line, index, length);
                    if (keywords != null)
                    {
                        seg.Add(new TextLineInfo.TextSegment(index, length, keywords.Foreground));
                    }
                    index = endIndex + 1;
                    continue;
                }

                ++index;
            }

            return blockForLineStart;
        }

        // Check if given line can match token using regular expression.
        private Match MatchToken(string line, int index, out Token token)
        {
            foreach (var t in this.profile.Tokens)
            {
                Match match = t.Pattern.Match(line, index);
                if (match.Success && match.Index == index)
                {
                    token = t;
                    return match;
                }
            }

            token = null;
            return null;
        }

        // Add segments to TextLineInfo if there is token matches.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, Token token, Match match)
        {
            if (token.Groups == null || token.Groups.Length == 0)
            {
                seg.Add(new TextLineInfo.TextSegment(match.Index, match.Length, token.Foreground));
                return;
            }

            int index = match.Index;
            var groups = token.Groups
                .Select(g => new { Foreground = g.Foreground, Captrue = match.Groups[g.Name] })
                .OrderBy(g => g.Captrue.Index);

            foreach (var g in groups)
            {
                if (index != g.Captrue.Index)
                {
                    seg.Add(new TextLineInfo.TextSegment(index, g.Captrue.Index - index, token.Foreground));
                }

                seg.Add(new TextLineInfo.TextSegment(g.Captrue.Index, g.Captrue.Length, g.Foreground));
                index = g.Captrue.Index + g.Captrue.Length;
            }

            if (index != match.Index + match.Length)
            {
                seg.Add(new TextLineInfo.TextSegment(index, match.Index + match.Length - index, token.Foreground));
            }
        }

        // Add segments to TextLineInfo if there is code block found.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, int index, int length, CodeBlock block, bool hasStart, bool hasEnd)
        {
            if (block.WrapperForeground == null)
            {
                seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));
                return;
            }

            if (hasStart)
            {
                seg.Add(new TextLineInfo.TextSegment(index, block.Start.Length, block.WrapperForeground));
                index += block.Start.Length;
                length -= block.Start.Length;
            }

            length = hasEnd ? length - block.End.Length : length;
            seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));

            if (hasEnd)
            {
                seg.Add(new TextLineInfo.TextSegment(index + length, block.End.Length, block.WrapperForeground));
            }
        }

        // Check if line starting from index matches a code block.
        private T MatchLineBlockStart<T>(IEnumerable<T> blocks, string line, int index) where T : CodeBlock
        {
            return blocks.FirstOrDefault(block => StartsWith(line, index, block.Start));
        }

        // Find the block end index.
        private int GetBlockEndIndex(string line, int index, CodeBlock block, bool ignoreBlockStart)
        {
            if (!ignoreBlockStart)
            {
                index += block.Start.Length;
            }

            if (block is SingleLineBlock && string.IsNullOrEmpty(block.End))
            {
                return line.Length;
            }

            for (int i = index; i < line.Length; ++i)
            {
                var escape = block.Escape;
                if (escape != null)
                {
                    string escapeString = block.Escape.EscapeString;
                    if (!string.IsNullOrEmpty(escapeString) && StartsWith(line, i, escapeString))
                    {
                        i += escapeString.Length; // skip next one;
                        continue;
                    }

                    string[] escapeItems = block.Escape.Items ?? (new string[0]);
                    string found = escapeItems.FirstOrDefault(item => StartsWith(line, i, item));
                    if (found != null)
                    {
                        i += found.Length - 1;
                        continue;
                    }
                }

                if (StartsWith(line, i, block.End))
                {
                    return i + block.End.Length;
                }
            }

            // not found.
            return -1;
        }

        // Get next index of end of the token.
        private int GetNextTokenEndIndex(string line, int startIndex)
        {
            for (int i = startIndex; i < line.Length; ++i)
            {
                // Matches delimeter.
                if (Array.BinarySearch(this.profile.Delimiter, line[i]) >= 0)
                {
                    return i;
                }

                if (Array.BinarySearch(this.profile.BackDelimiter, line[i]) >= 0)
                {
                    return i - 1;
                }
            }

            return line.Length;
        }

        // Check if the line starting from index matches a keyword.
        private KeywordCollection MatchKeyword(string line, int index, int length)
        {
            Expect.ArgumentNotNull(line, "line");
            Expect.ArgumentCheck(line.Length >= index + length, "Check line length");

            foreach (var keywords in this.profile.KeywordCollecions)
            {
                int result = keywords.Keywords.BinarySearch(k => CompareKeyword(k, line, index, length));
                if (result >= 0)
                {
                    return keywords;
                }
            }

            return null;
        }

        // Check if the line starting from index starts another string.
        private bool StartsWith(string line, int index, string another)
        {
            if (line.Length < another.Length + index)
            {
                return false;
            }

            for (int i = 0; i < another.Length; ++i)
            {
                if (GetChar(line[index + i]) != GetChar(another[i]))
                {
                    return false;
                }
            }

            return true;
        }

        // Compare a keyword.
        private int CompareKeyword(string keyword, string line, int index, int length)
        {
            for (int i = 0; i < Math.Min(keyword.Length, length); ++i)
            {
                int result = GetChar(keyword[i]) - GetChar(line[index + i]);
                if (result != 0)
                {
                    return result;
                }
            }

            return keyword.Length - length;
        }

        // Convert a char.
        private char GetChar(char c)
        {
            return this.profile.IgnoreCase ? char.ToUpper(c) : c;
        }

        #endregion
    }
}
//---------------------------------------------------------------------
// <copyright file="HighlightParser.cs" company="Microsoft">
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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// A class to parse text lines.
    /// </summary>
    public class HighlightParser
    {
        private HighlightProfile profile;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="profile"></param>
        public HighlightParser(HighlightProfile profile)
        {
            this.profile = profile;
        }

        /// <summary>
        /// Parse the text lines;
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public TextLineInfo[] Parse(string[] lines, int startLine)
        {
            Expect.ArgumentNotNull(lines, "lines");
            Expect.ArgumentCheck(startLine >= 0, "startLine should >= 0");

            MultiLinesBlock inBlock = null;
            TextLineInfo[] lineInfos = new TextLineInfo[lines.Length];
            for (int i = 0; i < lines.Length; ++i)
            {
                lineInfos[i] = new TextLineInfo(lines[i], startLine + i);
                if (this.profile != null)
                {
                    inBlock = ParseLine(lineInfos[i], inBlock);
                }
            }

            return lineInfos;
        }

        #region Private Methods

        // Parse single line.
        private MultiLinesBlock ParseLine(TextLineInfo info, MultiLinesBlock blockForLineStart)
        {
            var seg = info.Segments;
            string line = info.TextLine;

            int index = 0;
            while (index < line.Length)
            {
                int endIndex;
                // check multiline block
                bool lineStartInBlock = blockForLineStart != null;
                if (blockForLineStart != null
                    || (blockForLineStart = MatchLineBlockStart(this.profile.MultiLinesBlocks, line, index)) != null)
                {
                    endIndex = GetBlockEndIndex(line, index, blockForLineStart, lineStartInBlock);
                    // try next line.
                    if (endIndex < 0)
                    {
                        AddSegments(seg, index, line.Length - index, blockForLineStart, !lineStartInBlock, false);
                        return blockForLineStart;
                    }

                    AddSegments(seg, index, endIndex - index, blockForLineStart, !lineStartInBlock, true);
                    blockForLineStart = null;
                    index = endIndex;
                    continue;
                }

                // check single line block
                SingleLineBlock singleLineBlock = MatchLineBlockStart(this.profile.SingleLineBlocks, line, index);
                if (singleLineBlock != null)
                {
                    endIndex = GetBlockEndIndex(line, index, singleLineBlock, false);
                    endIndex = endIndex < 0 ? line.Length : endIndex;

                    AddSegments(seg, index, endIndex - index, singleLineBlock, true, true);
                    index = endIndex;
                    continue;
                }

                // check tokens
                Token token;
                var match = MatchToken(line, index, out token);
                if (match != null)
                {
                    AddSegments(seg, token, match);
                    index += match.Length;
                    continue;
                }

                // check keywords
                endIndex = GetNextTokenEndIndex(line, index);
                if (endIndex > index)
                {
                    int length = endIndex - index;
                    var keywords = MatchKeyword(line, index, length);
                    if (keywords != null)
                    {
                        seg.Add(new TextLineInfo.TextSegment(index, length, keywords.Foreground));
                    }
                    index = endIndex + 1;
                    continue;
                }

                ++index;
            }

            return blockForLineStart;
        }

        // Check if given line can match token using regular expression.
        private Match MatchToken(string line, int index, out Token token)
        {
            foreach (var t in this.profile.Tokens)
            {
                Match match = t.Pattern.Match(line, index);
                if (match.Success && match.Index == index)
                {
                    token = t;
                    return match;
                }
            }

            token = null;
            return null;
        }

        // Add segments to TextLineInfo if there is token matches.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, Token token, Match match)
        {
            if (token.Groups == null || token.Groups.Length == 0)
            {
                seg.Add(new TextLineInfo.TextSegment(match.Index, match.Length, token.Foreground));
                return;
            }

            int index = match.Index;
            var groups = token.Groups
                .Select(g => new { Foreground = g.Foreground, Captrue = match.Groups[g.Name] })
                .OrderBy(g => g.Captrue.Index);

            foreach (var g in groups)
            {
                if (index != g.Captrue.Index)
                {
                    seg.Add(new TextLineInfo.TextSegment(index, g.Captrue.Index - index, token.Foreground));
                }

                seg.Add(new TextLineInfo.TextSegment(g.Captrue.Index, g.Captrue.Length, g.Foreground));
                index = g.Captrue.Index + g.Captrue.Length;
            }

            if (index != match.Index + match.Length)
            {
                seg.Add(new TextLineInfo.TextSegment(index, match.Index + match.Length - index, token.Foreground));
            }
        }

        // Add segments to TextLineInfo if there is code block found.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, int index, int length, CodeBlock block, bool hasStart, bool hasEnd)
        {
            if (block.WrapperForeground == null)
            {
                seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));
                return;
            }

            if (hasStart)
            {
                seg.Add(new TextLineInfo.TextSegment(index, block.Start.Length, block.WrapperForeground));
                index += block.Start.Length;
                length -= block.Start.Length;
            }

            length = hasEnd ? length - block.End.Length : length;
            seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));

            if (hasEnd)
            {
                seg.Add(new TextLineInfo.TextSegment(index + length, block.End.Length, block.WrapperForeground));
            }
        }

        // Check if line starting from index matches a code block.
        private T MatchLineBlockStart<T>(IEnumerable<T> blocks, string line, int index) where T : CodeBlock
        {
            return blocks.FirstOrDefault(block => StartsWith(line, index, block.Start));
        }

        // Find the block end index.
        private int GetBlockEndIndex(string line, int index, CodeBlock block, bool ignoreBlockStart)
        {
            if (!ignoreBlockStart)
            {
                index += block.Start.Length;
            }

            if (block is SingleLineBlock && string.IsNullOrEmpty(block.End))
            {
                return line.Length;
            }

            for (int i = index; i < line.Length; ++i)
            {
                var escape = block.Escape;
                if (escape != null)
                {
                    string escapeString = block.Escape.EscapeString;
                    if (!string.IsNullOrEmpty(escapeString) && StartsWith(line, i, escapeString))
                    {
                        i += escapeString.Length; // skip next one;
                        continue;
                    }

                    string[] escapeItems = block.Escape.Items ?? (new string[0]);
                    string found = escapeItems.FirstOrDefault(item => StartsWith(line, i, item));
                    if (found != null)
                    {
                        i += found.Length - 1;
                        continue;
                    }
                }

                if (StartsWith(line, i, block.End))
                {
                    return i + block.End.Length;
                }
            }

            // not found.
            return -1;
        }

        // Get next index of end of the token.
        private int GetNextTokenEndIndex(string line, int startIndex)
        {
            for (int i = startIndex; i < line.Length; ++i)
            {
                // Matches delimeter.
                if (Array.BinarySearch(this.profile.Delimiter, line[i]) >= 0)
                {
                    return i;
                }

                if (Array.BinarySearch(this.profile.BackDelimiter, line[i]) >= 0)
                {
                    return i - 1;
                }
            }

            return line.Length;
        }

        // Check if the line starting from index matches a keyword.
        private KeywordCollection MatchKeyword(string line, int index, int length)
        {
            Expect.ArgumentNotNull(line, "line");
            Expect.ArgumentCheck(line.Length >= index + length, "Check line length");

            foreach (var keywords in this.profile.KeywordCollecions)
            {
                int result = keywords.Keywords.BinarySearch(k => CompareKeyword(k, line, index, length));
                if (result >= 0)
                {
                    return keywords;
                }
            }

            return null;
        }

        // Check if the line starting from index starts another string.
        private bool StartsWith(string line, int index, string another)
        {
            if (line.Length < another.Length + index)
            {
                return false;
            }

            for (int i = 0; i < another.Length; ++i)
            {
                if (GetChar(line[index + i]) != GetChar(another[i]))
                {
                    return false;
                }
            }

            return true;
        }

        // Compare a keyword.
        private int CompareKeyword(string keyword, string line, int index, int length)
        {
            for (int i = 0; i < Math.Min(keyword.Length, length); ++i)
            {
                int result = GetChar(keyword[i]) - GetChar(line[index + i]);
                if (result != 0)
                {
                    return result;
                }
            }

            return keyword.Length - length;
        }

        // Convert a char.
        private char GetChar(char c)
        {
            return this.profile.IgnoreCase ? char.ToUpper(c) : c;
        }

        #endregion
    }
}
//---------------------------------------------------------------------
// <copyright file="HighlightParser.cs" company="Microsoft">
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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// A class to parse text lines.
    /// </summary>
    public class HighlightParser
    {
        private HighlightProfile profile;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="profile"></param>
        public HighlightParser(HighlightProfile profile)
        {
            this.profile = profile;
        }

        /// <summary>
        /// Parse the text lines;
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public TextLineInfo[] Parse(string[] lines, int startLine)
        {
            Expect.ArgumentNotNull(lines, "lines");
            Expect.ArgumentCheck(startLine >= 0, "startLine should >= 0");

            MultiLinesBlock inBlock = null;
            TextLineInfo[] lineInfos = new TextLineInfo[lines.Length];
            for (int i = 0; i < lines.Length; ++i)
            {
                lineInfos[i] = new TextLineInfo(lines[i], startLine + i);
                if (this.profile != null)
                {
                    inBlock = ParseLine(lineInfos[i], inBlock);
                }
            }

            return lineInfos;
        }

        #region Private Methods

        // Parse single line.
        private MultiLinesBlock ParseLine(TextLineInfo info, MultiLinesBlock blockForLineStart)
        {
            var seg = info.Segments;
            string line = info.TextLine;

            int index = 0;
            while (index < line.Length)
            {
                int endIndex;
                // check multiline block
                bool lineStartInBlock = blockForLineStart != null;
                if (blockForLineStart != null
                    || (blockForLineStart = MatchLineBlockStart(this.profile.MultiLinesBlocks, line, index)) != null)
                {
                    endIndex = GetBlockEndIndex(line, index, blockForLineStart, lineStartInBlock);
                    // try next line.
                    if (endIndex < 0)
                    {
                        AddSegments(seg, index, line.Length - index, blockForLineStart, !lineStartInBlock, false);
                        return blockForLineStart;
                    }

                    AddSegments(seg, index, endIndex - index, blockForLineStart, !lineStartInBlock, true);
                    blockForLineStart = null;
                    index = endIndex;
                    continue;
                }

                // check single line block
                SingleLineBlock singleLineBlock = MatchLineBlockStart(this.profile.SingleLineBlocks, line, index);
                if (singleLineBlock != null)
                {
                    endIndex = GetBlockEndIndex(line, index, singleLineBlock, false);
                    endIndex = endIndex < 0 ? line.Length : endIndex;

                    AddSegments(seg, index, endIndex - index, singleLineBlock, true, true);
                    index = endIndex;
                    continue;
                }

                // check tokens
                Token token;
                var match = MatchToken(line, index, out token);
                if (match != null)
                {
                    AddSegments(seg, token, match);
                    index += match.Length;
                    continue;
                }

                // check keywords
                endIndex = GetNextTokenEndIndex(line, index);
                if (endIndex > index)
                {
                    int length = endIndex - index;
                    var keywords = MatchKeyword(line, index, length);
                    if (keywords != null)
                    {
                        seg.Add(new TextLineInfo.TextSegment(index, length, keywords.Foreground));
                    }
                    index = endIndex + 1;
                    continue;
                }

                ++index;
            }

            return blockForLineStart;
        }

        // Check if given line can match token using regular expression.
        private Match MatchToken(string line, int index, out Token token)
        {
            foreach (var t in this.profile.Tokens)
            {
                Match match = t.Pattern.Match(line, index);
                if (match.Success && match.Index == index)
                {
                    token = t;
                    return match;
                }
            }

            token = null;
            return null;
        }

        // Add segments to TextLineInfo if there is token matches.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, Token token, Match match)
        {
            if (token.Groups == null || token.Groups.Length == 0)
            {
                seg.Add(new TextLineInfo.TextSegment(match.Index, match.Length, token.Foreground));
                return;
            }

            int index = match.Index;
            var groups = token.Groups
                .Select(g => new { Foreground = g.Foreground, Captrue = match.Groups[g.Name] })
                .OrderBy(g => g.Captrue.Index);

            foreach (var g in groups)
            {
                if (index != g.Captrue.Index)
                {
                    seg.Add(new TextLineInfo.TextSegment(index, g.Captrue.Index - index, token.Foreground));
                }

                seg.Add(new TextLineInfo.TextSegment(g.Captrue.Index, g.Captrue.Length, g.Foreground));
                index = g.Captrue.Index + g.Captrue.Length;
            }

            if (index != match.Index + match.Length)
            {
                seg.Add(new TextLineInfo.TextSegment(index, match.Index + match.Length - index, token.Foreground));
            }
        }

        // Add segments to TextLineInfo if there is code block found.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, int index, int length, CodeBlock block, bool hasStart, bool hasEnd)
        {
            if (block.WrapperForeground == null)
            {
                seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));
                return;
            }

            if (hasStart)
            {
                seg.Add(new TextLineInfo.TextSegment(index, block.Start.Length, block.WrapperForeground));
                index += block.Start.Length;
                length -= block.Start.Length;
            }

            length = hasEnd ? length - block.End.Length : length;
            seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));

            if (hasEnd)
            {
                seg.Add(new TextLineInfo.TextSegment(index + length, block.End.Length, block.WrapperForeground));
            }
        }

        // Check if line starting from index matches a code block.
        private T MatchLineBlockStart<T>(IEnumerable<T> blocks, string line, int index) where T : CodeBlock
        {
            return blocks.FirstOrDefault(block => StartsWith(line, index, block.Start));
        }

        // Find the block end index.
        private int GetBlockEndIndex(string line, int index, CodeBlock block, bool ignoreBlockStart)
        {
            if (!ignoreBlockStart)
            {
                index += block.Start.Length;
            }

            if (block is SingleLineBlock && string.IsNullOrEmpty(block.End))
            {
                return line.Length;
            }

            for (int i = index; i < line.Length; ++i)
            {
                var escape = block.Escape;
                if (escape != null)
                {
                    string escapeString = block.Escape.EscapeString;
                    if (!string.IsNullOrEmpty(escapeString) && StartsWith(line, i, escapeString))
                    {
                        i += escapeString.Length; // skip next one;
                        continue;
                    }

                    string[] escapeItems = block.Escape.Items ?? (new string[0]);
                    string found = escapeItems.FirstOrDefault(item => StartsWith(line, i, item));
                    if (found != null)
                    {
                        i += found.Length - 1;
                        continue;
                    }
                }

                if (StartsWith(line, i, block.End))
                {
                    return i + block.End.Length;
                }
            }

            // not found.
            return -1;
        }

        // Get next index of end of the token.
        private int GetNextTokenEndIndex(string line, int startIndex)
        {
            for (int i = startIndex; i < line.Length; ++i)
            {
                // Matches delimeter.
                if (Array.BinarySearch(this.profile.Delimiter, line[i]) >= 0)
                {
                    return i;
                }

                if (Array.BinarySearch(this.profile.BackDelimiter, line[i]) >= 0)
                {
                    return i - 1;
                }
            }

            return line.Length;
        }

        // Check if the line starting from index matches a keyword.
        private KeywordCollection MatchKeyword(string line, int index, int length)
        {
            Expect.ArgumentNotNull(line, "line");
            Expect.ArgumentCheck(line.Length >= index + length, "Check line length");

            foreach (var keywords in this.profile.KeywordCollecions)
            {
                int result = keywords.Keywords.BinarySearch(k => CompareKeyword(k, line, index, length));
                if (result >= 0)
                {
                    return keywords;
                }
            }

            return null;
        }

        // Check if the line starting from index starts another string.
        private bool StartsWith(string line, int index, string another)
        {
            if (line.Length < another.Length + index)
            {
                return false;
            }

            for (int i = 0; i < another.Length; ++i)
            {
                if (GetChar(line[index + i]) != GetChar(another[i]))
                {
                    return false;
                }
            }

            return true;
        }

        // Compare a keyword.
        private int CompareKeyword(string keyword, string line, int index, int length)
        {
            for (int i = 0; i < Math.Min(keyword.Length, length); ++i)
            {
                int result = GetChar(keyword[i]) - GetChar(line[index + i]);
                if (result != 0)
                {
                    return result;
                }
            }

            return keyword.Length - length;
        }

        // Convert a char.
        private char GetChar(char c)
        {
            return this.profile.IgnoreCase ? char.ToUpper(c) : c;
        }

        #endregion
    }
}
//---------------------------------------------------------------------
// <copyright file="HighlightParser.cs" company="Microsoft">
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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// A class to parse text lines.
    /// </summary>
    public class HighlightParser
    {
        private HighlightProfile profile;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="profile"></param>
        public HighlightParser(HighlightProfile profile)
        {
            this.profile = profile;
        }

        /// <summary>
        /// Parse the text lines;
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public TextLineInfo[] Parse(string[] lines, int startLine)
        {
            Expect.ArgumentNotNull(lines, "lines");
            Expect.ArgumentCheck(startLine >= 0, "startLine should >= 0");

            MultiLinesBlock inBlock = null;
            TextLineInfo[] lineInfos = new TextLineInfo[lines.Length];
            for (int i = 0; i < lines.Length; ++i)
            {
                lineInfos[i] = new TextLineInfo(lines[i], startLine + i);
                if (this.profile != null)
                {
                    inBlock = ParseLine(lineInfos[i], inBlock);
                }
            }

            return lineInfos;
        }

        #region Private Methods

        // Parse single line.
        private MultiLinesBlock ParseLine(TextLineInfo info, MultiLinesBlock blockForLineStart)
        {
            var seg = info.Segments;
            string line = info.TextLine;

            int index = 0;
            while (index < line.Length)
            {
                int endIndex;
                // check multiline block
                bool lineStartInBlock = blockForLineStart != null;
                if (blockForLineStart != null
                    || (blockForLineStart = MatchLineBlockStart(this.profile.MultiLinesBlocks, line, index)) != null)
                {
                    endIndex = GetBlockEndIndex(line, index, blockForLineStart, lineStartInBlock);
                    // try next line.
                    if (endIndex < 0)
                    {
                        AddSegments(seg, index, line.Length - index, blockForLineStart, !lineStartInBlock, false);
                        return blockForLineStart;
                    }

                    AddSegments(seg, index, endIndex - index, blockForLineStart, !lineStartInBlock, true);
                    blockForLineStart = null;
                    index = endIndex;
                    continue;
                }

                // check single line block
                SingleLineBlock singleLineBlock = MatchLineBlockStart(this.profile.SingleLineBlocks, line, index);
                if (singleLineBlock != null)
                {
                    endIndex = GetBlockEndIndex(line, index, singleLineBlock, false);
                    endIndex = endIndex < 0 ? line.Length : endIndex;

                    AddSegments(seg, index, endIndex - index, singleLineBlock, true, true);
                    index = endIndex;
                    continue;
                }

                // check tokens
                Token token;
                var match = MatchToken(line, index, out token);
                if (match != null)
                {
                    AddSegments(seg, token, match);
                    index += match.Length;
                    continue;
                }

                // check keywords
                endIndex = GetNextTokenEndIndex(line, index);
                if (endIndex > index)
                {
                    int length = endIndex - index;
                    var keywords = MatchKeyword(line, index, length);
                    if (keywords != null)
                    {
                        seg.Add(new TextLineInfo.TextSegment(index, length, keywords.Foreground));
                    }
                    index = endIndex + 1;
                    continue;
                }

                ++index;
            }

            return blockForLineStart;
        }

        // Check if given line can match token using regular expression.
        private Match MatchToken(string line, int index, out Token token)
        {
            foreach (var t in this.profile.Tokens)
            {
                Match match = t.Pattern.Match(line, index);
                if (match.Success && match.Index == index)
                {
                    token = t;
                    return match;
                }
            }

            token = null;
            return null;
        }

        // Add segments to TextLineInfo if there is token matches.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, Token token, Match match)
        {
            if (token.Groups == null || token.Groups.Length == 0)
            {
                seg.Add(new TextLineInfo.TextSegment(match.Index, match.Length, token.Foreground));
                return;
            }

            int index = match.Index;
            var groups = token.Groups
                .Select(g => new { Foreground = g.Foreground, Captrue = match.Groups[g.Name] })
                .OrderBy(g => g.Captrue.Index);

            foreach (var g in groups)
            {
                if (index != g.Captrue.Index)
                {
                    seg.Add(new TextLineInfo.TextSegment(index, g.Captrue.Index - index, token.Foreground));
                }

                seg.Add(new TextLineInfo.TextSegment(g.Captrue.Index, g.Captrue.Length, g.Foreground));
                index = g.Captrue.Index + g.Captrue.Length;
            }

            if (index != match.Index + match.Length)
            {
                seg.Add(new TextLineInfo.TextSegment(index, match.Index + match.Length - index, token.Foreground));
            }
        }

        // Add segments to TextLineInfo if there is code block found.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, int index, int length, CodeBlock block, bool hasStart, bool hasEnd)
        {
            if (block.WrapperForeground == null)
            {
                seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));
                return;
            }

            if (hasStart)
            {
                seg.Add(new TextLineInfo.TextSegment(index, block.Start.Length, block.WrapperForeground));
                index += block.Start.Length;
                length -= block.Start.Length;
            }

            length = hasEnd ? length - block.End.Length : length;
            seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));

            if (hasEnd)
            {
                seg.Add(new TextLineInfo.TextSegment(index + length, block.End.Length, block.WrapperForeground));
            }
        }

        // Check if line starting from index matches a code block.
        private T MatchLineBlockStart<T>(IEnumerable<T> blocks, string line, int index) where T : CodeBlock
        {
            return blocks.FirstOrDefault(block => StartsWith(line, index, block.Start));
        }

        // Find the block end index.
        private int GetBlockEndIndex(string line, int index, CodeBlock block, bool ignoreBlockStart)
        {
            if (!ignoreBlockStart)
            {
                index += block.Start.Length;
            }

            if (block is SingleLineBlock && string.IsNullOrEmpty(block.End))
            {
                return line.Length;
            }

            for (int i = index; i < line.Length; ++i)
            {
                var escape = block.Escape;
                if (escape != null)
                {
                    string escapeString = block.Escape.EscapeString;
                    if (!string.IsNullOrEmpty(escapeString) && StartsWith(line, i, escapeString))
                    {
                        i += escapeString.Length; // skip next one;
                        continue;
                    }

                    string[] escapeItems = block.Escape.Items ?? (new string[0]);
                    string found = escapeItems.FirstOrDefault(item => StartsWith(line, i, item));
                    if (found != null)
                    {
                        i += found.Length - 1;
                        continue;
                    }
                }

                if (StartsWith(line, i, block.End))
                {
                    return i + block.End.Length;
                }
            }

            // not found.
            return -1;
        }

        // Get next index of end of the token.
        private int GetNextTokenEndIndex(string line, int startIndex)
        {
            for (int i = startIndex; i < line.Length; ++i)
            {
                // Matches delimeter.
                if (Array.BinarySearch(this.profile.Delimiter, line[i]) >= 0)
                {
                    return i;
                }

                if (Array.BinarySearch(this.profile.BackDelimiter, line[i]) >= 0)
                {
                    return i - 1;
                }
            }

            return line.Length;
        }

        // Check if the line starting from index matches a keyword.
        private KeywordCollection MatchKeyword(string line, int index, int length)
        {
            Expect.ArgumentNotNull(line, "line");
            Expect.ArgumentCheck(line.Length >= index + length, "Check line length");

            foreach (var keywords in this.profile.KeywordCollecions)
            {
                int result = keywords.Keywords.BinarySearch(k => CompareKeyword(k, line, index, length));
                if (result >= 0)
                {
                    return keywords;
                }
            }

            return null;
        }

        // Check if the line starting from index starts another string.
        private bool StartsWith(string line, int index, string another)
        {
            if (line.Length < another.Length + index)
            {
                return false;
            }

            for (int i = 0; i < another.Length; ++i)
            {
                if (GetChar(line[index + i]) != GetChar(another[i]))
                {
                    return false;
                }
            }

            return true;
        }

        // Compare a keyword.
        private int CompareKeyword(string keyword, string line, int index, int length)
        {
            for (int i = 0; i < Math.Min(keyword.Length, length); ++i)
            {
                int result = GetChar(keyword[i]) - GetChar(line[index + i]);
                if (result != 0)
                {
                    return result;
                }
            }

            return keyword.Length - length;
        }

        // Convert a char.
        private char GetChar(char c)
        {
            return this.profile.IgnoreCase ? char.ToUpper(c) : c;
        }

        #endregion
    }
}
//---------------------------------------------------------------------
// <copyright file="HighlightParser.cs" company="Microsoft">
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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// A class to parse text lines.
    /// </summary>
    public class HighlightParser
    {
        private HighlightProfile profile;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="profile"></param>
        public HighlightParser(HighlightProfile profile)
        {
            this.profile = profile;
        }

        /// <summary>
        /// Parse the text lines;
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public TextLineInfo[] Parse(string[] lines, int startLine)
        {
            Expect.ArgumentNotNull(lines, "lines");
            Expect.ArgumentCheck(startLine >= 0, "startLine should >= 0");

            MultiLinesBlock inBlock = null;
            TextLineInfo[] lineInfos = new TextLineInfo[lines.Length];
            for (int i = 0; i < lines.Length; ++i)
            {
                lineInfos[i] = new TextLineInfo(lines[i], startLine + i);
                if (this.profile != null)
                {
                    inBlock = ParseLine(lineInfos[i], inBlock);
                }
            }

            return lineInfos;
        }

        #region Private Methods

        // Parse single line.
        private MultiLinesBlock ParseLine(TextLineInfo info, MultiLinesBlock blockForLineStart)
        {
            var seg = info.Segments;
            string line = info.TextLine;

            int index = 0;
            while (index < line.Length)
            {
                int endIndex;
                // check multiline block
                bool lineStartInBlock = blockForLineStart != null;
                if (blockForLineStart != null
                    || (blockForLineStart = MatchLineBlockStart(this.profile.MultiLinesBlocks, line, index)) != null)
                {
                    endIndex = GetBlockEndIndex(line, index, blockForLineStart, lineStartInBlock);
                    // try next line.
                    if (endIndex < 0)
                    {
                        AddSegments(seg, index, line.Length - index, blockForLineStart, !lineStartInBlock, false);
                        return blockForLineStart;
                    }

                    AddSegments(seg, index, endIndex - index, blockForLineStart, !lineStartInBlock, true);
                    blockForLineStart = null;
                    index = endIndex;
                    continue;
                }

                // check single line block
                SingleLineBlock singleLineBlock = MatchLineBlockStart(this.profile.SingleLineBlocks, line, index);
                if (singleLineBlock != null)
                {
                    endIndex = GetBlockEndIndex(line, index, singleLineBlock, false);
                    endIndex = endIndex < 0 ? line.Length : endIndex;

                    AddSegments(seg, index, endIndex - index, singleLineBlock, true, true);
                    index = endIndex;
                    continue;
                }

                // check tokens
                Token token;
                var match = MatchToken(line, index, out token);
                if (match != null)
                {
                    AddSegments(seg, token, match);
                    index += match.Length;
                    continue;
                }

                // check keywords
                endIndex = GetNextTokenEndIndex(line, index);
                if (endIndex > index)
                {
                    int length = endIndex - index;
                    var keywords = MatchKeyword(line, index, length);
                    if (keywords != null)
                    {
                        seg.Add(new TextLineInfo.TextSegment(index, length, keywords.Foreground));
                    }
                    index = endIndex + 1;
                    continue;
                }

                ++index;
            }

            return blockForLineStart;
        }

        // Check if given line can match token using regular expression.
        private Match MatchToken(string line, int index, out Token token)
        {
            foreach (var t in this.profile.Tokens)
            {
                Match match = t.Pattern.Match(line, index);
                if (match.Success && match.Index == index)
                {
                    token = t;
                    return match;
                }
            }

            token = null;
            return null;
        }

        // Add segments to TextLineInfo if there is token matches.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, Token token, Match match)
        {
            if (token.Groups == null || token.Groups.Length == 0)
            {
                seg.Add(new TextLineInfo.TextSegment(match.Index, match.Length, token.Foreground));
                return;
            }

            int index = match.Index;
            var groups = token.Groups
                .Select(g => new { Foreground = g.Foreground, Captrue = match.Groups[g.Name] })
                .OrderBy(g => g.Captrue.Index);

            foreach (var g in groups)
            {
                if (index != g.Captrue.Index)
                {
                    seg.Add(new TextLineInfo.TextSegment(index, g.Captrue.Index - index, token.Foreground));
                }

                seg.Add(new TextLineInfo.TextSegment(g.Captrue.Index, g.Captrue.Length, g.Foreground));
                index = g.Captrue.Index + g.Captrue.Length;
            }

            if (index != match.Index + match.Length)
            {
                seg.Add(new TextLineInfo.TextSegment(index, match.Index + match.Length - index, token.Foreground));
            }
        }

        // Add segments to TextLineInfo if there is code block found.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, int index, int length, CodeBlock block, bool hasStart, bool hasEnd)
        {
            if (block.WrapperForeground == null)
            {
                seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));
                return;
            }

            if (hasStart)
            {
                seg.Add(new TextLineInfo.TextSegment(index, block.Start.Length, block.WrapperForeground));
                index += block.Start.Length;
                length -= block.Start.Length;
            }

            length = hasEnd ? length - block.End.Length : length;
            seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));

            if (hasEnd)
            {
                seg.Add(new TextLineInfo.TextSegment(index + length, block.End.Length, block.WrapperForeground));
            }
        }

        // Check if line starting from index matches a code block.
        private T MatchLineBlockStart<T>(IEnumerable<T> blocks, string line, int index) where T : CodeBlock
        {
            return blocks.FirstOrDefault(block => StartsWith(line, index, block.Start));
        }

        // Find the block end index.
        private int GetBlockEndIndex(string line, int index, CodeBlock block, bool ignoreBlockStart)
        {
            if (!ignoreBlockStart)
            {
                index += block.Start.Length;
            }

            if (block is SingleLineBlock && string.IsNullOrEmpty(block.End))
            {
                return line.Length;
            }

            for (int i = index; i < line.Length; ++i)
            {
                var escape = block.Escape;
                if (escape != null)
                {
                    string escapeString = block.Escape.EscapeString;
                    if (!string.IsNullOrEmpty(escapeString) && StartsWith(line, i, escapeString))
                    {
                        i += escapeString.Length; // skip next one;
                        continue;
                    }

                    string[] escapeItems = block.Escape.Items ?? (new string[0]);
                    string found = escapeItems.FirstOrDefault(item => StartsWith(line, i, item));
                    if (found != null)
                    {
                        i += found.Length - 1;
                        continue;
                    }
                }

                if (StartsWith(line, i, block.End))
                {
                    return i + block.End.Length;
                }
            }

            // not found.
            return -1;
        }

        // Get next index of end of the token.
        private int GetNextTokenEndIndex(string line, int startIndex)
        {
            for (int i = startIndex; i < line.Length; ++i)
            {
                // Matches delimeter.
                if (Array.BinarySearch(this.profile.Delimiter, line[i]) >= 0)
                {
                    return i;
                }

                if (Array.BinarySearch(this.profile.BackDelimiter, line[i]) >= 0)
                {
                    return i - 1;
                }
            }

            return line.Length;
        }

        // Check if the line starting from index matches a keyword.
        private KeywordCollection MatchKeyword(string line, int index, int length)
        {
            Expect.ArgumentNotNull(line, "line");
            Expect.ArgumentCheck(line.Length >= index + length, "Check line length");

            foreach (var keywords in this.profile.KeywordCollecions)
            {
                int result = keywords.Keywords.BinarySearch(k => CompareKeyword(k, line, index, length));
                if (result >= 0)
                {
                    return keywords;
                }
            }

            return null;
        }

        // Check if the line starting from index starts another string.
        private bool StartsWith(string line, int index, string another)
        {
            if (line.Length < another.Length + index)
            {
                return false;
            }

            for (int i = 0; i < another.Length; ++i)
            {
                if (GetChar(line[index + i]) != GetChar(another[i]))
                {
                    return false;
                }
            }

            return true;
        }

        // Compare a keyword.
        private int CompareKeyword(string keyword, string line, int index, int length)
        {
            for (int i = 0; i < Math.Min(keyword.Length, length); ++i)
            {
                int result = GetChar(keyword[i]) - GetChar(line[index + i]);
                if (result != 0)
                {
                    return result;
                }
            }

            return keyword.Length - length;
        }

        // Convert a char.
        private char GetChar(char c)
        {
            return this.profile.IgnoreCase ? char.ToUpper(c) : c;
        }

        #endregion
    }
}
//---------------------------------------------------------------------
// <copyright file="HighlightParser.cs" company="Microsoft">
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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// A class to parse text lines.
    /// </summary>
    public class HighlightParser
    {
        private HighlightProfile profile;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="profile"></param>
        public HighlightParser(HighlightProfile profile)
        {
            this.profile = profile;
        }

        /// <summary>
        /// Parse the text lines;
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public TextLineInfo[] Parse(string[] lines, int startLine)
        {
            Expect.ArgumentNotNull(lines, "lines");
            Expect.ArgumentCheck(startLine >= 0, "startLine should >= 0");

            MultiLinesBlock inBlock = null;
            TextLineInfo[] lineInfos = new TextLineInfo[lines.Length];
            for (int i = 0; i < lines.Length; ++i)
            {
                lineInfos[i] = new TextLineInfo(lines[i], startLine + i);
                if (this.profile != null)
                {
                    inBlock = ParseLine(lineInfos[i], inBlock);
                }
            }

            return lineInfos;
        }

        #region Private Methods

        // Parse single line.
        private MultiLinesBlock ParseLine(TextLineInfo info, MultiLinesBlock blockForLineStart)
        {
            var seg = info.Segments;
            string line = info.TextLine;

            int index = 0;
            while (index < line.Length)
            {
                int endIndex;
                // check multiline block
                bool lineStartInBlock = blockForLineStart != null;
                if (blockForLineStart != null
                    || (blockForLineStart = MatchLineBlockStart(this.profile.MultiLinesBlocks, line, index)) != null)
                {
                    endIndex = GetBlockEndIndex(line, index, blockForLineStart, lineStartInBlock);
                    // try next line.
                    if (endIndex < 0)
                    {
                        AddSegments(seg, index, line.Length - index, blockForLineStart, !lineStartInBlock, false);
                        return blockForLineStart;
                    }

                    AddSegments(seg, index, endIndex - index, blockForLineStart, !lineStartInBlock, true);
                    blockForLineStart = null;
                    index = endIndex;
                    continue;
                }

                // check single line block
                SingleLineBlock singleLineBlock = MatchLineBlockStart(this.profile.SingleLineBlocks, line, index);
                if (singleLineBlock != null)
                {
                    endIndex = GetBlockEndIndex(line, index, singleLineBlock, false);
                    endIndex = endIndex < 0 ? line.Length : endIndex;

                    AddSegments(seg, index, endIndex - index, singleLineBlock, true, true);
                    index = endIndex;
                    continue;
                }

                // check tokens
                Token token;
                var match = MatchToken(line, index, out token);
                if (match != null)
                {
                    AddSegments(seg, token, match);
                    index += match.Length;
                    continue;
                }

                // check keywords
                endIndex = GetNextTokenEndIndex(line, index);
                if (endIndex > index)
                {
                    int length = endIndex - index;
                    var keywords = MatchKeyword(line, index, length);
                    if (keywords != null)
                    {
                        seg.Add(new TextLineInfo.TextSegment(index, length, keywords.Foreground));
                    }
                    index = endIndex + 1;
                    continue;
                }

                ++index;
            }

            return blockForLineStart;
        }

        // Check if given line can match token using regular expression.
        private Match MatchToken(string line, int index, out Token token)
        {
            foreach (var t in this.profile.Tokens)
            {
                Match match = t.Pattern.Match(line, index);
                if (match.Success && match.Index == index)
                {
                    token = t;
                    return match;
                }
            }

            token = null;
            return null;
        }

        // Add segments to TextLineInfo if there is token matches.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, Token token, Match match)
        {
            if (token.Groups == null || token.Groups.Length == 0)
            {
                seg.Add(new TextLineInfo.TextSegment(match.Index, match.Length, token.Foreground));
                return;
            }

            int index = match.Index;
            var groups = token.Groups
                .Select(g => new { Foreground = g.Foreground, Captrue = match.Groups[g.Name] })
                .OrderBy(g => g.Captrue.Index);

            foreach (var g in groups)
            {
                if (index != g.Captrue.Index)
                {
                    seg.Add(new TextLineInfo.TextSegment(index, g.Captrue.Index - index, token.Foreground));
                }

                seg.Add(new TextLineInfo.TextSegment(g.Captrue.Index, g.Captrue.Length, g.Foreground));
                index = g.Captrue.Index + g.Captrue.Length;
            }

            if (index != match.Index + match.Length)
            {
                seg.Add(new TextLineInfo.TextSegment(index, match.Index + match.Length - index, token.Foreground));
            }
        }

        // Add segments to TextLineInfo if there is code block found.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, int index, int length, CodeBlock block, bool hasStart, bool hasEnd)
        {
            if (block.WrapperForeground == null)
            {
                seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));
                return;
            }

            if (hasStart)
            {
                seg.Add(new TextLineInfo.TextSegment(index, block.Start.Length, block.WrapperForeground));
                index += block.Start.Length;
                length -= block.Start.Length;
            }

            length = hasEnd ? length - block.End.Length : length;
            seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));

            if (hasEnd)
            {
                seg.Add(new TextLineInfo.TextSegment(index + length, block.End.Length, block.WrapperForeground));
            }
        }

        // Check if line starting from index matches a code block.
        private T MatchLineBlockStart<T>(IEnumerable<T> blocks, string line, int index) where T : CodeBlock
        {
            return blocks.FirstOrDefault(block => StartsWith(line, index, block.Start));
        }

        // Find the block end index.
        private int GetBlockEndIndex(string line, int index, CodeBlock block, bool ignoreBlockStart)
        {
            if (!ignoreBlockStart)
            {
                index += block.Start.Length;
            }

            if (block is SingleLineBlock && string.IsNullOrEmpty(block.End))
            {
                return line.Length;
            }

            for (int i = index; i < line.Length; ++i)
            {
                var escape = block.Escape;
                if (escape != null)
                {
                    string escapeString = block.Escape.EscapeString;
                    if (!string.IsNullOrEmpty(escapeString) && StartsWith(line, i, escapeString))
                    {
                        i += escapeString.Length; // skip next one;
                        continue;
                    }

                    string[] escapeItems = block.Escape.Items ?? (new string[0]);
                    string found = escapeItems.FirstOrDefault(item => StartsWith(line, i, item));
                    if (found != null)
                    {
                        i += found.Length - 1;
                        continue;
                    }
                }

                if (StartsWith(line, i, block.End))
                {
                    return i + block.End.Length;
                }
            }

            // not found.
            return -1;
        }

        // Get next index of end of the token.
        private int GetNextTokenEndIndex(string line, int startIndex)
        {
            for (int i = startIndex; i < line.Length; ++i)
            {
                // Matches delimeter.
                if (Array.BinarySearch(this.profile.Delimiter, line[i]) >= 0)
                {
                    return i;
                }

                if (Array.BinarySearch(this.profile.BackDelimiter, line[i]) >= 0)
                {
                    return i - 1;
                }
            }

            return line.Length;
        }

        // Check if the line starting from index matches a keyword.
        private KeywordCollection MatchKeyword(string line, int index, int length)
        {
            Expect.ArgumentNotNull(line, "line");
            Expect.ArgumentCheck(line.Length >= index + length, "Check line length");

            foreach (var keywords in this.profile.KeywordCollecions)
            {
                int result = keywords.Keywords.BinarySearch(k => CompareKeyword(k, line, index, length));
                if (result >= 0)
                {
                    return keywords;
                }
            }

            return null;
        }

        // Check if the line starting from index starts another string.
        private bool StartsWith(string line, int index, string another)
        {
            if (line.Length < another.Length + index)
            {
                return false;
            }

            for (int i = 0; i < another.Length; ++i)
            {
                if (GetChar(line[index + i]) != GetChar(another[i]))
                {
                    return false;
                }
            }

            return true;
        }

        // Compare a keyword.
        private int CompareKeyword(string keyword, string line, int index, int length)
        {
            for (int i = 0; i < Math.Min(keyword.Length, length); ++i)
            {
                int result = GetChar(keyword[i]) - GetChar(line[index + i]);
                if (result != 0)
                {
                    return result;
                }
            }

            return keyword.Length - length;
        }

        // Convert a char.
        private char GetChar(char c)
        {
            return this.profile.IgnoreCase ? char.ToUpper(c) : c;
        }

        #endregion
    }
}
//---------------------------------------------------------------------
// <copyright file="HighlightParser.cs" company="Microsoft">
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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// A class to parse text lines.
    /// </summary>
    public class HighlightParser
    {
        private HighlightProfile profile;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="profile"></param>
        public HighlightParser(HighlightProfile profile)
        {
            this.profile = profile;
        }

        /// <summary>
        /// Parse the text lines;
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public TextLineInfo[] Parse(string[] lines, int startLine)
        {
            Expect.ArgumentNotNull(lines, "lines");
            Expect.ArgumentCheck(startLine >= 0, "startLine should >= 0");

            MultiLinesBlock inBlock = null;
            TextLineInfo[] lineInfos = new TextLineInfo[lines.Length];
            for (int i = 0; i < lines.Length; ++i)
            {
                lineInfos[i] = new TextLineInfo(lines[i], startLine + i);
                if (this.profile != null)
                {
                    inBlock = ParseLine(lineInfos[i], inBlock);
                }
            }

            return lineInfos;
        }

        #region Private Methods

        // Parse single line.
        private MultiLinesBlock ParseLine(TextLineInfo info, MultiLinesBlock blockForLineStart)
        {
            var seg = info.Segments;
            string line = info.TextLine;

            int index = 0;
            while (index < line.Length)
            {
                int endIndex;
                // check multiline block
                bool lineStartInBlock = blockForLineStart != null;
                if (blockForLineStart != null
                    || (blockForLineStart = MatchLineBlockStart(this.profile.MultiLinesBlocks, line, index)) != null)
                {
                    endIndex = GetBlockEndIndex(line, index, blockForLineStart, lineStartInBlock);
                    // try next line.
                    if (endIndex < 0)
                    {
                        AddSegments(seg, index, line.Length - index, blockForLineStart, !lineStartInBlock, false);
                        return blockForLineStart;
                    }

                    AddSegments(seg, index, endIndex - index, blockForLineStart, !lineStartInBlock, true);
                    blockForLineStart = null;
                    index = endIndex;
                    continue;
                }

                // check single line block
                SingleLineBlock singleLineBlock = MatchLineBlockStart(this.profile.SingleLineBlocks, line, index);
                if (singleLineBlock != null)
                {
                    endIndex = GetBlockEndIndex(line, index, singleLineBlock, false);
                    endIndex = endIndex < 0 ? line.Length : endIndex;

                    AddSegments(seg, index, endIndex - index, singleLineBlock, true, true);
                    index = endIndex;
                    continue;
                }

                // check tokens
                Token token;
                var match = MatchToken(line, index, out token);
                if (match != null)
                {
                    AddSegments(seg, token, match);
                    index += match.Length;
                    continue;
                }

                // check keywords
                endIndex = GetNextTokenEndIndex(line, index);
                if (endIndex > index)
                {
                    int length = endIndex - index;
                    var keywords = MatchKeyword(line, index, length);
                    if (keywords != null)
                    {
                        seg.Add(new TextLineInfo.TextSegment(index, length, keywords.Foreground));
                    }
                    index = endIndex + 1;
                    continue;
                }

                ++index;
            }

            return blockForLineStart;
        }

        // Check if given line can match token using regular expression.
        private Match MatchToken(string line, int index, out Token token)
        {
            foreach (var t in this.profile.Tokens)
            {
                Match match = t.Pattern.Match(line, index);
                if (match.Success && match.Index == index)
                {
                    token = t;
                    return match;
                }
            }

            token = null;
            return null;
        }

        // Add segments to TextLineInfo if there is token matches.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, Token token, Match match)
        {
            if (token.Groups == null || token.Groups.Length == 0)
            {
                seg.Add(new TextLineInfo.TextSegment(match.Index, match.Length, token.Foreground));
                return;
            }

            int index = match.Index;
            var groups = token.Groups
                .Select(g => new { Foreground = g.Foreground, Captrue = match.Groups[g.Name] })
                .OrderBy(g => g.Captrue.Index);

            foreach (var g in groups)
            {
                if (index != g.Captrue.Index)
                {
                    seg.Add(new TextLineInfo.TextSegment(index, g.Captrue.Index - index, token.Foreground));
                }

                seg.Add(new TextLineInfo.TextSegment(g.Captrue.Index, g.Captrue.Length, g.Foreground));
                index = g.Captrue.Index + g.Captrue.Length;
            }

            if (index != match.Index + match.Length)
            {
                seg.Add(new TextLineInfo.TextSegment(index, match.Index + match.Length - index, token.Foreground));
            }
        }

        // Add segments to TextLineInfo if there is code block found.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, int index, int length, CodeBlock block, bool hasStart, bool hasEnd)
        {
            if (block.WrapperForeground == null)
            {
                seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));
                return;
            }

            if (hasStart)
            {
                seg.Add(new TextLineInfo.TextSegment(index, block.Start.Length, block.WrapperForeground));
                index += block.Start.Length;
                length -= block.Start.Length;
            }

            length = hasEnd ? length - block.End.Length : length;
            seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));

            if (hasEnd)
            {
                seg.Add(new TextLineInfo.TextSegment(index + length, block.End.Length, block.WrapperForeground));
            }
        }

        // Check if line starting from index matches a code block.
        private T MatchLineBlockStart<T>(IEnumerable<T> blocks, string line, int index) where T : CodeBlock
        {
            return blocks.FirstOrDefault(block => StartsWith(line, index, block.Start));
        }

        // Find the block end index.
        private int GetBlockEndIndex(string line, int index, CodeBlock block, bool ignoreBlockStart)
        {
            if (!ignoreBlockStart)
            {
                index += block.Start.Length;
            }

            if (block is SingleLineBlock && string.IsNullOrEmpty(block.End))
            {
                return line.Length;
            }

            for (int i = index; i < line.Length; ++i)
            {
                var escape = block.Escape;
                if (escape != null)
                {
                    string escapeString = block.Escape.EscapeString;
                    if (!string.IsNullOrEmpty(escapeString) && StartsWith(line, i, escapeString))
                    {
                        i += escapeString.Length; // skip next one;
                        continue;
                    }

                    string[] escapeItems = block.Escape.Items ?? (new string[0]);
                    string found = escapeItems.FirstOrDefault(item => StartsWith(line, i, item));
                    if (found != null)
                    {
                        i += found.Length - 1;
                        continue;
                    }
                }

                if (StartsWith(line, i, block.End))
                {
                    return i + block.End.Length;
                }
            }

            // not found.
            return -1;
        }

        // Get next index of end of the token.
        private int GetNextTokenEndIndex(string line, int startIndex)
        {
            for (int i = startIndex; i < line.Length; ++i)
            {
                // Matches delimeter.
                if (Array.BinarySearch(this.profile.Delimiter, line[i]) >= 0)
                {
                    return i;
                }

                if (Array.BinarySearch(this.profile.BackDelimiter, line[i]) >= 0)
                {
                    return i - 1;
                }
            }

            return line.Length;
        }

        // Check if the line starting from index matches a keyword.
        private KeywordCollection MatchKeyword(string line, int index, int length)
        {
            Expect.ArgumentNotNull(line, "line");
            Expect.ArgumentCheck(line.Length >= index + length, "Check line length");

            foreach (var keywords in this.profile.KeywordCollecions)
            {
                int result = keywords.Keywords.BinarySearch(k => CompareKeyword(k, line, index, length));
                if (result >= 0)
                {
                    return keywords;
                }
            }

            return null;
        }

        // Check if the line starting from index starts another string.
        private bool StartsWith(string line, int index, string another)
        {
            if (line.Length < another.Length + index)
            {
                return false;
            }

            for (int i = 0; i < another.Length; ++i)
            {
                if (GetChar(line[index + i]) != GetChar(another[i]))
                {
                    return false;
                }
            }

            return true;
        }

        // Compare a keyword.
        private int CompareKeyword(string keyword, string line, int index, int length)
        {
            for (int i = 0; i < Math.Min(keyword.Length, length); ++i)
            {
                int result = GetChar(keyword[i]) - GetChar(line[index + i]);
                if (result != 0)
                {
                    return result;
                }
            }

            return keyword.Length - length;
        }

        // Convert a char.
        private char GetChar(char c)
        {
            return this.profile.IgnoreCase ? char.ToUpper(c) : c;
        }

        #endregion
    }
}
//---------------------------------------------------------------------
// <copyright file="HighlightParser.cs" company="Microsoft">
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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// A class to parse text lines.
    /// </summary>
    public class HighlightParser
    {
        private HighlightProfile profile;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="profile"></param>
        public HighlightParser(HighlightProfile profile)
        {
            this.profile = profile;
        }

        /// <summary>
        /// Parse the text lines;
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public TextLineInfo[] Parse(string[] lines, int startLine)
        {
            Expect.ArgumentNotNull(lines, "lines");
            Expect.ArgumentCheck(startLine >= 0, "startLine should >= 0");

            MultiLinesBlock inBlock = null;
            TextLineInfo[] lineInfos = new TextLineInfo[lines.Length];
            for (int i = 0; i < lines.Length; ++i)
            {
                lineInfos[i] = new TextLineInfo(lines[i], startLine + i);
                if (this.profile != null)
                {
                    inBlock = ParseLine(lineInfos[i], inBlock);
                }
            }

            return lineInfos;
        }

        #region Private Methods

        // Parse single line.
        private MultiLinesBlock ParseLine(TextLineInfo info, MultiLinesBlock blockForLineStart)
        {
            var seg = info.Segments;
            string line = info.TextLine;

            int index = 0;
            while (index < line.Length)
            {
                int endIndex;
                // check multiline block
                bool lineStartInBlock = blockForLineStart != null;
                if (blockForLineStart != null
                    || (blockForLineStart = MatchLineBlockStart(this.profile.MultiLinesBlocks, line, index)) != null)
                {
                    endIndex = GetBlockEndIndex(line, index, blockForLineStart, lineStartInBlock);
                    // try next line.
                    if (endIndex < 0)
                    {
                        AddSegments(seg, index, line.Length - index, blockForLineStart, !lineStartInBlock, false);
                        return blockForLineStart;
                    }

                    AddSegments(seg, index, endIndex - index, blockForLineStart, !lineStartInBlock, true);
                    blockForLineStart = null;
                    index = endIndex;
                    continue;
                }

                // check single line block
                SingleLineBlock singleLineBlock = MatchLineBlockStart(this.profile.SingleLineBlocks, line, index);
                if (singleLineBlock != null)
                {
                    endIndex = GetBlockEndIndex(line, index, singleLineBlock, false);
                    endIndex = endIndex < 0 ? line.Length : endIndex;

                    AddSegments(seg, index, endIndex - index, singleLineBlock, true, true);
                    index = endIndex;
                    continue;
                }

                // check tokens
                Token token;
                var match = MatchToken(line, index, out token);
                if (match != null)
                {
                    AddSegments(seg, token, match);
                    index += match.Length;
                    continue;
                }

                // check keywords
                endIndex = GetNextTokenEndIndex(line, index);
                if (endIndex > index)
                {
                    int length = endIndex - index;
                    var keywords = MatchKeyword(line, index, length);
                    if (keywords != null)
                    {
                        seg.Add(new TextLineInfo.TextSegment(index, length, keywords.Foreground));
                    }
                    index = endIndex + 1;
                    continue;
                }

                ++index;
            }

            return blockForLineStart;
        }

        // Check if given line can match token using regular expression.
        private Match MatchToken(string line, int index, out Token token)
        {
            foreach (var t in this.profile.Tokens)
            {
                Match match = t.Pattern.Match(line, index);
                if (match.Success && match.Index == index)
                {
                    token = t;
                    return match;
                }
            }

            token = null;
            return null;
        }

        // Add segments to TextLineInfo if there is token matches.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, Token token, Match match)
        {
            if (token.Groups == null || token.Groups.Length == 0)
            {
                seg.Add(new TextLineInfo.TextSegment(match.Index, match.Length, token.Foreground));
                return;
            }

            int index = match.Index;
            var groups = token.Groups
                .Select(g => new { Foreground = g.Foreground, Captrue = match.Groups[g.Name] })
                .OrderBy(g => g.Captrue.Index);

            foreach (var g in groups)
            {
                if (index != g.Captrue.Index)
                {
                    seg.Add(new TextLineInfo.TextSegment(index, g.Captrue.Index - index, token.Foreground));
                }

                seg.Add(new TextLineInfo.TextSegment(g.Captrue.Index, g.Captrue.Length, g.Foreground));
                index = g.Captrue.Index + g.Captrue.Length;
            }

            if (index != match.Index + match.Length)
            {
                seg.Add(new TextLineInfo.TextSegment(index, match.Index + match.Length - index, token.Foreground));
            }
        }

        // Add segments to TextLineInfo if there is code block found.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, int index, int length, CodeBlock block, bool hasStart, bool hasEnd)
        {
            if (block.WrapperForeground == null)
            {
                seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));
                return;
            }

            if (hasStart)
            {
                seg.Add(new TextLineInfo.TextSegment(index, block.Start.Length, block.WrapperForeground));
                index += block.Start.Length;
                length -= block.Start.Length;
            }

            length = hasEnd ? length - block.End.Length : length;
            seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));

            if (hasEnd)
            {
                seg.Add(new TextLineInfo.TextSegment(index + length, block.End.Length, block.WrapperForeground));
            }
        }

        // Check if line starting from index matches a code block.
        private T MatchLineBlockStart<T>(IEnumerable<T> blocks, string line, int index) where T : CodeBlock
        {
            return blocks.FirstOrDefault(block => StartsWith(line, index, block.Start));
        }

        // Find the block end index.
        private int GetBlockEndIndex(string line, int index, CodeBlock block, bool ignoreBlockStart)
        {
            if (!ignoreBlockStart)
            {
                index += block.Start.Length;
            }

            if (block is SingleLineBlock && string.IsNullOrEmpty(block.End))
            {
                return line.Length;
            }

            for (int i = index; i < line.Length; ++i)
            {
                var escape = block.Escape;
                if (escape != null)
                {
                    string escapeString = block.Escape.EscapeString;
                    if (!string.IsNullOrEmpty(escapeString) && StartsWith(line, i, escapeString))
                    {
                        i += escapeString.Length; // skip next one;
                        continue;
                    }

                    string[] escapeItems = block.Escape.Items ?? (new string[0]);
                    string found = escapeItems.FirstOrDefault(item => StartsWith(line, i, item));
                    if (found != null)
                    {
                        i += found.Length - 1;
                        continue;
                    }
                }

                if (StartsWith(line, i, block.End))
                {
                    return i + block.End.Length;
                }
            }

            // not found.
            return -1;
        }

        // Get next index of end of the token.
        private int GetNextTokenEndIndex(string line, int startIndex)
        {
            for (int i = startIndex; i < line.Length; ++i)
            {
                // Matches delimeter.
                if (Array.BinarySearch(this.profile.Delimiter, line[i]) >= 0)
                {
                    return i;
                }

                if (Array.BinarySearch(this.profile.BackDelimiter, line[i]) >= 0)
                {
                    return i - 1;
                }
            }

            return line.Length;
        }

        // Check if the line starting from index matches a keyword.
        private KeywordCollection MatchKeyword(string line, int index, int length)
        {
            Expect.ArgumentNotNull(line, "line");
            Expect.ArgumentCheck(line.Length >= index + length, "Check line length");

            foreach (var keywords in this.profile.KeywordCollecions)
            {
                int result = keywords.Keywords.BinarySearch(k => CompareKeyword(k, line, index, length));
                if (result >= 0)
                {
                    return keywords;
                }
            }

            return null;
        }

        // Check if the line starting from index starts another string.
        private bool StartsWith(string line, int index, string another)
        {
            if (line.Length < another.Length + index)
            {
                return false;
            }

            for (int i = 0; i < another.Length; ++i)
            {
                if (GetChar(line[index + i]) != GetChar(another[i]))
                {
                    return false;
                }
            }

            return true;
        }

        // Compare a keyword.
        private int CompareKeyword(string keyword, string line, int index, int length)
        {
            for (int i = 0; i < Math.Min(keyword.Length, length); ++i)
            {
                int result = GetChar(keyword[i]) - GetChar(line[index + i]);
                if (result != 0)
                {
                    return result;
                }
            }

            return keyword.Length - length;
        }

        // Convert a char.
        private char GetChar(char c)
        {
            return this.profile.IgnoreCase ? char.ToUpper(c) : c;
        }

        #endregion
    }
}
//---------------------------------------------------------------------
// <copyright file="HighlightParser.cs" company="Microsoft">
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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// A class to parse text lines.
    /// </summary>
    public class HighlightParser
    {
        private HighlightProfile profile;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="profile"></param>
        public HighlightParser(HighlightProfile profile)
        {
            this.profile = profile;
        }

        /// <summary>
        /// Parse the text lines;
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public TextLineInfo[] Parse(string[] lines, int startLine)
        {
            Expect.ArgumentNotNull(lines, "lines");
            Expect.ArgumentCheck(startLine >= 0, "startLine should >= 0");

            MultiLinesBlock inBlock = null;
            TextLineInfo[] lineInfos = new TextLineInfo[lines.Length];
            for (int i = 0; i < lines.Length; ++i)
            {
                lineInfos[i] = new TextLineInfo(lines[i], startLine + i);
                if (this.profile != null)
                {
                    inBlock = ParseLine(lineInfos[i], inBlock);
                }
            }

            return lineInfos;
        }

        #region Private Methods

        // Parse single line.
        private MultiLinesBlock ParseLine(TextLineInfo info, MultiLinesBlock blockForLineStart)
        {
            var seg = info.Segments;
            string line = info.TextLine;

            int index = 0;
            while (index < line.Length)
            {
                int endIndex;
                // check multiline block
                bool lineStartInBlock = blockForLineStart != null;
                if (blockForLineStart != null
                    || (blockForLineStart = MatchLineBlockStart(this.profile.MultiLinesBlocks, line, index)) != null)
                {
                    endIndex = GetBlockEndIndex(line, index, blockForLineStart, lineStartInBlock);
                    // try next line.
                    if (endIndex < 0)
                    {
                        AddSegments(seg, index, line.Length - index, blockForLineStart, !lineStartInBlock, false);
                        return blockForLineStart;
                    }

                    AddSegments(seg, index, endIndex - index, blockForLineStart, !lineStartInBlock, true);
                    blockForLineStart = null;
                    index = endIndex;
                    continue;
                }

                // check single line block
                SingleLineBlock singleLineBlock = MatchLineBlockStart(this.profile.SingleLineBlocks, line, index);
                if (singleLineBlock != null)
                {
                    endIndex = GetBlockEndIndex(line, index, singleLineBlock, false);
                    endIndex = endIndex < 0 ? line.Length : endIndex;

                    AddSegments(seg, index, endIndex - index, singleLineBlock, true, true);
                    index = endIndex;
                    continue;
                }

                // check tokens
                Token token;
                var match = MatchToken(line, index, out token);
                if (match != null)
                {
                    AddSegments(seg, token, match);
                    index += match.Length;
                    continue;
                }

                // check keywords
                endIndex = GetNextTokenEndIndex(line, index);
                if (endIndex > index)
                {
                    int length = endIndex - index;
                    var keywords = MatchKeyword(line, index, length);
                    if (keywords != null)
                    {
                        seg.Add(new TextLineInfo.TextSegment(index, length, keywords.Foreground));
                    }
                    index = endIndex + 1;
                    continue;
                }

                ++index;
            }

            return blockForLineStart;
        }

        // Check if given line can match token using regular expression.
        private Match MatchToken(string line, int index, out Token token)
        {
            foreach (var t in this.profile.Tokens)
            {
                Match match = t.Pattern.Match(line, index);
                if (match.Success && match.Index == index)
                {
                    token = t;
                    return match;
                }
            }

            token = null;
            return null;
        }

        // Add segments to TextLineInfo if there is token matches.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, Token token, Match match)
        {
            if (token.Groups == null || token.Groups.Length == 0)
            {
                seg.Add(new TextLineInfo.TextSegment(match.Index, match.Length, token.Foreground));
                return;
            }

            int index = match.Index;
            var groups = token.Groups
                .Select(g => new { Foreground = g.Foreground, Captrue = match.Groups[g.Name] })
                .OrderBy(g => g.Captrue.Index);

            foreach (var g in groups)
            {
                if (index != g.Captrue.Index)
                {
                    seg.Add(new TextLineInfo.TextSegment(index, g.Captrue.Index - index, token.Foreground));
                }

                seg.Add(new TextLineInfo.TextSegment(g.Captrue.Index, g.Captrue.Length, g.Foreground));
                index = g.Captrue.Index + g.Captrue.Length;
            }

            if (index != match.Index + match.Length)
            {
                seg.Add(new TextLineInfo.TextSegment(index, match.Index + match.Length - index, token.Foreground));
            }
        }

        // Add segments to TextLineInfo if there is code block found.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, int index, int length, CodeBlock block, bool hasStart, bool hasEnd)
        {
            if (block.WrapperForeground == null)
            {
                seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));
                return;
            }

            if (hasStart)
            {
                seg.Add(new TextLineInfo.TextSegment(index, block.Start.Length, block.WrapperForeground));
                index += block.Start.Length;
                length -= block.Start.Length;
            }

            length = hasEnd ? length - block.End.Length : length;
            seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));

            if (hasEnd)
            {
                seg.Add(new TextLineInfo.TextSegment(index + length, block.End.Length, block.WrapperForeground));
            }
        }

        // Check if line starting from index matches a code block.
        private T MatchLineBlockStart<T>(IEnumerable<T> blocks, string line, int index) where T : CodeBlock
        {
            return blocks.FirstOrDefault(block => StartsWith(line, index, block.Start));
        }

        // Find the block end index.
        private int GetBlockEndIndex(string line, int index, CodeBlock block, bool ignoreBlockStart)
        {
            if (!ignoreBlockStart)
            {
                index += block.Start.Length;
            }

            if (block is SingleLineBlock && string.IsNullOrEmpty(block.End))
            {
                return line.Length;
            }

            for (int i = index; i < line.Length; ++i)
            {
                var escape = block.Escape;
                if (escape != null)
                {
                    string escapeString = block.Escape.EscapeString;
                    if (!string.IsNullOrEmpty(escapeString) && StartsWith(line, i, escapeString))
                    {
                        i += escapeString.Length; // skip next one;
                        continue;
                    }

                    string[] escapeItems = block.Escape.Items ?? (new string[0]);
                    string found = escapeItems.FirstOrDefault(item => StartsWith(line, i, item));
                    if (found != null)
                    {
                        i += found.Length - 1;
                        continue;
                    }
                }

                if (StartsWith(line, i, block.End))
                {
                    return i + block.End.Length;
                }
            }

            // not found.
            return -1;
        }

        // Get next index of end of the token.
        private int GetNextTokenEndIndex(string line, int startIndex)
        {
            for (int i = startIndex; i < line.Length; ++i)
            {
                // Matches delimeter.
                if (Array.BinarySearch(this.profile.Delimiter, line[i]) >= 0)
                {
                    return i;
                }

                if (Array.BinarySearch(this.profile.BackDelimiter, line[i]) >= 0)
                {
                    return i - 1;
                }
            }

            return line.Length;
        }

        // Check if the line starting from index matches a keyword.
        private KeywordCollection MatchKeyword(string line, int index, int length)
        {
            Expect.ArgumentNotNull(line, "line");
            Expect.ArgumentCheck(line.Length >= index + length, "Check line length");

            foreach (var keywords in this.profile.KeywordCollecions)
            {
                int result = keywords.Keywords.BinarySearch(k => CompareKeyword(k, line, index, length));
                if (result >= 0)
                {
                    return keywords;
                }
            }

            return null;
        }

        // Check if the line starting from index starts another string.
        private bool StartsWith(string line, int index, string another)
        {
            if (line.Length < another.Length + index)
            {
                return false;
            }

            for (int i = 0; i < another.Length; ++i)
            {
                if (GetChar(line[index + i]) != GetChar(another[i]))
                {
                    return false;
                }
            }

            return true;
        }

        // Compare a keyword.
        private int CompareKeyword(string keyword, string line, int index, int length)
        {
            for (int i = 0; i < Math.Min(keyword.Length, length); ++i)
            {
                int result = GetChar(keyword[i]) - GetChar(line[index + i]);
                if (result != 0)
                {
                    return result;
                }
            }

            return keyword.Length - length;
        }

        // Convert a char.
        private char GetChar(char c)
        {
            return this.profile.IgnoreCase ? char.ToUpper(c) : c;
        }

        #endregion
    }
}
//---------------------------------------------------------------------
// <copyright file="HighlightParser.cs" company="Microsoft">
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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// A class to parse text lines.
    /// </summary>
    public class HighlightParser
    {
        private HighlightProfile profile;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="profile"></param>
        public HighlightParser(HighlightProfile profile)
        {
            this.profile = profile;
        }

        /// <summary>
        /// Parse the text lines;
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public TextLineInfo[] Parse(string[] lines, int startLine)
        {
            Expect.ArgumentNotNull(lines, "lines");
            Expect.ArgumentCheck(startLine >= 0, "startLine should >= 0");

            MultiLinesBlock inBlock = null;
            TextLineInfo[] lineInfos = new TextLineInfo[lines.Length];
            for (int i = 0; i < lines.Length; ++i)
            {
                lineInfos[i] = new TextLineInfo(lines[i], startLine + i);
                if (this.profile != null)
                {
                    inBlock = ParseLine(lineInfos[i], inBlock);
                }
            }

            return lineInfos;
        }

        #region Private Methods

        // Parse single line.
        private MultiLinesBlock ParseLine(TextLineInfo info, MultiLinesBlock blockForLineStart)
        {
            var seg = info.Segments;
            string line = info.TextLine;

            int index = 0;
            while (index < line.Length)
            {
                int endIndex;
                // check multiline block
                bool lineStartInBlock = blockForLineStart != null;
                if (blockForLineStart != null
                    || (blockForLineStart = MatchLineBlockStart(this.profile.MultiLinesBlocks, line, index)) != null)
                {
                    endIndex = GetBlockEndIndex(line, index, blockForLineStart, lineStartInBlock);
                    // try next line.
                    if (endIndex < 0)
                    {
                        AddSegments(seg, index, line.Length - index, blockForLineStart, !lineStartInBlock, false);
                        return blockForLineStart;
                    }

                    AddSegments(seg, index, endIndex - index, blockForLineStart, !lineStartInBlock, true);
                    blockForLineStart = null;
                    index = endIndex;
                    continue;
                }

                // check single line block
                SingleLineBlock singleLineBlock = MatchLineBlockStart(this.profile.SingleLineBlocks, line, index);
                if (singleLineBlock != null)
                {
                    endIndex = GetBlockEndIndex(line, index, singleLineBlock, false);
                    endIndex = endIndex < 0 ? line.Length : endIndex;

                    AddSegments(seg, index, endIndex - index, singleLineBlock, true, true);
                    index = endIndex;
                    continue;
                }

                // check tokens
                Token token;
                var match = MatchToken(line, index, out token);
                if (match != null)
                {
                    AddSegments(seg, token, match);
                    index += match.Length;
                    continue;
                }

                // check keywords
                endIndex = GetNextTokenEndIndex(line, index);
                if (endIndex > index)
                {
                    int length = endIndex - index;
                    var keywords = MatchKeyword(line, index, length);
                    if (keywords != null)
                    {
                        seg.Add(new TextLineInfo.TextSegment(index, length, keywords.Foreground));
                    }
                    index = endIndex + 1;
                    continue;
                }

                ++index;
            }

            return blockForLineStart;
        }

        // Check if given line can match token using regular expression.
        private Match MatchToken(string line, int index, out Token token)
        {
            foreach (var t in this.profile.Tokens)
            {
                Match match = t.Pattern.Match(line, index);
                if (match.Success && match.Index == index)
                {
                    token = t;
                    return match;
                }
            }

            token = null;
            return null;
        }

        // Add segments to TextLineInfo if there is token matches.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, Token token, Match match)
        {
            if (token.Groups == null || token.Groups.Length == 0)
            {
                seg.Add(new TextLineInfo.TextSegment(match.Index, match.Length, token.Foreground));
                return;
            }

            int index = match.Index;
            var groups = token.Groups
                .Select(g => new { Foreground = g.Foreground, Captrue = match.Groups[g.Name] })
                .OrderBy(g => g.Captrue.Index);

            foreach (var g in groups)
            {
                if (index != g.Captrue.Index)
                {
                    seg.Add(new TextLineInfo.TextSegment(index, g.Captrue.Index - index, token.Foreground));
                }

                seg.Add(new TextLineInfo.TextSegment(g.Captrue.Index, g.Captrue.Length, g.Foreground));
                index = g.Captrue.Index + g.Captrue.Length;
            }

            if (index != match.Index + match.Length)
            {
                seg.Add(new TextLineInfo.TextSegment(index, match.Index + match.Length - index, token.Foreground));
            }
        }

        // Add segments to TextLineInfo if there is code block found.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, int index, int length, CodeBlock block, bool hasStart, bool hasEnd)
        {
            if (block.WrapperForeground == null)
            {
                seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));
                return;
            }

            if (hasStart)
            {
                seg.Add(new TextLineInfo.TextSegment(index, block.Start.Length, block.WrapperForeground));
                index += block.Start.Length;
                length -= block.Start.Length;
            }

            length = hasEnd ? length - block.End.Length : length;
            seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));

            if (hasEnd)
            {
                seg.Add(new TextLineInfo.TextSegment(index + length, block.End.Length, block.WrapperForeground));
            }
        }

        // Check if line starting from index matches a code block.
        private T MatchLineBlockStart<T>(IEnumerable<T> blocks, string line, int index) where T : CodeBlock
        {
            return blocks.FirstOrDefault(block => StartsWith(line, index, block.Start));
        }

        // Find the block end index.
        private int GetBlockEndIndex(string line, int index, CodeBlock block, bool ignoreBlockStart)
        {
            if (!ignoreBlockStart)
            {
                index += block.Start.Length;
            }

            if (block is SingleLineBlock && string.IsNullOrEmpty(block.End))
            {
                return line.Length;
            }

            for (int i = index; i < line.Length; ++i)
            {
                var escape = block.Escape;
                if (escape != null)
                {
                    string escapeString = block.Escape.EscapeString;
                    if (!string.IsNullOrEmpty(escapeString) && StartsWith(line, i, escapeString))
                    {
                        i += escapeString.Length; // skip next one;
                        continue;
                    }

                    string[] escapeItems = block.Escape.Items ?? (new string[0]);
                    string found = escapeItems.FirstOrDefault(item => StartsWith(line, i, item));
                    if (found != null)
                    {
                        i += found.Length - 1;
                        continue;
                    }
                }

                if (StartsWith(line, i, block.End))
                {
                    return i + block.End.Length;
                }
            }

            // not found.
            return -1;
        }

        // Get next index of end of the token.
        private int GetNextTokenEndIndex(string line, int startIndex)
        {
            for (int i = startIndex; i < line.Length; ++i)
            {
                // Matches delimeter.
                if (Array.BinarySearch(this.profile.Delimiter, line[i]) >= 0)
                {
                    return i;
                }

                if (Array.BinarySearch(this.profile.BackDelimiter, line[i]) >= 0)
                {
                    return i - 1;
                }
            }

            return line.Length;
        }

        // Check if the line starting from index matches a keyword.
        private KeywordCollection MatchKeyword(string line, int index, int length)
        {
            Expect.ArgumentNotNull(line, "line");
            Expect.ArgumentCheck(line.Length >= index + length, "Check line length");

            foreach (var keywords in this.profile.KeywordCollecions)
            {
                int result = keywords.Keywords.BinarySearch(k => CompareKeyword(k, line, index, length));
                if (result >= 0)
                {
                    return keywords;
                }
            }

            return null;
        }

        // Check if the line starting from index starts another string.
        private bool StartsWith(string line, int index, string another)
        {
            if (line.Length < another.Length + index)
            {
                return false;
            }

            for (int i = 0; i < another.Length; ++i)
            {
                if (GetChar(line[index + i]) != GetChar(another[i]))
                {
                    return false;
                }
            }

            return true;
        }

        // Compare a keyword.
        private int CompareKeyword(string keyword, string line, int index, int length)
        {
            for (int i = 0; i < Math.Min(keyword.Length, length); ++i)
            {
                int result = GetChar(keyword[i]) - GetChar(line[index + i]);
                if (result != 0)
                {
                    return result;
                }
            }

            return keyword.Length - length;
        }

        // Convert a char.
        private char GetChar(char c)
        {
            return this.profile.IgnoreCase ? char.ToUpper(c) : c;
        }

        #endregion
    }
}
//---------------------------------------------------------------------
// <copyright file="HighlightParser.cs" company="Microsoft">
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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// A class to parse text lines.
    /// </summary>
    public class HighlightParser
    {
        private HighlightProfile profile;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="profile"></param>
        public HighlightParser(HighlightProfile profile)
        {
            this.profile = profile;
        }

        /// <summary>
        /// Parse the text lines;
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public TextLineInfo[] Parse(string[] lines, int startLine)
        {
            Expect.ArgumentNotNull(lines, "lines");
            Expect.ArgumentCheck(startLine >= 0, "startLine should >= 0");

            MultiLinesBlock inBlock = null;
            TextLineInfo[] lineInfos = new TextLineInfo[lines.Length];
            for (int i = 0; i < lines.Length; ++i)
            {
                lineInfos[i] = new TextLineInfo(lines[i], startLine + i);
                if (this.profile != null)
                {
                    inBlock = ParseLine(lineInfos[i], inBlock);
                }
            }

            return lineInfos;
        }

        #region Private Methods

        // Parse single line.
        private MultiLinesBlock ParseLine(TextLineInfo info, MultiLinesBlock blockForLineStart)
        {
            var seg = info.Segments;
            string line = info.TextLine;

            int index = 0;
            while (index < line.Length)
            {
                int endIndex;
                // check multiline block
                bool lineStartInBlock = blockForLineStart != null;
                if (blockForLineStart != null
                    || (blockForLineStart = MatchLineBlockStart(this.profile.MultiLinesBlocks, line, index)) != null)
                {
                    endIndex = GetBlockEndIndex(line, index, blockForLineStart, lineStartInBlock);
                    // try next line.
                    if (endIndex < 0)
                    {
                        AddSegments(seg, index, line.Length - index, blockForLineStart, !lineStartInBlock, false);
                        return blockForLineStart;
                    }

                    AddSegments(seg, index, endIndex - index, blockForLineStart, !lineStartInBlock, true);
                    blockForLineStart = null;
                    index = endIndex;
                    continue;
                }

                // check single line block
                SingleLineBlock singleLineBlock = MatchLineBlockStart(this.profile.SingleLineBlocks, line, index);
                if (singleLineBlock != null)
                {
                    endIndex = GetBlockEndIndex(line, index, singleLineBlock, false);
                    endIndex = endIndex < 0 ? line.Length : endIndex;

                    AddSegments(seg, index, endIndex - index, singleLineBlock, true, true);
                    index = endIndex;
                    continue;
                }

                // check tokens
                Token token;
                var match = MatchToken(line, index, out token);
                if (match != null)
                {
                    AddSegments(seg, token, match);
                    index += match.Length;
                    continue;
                }

                // check keywords
                endIndex = GetNextTokenEndIndex(line, index);
                if (endIndex > index)
                {
                    int length = endIndex - index;
                    var keywords = MatchKeyword(line, index, length);
                    if (keywords != null)
                    {
                        seg.Add(new TextLineInfo.TextSegment(index, length, keywords.Foreground));
                    }
                    index = endIndex + 1;
                    continue;
                }

                ++index;
            }

            return blockForLineStart;
        }

        // Check if given line can match token using regular expression.
        private Match MatchToken(string line, int index, out Token token)
        {
            foreach (var t in this.profile.Tokens)
            {
                Match match = t.Pattern.Match(line, index);
                if (match.Success && match.Index == index)
                {
                    token = t;
                    return match;
                }
            }

            token = null;
            return null;
        }

        // Add segments to TextLineInfo if there is token matches.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, Token token, Match match)
        {
            if (token.Groups == null || token.Groups.Length == 0)
            {
                seg.Add(new TextLineInfo.TextSegment(match.Index, match.Length, token.Foreground));
                return;
            }

            int index = match.Index;
            var groups = token.Groups
                .Select(g => new { Foreground = g.Foreground, Captrue = match.Groups[g.Name] })
                .OrderBy(g => g.Captrue.Index);

            foreach (var g in groups)
            {
                if (index != g.Captrue.Index)
                {
                    seg.Add(new TextLineInfo.TextSegment(index, g.Captrue.Index - index, token.Foreground));
                }

                seg.Add(new TextLineInfo.TextSegment(g.Captrue.Index, g.Captrue.Length, g.Foreground));
                index = g.Captrue.Index + g.Captrue.Length;
            }

            if (index != match.Index + match.Length)
            {
                seg.Add(new TextLineInfo.TextSegment(index, match.Index + match.Length - index, token.Foreground));
            }
        }

        // Add segments to TextLineInfo if there is code block found.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, int index, int length, CodeBlock block, bool hasStart, bool hasEnd)
        {
            if (block.WrapperForeground == null)
            {
                seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));
                return;
            }

            if (hasStart)
            {
                seg.Add(new TextLineInfo.TextSegment(index, block.Start.Length, block.WrapperForeground));
                index += block.Start.Length;
                length -= block.Start.Length;
            }

            length = hasEnd ? length - block.End.Length : length;
            seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));

            if (hasEnd)
            {
                seg.Add(new TextLineInfo.TextSegment(index + length, block.End.Length, block.WrapperForeground));
            }
        }

        // Check if line starting from index matches a code block.
        private T MatchLineBlockStart<T>(IEnumerable<T> blocks, string line, int index) where T : CodeBlock
        {
            return blocks.FirstOrDefault(block => StartsWith(line, index, block.Start));
        }

        // Find the block end index.
        private int GetBlockEndIndex(string line, int index, CodeBlock block, bool ignoreBlockStart)
        {
            if (!ignoreBlockStart)
            {
                index += block.Start.Length;
            }

            if (block is SingleLineBlock && string.IsNullOrEmpty(block.End))
            {
                return line.Length;
            }

            for (int i = index; i < line.Length; ++i)
            {
                var escape = block.Escape;
                if (escape != null)
                {
                    string escapeString = block.Escape.EscapeString;
                    if (!string.IsNullOrEmpty(escapeString) && StartsWith(line, i, escapeString))
                    {
                        i += escapeString.Length; // skip next one;
                        continue;
                    }

                    string[] escapeItems = block.Escape.Items ?? (new string[0]);
                    string found = escapeItems.FirstOrDefault(item => StartsWith(line, i, item));
                    if (found != null)
                    {
                        i += found.Length - 1;
                        continue;
                    }
                }

                if (StartsWith(line, i, block.End))
                {
                    return i + block.End.Length;
                }
            }

            // not found.
            return -1;
        }

        // Get next index of end of the token.
        private int GetNextTokenEndIndex(string line, int startIndex)
        {
            for (int i = startIndex; i < line.Length; ++i)
            {
                // Matches delimeter.
                if (Array.BinarySearch(this.profile.Delimiter, line[i]) >= 0)
                {
                    return i;
                }

                if (Array.BinarySearch(this.profile.BackDelimiter, line[i]) >= 0)
                {
                    return i - 1;
                }
            }

            return line.Length;
        }

        // Check if the line starting from index matches a keyword.
        private KeywordCollection MatchKeyword(string line, int index, int length)
        {
            Expect.ArgumentNotNull(line, "line");
            Expect.ArgumentCheck(line.Length >= index + length, "Check line length");

            foreach (var keywords in this.profile.KeywordCollecions)
            {
                int result = keywords.Keywords.BinarySearch(k => CompareKeyword(k, line, index, length));
                if (result >= 0)
                {
                    return keywords;
                }
            }

            return null;
        }

        // Check if the line starting from index starts another string.
        private bool StartsWith(string line, int index, string another)
        {
            if (line.Length < another.Length + index)
            {
                return false;
            }

            for (int i = 0; i < another.Length; ++i)
            {
                if (GetChar(line[index + i]) != GetChar(another[i]))
                {
                    return false;
                }
            }

            return true;
        }

        // Compare a keyword.
        private int CompareKeyword(string keyword, string line, int index, int length)
        {
            for (int i = 0; i < Math.Min(keyword.Length, length); ++i)
            {
                int result = GetChar(keyword[i]) - GetChar(line[index + i]);
                if (result != 0)
                {
                    return result;
                }
            }

            return keyword.Length - length;
        }

        // Convert a char.
        private char GetChar(char c)
        {
            return this.profile.IgnoreCase ? char.ToUpper(c) : c;
        }

        #endregion
    }
}
//---------------------------------------------------------------------
// <copyright file="HighlightParser.cs" company="Microsoft">
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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// A class to parse text lines.
    /// </summary>
    public class HighlightParser
    {
        private HighlightProfile profile;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="profile"></param>
        public HighlightParser(HighlightProfile profile)
        {
            this.profile = profile;
        }

        /// <summary>
        /// Parse the text lines;
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public TextLineInfo[] Parse(string[] lines, int startLine)
        {
            Expect.ArgumentNotNull(lines, "lines");
            Expect.ArgumentCheck(startLine >= 0, "startLine should >= 0");

            MultiLinesBlock inBlock = null;
            TextLineInfo[] lineInfos = new TextLineInfo[lines.Length];
            for (int i = 0; i < lines.Length; ++i)
            {
                lineInfos[i] = new TextLineInfo(lines[i], startLine + i);
                if (this.profile != null)
                {
                    inBlock = ParseLine(lineInfos[i], inBlock);
                }
            }

            return lineInfos;
        }

        #region Private Methods

        // Parse single line.
        private MultiLinesBlock ParseLine(TextLineInfo info, MultiLinesBlock blockForLineStart)
        {
            var seg = info.Segments;
            string line = info.TextLine;

            int index = 0;
            while (index < line.Length)
            {
                int endIndex;
                // check multiline block
                bool lineStartInBlock = blockForLineStart != null;
                if (blockForLineStart != null
                    || (blockForLineStart = MatchLineBlockStart(this.profile.MultiLinesBlocks, line, index)) != null)
                {
                    endIndex = GetBlockEndIndex(line, index, blockForLineStart, lineStartInBlock);
                    // try next line.
                    if (endIndex < 0)
                    {
                        AddSegments(seg, index, line.Length - index, blockForLineStart, !lineStartInBlock, false);
                        return blockForLineStart;
                    }

                    AddSegments(seg, index, endIndex - index, blockForLineStart, !lineStartInBlock, true);
                    blockForLineStart = null;
                    index = endIndex;
                    continue;
                }

                // check single line block
                SingleLineBlock singleLineBlock = MatchLineBlockStart(this.profile.SingleLineBlocks, line, index);
                if (singleLineBlock != null)
                {
                    endIndex = GetBlockEndIndex(line, index, singleLineBlock, false);
                    endIndex = endIndex < 0 ? line.Length : endIndex;

                    AddSegments(seg, index, endIndex - index, singleLineBlock, true, true);
                    index = endIndex;
                    continue;
                }

                // check tokens
                Token token;
                var match = MatchToken(line, index, out token);
                if (match != null)
                {
                    AddSegments(seg, token, match);
                    index += match.Length;
                    continue;
                }

                // check keywords
                endIndex = GetNextTokenEndIndex(line, index);
                if (endIndex > index)
                {
                    int length = endIndex - index;
                    var keywords = MatchKeyword(line, index, length);
                    if (keywords != null)
                    {
                        seg.Add(new TextLineInfo.TextSegment(index, length, keywords.Foreground));
                    }
                    index = endIndex + 1;
                    continue;
                }

                ++index;
            }

            return blockForLineStart;
        }

        // Check if given line can match token using regular expression.
        private Match MatchToken(string line, int index, out Token token)
        {
            foreach (var t in this.profile.Tokens)
            {
                Match match = t.Pattern.Match(line, index);
                if (match.Success && match.Index == index)
                {
                    token = t;
                    return match;
                }
            }

            token = null;
            return null;
        }

        // Add segments to TextLineInfo if there is token matches.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, Token token, Match match)
        {
            if (token.Groups == null || token.Groups.Length == 0)
            {
                seg.Add(new TextLineInfo.TextSegment(match.Index, match.Length, token.Foreground));
                return;
            }

            int index = match.Index;
            var groups = token.Groups
                .Select(g => new { Foreground = g.Foreground, Captrue = match.Groups[g.Name] })
                .OrderBy(g => g.Captrue.Index);

            foreach (var g in groups)
            {
                if (index != g.Captrue.Index)
                {
                    seg.Add(new TextLineInfo.TextSegment(index, g.Captrue.Index - index, token.Foreground));
                }

                seg.Add(new TextLineInfo.TextSegment(g.Captrue.Index, g.Captrue.Length, g.Foreground));
                index = g.Captrue.Index + g.Captrue.Length;
            }

            if (index != match.Index + match.Length)
            {
                seg.Add(new TextLineInfo.TextSegment(index, match.Index + match.Length - index, token.Foreground));
            }
        }

        // Add segments to TextLineInfo if there is code block found.
        private void AddSegments(IList<TextLineInfo.TextSegment> seg, int index, int length, CodeBlock block, bool hasStart, bool hasEnd)
        {
            if (block.WrapperForeground == null)
            {
                seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));
                return;
            }

            if (hasStart)
            {
                seg.Add(new TextLineInfo.TextSegment(index, block.Start.Length, block.WrapperForeground));
                index += block.Start.Length;
                length -= block.Start.Length;
            }

            length = hasEnd ? length - block.End.Length : length;
            seg.Add(new TextLineInfo.TextSegment(index, length, block.Foreground));

            if (hasEnd)
            {
                seg.Add(new TextLineInfo.TextSegment(index + length, block.End.Length, block.WrapperForeground));
            }
        }

        // Check if line starting from index matches a code block.
        private T MatchLineBlockStart<T>(IEnumerable<T> blocks, string line, int index) where T : CodeBlock
        {
            return blocks.FirstOrDefault(block => StartsWith(line, index, block.Start));
        }

        // Find the block end index.
        private int GetBlockEndIndex(string line, int index, CodeBlock block, bool ignoreBlockStart)
        {
            if (!ignoreBlockStart)
            {
                index += block.Start.Length;
            }

            if (block is SingleLineBlock && string.IsNullOrEmpty(block.End))
            {
                return line.Length;
            }

            for (int i = index; i < line.Length; ++i)
            {
                var escape = block.Escape;
                if (escape != null)
                {
                    string escapeString = block.Escape.EscapeString;
                    if (!string.IsNullOrEmpty(escapeString) && StartsWith(line, i, escapeString))
                    {
                        i += escapeString.Length; // skip next one;
                        continue;
                    }

                    string[] escapeItems = block.Escape.Items ?? (new string[0]);
                    string found = escapeItems.FirstOrDefault(item => StartsWith(line, i, item));
                    if (found != null)
                    {
                        i += found.Length - 1;
                        continue;
                    }
                }

                if (StartsWith(line, i, block.End))
                {
                    return i + block.End.Length;
                }
            }

            // not found.
            return -1;
        }

        // Get next index of end of the token.
        private int GetNextTokenEndIndex(string line, int startIndex)
        {
            for (int i = startIndex; i < line.Length; ++i)
            {
                // Matches delimeter.
                if (Array.BinarySearch(this.profile.Delimiter, line[i]) >= 0)
                {
                    return i;
                }

                if (Array.BinarySearch(this.profile.BackDelimiter, line[i]) >= 0)
                {
                    return i - 1;
                }
            }

            return line.Length;
        }

        // Check if the line starting from index matches a keyword.
        private KeywordCollection MatchKeyword(string line, int index, int length)
        {
            Expect.ArgumentNotNull(line, "line");
            Expect.ArgumentCheck(line.Length >= index + length, "Check line length");

            foreach (var keywords in this.profile.KeywordCollecions)
            {
                int result = keywords.Keywords.BinarySearch(k => CompareKeyword(k, line, index, length));
                if (result >= 0)
                {
                    return keywords;
                }
            }

            return null;
        }

        // Check if the line starting from index starts another string.
        private bool StartsWith(string line, int index, string another)
        {
            if (line.Length < another.Length + index)
            {
                return false;
            }

            for (int i = 0; i < another.Length; ++i)
            {
                if (GetChar(line[index + i]) != GetChar(another[i]))
                {
                    return false;
                }
            }

            return true;
        }

        // Compare a keyword.
        private int CompareKeyword(string keyword, string line, int index, int length)
        {
            for (int i = 0; i < Math.Min(keyword.Length, length); ++i)
            {
                int result = GetChar(keyword[i]) - GetChar(line[index + i]);
                if (result != 0)
                {
                    return result;
                }
            }

            return keyword.Length - length;
        }

        // Convert a char.
        private char GetChar(char c)
        {
            return this.profile.IgnoreCase ? char.ToUpper(c) : c;
        }

        #endregion
    }
}

