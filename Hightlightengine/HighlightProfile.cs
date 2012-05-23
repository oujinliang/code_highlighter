//---------------------------------------------------------------------
// <copyright file="HighlightProfile.cs" company="Microsoft">
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
    using System.Text.RegularExpressions;
    using System.Windows.Media;

    /// <summary>
    /// Represent a source code syntax highlight profile.
    /// </summary>
    public class HighlightProfile
    {
        /// <summary> delimiters </summary>
        public char[] Delimiter { get; internal set; }

        /// <summary> back delimiters, if find a delimeter, push back a character </summary>
        public char[] BackDelimiter { get; internal set; }

        /// <summary> ignore case ? </summary>
        public bool IgnoreCase { get; internal set; }

        /// <summary> All keyword collections </summary>
        public KeywordCollection[] KeywordCollecions { get; internal set; }

        /// <summary> All single line block. </summary>
        public SingleLineBlock[] SingleLineBlocks { get; internal set; }

        /// <summary> All multiple lines block. </summary>
        public MultiLinesBlock[] MultiLinesBlocks { get; internal set; }

        /// <summary> All tokens using regular expression. </summary>
        public Token[] Tokens { get; internal set; }
    }

    /// <summary>
    /// Represent a highlight element.
    /// </summary>
    public interface HighLightElement
    {
        /// <summary> Name of the element .</summary>
        string Name { get; }

        /// <summary> Foreground color of the element. </summary>
        Brush Foreground { get; }
    }

    /// <summary>
    /// Keywords 
    /// </summary>
    public class KeywordCollection : HighLightElement
    {
        /// <summary> Name of the element .</summary>
        public string Name { get; internal set; }

        /// <summary> Foreground color of the element. </summary>
        public Brush Foreground { get; internal set; }

        /// <summary> All keywords, sorted. </summary>
        public string[] Keywords { get; internal set; }
    }

    /// <summary>
    /// A code block.
    /// </summary>
    public class CodeBlock : HighLightElement
    {
        /// <summary> Name of the element .</summary>
        public string Name { get; internal set; }

        /// <summary> Foreground color of the element. </summary>
        public Brush Foreground { get; internal set; }

        /// <summary> If block has start and end element, the foreground color of start/end part. </summary>
        public Brush WrapperForeground { get; internal set; }

        /// <summary> block escape. </summary>
        public BlockEscape Escape { get; internal set; }

        /// <summary> start part. </summary>
        public string Start { get; internal set; }

        /// <summary> end part. </summary>
        public string End { get; internal set; }
    }

    /// <summary>
    /// Block escape.
    /// </summary>
    public class BlockEscape
    {
        /// <summary> In a code block, if find this escape string, skip next char. </summary>
        public string EscapeString { get; internal set; }

        /// <summary> If find one of the items, skip this item. </summary>
        public string[] Items { get; internal set; }
    }

    /// <summary>
    /// Single line block.
    /// </summary>
    public class SingleLineBlock : CodeBlock
    {
    }

    /// <summary>
    /// Multiple lines block.
    /// </summary>
    public class MultiLinesBlock : CodeBlock
    {
    }

    /// <summary>
    /// Represent token 
    /// </summary>
    public class Token : HighLightElement
    {
        /// <summary> Name of the element .</summary>
        public string Name { get; internal set; }

        /// <summary> Foreground color of the element. </summary>
        public Brush Foreground { get; internal set; }

        /// <summary> pattern to match .</summary>
        public Regex Pattern { get; internal set; }

        /// <summary> match groups. </summary>
        public TokenMatch[] Groups { get; internal set; }
    }

    /// <summary>
    /// Token match group.
    /// </summary>
    public class TokenMatch : HighLightElement
    {
        /// <summary> Name of the element .</summary>
        public string Name { get; internal set; }

        /// <summary> Foreground color of the element. </summary>
        public Brush Foreground { get; internal set; }
    }
}
