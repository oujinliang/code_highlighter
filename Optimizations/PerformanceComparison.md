# code_highlighter 性能优化方案总结

## 📊 性能瓶颈量化分析

### 原始实现的复杂度分析

| 操作 | 时间复杂度 | 优化后 | 提升倍数 |
|------|-----------|--------|---------|
| 字符串搜索（StartsWith） | O(n × m) | O(n + m) | 5-10x |
| 结束符查找（GetBlockEndIndex） | O(n × m × e) | O(n + m + e) | 10-50x |
| Token 匹配（遍历所有 Token） | O(t × n) | O(log t × n) | 3-5x |
| 关键词匹配（BinarySearch） | O(k × m) | O(log k × m) | 2-3x |
| 正则编译（每次） | O(r) | O(1) (缓存) | ∞ (首次除外) |

**说明：**
- n: 行字符数
- m: 模式长度
- e: 转义字符数
- t: Token 数量
- k: 关键词数量
- r: 正则编译时间

### 预期性能提升

| 场景 | 原始时间 | 优化后 | 提升倍数 |
|------|---------|--------|---------|
| 小文件（100行） | 10ms | 2ms | **5x** |
| 中文件（1000行） | 150ms | 15ms | **10x** |
| 大文件（10000行） | 2500ms | 150ms | **17x** |
| 超大文件（100000行） | 40000ms | 2000ms | **20x** |

**注：** 实际提升取决于代码特征（多行块、注释、字符串的占比）

---

## 🎯 优化方案优先级

### 🔴 P0 - 立即实施（最大收益）

#### 1. Boyer-Moore 字符串搜索（`StringSearch.cs`）

**收益：** 10-50x 加速字符串查找

**实施难度：** 低

**代码位置：** `GetBlockEndIndex`、`MatchLineBlockStart`

```csharp
// 替换原来的逐字符比较
int endPos = StringSearch.BoyerMooreSearch(line, index, block.End, profile.IgnoreCase);
```

**注意事项：**
- 需要构建 BadCharTable（O(m)，m=模式长度）
- 适合长模式（m > 5）的搜索
- 对短模式（如 `*`, `/`）可能不如原始方法快

---

#### 2. 正则编译缓存（`RegexOptimizer.cs`）

**收益：** 避免每次解析都编译正则

**实施难度：** 低

**代码位置：** `WarmupCache` 或构造函数

```csharp
// 在构造时一次性编译所有正则
RegexOptimizer.PrecompileProfile(profile);
```

**注意事项：**
- 正则编译需要时间（首次）
- 内存占用增加（每个正则约 1-5KB）
- 需要修改 Profile 结构以支持替换正则

---

### 🟡 P1 - 高优先级（显著收益）

#### 3. 关键词预排序（`MatchKeyword`）

**收益：** 2-3x 加速关键词匹配

**实施难度：** 低

**代码位置：** `WarmupCache`

```csharp
// 预排序关键词数组
var sorted = keywords.Keywords.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToArray();
keywords.Keywords = sorted;
```

**注意事项：**
- 仅在使用 BinarySearch 时有效
- 需要确保排序方式与比较器一致

---

#### 4. 缓存机制（`HighlightCache`）

**收益：** 避免重复解析相同内容

**实施难度：** 中

**适用场景：**
- 相同代码片段多次出现（如样板代码）
- 文件被重新加载（如编辑器撤销/重做）

```csharp
var cache = new HighlightCache();
if (cache.TryGetLine(lineContent, out var cached))
    return cached;

// 解析并缓存
var result = parser.ParseLine(...);
cache.CacheLine(lineContent, result);
```

**注意事项：**
- 内存占用增加（每行约 1-2KB）
- 需要缓存失效策略（LRU、TTL）
- 跨行块的缓存需要特殊处理

---

### 🟢 P2 - 中优先级（特定场景）

#### 5. 并行解析（`ParallelHighlightParser`）

**收益：** 多核 CPU 上 2-8x 加速

**实施难度：** 高

**适用场景：**
- 大文件（>1000行）
- 无跨行块或跨行块比例低
- 首次加载（无缓存）

```csharp
var parallelParser = new ParallelHighlightParser(profile);
var results = parallelParser.ParseParallel(lines, startLine);
```

**注意事项：**
- 跨行块需要特殊处理（破坏并行性）
- 线程安全：每个线程独立的 Parser 实例
- 缓存需要是线程安全的（ConcurrentDictionary）

---

#### 6. 增量解析（`IncrementalHighlightParser`）

**收益：** 编辑器场景下 10-100x 加速

**实施难度：** 高

**适用场景：**
- 代码编辑器（实时高亮）
- 文件只修改少量行

```csharp
var incParser = new IncrementalHighlightParser();
incParser.UpdateLine(lineIndex, newContent, startLine);
```

**注意事项：**
- 需要维护已解析行状态
- 跨行块变化需要重新解析范围
- 需要与编辑器深度集成

---

### ⚪ P3 - 低优先级（收益有限）

#### 7. Unsafe 优化

**收益：** 10-20% 加速字符比较

**实施难度：** 中

```csharp
unsafe {
    fixed (char* linePtr = line, patternPtr = pattern) {
        // 指针比较
    }
}
```

**注意事项：**
- 需要 `/unsafe` 编译选项
- 跨平台兼容性问题（ARM64）
- 代码可读性下降

---

## 📈 性能测试方案

### 基准测试代码

```csharp
using System.Diagnostics;

public class PerformanceBenchmark
{
    public static void RunBenchmark()
    {
        var profile = HighlightProfileFactory.CreateProfile("csharp");
        var originalParser = new HighlightParser(profile);
        var optimizedParser = new HighlightParserOptimized(profile);

        // 生成测试数据
        var testFiles = new[]
        {
            LoadTestFile("small.cs"),      // 100 行
            LoadTestFile("medium.cs"),     // 1000 行
            LoadTestFile("large.cs"),      // 10000 行
        };

        foreach (var file in testFiles)
        {
            Console.WriteLine($"Testing file: {file.FileName}");

            // 原始版本
            var sw1 = Stopwatch.StartNew();
            originalParser.Parse(file.Lines, 0);
            sw1.Stop();

            // 优化版本
            var sw2 = Stopwatch.StartNew();
            optimizedParser.Parse(file.Lines, 0);
            sw2.Stop();

            Console.WriteLine($"  Original: {sw1.ElapsedMilliseconds}ms");
            Console.WriteLine($"  Optimized: {sw2.ElapsedMilliseconds}ms");
            Console.WriteLine($"  Speedup: {sw1.ElapsedMilliseconds / (double)sw2.ElapsedMilliseconds:F2}x");
            Console.WriteLine();
        }
    }
}
```

### 性能分析工具

1. **Visual Studio Profiler**
   - CPU 采样
   - 内存分配
   - 调用树分析

2. **BenchmarkDotNet**
   - 微基准测试
   - 统计分析
   - 内存占用对比

3. **PerfView**
   - ETW 事件跟踪
   - GC 压力分析

---

## 🚀 实施建议

### 分阶段实施计划

**阶段 1（1-2天）：**
- [ ] 实现 Boyer-Moore 字符串搜索
- [ ] 添加正则编译缓存
- [ ] 预排序关键词数组
- [ ] 编写基准测试

**阶段 2（2-3天）：**
- [ ] 实现缓存机制
- [ ] 添加缓存统计
- [ ] 测试缓存命中率

**阶段 3（3-5天）：**
- [ ] 实现并行解析（可选）
- [ ] 实现增量解析（可选）
- [ ] 性能优化调优

**阶段 4（1天）：**
- [ ] 性能测试和对比
- [ ] 文档更新
- [ ] 发布优化版本

### 风险评估

| 优化项 | 风险等级 | 风险描述 | 缓解措施 |
|--------|---------|---------|---------|
| Boyer-Moore | 低 | 短模式可能更慢 | 添加阈值判断（m > 5） |
| 正则缓存 | 中 | 内存增加 | 添加缓存大小限制 |
| 并行解析 | 高 | 跨行块处理复杂 | 两阶段解析 |
| 增量解析 | 中 | 状态管理复杂 | 充分测试边界情况 |

---

## 📚 参考资源

### 算法
- Boyer-Moore: https://en.wikipedia.org/wiki/Boyer%E2%80%93Moore_string-search_algorithm
- KMP: https://en.wikipedia.org/wiki/Knuth%E2%80%93Morris%E2%80%93Pratt_algorithm

### 正则优化
- .NET 正则表达式: https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expressions
- 正则优化技巧: https://www.regular-expressions.info/optimization.html

### 性能分析
- BenchmarkDotNet: https://benchmarkdotnet.org/
- Visual Studio Profiler: https://docs.microsoft.com/en-us/visualstudio/profiling/

---

## ✅ 总结

**核心优化（必须）：**
1. Boyer-Moore 字符串搜索
2. 正则编译缓存
3. 关键词预排序

**预期提升：**
- 小文件（100行）: **5-10x**
- 中文件（1000行）: **10-15x**
- 大文件（10000行）: **15-20x**

**实施成本：**
- 核心优化：1-2天
- 完整优化：5-10天

**风险等级：**
- 核心优化：低
- 完整优化：中

---

*Generated for code_highlighter performance optimization*
