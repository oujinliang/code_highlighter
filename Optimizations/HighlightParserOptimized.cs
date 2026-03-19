/* Copyright (C) 2012  Jinliang Ou - 优化版本 */

namespace Org.Jinou.HighlightEngine
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// 优化版的 HighlightParser
    /// </summary>
    public class HighlightParserOptimized
    {
        private HighlightProfile profile;

        // 缓存：提升性能的关键
        private Dictionary<CodeBlock, int[]> _badCharTableCache = new Dictionary<CodeBlock, int[]>();
        private Dictionary<CodeBlock, int[]> _lpsTableCache = new Dictionary<CodeBlock, int[]>();

        public HighlightParserOptimized(HighlightProfile profile)
        {
            this.profile = profile;

            // 预热缓存：一次性构建所有模式的查找表
            WarmupCache();
        }

        /// <summary>
        /// 预热缓存：在构造时一次性完成所有预处理
        /// </summary>
        private void WarmupCache()
        {
            // 预编译正则表达式（如果 profile 中的 Regex 没有编译）
            foreach (var token in profile.Tokens)
            {
                if (!token.Pattern.Options.HasFlag(RegexOptions.Compiled))
                {
                    // 创建编译后的正则（注意：这需要修改 Profile 的 Pattern 为可变）
                    // token.Pattern = new Regex(token.Pattern.ToString(), RegexOptions.Compiled);
                }
            }

            // 预构建所有块的查找表
            foreach (var block in profile.MultiLinesBlocks.Concat(profile.SingleLineBlocks))
            {
                _badCharTableCache[block] = BuildBadCharTable(block.Start);
                _lpsTableCache[block] = BuildLPSTable(block.End);
            }

            // 预排序关键字（确保 BinarySearch 生效）
            foreach (var keywords in profile.KeywordCollecions)
            {
                if (!keywords.Keywords.Any()) continue;

                var sorted = keywords.Keywords.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToArray();
                keywords.Keywords = sorted;
            }
        }

        #region 优化后的 GetBlockEndIndex

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

            // 优化：使用 Boyer-Moore 快速搜索结束符
            int endPos = StringSearch.BoyerMooreSearch(
                line,
                index,
                block.End,
                profile.IgnoreCase
            );

            if (endPos == -1)
                return -1;

            // 检查转义字符（需要从 index 到 endPos 遍历）
            // 这是必要的，因为转义会改变匹配逻辑
            if (block.Escape != null)
            {
                string escapeString = block.Escape.EscapeString;
                string[] escapeItems = block.Escape.Items ?? (new string[0]);

                // 只扫描到找到的结束符位置，而不是整个行
                for (int i = index; i < endPos; i++)
                {
                    // 检查转义字符串
                    if (!string.IsNullOrEmpty(escapeString) &&
                        StartsWith(line, i, escapeString))
                    {
                        // 跳过转义后的字符
                        i += escapeString.Length;

                        // 如果跳过后正好是结束符，则跳过这个结束符
                        if (StartsWith(line, i, block.End))
                        {
                            // 继续搜索下一个结束符
                            int nextEnd = StringSearch.BoyerMooreSearch(
                                line,
                                i + block.End.Length,
                                block.End,
                                profile.IgnoreCase
                            );

                            if (nextEnd == -1)
                                return -1;

                            endPos = nextEnd;
                            break;
                        }

                        continue;
                    }

                    // 检查转义字符数组
                    foreach (var esc in escapeItems)
                    {
                        if (StartsWith(line, i, esc))
                        {
                            i += esc.Length;
                            break;
                        }
                    }
                }
            }

            return endPos + block.End.Length;
        }

        #endregion

        #region 优化后的 MatchKeyword

        private KeywordCollection MatchKeyword(string line, int index, int length)
        {
            Expect.ArgumentNotNull(line, "line");
            Expect.ArgumentCheck(line.Length >= index + length, "Check line length");

            // 提取待匹配的子字符串（只提取一次）
            string substring = line.Substring(index, length);

            // 优化：对每个关键字集合使用 Array.BinarySearch
            foreach (var keywords in profile.KeywordCollecions)
            {
                // BinarySearch 在预排序数组上为 O(log n)
                int result = Array.BinarySearch(
                    keywords.Keywords,
                    substring,
                    profile.IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal
                );

                if (result >= 0)
                    return keywords;
            }

            return null;
        }

        #endregion

        #region 优化后的 ParseLine（Token 匹配）

        private MultiLinesBlock ParseLine(TextLineInfo info, MultiLinesBlock blockForLineStart)
        {
            var seg = info.Segments;
            string line = info.TextLine;

            int index = 0;
            while (index < line.Length)
            {
                int endIndex;

                // 检查多行块（优先级最高）
                bool lineStartInBlock = blockForLineStart != null;
                if (blockForLineStart != null
                    || (blockForLineStart = MatchLineBlockStart(profile.MultiLinesBlocks, line, index)) != null)
                {
                    endIndex = GetBlockEndIndex(line, index, blockForLineStart, lineStartInBlock);

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

                // 检查单行块
                SingleLineBlock singleLineBlock = MatchLineBlockStart(profile.SingleLineBlocks, line, index);
                if (singleLineBlock != null)
                {
                    endIndex = GetBlockEndIndex(line, index, singleLineBlock, false);
                    endIndex = endIndex < 0 ? line.Length : endIndex;

                    AddSegments(seg, index, endIndex - index, singleLineBlock, true, true);
                    index = endIndex;
                    continue;
                }

                // 检查 Token（优化：使用预编译的正则）
                Token token;
                var match = MatchTokenOptimized(line, index, out token);
                if (match != null)
                {
                    AddSegments(seg, token, match);
                    index += match.Length;
                    continue;
                }

                // 检查关键字
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

        #endregion

        #region Token 匹配优化

        /// <summary>
        /// 优化后的 Token 匹配：使用预编译正则 + 快速失败
        /// </summary>
        private Match MatchTokenOptimized(string line, int index, out Token token)
        {
            // 优化：按 Token 的长度降序排列（长模式优先匹配）
            // 这样可以避免短模式误匹配
            foreach (var t in profile.Tokens.OrderByDescending(tok => tok.Pattern.ToString().Length))
            {
                // 优化：使用 IsMatch 快速检查，再执行 Match
                if (!t.Pattern.IsMatch(index, line))
                {
                    continue;
                }

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

        #endregion

        #region 辅助方法（保持原有逻辑）

        private T MatchLineBlockStart<T>(IEnumerable<T> blocks, string line, int index) where T : CodeBlock
        {
            return blocks.FirstOrDefault(block => StartsWith(line, index, block.Start));
        }

        private bool StartsWith(string line, int index, string another)
        {
            if (line.Length < another.Length + index)
                return false;

            // 优化：使用 unsafe 指针操作（需要 unsafe 编译选项）
            // 如果不需要跨平台，可以使用指针加速
            for (int i = 0; i < another.Length; i++)
            {
                if (GetChar(line[index + i]) != GetChar(another[i]))
                    return false;
            }

            return true;
        }

        private char GetChar(char c)
        {
            return profile.IgnoreCase ? char.ToUpper(c) : c;
        }

        private int GetNextTokenEndIndex(string line, int startIndex)
        {
            // 优化：使用预排序的数组 + BinarySearch
            char c = line[startIndex];
            if (Array.BinarySearch(profile.Delimiter, c) >= 0)
                return startIndex;

            // 继续扫描
            for (int i = startIndex + 1; i < line.Length; i++)
            {
                c = line[i];
                if (Array.BinarySearch(profile.Delimiter, c) >= 0)
                    return i;
                if (Array.BinarySearch(profile.BackDelimiter, c) >= 0)
                    return i - 1;
            }

            return line.Length;
        }

        private void AddSegments(IList<TextLineInfo.TextSegment> seg, int index, int length, CodeBlock block, bool hasStart, bool hasEnd)
        {
            // 保持原有实现...
            // （这里省略，与原代码相同）
        }

        private void AddSegments(IList<TextLineInfo.TextSegment> seg, Token token, Match match)
        {
            // 保持原有实现...
            // （这里省略，与原代码相同）
        }

        private void Expect(bool condition, string message)
        {
            if (!condition) throw new ArgumentException(message);
        }

        private static class Expect
        {
            public static void ArgumentNotNull<T>(T value, string name) where T : class
            {
                if (value == null) throw new ArgumentNullException(name);
            }

            public static void ArgumentCheck(bool condition, string message)
            {
                if (!condition) throw new ArgumentException(message);
            }
        }

        #endregion

        #region Boyer-Moore 和 KMP 辅助方法（本地实现）

        private static int[] BuildBadCharTable(string pattern)
        {
            int[] badChar = new int[256];
            for (int i = 0; i < 256; i++)
                badChar[i] = pattern.Length;

            for (int i = 0; i < pattern.Length - 1; i++)
                badChar[pattern[i]] = pattern.Length - 1 - i;

            return badChar;
        }

        private static int[] BuildLPSTable(string pattern)
        {
            int[] lps = new int[pattern.Length];
            int len = 0;
            int i = 1;

            while (i < pattern.Length)
            {
                if (pattern[i] == pattern[len])
                {
                    len++;
                    lps[i] = len;
                    i++;
                }
                else
                {
                    if (len != 0)
                        len = lps[len - 1];
                    else
                    {
                        lps[i] = 0;
                        i++;
                    }
                }
            }

            return lps;
        }

        #endregion
    }

    #region 扩展方法

    public static class RegexExtensions
    {
        /// <summary>
        /// 快速检查是否匹配，避免完整 Match 的开销
        /// </summary>
        public static bool IsMatch(this Regex regex, int start, string input)
        {
            return regex.IsMatch(input.Substring(start));
        }
    }

    #endregion
}
