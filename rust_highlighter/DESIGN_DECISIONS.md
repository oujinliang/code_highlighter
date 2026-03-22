# Highlight Engine - 设计决策文档

## 1. 技术栈选择

### 1.1 编程语言：Rust

**决策**: 使用 Rust 重写原始的 C# 实现

**理由**:

#### 性能优势
- **零成本抽象**: Rust 的抽象层在编译时被完全优化，运行时无额外开销
- **内存控制**: 无需垃圾回收器，可精确控制内存分配和释放
- **并发安全**: 编译时保证线程安全，避免数据竞争

#### 安全性
- **内存安全**: 所有权系统在编译时防止内存泄漏、悬空指针等问题
- **线程安全**: 无数据竞争的并发编程
- **类型安全**: 强类型系统在编译时捕获许多错误

#### 生态系统
- **现代工具链**: Cargo 包管理器、rustfmt 格式化、clippy 代码检查
- **丰富的库**: crates.io 提供高质量的第三方库
- **跨平台**: 支持多种操作系统和架构

**权衡考虑**:
- **学习曲线**: Rust 的所有权系统对初学者有一定难度
- **编译时间**: 相比 C# 编译时间较长
- **生态成熟度**: 某些领域的库可能不如 C# 丰富

### 1.2 配置格式：TOML

**决策**: 使用 TOML 替代原始的 XML 格式

**理由**:

#### 可读性
```toml
# TOML - 清晰简洁
name = "csharp"
extensions = [".cs", ".csx"]
delimiters = [" ", "\t", "(", ")"]

[[keywords]]
name = "keyword"
keywords = ["abstract", "as", "base"]
```

```xml
<!-- XML - 冗长复杂 -->
<profile>
    <name>csharp</name>
    <extensions>
        <extension>.cs</extension>
        <extension>.csx</extension>
    </extensions>
    <delimiters>
        <delimiter> </delimiter>
        <delimiter>\t</delimiter>
    </delimiters>
</profile>
```

#### Rust 集成
- **serde 生态**: `toml` crate 与 serde 完美集成
- **类型安全**: 自动反序列化为 Rust 结构体
- **错误处理**: 提供详细的解析错误信息

#### 维护性
- **人工编辑**: 更容易手动创建和修改配置文件
- **版本控制**: 更清晰的 diff 显示
- **文档生成**: 更容易生成配置文档

### 1.3 错误处理：thiserror + anyhow

**决策**: 使用 `thiserror` 定义库错误类型，`anyhow` 处理 CLI 错误

**理由**:

#### 库内部错误处理
```rust
#[derive(Error, Debug)]
pub enum HighlightError {
    #[error("IO error: {0}")]
    Io(#[from] io::Error),
    
    #[error("TOML parsing error: {0}")]
    Toml(#[from] toml::de::Error),
    
    #[error("Language not found: {0}")]
    LanguageNotFound(String),
}
```

**优势**:
- **类型安全**: 编译时检查错误处理
- **详细信息**: 提供具体的错误类型和上下文
- **自动转换**: `#[from]` 属性自动实现 `From` trait

#### CLI 错误处理
```rust
fn run(cli: Cli) -> anyhow::Result<()> {
    let profile = profile_manager.get_profile(&language_name)
        .ok_or_else(|| {
            anyhow::anyhow!("Language '{}' not found", language_name)
        })?;
    // ...
}
```

**优势**:
- **上下文丰富**: 可以添加详细的错误上下文
- **链式错误**: 支持错误链的构建和传播
- **简化代码**: 减少样板代码

### 1.4 命令行解析：clap 4.x

**决策**: 使用 clap 4.x 的 derive 宏

**理由**:

#### 类型安全
```rust
#[derive(Parser)]
#[command(name = "highlight")]
struct Cli {
    /// Input file to highlight
    file: PathBuf,
    
    /// Output format (html or terminal)
    #[arg(short, long, default_value = "terminal")]
    format: String,
    
    /// Show line numbers
    #[arg(short = 'n', long)]
    line_numbers: bool,
}
```

**优势**:
- **编译时检查**: 参数类型和约束在编译时验证
- **自动生成**: 自动生成帮助信息和错误消息
- **文档集成**: 结构体文档自动成为帮助文本

#### 现代特性
- **derive 宏**: 声明式定义命令行接口
- **丰富的类型支持**: 支持 PathBuf、Vec、Option 等复杂类型
- **环境变量**: 支持从环境变量读取参数

## 2. 架构设计决策

### 2.0 语言与主题分离设计

**决策**: 严格分离语言语法定义和颜色主题配置

**设计原则**:
- **语言配置** (`languages/*.toml`): 只定义语法结构（关键字、代码块、正则表达式），使用 token 类型名称（如 `"keyword"`, `"comment"`）
- **主题配置** (`themes/*.toml`): 定义 token 类型到颜色的映射，支持多种主题切换
- **关注点分离**: 语法定义与视觉呈现完全解耦

**实现机制**:
```rust
// 语言配置只定义语法结构，不包含颜色
[[keywords]]
name = "keyword"
keywords = ["abstract", "as", "base", "bool", "break"]

// 主题配置定义 token 类型到颜色的映射
[colors]
keyword = "#0000FF"  // Blue
comment = "#008000"  // Green
string = "#8B0000"   // DarkRed
```

**优势**:
- **主题切换**: 同一语言可以使用不同颜色主题
- **一致性**: 所有语言共享相同主题的颜色映射
- **可维护性**: 修改颜色只需更新主题文件，无需修改语言配置
- **扩展性**: 易于创建新的颜色主题

**迁移过程**:
- 从原始 C# 版本的 XML 格式迁移时，将颜色信息从语言配置中分离
- 创建独立的主题系统，支持 light、dark、monokai、solarized 等预设主题
- 确保所有语言配置文件都遵循纯语法定义的原则

### 2.1 模块化设计

**决策**: 采用清晰的模块分离，每个模块负责单一职责

**模块结构**:
```
src/
├── lib.rs          # 库入口，重新导出公共 API
├── error.rs        # 错误类型定义
├── profile.rs      # 语言配置管理
├── parser.rs       # 语法解析引擎
├── renderer.rs     # 输出渲染器
├── theme.rs        # 主题管理
└── interner.rs     # 字符串优化
```

**优势**:
- **单一职责**: 每个模块专注于特定功能
- **可测试性**: 模块可以独立测试
- **可维护性**: 修改影响范围小，易于理解
- **可扩展性**: 新功能可以作为新模块添加

### 2.2 配置驱动设计

**决策**: 使用外部配置文件定义语言语法，而非硬编码

**理由**:
- **灵活性**: 无需重新编译即可添加新语言支持
- **可维护性**: 语法定义与代码分离
- **用户友好**: 用户可以自定义语言配置
- **社区贡献**: 更容易接受社区贡献的语言配置

**实现机制**:
```rust
pub struct ProfileManager {
    profiles: HashMap<String, HighlightProfile>,
    extension_map: HashMap<String, String>,
}

impl ProfileManager {
    pub fn load_profiles_from_dir(&mut self, dir: &Path) -> Result<()> {
        for entry in std::fs::read_dir(dir)? {
            let path = entry?.path();
            if path.extension().map_or(false, |ext| ext == "toml") {
                let profile = HighlightProfile::load(&path)?;
                self.add_profile(profile);
            }
        }
        Ok(())
    }
}
```

### 2.3 解析器状态机设计

**决策**: 使用状态机处理多行语法结构

**状态跟踪**:
```rust
pub struct MultiLineState {
    pub token_type: String,      // 当前块类型（如 "comment"）
    pub end_marker: String,      // 结束标记（如 "*/"）
    pub escape: Option<BlockEscape>, // 转义配置
}
```

**解析流程**:
1. **行开始**: 检查是否有未完成的多行状态
2. **状态处理**: 如果在多行块中，查找结束标记
3. **正常解析**: 如果不在多行块中，按行解析语法元素
4. **状态更新**: 更新多行状态供下一行使用

**优势**:
- **正确性**: 正确处理跨行的语法结构
- **性能**: 避免重复解析已处理的内容
- **可扩展性**: 易于添加新的多行语法支持

### 2.4 内存管理策略

**决策**: 使用引用计数 (`Rc`) 和字符串驻留

**字符串驻留**:
```rust
pub struct StringInterner {
    cache: HashMap<String, Rc<String>>,
}

impl StringInterner {
    pub fn intern(&mut self, s: &str) -> Rc<String> {
        self.cache
            .entry(s.to_string())
            .or_insert_with(|| Rc::new(s.to_string()))
            .clone()
    }
}
```

**使用场景**:
- **Token 类型名称**: 如 "keyword"、"comment" 等重复出现的字符串
- **语言名称**: 在多个地方引用的语言标识符
- **主题颜色**: 在渲染时重复使用的颜色值

**优势**:
- **内存效率**: 避免重复字符串的内存分配
- **比较性能**: `Rc` 指针比较比字符串内容比较更快
- **共享所有权**: 适合在多个地方共享不可变数据

**权衡**:
- **运行时开销**: 引用计数的维护成本
- **循环引用**: 需要避免循环引用导致的内存泄漏
- **单线程**: `Rc` 不是线程安全的，限制了并发使用

## 3. 性能优化决策

### 3.1 快速字符查找

**决策**: 使用布尔数组作为 ASCII 字符查找表

**实现**:
```rust
pub struct HighlightParser {
    // 快速查找表，用于 ASCII 字符 (0-127)
    ascii_delimiter_table: [bool; 128],
}

impl HighlightParser {
    pub fn new(profile: HighlightProfile) -> Self {
        let mut ascii_delimiter_table = [false; 128];
        for &c in &profile.language.delimiters {
            if (c as usize) < 128 {
                ascii_delimiter_table[c as usize] = true;
            }
        }
        // ...
    }
    
    fn is_delimiter(&self, c: char) -> bool {
        if (c as usize) < 128 {
            self.ascii_delimiter_table[c as usize]
        } else {
            self.delimiter_set.contains(&c)
        }
    }
}
```

**优势**:
- **O(1) 查找**: 数组索引查找，常数时间复杂度
- **缓存友好**: 连续内存访问，CPU 缓存命中率高
- **无分支**: 避免条件分支，提高预测准确性

### 3.2 关键字匹配优化

**决策**: 预排序关键字列表，使用二分查找

**实现**:
```rust
pub struct KeywordCollection {
    pub name: String,
    pub keywords: Vec<String>,
    pub sorted_keywords: Vec<String>, // 预排序用于二分查找
}

impl KeywordCollection {
    pub fn contains(&self, word: &str) -> bool {
        self.sorted_keywords.binary_search(&word.to_string()).is_ok()
    }
}
```

**优势**:
- **O(log n) 查找**: 二分查找的时间复杂度
- **内存局部性**: 排序后的数据在内存中连续存储
- **预处理**: 一次性排序成本，多次查询收益

### 3.3 正则表达式预编译

**决策**: 在配置加载时预编译所有正则表达式

**实现**:
```rust
pub struct HighlightProfile {
    pub language: LanguageProfile,
    pub compiled_tokens: Vec<CompiledToken>,
}

pub struct CompiledToken {
    pub name: String,
    pub regex: Regex,
    pub groups: Vec<TokenGroup>,
}

impl HighlightProfile {
    pub fn new(language: LanguageProfile) -> Result<Self> {
        let mut compiled_tokens = Vec::new();
        
        for token_def in &language.tokens {
            let regex = Regex::new(&token_def.pattern)?;
            compiled_tokens.push(CompiledToken {
                name: token_def.name.clone(),
                regex,
                groups: token_def.groups.clone(),
            });
        }
        
        Ok(Self {
            language,
            compiled_tokens,
        })
    }
}
```

**优势**:
- **避免重复编译**: 正则表达式只编译一次
- **错误提前发现**: 配置加载时就能发现正则表达式错误
- **运行时性能**: 解析时直接使用编译好的正则表达式

## 4. 可扩展性设计决策

### 4.1 插件化架构

**决策**: 设计可插拔的组件系统

**扩展点**:
1. **语言配置**: 通过 TOML 文件添加新语言支持
2. **主题系统**: 通过配置文件自定义颜色主题
3. **渲染器**: 可以实现新的输出格式渲染器
4. **解析器**: 可以扩展解析规则和语法支持

**实现机制**:
```rust
// 渲染器 trait，支持扩展新的输出格式
pub trait Renderer {
    fn render(&self, lines: &[TextLineInfo]) -> String;
}

// 主题管理器，支持动态加载主题
pub struct ThemeManager {
    themes: HashMap<String, Theme>,
}

impl ThemeManager {
    pub fn load_themes_from_dir(&mut self, dir: &Path) -> Result<()> {
        // 动态加载目录中的所有主题文件
    }
}
```

### 4.2 配置验证

**决策**: 在配置加载时进行严格验证

**验证内容**:
- **语法正确性**: TOML 语法和结构验证
- **必填字段**: 检查必要的配置项是否存在
- **类型安全**: 确保配置值的类型正确
- **引用完整性**: 验证颜色引用、扩展名唯一性等

**实现**:
```rust
impl LanguageProfile {
    pub fn validate(&self) -> Result<()> {
        // 检查必填字段
        if self.name.is_empty() {
            return Err(HighlightError::InvalidProfile("Language name cannot be empty".to_string()));
        }
        
        // 检查扩展名唯一性
        let mut extensions = HashSet::new();
        for ext in &self.extensions {
            if !extensions.insert(ext) {
                return Err(HighlightError::InvalidProfile(
                    format!("Duplicate extension: {}", ext)
                ));
            }
        }
        
        // 验证正则表达式
        for token in &self.tokens {
            Regex::new(&token.pattern).map_err(|e| {
                HighlightError::InvalidProfile(
                    format!("Invalid regex pattern '{}': {}", token.pattern, e)
                )
            })?;
        }
        
        Ok(())
    }
}
```

### 4.3 向后兼容性

**决策**: 保持 API 稳定性，支持渐进式升级

**策略**:
- **语义版本**: 遵循 SemVer 规范，主版本号变更表示不兼容的 API 变更
- **废弃警告**: 对即将废弃的功能提供编译时警告
- **迁移指南**: 提供详细的版本迁移文档
- **兼容层**: 在必要时提供兼容性适配器

## 5. 测试策略决策

### 5.1 测试金字塔

**决策**: 采用分层的测试策略

```
        /\
       /  \      E2E Tests (少量)
      /    \     验证完整流程
     /------\
    /        \   Integration Tests (适量)
   /          \  验证模块间协作
  /------------\
 /              \ Unit Tests (大量)
/                \ 验证单个函数/方法
```

**单元测试**:
- **覆盖率目标**: 核心模块 > 90%
- **测试范围**: 每个公共函数和方法
- **边界条件**: 空输入、特殊字符、超长行等

**集成测试**:
- **模块协作**: 测试模块间的接口和交互
- **配置加载**: 验证配置文件的加载和解析
- **端到端**: 从输入到输出的完整流程

### 5.2 测试数据管理

**决策**: 使用内联测试数据和外部测试文件结合

**内联测试**:
```rust
#[cfg(test)]
mod tests {
    use super::*;
    
    #[test]
    fn test_parse_simple_keyword() {
        let code = "fn main() {}";
        let parser = create_test_parser();
        let result = parser.parse_line(code, 1, &mut None).unwrap();
        
        assert_eq!(result.segments.len(), 3);
        assert_eq!(result.segments[0].token_type.as_deref(), Some("keyword"));
    }
}
```

**外部测试文件**:
```
tests/
├── fixtures/
│   ├── csharp/
│   │   ├── simple.cs
│   │   ├── comments.cs
│   │   └── strings.cs
│   └── rust/
│       ├── simple.rs
│       └── complex.rs
└── integration/
    ├── parser_tests.rs
    └── renderer_tests.rs
```

### 5.3 性能测试

**决策**: 建立性能基准和回归测试

**基准测试**:
```rust
use criterion::{black_box, criterion_group, criterion_main, Criterion};

fn benchmark_parse_large_file(c: &mut Criterion) {
    let code = include_str!("../tests/fixtures/large_file.rs");
    let parser = create_test_parser();
    
    c.bench_function("parse_large_file", |b| {
        b.iter(|| {
            let lines: Vec<&str> = code.lines().collect();
            parser.parse(black_box(&lines), 1).unwrap()
        })
    });
}

criterion_group!(benches, benchmark_parse_large_file);
criterion_main!(benches);
```

**性能监控**:
- **CI 集成**: 在持续集成中运行性能测试
- **回归检测**: 自动检测性能回归
- **基准报告**: 生成性能趋势报告

## 6. 部署和分发决策

### 6.1 包管理策略

**决策**: 支持多种分发渠道

**库分发**:
- **crates.io**: 主要的 Rust 包注册表
- **语义版本**: 严格遵循 SemVer 规范
- **文档托管**: 使用 docs.rs 自动生成文档

**CLI 工具分发**:
- **二进制发布**: GitHub Releases 提供预编译二进制
- **包管理器**: 支持 Homebrew、apt、yum 等
- **容器化**: Docker 镜像用于云部署

### 6.2 版本管理

**决策**: 使用语义版本控制 (SemVer)

**版本号规则**:
- **主版本号 (X.0.0)**: 不兼容的 API 变更
- **次版本号 (0.X.0)**: 向后兼容的功能性新增
- **修订号 (0.0.X)**: 向后兼容的问题修正

**变更日志**:
```markdown
# Changelog

## [0.2.0] - 2024-01-15
### Added
- 支持 Python 语言配置
- 新增 monokai 主题
- 添加行号显示功能

### Changed
- 优化大文件解析性能
- 改进错误信息的详细程度

### Fixed
- 修复多行字符串解析问题
- 解决内存泄漏问题

## [0.1.0] - 2024-01-01
### Added
- 初始版本发布
- 支持 C#、Rust、JavaScript 语言
- HTML 和终端输出格式
- 基础主题支持
```

## 7. 未来演进决策

### 7.1 技术债务管理

**决策**: 建立技术债务跟踪和偿还机制

**债务分类**:
- **紧急债务**: 影响功能或安全的严重问题
- **重要债务**: 影响性能或可维护性的问题
- **一般债务**: 代码质量改进，非紧急

**偿还策略**:
- **版本规划**: 每个主版本至少偿还一个重要债务
- **重构窗口**: 在功能开发间隙进行重构
- **社区贡献**: 鼓励社区帮助偿还技术债务

### 7.2 演进路径

**短期目标 (0.x 版本)**:
- 完善核心功能和稳定性
- 扩展语言支持范围
- 优化性能和内存使用

**中期目标 (1.0 版本)**:
- API 稳定和标准化
- 完整的文档和示例
- 生产环境就绪

**长期目标 (2.0+ 版本)**:
- 增量解析和实时更新
- 语言服务器协议支持
- WebAssembly 和云原生支持

## 8. 总结

这些设计决策基于以下核心原则:

1. **性能优先**: 选择 Rust 和优化算法确保高性能
2. **安全可靠**: 利用 Rust 的安全特性和严格的错误处理
3. **可扩展性**: 模块化设计和配置驱动架构支持灵活扩展
4. **用户体验**: 简洁的 API 和丰富的配置选项
5. **长期维护**: 清晰的架构和完善的测试支持长期演进

每个决策都经过权衡考虑，在性能、安全性、可维护性和开发效率之间找到平衡点。这些决策为项目的成功奠定了坚实的基础，同时为未来的发展预留了足够的灵活性。