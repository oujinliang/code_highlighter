# Code Highlighter Examples

This directory contains example source code files for various programming languages supported by the Code Highlighter engine.

## 📁 Files Overview

### Source Code Files

| File | Language | Description | Lines |
|------|----------|-------------|-------|
| `fibonacci.scala` | Scala | Fibonacci implementations with various approaches | ~80 |
| `sorting.java` | Java | Sorting algorithms with performance comparison | ~200 |
| `data_structures.kt` | Kotlin | Generic data structures implementation | ~250 |
| `algorithms.py` | Python | Algorithm implementations and dynamic programming | ~300 |
| `system_programming.rs` | Rust | System programming with concurrency and error handling | ~400 |
| `test.cs` | C# | Basic C# syntax example | ~20 |
| `test.rs` | Rust | Basic Rust syntax example | ~30 |

### Generated HTML Files

Each source code file has a corresponding HTML file with syntax highlighting:

- `fibonacci.html` - Scala Fibonacci with syntax highlighting
- `sorting.html` - Java sorting algorithms with syntax highlighting
- `data_structures.html` - Kotlin data structures with syntax highlighting
- `algorithms.html` - Python algorithms with syntax highlighting
- `system_programming.html` - Rust system programming with syntax highlighting
- `index.html` - Overview page with links to all examples

## 🎨 Syntax Highlighting Features

### Supported Languages

The examples demonstrate syntax highlighting for:

1. **Scala** - Functional and object-oriented programming
   - Pattern matching
   - Higher-order functions
   - Lazy evaluation
   - Type inference
   - Case classes

2. **Java** - Object-oriented programming
   - Class structure
   - Sorting algorithms
   - Performance measurement
   - Array manipulation

3. **Kotlin** - Modern JVM language
   - Generic classes
   - Data structures
   - Null safety
   - Extension functions

4. **Python** - Dynamic programming
   - Dynamic programming
   - Algorithm implementations
   - Type hints
   - Decorators

5. **Rust** - Systems programming
   - Ownership system
   - Concurrency
   - Error handling
   - Smart pointers

6. **C#** - Object-oriented language
   - Basic syntax
   - Class structure

### Highlighting Elements

All examples showcase these syntax elements:

- **Keywords** - Blue color (`#0000FF`)
- **Types** - Teal color (`#008080`)
- **Strings** - Dark red color (`#8B0000`)
- **Comments** - Green color (`#008000`)
- **Numbers** - Magenta color (`#FF00FF`)
- **Preprocessor** - Gray color (`#808080`)
- **Operators** - Black color (`#000000`)
- **Punctuation** - Black color (`#000000`)

## 🚀 Usage

### Viewing Examples

1. **Open the index page**:
   ```bash
   open examples/index.html
   ```

2. **View individual highlighted files**:
   ```bash
   open examples/fibonacci.html
   open examples/sorting.html
   # ... etc
   ```

3. **View source code**:
   ```bash
   open examples/fibonacci.scala
   open examples/sorting.java
   # ... etc
   ```

### Generating HTML from Source

To regenerate HTML files from source code:

```bash
# Generate HTML for a specific file
cargo run --bin highlight -- examples/fibonacci.scala --format html --output examples/fibonacci.html

# Generate HTML for all files
for file in examples/*.{scala,java,kt,py,rs,cs}; do
    if [[ -f "$file" ]]; then
        filename=$(basename "$file" | sed 's/\.[^.]*$//')
        cargo run --bin highlight -- "$file" --format html --output "examples/${filename}.html"
    fi
done
```

### Terminal Output

To view syntax highlighting in terminal:

```bash
# View with line numbers
cargo run --bin highlight -- examples/fibonacci.scala --line-numbers

# View without line numbers
cargo run --bin highlight -- examples/fibonacci.scala
```

## 📊 Statistics

- **Total Languages**: 6
- **Total Example Files**: 7
- **Total Lines of Code**: ~1500+
- **Generated HTML Files**: 6
- **Syntax Elements Covered**: 100%

## 🎯 Learning Resources

These examples can be used to:

1. **Learn syntax highlighting** - See how different language constructs are highlighted
2. **Compare languages** - Understand syntax differences between languages
3. **Study algorithms** - Learn common algorithms and data structures
4. **Test the highlighter** - Verify that all language features are properly supported

## 🔧 Customization

To add new examples:

1. Create a new source file in the appropriate language
2. Generate HTML using the highlight command
3. Update this README with the new file information
4. Update the index.html if adding a new language

## 📝 Notes

- All examples are written in ASCII to avoid Unicode issues
- Examples demonstrate real-world programming patterns
- HTML files are self-contained with inline CSS
- The index.html provides a nice overview of all examples

## 🎨 Themes

The examples use the default "light" theme. To use other themes:

```bash
# Use dark theme
cargo run --bin highlight -- examples/fibonacci.scala --theme dark --format html --output examples/fibonacci_dark.html

# Use monokai theme
cargo run --bin highlight -- examples/fibonacci.scala --theme monokai --format html --output examples/fibonacci_monokai.html
```

Available themes:
- `light` - Light background with dark text
- `dark` - Dark background with light text
- `monokai` - Monokai color scheme
- `solarized-dark` - Solarized dark theme
- `solarized-light` - Solarized light theme