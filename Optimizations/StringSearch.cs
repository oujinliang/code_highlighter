/* Copyright (C) 2012  Jinliang Ou */

namespace Org.Jinou.HighlightEngine
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// 高性能字符串搜索算法，优化代码高亮的字符串匹配
    /// </summary>
    public static class StringSearch
    {
        #region Boyer-Moore 算法（适合长模式匹配）

        private static Dictionary<string, int[]> _badCharTableCache = new Dictionary<string, int[]>();

        /// <summary>
        /// Boyer-Moore 坏字符规则预处理
        /// </summary>
        private static int[] BuildBadCharTable(string pattern)
        {
            if (_badCharTableCache.TryGetValue(pattern, out var cached))
                return cached;

            int[] badChar = new int[256]; // ASCII 字符集
            for (int i = 0; i < 256; i++)
                badChar[i] = pattern.Length;

            for (int i = 0; i < pattern.Length - 1; i++)
                badChar[pattern[i]] = pattern.Length - 1 - i;

            _badCharTableCache[pattern] = badChar;
            return badChar;
        }

        /// <summary>
        /// Boyer-Moore 搜索（适合搜索结束符如 "*/", "*/" 等）
        /// </summary>
        public static int BoyerMooreSearch(string text, int start, string pattern, bool ignoreCase)
        {
            if (string.IsNullOrEmpty(pattern) || pattern.Length > text.Length - start)
                return -1;

            int[] badChar = BuildBadCharTable(pattern);
            int patternLen = pattern.Length;
            int textLen = text.Length;

            int i = start + patternLen - 1;
            while (i < textLen)
            {
                int j = patternLen - 1;
                while (j >= 0 && EqualsChar(text[i - (patternLen - 1 - j)], pattern[j], ignoreCase))
                    j--;

                if (j < 0)
                    return i - patternLen + 1;

                int badShift = badChar[text[i]];
                i += Math.Max(1, badShift);
            }

            return -1;
        }

        #endregion

        #region KMP 算法（适合多次搜索相同模式）

        private static Dictionary<string, int[]> _lpsTableCache = new Dictionary<string, int[]>();

        /// <summary>
        /// KMP 预处理计算 LPS（最长前缀后缀）数组
        /// </summary>
        private static int[] BuildLPSTable(string pattern)
        {
            if (_lpsTableCache.TryGetValue(pattern, out var cached))
                return cached;

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
                    {
                        len = lps[len - 1];
                    }
                    else
                    {
                        lps[i] = 0;
                        i++;
                    }
                }
            }

            _lpsTableCache[pattern] = lps;
            return lps;
        }

        /// <summary>
        /// KMP 搜索（适合搜索关键词）
        /// </summary>
        public static int KMPSearch(string text, int start, string pattern, bool ignoreCase)
        {
            if (string.IsNullOrEmpty(pattern) || pattern.Length > text.Length - start)
                return -1;

            int[] lps = BuildLPSTable(pattern);
            int i = start;
            int j = 0;

            while (i < text.Length && j < pattern.Length)
            {
                if (EqualsChar(text[i], pattern[j], ignoreCase))
                {
                    i++;
                    j++;
                }

                if (j == pattern.Length)
                {
                    return i - j;
                }
                else if (i < text.Length && !EqualsChar(text[i], pattern[j], ignoreCase))
                {
                    if (j != 0)
                        j = lps[j - 1];
                    else
                        i++;
                }
            }

            return -1;
        }

        #endregion

        #region 工具方法

        private static bool EqualsChar(char a, char b, bool ignoreCase)
        {
            return ignoreCase ? char.ToUpper(a) == char.ToUpper(b) : a == b;
        }

        #endregion
    }
}
