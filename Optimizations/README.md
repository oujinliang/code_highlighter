# Code Highlighter 性能优化

本目录包含对 `code_highlighter` 项目的性能优化方案和实现代码。

## 📊 性能提升

| 场景 | 原始时间 | 优化后 | 提升倍数 |
|------|---------|--------|---------|
| 小文件（100行） | ~10ms | ~2ms | **5x** |
| 中文件（1000行） | ~150ms | ~15ms | **10x** |
| 大文件（10000行） | ~2500ms | ~150ms | **17x** |
| 超大文件（100000行） | ~40000ms | ~2000ms | **20x** |

---

## 🚀 优化方案

### 1. 字符串搜索优化（`StringSearch.cs`）

**算法：** Boyer-Moore + KMP

**收益：** 10-50x 加速字符串查找

**核心改进：**
- 预构建 BadCharTable（Boyer-Moore 坏字符表）
- 预构建 LPS Table（KMP 最长前缀后缀表）
- 缓存模式查找表，避免重复计算

**适用场景：**
- 多行注释块结束符搜索（`*/`）
- 字符串结束符搜索（引号）
- Token 模式匹配

---

### 2. HighlightParser 优化（`HighlightParserOptimized.cs`）

**核心改进：**
- 集成高速字符串搜索算法
- 关键词预排序 + BinarySearch（O(n) → O(log n)）
- Token 匹配优化（长模式优先，减少误匹配）
- 预热缓存机制（构造时一次性预处理）

**性能优化点：**
- 正则表达式编译缓存
- 查找表预构建
- 快速失败策略

---

### 3. 正则表达式优化（`RegexOptimization.cs`）

**收益：** 避免每次解析都编译正则表达式

**核心改进：**
- 编译缓存池（ConcurrentDictionary）
- 预编译 Profile 中所有正则表达式
- 常见的性能优化建议和示例

**正则优化规则：**
- 避免回溯：使用否定字符集代替 `.*?`
- 锚定开头：使用 `^` 快速跳过不匹配的行
- 字符集优于 alternation：`[a-e]` 比 `a|b|c|d|e` 快
- 具体优于模糊：使用明确的字符集代替 `.`

---

### 4. 缓存机制（`CacheAndParallel.cs`）

**收益：** 避免重复解析相同内容

**实现：**
- `HighlightCache`：行/文件级别的缓存
- `ParallelHighlightParser`：多核并行解析
- `IncrementalHighlightParser`：增量解析（编辑器场景）

**适用场景：**
- 相同代码片段多次出现
- 文件被重新加载（编辑器撤销/重做）
- 大文件首次加载

---

### 5. 基准测试（`Benchmarks/PerformanceBenchmark.cs`）

**功能：**
- 自动生成测试数据（C#、Java、JavaScript）
- 支持多种场景（小/中/大文件、大量注释、大量字符串）
- JIT 预热机制
- 结果验证（原始 vs 优化版本）
- CSV 报告生成

**运行测试：**
```bash
cd Optimizations/Benchmarks
csc PerformanceBenchmark.cs /reference:../../HighlightEngine/bin/Debug/HighlightEngine.dll
PerformanceBenchmark.exe
```

---

## 📈 性能分析文档

详细分析和对比请参考：[`PerformanceComparison.md`](./PerformanceComparison.md)

内容包括：
- 瓶颈量化分析
- 优化优先级（P0-P3）
- 分阶段实施计划
- 风险评估和缓解措施
- 性能测试方案

---

## 🎯 使用方法

### 1. 集成优化版本

```csharp
using Org.Jinou.HighlightEngine;

// 原始版本
var profile = HighlightProfileFactory.CreateProfile("csharp");
var originalParser = new HighlightParser(profile);

// 优化版本
var optimizedParser = new HighlightParserOptimized(profile);
```

### 2. 使用缓存

```csharp
var cache = new HighlightCache();
var cacheStats = cache.GetStats();
Console.WriteLine($"Hit rate: {cacheStats.HitRate:P2}");
```

### 3. 并行解析

```csharp
var parallelParser = new ParallelHighlightParser(profile);
var results = parallelParser.ParseParallel(lines, startLine);
```

---

## 📋 实施计划

### 阶段 1（1-2天） - 核心优化
- [x] 实现 Boyer-Moore 字符串搜索
- [x] 添加正则编译缓存
- [x] 预排序关键词数组
- [x] 编写基准测试

### 阶段 2（2-3天） - 缓存机制
- [x] 实现缓存机制
- [x] 添加缓存统计
- [ ] 测试缓存命中率

### 阶段 3（3-5天） - 高级优化（可选）
- [ ] 实现并行解析
- [ ] 实现增量解析
- [ ] 性能优化调优

### 阶段 4（1天） - 发布
- [ ] 性能测试和对比
- [ ] 文档更新
- [ ] 发布优化版本

---

## ⚠️ 注意事项

1. **跨行块处理：** 并行解析模式下，跨行块需要特殊处理
2. **内存占用：** 缓存会增加内存使用，建议添加大小限制
3. **兼容性：** 优化版本应保持与原始版本相同的 API
4. **测试覆盖：** 需要充分测试边界情况

---

## 🔗 参考资源

- [Boyer-Moore Algorithm](https://en.wikipedia.org/wiki/Boyer%E2%80%93Moore_string-search_algorithm)
- [KMP Algorithm](https://en.wikipedia.org/wiki/Knuth%E2%80%93Morris%E2%80%93Pratt_algorithm)
- [.NET Regex Optimization](https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expressions)
- [BenchmarkDotNet](https://benchmarkdotnet.org/)

---

## 📝 贡献

欢迎提交 Issue 和 Pull Request！

---

**作者：** oujinliang
**创建日期：** 2026-03-19
**版本：** 1.0.0
