/* Copyright (C) 2012  Jinliang Ou - 正则表达式优化 */

namespace Org.Jinou.HighlightEngine
{
    using System;
    using System.Collections.Concurrent;
    using System.Text.RegularExpressions;

    /// <summary>
    /// 正则表达式优化策略
    /// </summary>
    public class RegexOptimizer
    {
        #region 正则编译池（复用已编译的正则）

        private static readonly ConcurrentDictionary<string, Regex> _regexCache =
            new ConcurrentDictionary<string, Regex>();

        /// <summary>
        /// 获取编译后的正则表达式（带缓存）
        /// </summary>
        public static Regex GetCompiledRegex(string pattern, bool ignoreCase)
        {
            string cacheKey = ignoreCase ? $"i:{pattern}" : pattern;

            return _regexCache.GetOrAdd(cacheKey, key =>
            {
                var options = RegexOptions.Compiled;
                if (ignoreCase)
                    options |= RegexOptions.IgnoreCase;

                return new Regex(pattern, options);
            });
        }

        /// <summary>
        /// 预编译 Profile 中的所有正则表达式
        /// </summary>
        public static void PrecompileProfile(HighlightProfile profile)
        {
            foreach (var token in profile.Tokens)
            {
                // 创建编译后的正则
                var compiledPattern = GetCompiledRegex(
                    token.Pattern.ToString(),
                    profile.IgnoreCase
                );

                // 替换原正则（需要 Profile 的 Pattern 可变）
                // 这里假设 Token 有 SetPattern 方法
                // token.SetPattern(compiledPattern);
            }
        }

        #endregion

        #region 正则优化建议

        /// <summary>
        /// 常见的正则性能优化建议
        /// </summary>
        public static class OptimizationRules
        {
            // 1. 避免回溯
            // ❌ .*?  (非贪婪匹配会导致大量回溯)
            // ✅ [^"]*  (否定字符集，无回溯)

            // 2. 使用原子分组
            // ❌ (?>...)  (C# 不支持原子分组，但可用 possessive quantifiers)
            // ✅ [^"]++  (占有量词，禁止回溯)

            // 3. 锚定开头
            // ❌ \bkeyword\b  (每次都要检查单词边界)
            // ✅ ^\bkeyword\b  (只检查行首)

            // 4. 避免嵌套量词
            // ❌ (a+)+  (指数级复杂度)
            // ✅ a{1,10}  (线性复杂度)

            // 5. 字符集优于 alternation
            // ❌ a|b|c|d|e
            // ✅ [a-e]

            // 6. 具体优于模糊
            // ❌ .+  (匹配任意字符)
            // ✅ [a-zA-Z0-9_]+  (仅匹配标识符)
        }

        #endregion

        #region 实际应用示例

        /// <summary>
        /// 优化前：低效的正则
        /// </summary>
        public static readonly string[] InefficientPatterns = new[]
        {
            // ❌ 过于宽泛，导致大量回溯
            @".*?\b(?:int|float|double|string|void)\b.*?",

            // ❌ 嵌套量词，可能指数爆炸
            @"(\/\*.*?\*\/)+",

            // ❌ 非贪婪 + 复杂匹配
            @".*?=""[^""]*?""",
        };

        /// <summary>
        /// 优化后：高效的替代方案
        /// </summary>
        public static readonly string[] OptimizedPatterns = new[]
        {
            // ✅ 使用字符集，避免回溯
            @"\b(?:int|float|double|string|void)\b",

            // ✅ 简单匹配，不做嵌套
            @"\/\*[\s\S]*?\*\/",

            // ✅ 明确的字符集，精确匹配
            @"=""[^""]*""",
        };

        #endregion
    }
}
