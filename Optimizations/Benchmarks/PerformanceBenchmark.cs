/* Copyright (C) 2012  Jinliang Ou - 性能基准测试 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Org.Jinou.HighlightEngine.Benchmarks
{
    /// <summary>
    /// 性能基准测试
    /// </summary>
    public class PerformanceBenchmark
    {
        public class BenchmarkResult
        {
            public string FileName { get; set; }
            public int LineCount { get; set; }
            public long OriginalTimeMs { get; set; }
            public long OptimizedTimeMs { get; set; }
            public double Speedup { get; set; }
            public string Category { get; set; }

            public override string ToString()
            {
                return $"{FileName} ({LineCount} lines): {OriginalTimeMs}ms → {OptimizedTimeMs}ms ({Speedup:F2}x)";
            }
        }

        public class BenchmarkReport
        {
            public List<BenchmarkResult> Results { get; set; } = new List<BenchmarkResult>();
            public DateTime TestTime { get; set; }
            public string MachineInfo { get; set; }

            public void Print()
            {
                Console.WriteLine("========================================");
                Console.WriteLine("Code Highlighter Performance Benchmark");
                Console.WriteLine("========================================");
                Console.WriteLine($"Test Time: {TestTime:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"Machine: {MachineInfo}");
                Console.WriteLine($"CPU Cores: {Environment.ProcessorCount}");
                Console.WriteLine();

                foreach (var category in Results.GroupBy(r => r.Category))
                {
                    Console.WriteLine($"--- {category.Key} ---");
                    foreach (var result in category)
                    {
                        Console.WriteLine(result);
                    }
                    Console.WriteLine();
                }

                var avgSpeedup = Results.Average(r => r.Speedup);
                Console.WriteLine($"Average Speedup: {avgSpeedup:F2}x");
                Console.WriteLine();
            }

            public void SaveToFile(string path)
            {
                using (var writer = new StreamWriter(path))
                {
                    writer.WriteLine("Code Highlighter Performance Benchmark Report");
                    writer.WriteLine($"Test Time: {TestTime:yyyy-MM-dd HH:mm:ss}");
                    writer.WriteLine($"Machine: {MachineInfo}");
                    writer.WriteLine($"CPU Cores: {Environment.ProcessorCount}");
                    writer.WriteLine();

                    writer.WriteLine("File Name,Line Count,Original Time (ms),Optimized Time (ms),Speedup,Category");
                    foreach (var result in Results)
                    {
                        writer.WriteLine($"{result.FileName},{result.LineCount},{result.OriginalTimeMs},{result.OptimizedTimeMs},{result.Speedup:F2},{result.Category}");
                    }

                    var avgSpeedup = Results.Average(r => r.Speedup);
                    writer.WriteLine($",,,Avg Speedup,{avgSpeedup:F2}x,");
                }
            }
        }

        /// <summary>
        /// 运行完整基准测试
        /// </summary>
        public static BenchmarkReport RunFullBenchmark()
        {
            var report = new BenchmarkReport
            {
                TestTime = DateTime.Now,
                MachineInfo = Environment.MachineName
            };

            Console.WriteLine("Generating test data...");
            var testData = GenerateTestData();

            Console.WriteLine("Warming up JIT compiler...");
            WarmupJIT();

            Console.WriteLine("\nRunning benchmarks...\n");

            foreach (var data in testData)
            {
                var result = BenchmarkSingleFile(data);
                report.Results.Add(result);
                Console.WriteLine(result);
            }

            return report;
        }

        /// <summary>
        /// 单个文件的基准测试
        /// </summary>
        public static BenchmarkResult BenchmarkSingleFile(TestFileData data)
        {
            var profile = HighlightProfileFactory.CreateProfile(data.Language);
            var originalParser = new HighlightParser(profile);
            var optimizedParser = new HighlightParserOptimized(profile);

            // 原始版本
            var sw1 = Stopwatch.StartNew();
            var originalResult = originalParser.Parse(data.Lines, 0);
            sw1.Stop();

            // 优化版本
            var sw2 = Stopwatch.StartNew();
            var optimizedResult = optimizedParser.Parse(data.Lines, 0);
            sw2.Stop();

            // 验证结果一致性
            VerifyResults(originalResult, optimizedResult, data.FileName);

            return new BenchmarkResult
            {
                FileName = data.FileName,
                LineCount = data.Lines.Length,
                OriginalTimeMs = sw1.ElapsedMilliseconds,
                OptimizedTimeMs = sw2.ElapsedMilliseconds,
                Speedup = sw1.ElapsedMilliseconds / (double)Math.Max(sw2.ElapsedMilliseconds, 1),
                Category = data.Category
            };
        }

        /// <summary>
        /// 生成测试数据
        /// </summary>
        public static List<TestFileData> GenerateTestData()
        {
            var testData = new List<TestFileData>();

            // 1. 小文件（100行）
            testData.Add(new TestFileData
            {
                FileName = "small.cs",
                Category = "Small Files",
                Language = "csharp",
                Lines = GenerateCSharpCode(100)
            });

            testData.Add(new TestFileData
            {
                FileName = "small.java",
                Category = "Small Files",
                Language = "java",
                Lines = GenerateJavaCode(100)
            });

            testData.Add(new TestFileData
            {
                FileName = "small.js",
                Category = "Small Files",
                Language = "javascript",
                Lines = GenerateJavaScriptCode(100)
            });

            // 2. 中文件（1000行）
            testData.Add(new TestFileData
            {
                FileName = "medium.cs",
                Category = "Medium Files",
                Language = "csharp",
                Lines = GenerateCSharpCode(1000)
            });

            testData.Add(new TestFileData
            {
                FileName = "medium.java",
                Category = "Medium Files",
                Language = "java",
                Lines = GenerateJavaCode(1000)
            });

            testData.Add(new TestFileData
            {
                FileName = "medium.js",
                Category = "Medium Files",
                Language = "javascript",
                Lines = GenerateJavaScriptCode(1000)
            });

            // 3. 大文件（10000行）
            testData.Add(new TestFileData
            {
                FileName = "large.cs",
                Category = "Large Files",
                Language = "csharp",
                Lines = GenerateCSharpCode(10000)
            });

            testData.Add(new TestFileData
            {
                FileName = "large.java",
                Category = "Large Files",
                Language = "java",
                Lines = GenerateJavaCode(10000)
            });

            // 4. 特殊场景：大量注释
            testData.Add(new TestFileData
            {
                FileName = "heavy-comments.cs",
                Category = "Special Cases",
                Language = "csharp",
                Lines = GenerateHeavyCommentsCode(1000)
            });

            // 5. 特殊场景：大量字符串
            testData.Add(new TestFileData
            {
                FileName = "heavy-strings.cs",
                Category = "Special Cases",
                Language = "csharp",
                Lines = GenerateHeavyStringsCode(1000)
            });

            return testData;
        }

        /// <summary>
        /// 预热 JIT 编译器
        /// </summary>
        private static void WarmupJIT()
        {
            var profile = HighlightProfileFactory.CreateProfile("csharp");
            var parser = new HighlightParser(profile);
            var optimizedParser = new HighlightParserOptimized(profile);

            var lines = GenerateCSharpCode(10);
            parser.Parse(lines, 0);
            optimizedParser.Parse(lines, 0);
        }

        /// <summary>
        /// 验证结果一致性
        /// </summary>
        private static void VerifyResults(TextLineInfo[] original, TextLineInfo[] optimized, string fileName)
        {
            if (original.Length != optimized.Length)
            {
                Console.WriteLine($"  ⚠️  Warning: Line count mismatch in {fileName}");
                return;
            }

            for (int i = 0; i < original.Length; i++)
            {
                if (original[i].Segments.Count != optimized[i].Segments.Count)
                {
                    Console.WriteLine($"  ⚠️  Warning: Segment count mismatch at line {i} in {fileName}");
                }
            }
        }

        #region 测试数据生成器

        private static string[] GenerateCSharpCode(int lines)
        {
            var code = new List<string>();

            // 文件头
            code.Add("using System;");
            code.Add("using System.Collections.Generic;");
            code.Add("using System.Linq;");
            code.Add("using System.Text;");
            code.Add("");
            code.Add("namespace TestProject");
            code.Add("{");
            code.Add("    /// <summary>");
            code.Add("    /// 测试类");
            code.Add("    /// </summary>");
            code.Add("    public class TestClass");
            code.Add("    {");

            // 生成代码行
            for (int i = 0; i < lines - 20; i++)
            {
                if (i % 10 == 0)
                {
                    code.Add("        // Single line comment");
                }
                else if (i % 50 == 0)
                {
                    code.Add("        /* Multi-line");
                    code.Add("         * comment");
                    code.Add("         */");
                }
                else
                {
                    var lineType = i % 7;
                    switch (lineType)
                    {
                        case 0:
                            code.Add($"        private int _field{i} = {i};");
                            break;
                        case 1:
                            code.Add($"        public string Property{i} {{ get; set; }}");
                            break;
                        case 2:
                            code.Add($"        public void Method{i}()");
                            code.Add("        {");
                            code.Add("            int x = 0;");
                            code.Add("            string s = \"test string\";");
                            code.Add("            var list = new List<int>();");
                            code.Add("            list.Add(x);");
                            code.Add("            return;");
                            code.Add("        }");
                            break;
                        case 3:
                            code.Add($"        public int Method{i}(int a, int b)");
                            code.Add("        {");
                            code.Add("            return a + b;");
                            code.Add("        }");
                            break;
                        case 4:
                            code.Add($"        private class InnerClass{i}");
                            code.Add("        {");
                            code.Add("            public int Value;");
                            code.Add("        }");
                            break;
                        case 5:
                            code.Add($"        public enum Enum{i}");
                            code.Add("        {");
                            code.Add("            Value1,");
                            code.Add("            Value2,");
                            code.Add("            Value3");
                            code.Add("        }");
                            break;
                        case 6:
                            code.Add($"        public string StringProperty {{ get => \"string\"; set => _value = value; }}");
                            break;
                    }
                }
            }

            code.Add("    }");
            code.Add("}");

            return code.ToArray();
        }

        private static string[] GenerateJavaCode(int lines)
        {
            var code = new List<string>();

            code.Add("package com.test;");
            code.Add("");
            code.Add("import java.util.List;");
            code.Add("import java.util.ArrayList;");
            code.Add("");
            code.Add("/**");
            code.Add(" * Test class");
            code.Add(" */");
            code.Add("public class TestClass {");
            code.Add("    private int value;");
            code.Add("");

            for (int i = 0; i < lines - 15; i++)
            {
                if (i % 10 == 0)
                {
                    code.Add("    // Single line comment");
                }
                else if (i % 5 == 0)
                {
                    code.Add($"    public void method{i}() {{");
                    code.Add("        int x = 0;");
                    code.Add("        String s = \"test\";");
                    code.Add("        List<Integer> list = new ArrayList<>();");
                    code.Add("    }");
                }
                else
                {
                    code.Add($"    private int field{i} = {i};");
                }
            }

            code.Add("}");

            return code.ToArray();
        }

        private static string[] GenerateJavaScriptCode(int lines)
        {
            var code = new List<string>();

            code.Add("// JavaScript test file");
            code.Add("");
            code.Add("class TestClass {");
            code.Add("    constructor() {");
            code.Add("        this.value = 0;");
            code.Add("    }");
            code.Add("");

            for (int i = 0; i < lines - 15; i++)
            {
                if (i % 10 == 0)
                {
                    code.Add("    // Single line comment");
                }
                else if (i % 5 == 0)
                {
                    code.Add($"    method{i}() {{");
                    code.Add("        let x = 0;");
                    code.Add("        const s = 'test';");
                    code.Add("        return x;");
                    code.Add("    }");
                }
                else
                {
                    code.Add($"    field{i} = {i};");
                }
            }

            code.Add("}");

            return code.ToArray();
        }

        private static string[] GenerateHeavyCommentsCode(int lines)
        {
            var code = new List<string>();

            code.Add("using System;");
            code.Add("");
            code.Add("// This file contains many comments");
            code.Add("");

            for (int i = 0; i < lines; i++)
            {
                if (i % 2 == 0)
                {
                    code.Add($"        // Comment line {i} with some explanation");
                }
                else if (i % 10 == 0)
                {
                    code.Add("        /*");
                    code.Add($"         * Multi-line comment {i}");
                    code.Add("         * with multiple lines");
                    code.Add("         */");
                }
                else
                {
                    code.Add($"        int field{i} = {i};");
                }
            }

            return code.ToArray();
        }

        private static string[] GenerateHeavyStringsCode(int lines)
        {
            var code = new List<string>();

            code.Add("using System;");
            code.Add("");

            for (int i = 0; i < lines; i++)
            {
                if (i % 2 == 0)
                {
                    code.Add($"        string str{i} = \"This is a string with some content {i}\";");
                }
                else if (i % 10 == 0)
                {
                    code.Add("        string multiLine = @\"");
                    code.Add("            Multi-line");
                    code.Add($"            string {i}");
                    code.Add("        \";");
                }
                else
                {
                    code.Add($"        int field{i} = {i};");
                }
            }

            return code.ToArray();
        }

        #endregion

        #region 测试数据类

        public class TestFileData
        {
            public string FileName { get; set; }
            public string Category { get; set; }
            public string Language { get; set; }
            public string[] Lines { get; set; }
        }

        #endregion

        #region 主程序

        public static void Main(string[] args)
        {
            Console.WriteLine("Code Highlighter Performance Benchmark");
            Console.WriteLine("======================================\n");

            try
            {
                // 运行基准测试
                var report = RunFullBenchmark();

                // 打印报告
                Console.WriteLine();
                report.Print();

                // 保存报告
                string reportPath = $"benchmark_report_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                report.SaveToFile(reportPath);
                Console.WriteLine($"Report saved to: {reportPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        #endregion
    }
}
