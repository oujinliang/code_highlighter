/* Copyright (C) 2012  Jinliang Ou - 缓存与并行优化 */

namespace Org.Jinou.HighlightEngine
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// 缓存管理器：复用已解析的结果
    /// </summary>
    public class HighlightCache
    {
        // 行内容 → 解析结果
        private ConcurrentDictionary<string, TextLineInfo> _lineCache =
            new ConcurrentDictionary<string, TextLineInfo>();

        // 文件路径 → 所有行的解析结果
        private ConcurrentDictionary<string, TextLineInfo[]> _fileCache =
            new ConcurrentDictionary<string, TextLineInfo[]>();

        // 缓存统计
        private long _cacheHits = 0;
        private long _cacheMisses = 0;

        /// <summary>
        /// 获取缓存的行解析结果
        /// </summary>
        public bool TryGetLine(string lineContent, out TextLineInfo result)
        {
            if (_lineCache.TryGetValue(lineContent, out result))
            {
                _cacheHits++;
                return true;
            }

            _cacheMisses++;
            return false;
        }

        /// <summary>
        /// 缓存行的解析结果
        /// </summary>
        public void CacheLine(string lineContent, TextLineInfo result)
        {
            _lineCache.TryAdd(lineContent, result);
        }

        /// <summary>
        /// 获取缓存的文件解析结果
        /// </summary>
        public bool TryGetFile(string filePath, out TextLineInfo[] result)
        {
            return _fileCache.TryGetValue(filePath, out result);
        }

        /// <summary>
        /// 缓存文件的解析结果
        /// </summary>
        public void CacheFile(string filePath, TextLineInfo[] result)
        {
            _fileCache.TryAdd(filePath, result);
        }

        /// <summary>
        /// 清除缓存
        /// </summary>
        public void Clear()
        {
            _lineCache.Clear();
            _fileCache.Clear();
            _cacheHits = 0;
            _cacheMisses = 0;
        }

        /// <summary>
        /// 获取缓存命中率
        /// </summary>
        public double GetHitRate()
        {
            long total = _cacheHits + _cacheMisses;
            return total > 0 ? (double)_cacheHits / total : 0.0;
        }

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        public (int LineCacheSize, int FileCacheSize, long Hits, long Misses, double HitRate) GetStats()
        {
            return (
                _lineCache.Count,
                _fileCache.Count,
                _cacheHits,
                _cacheMisses,
                GetHitRate()
            );
        }
    }

    /// <summary>
    /// 并行解析器：利用多核 CPU 加速解析
    /// </summary>
    public class ParallelHighlightParser
    {
        private HighlightProfile profile;
        private HighlightCache cache;

        public ParallelHighlightParser(HighlightProfile profile)
        {
            this.profile = profile;
            this.cache = new HighlightCache();
        }

        /// <summary>
        /// 并行解析多行文本
        /// </summary>
        public TextLineInfo[] ParseParallel(string[] lines, int startLine)
        {
            // 尝试从文件缓存获取
            string fileKey = GetFileKey(lines);
            if (cache.TryGetFile(fileKey, out var cached))
            {
                return cached;
            }

            // 并行解析（注意：多行块的跨行处理需要特殊处理）
            TextLineInfo[] results = new TextLineInfo[lines.Length];

            // 分块并行处理
            int chunkSize = Math.Max(1, lines.Length / Environment.ProcessorCount);
            var chunks = Enumerable.Range(0, (lines.Length + chunkSize - 1) / chunkSize)
                .Select(i => (Start: i * chunkSize, End: Math.Min((i + 1) * chunkSize, lines.Length)))
                .ToArray();

            // 并行处理每个块
            Parallel.ForEach(chunks, chunk =>
            {
                var parser = new HighlightParser(profile);

                for (int i = chunk.Start; i < chunk.End; i++)
                {
                    // 检查行缓存
                    if (!cache.TryGetLine(lines[i], out var lineResult))
                    {
                        lineResult = new TextLineInfo(lines[i], startLine + i);
                        if (profile != null)
                        {
                            // 注意：跨行块在并行模式下需要特殊处理
                            // 这里简化处理，假设没有跨行块
                            parser.ParseLine(lineResult, null);
                        }

                        cache.CacheLine(lines[i], lineResult);
                    }

                    results[i] = lineResult;
                }
            });

            // 处理跨行块（需要顺序处理）
            // 这里是简化版本，完整实现需要先收集所有块，再处理边界
            // 实际项目中，可能需要两阶段解析：
            // 1. 并行处理单行块、token、keyword
            // 2. 顺序处理多行块

            // 缓存结果
            cache.CacheFile(fileKey, results);

            return results;
        }

        /// <summary>
        /// 生成文件缓存键
        /// </summary>
        private string GetFileKey(string[] lines)
        {
            // 简单实现：使用行数 + 第一行 + 最后一行
            // 生产环境应使用文件哈希或修改时间
            return $"{lines.Length}:{lines[0]}:{lines[lines.Length - 1]}";
        }

        /// <summary>
        /// 获取缓存统计
        /// </summary>
        public (int LineCacheSize, int FileCacheSize, long Hits, long Misses, double HitRate) GetCacheStats()
        {
            return cache.GetStats();
        }

        /// <summary>
        /// 清除缓存
        /// </summary>
        public void ClearCache()
        {
            cache.Clear();
        }
    }

    /// <summary>
    /// 增量解析器：只重新解析变化的部分
    /// </summary>
    public class IncrementalHighlightParser
    {
        private HighlightProfile profile;
        private Dictionary<int, TextLineInfo> _parsedLines = new Dictionary<int, TextLineInfo>();

        /// <summary>
        /// 增量更新：只重新解析变化的行
        /// </summary>
        public void UpdateLine(int lineIndex, string newContent, int startLine)
        {
            var parser = new HighlightParser(profile);
            var result = new TextLineInfo(newContent, startLine + lineIndex);

            if (profile != null)
            {
                parser.ParseLine(result, null);
            }

            _parsedLines[lineIndex] = result;
        }

        /// <summary>
        /// 批量更新：支持范围更新
        /// </summary>
        public void UpdateRange(int startIndex, int endIndex, string[] newContent, int startLine)
        {
            for (int i = startIndex; i <= endIndex && i < newContent.Length; i++)
            {
                UpdateLine(i, newContent[i], startLine);
            }
        }

        /// <summary>
        /// 获取已解析的行
        /// </summary>
        public TextLineInfo GetLine(int lineIndex)
        {
            return _parsedLines.TryGetValue(lineIndex, out var result) ? result : null;
        }

        /// <summary>
        /// 清除缓存
        /// </summary>
        public void Clear()
        {
            _parsedLines.Clear();
        }
    }
}
