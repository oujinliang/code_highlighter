# Highlight Engine (Rust)

A Rust port of the original C# syntax highlighting engine. This library provides syntax highlighting for source code with support for multiple programming languages.

## Quick Start

```bash
# Build the project
cargo build

# Highlight a C# file
cargo run --bin highlight -- examples/test.cs --languages-dir languages

# Generate HTML output with dark theme
cargo run --bin highlight -- examples/test.cs --format html --output output.html --languages-dir languages --theme dark

# Show line numbers with monokai theme
cargo run --bin highlight -- examples/test.rs --line-numbers --languages-dir languages --theme monokai
```

## Features

- **Multiple Language Support**: Load language definitions from TOML configuration files
- **Flexible Highlighting**: Support for keywords, comments, strings, numbers, and custom regex patterns
- **Multiple Output Formats**: Generate HTML or terminal (ANSI) output
- **Customizable Themes**: Multiple built-in themes (light, dark, monokai, solarized) and custom theme support
- **CLI Tool**: Command-line interface for highlighting files
- **Extensible**: Easy to add new language support and themes

## Installation

### As a Library

Add to your `Cargo.toml`:

```toml
[dependencies]
highlight-engine = "0.1.0"
```

### As a CLI Tool

```bash
cargo install --path .
```

## Usage

### Library Usage

```rust
use highlight_engine::{HighlightParser, HtmlRenderer, ProfileManager};
use std::path::Path;

fn main() -> anyhow::Result<()> {
    // Load language profiles
    let mut profile_manager = ProfileManager::new();
    profile_manager.load_profiles_from_dir(Path::new("languages"))?;
    
    // Get a profile
    let profile = profile_manager.get_profile("rust")
        .ok_or_else(|| anyhow::anyhow!("Rust profile not found"))?;
    
    // Parse code
    let code = r#"
fn main() {
    println!("Hello, World!");
}
"#;
    
    let lines: Vec<&str> = code.lines().collect();
    let parser = HighlightParser::new(profile.clone());
    let parsed_lines = parser.parse(&lines, 1)?;
    
    // Render to HTML
    let renderer = HtmlRenderer::new();
    let html = renderer.render(&parsed_lines);
    
    println!("{}", html);
    
    Ok(())
}
```

### CLI Usage

```bash
# Highlight a file and output to terminal
highlight examples/test.cs --languages-dir languages

# Generate HTML output
highlight examples/test.cs --format html --output output.html --languages-dir languages

# Specify language explicitly
highlight myfile.txt --language rust --languages-dir languages

# Show line numbers
highlight examples/test.cs --line-numbers --languages-dir languages

# Disable colors in terminal output
highlight examples/test.cs --no-colors --languages-dir languages
```

## Language Configuration

Language definitions are stored in TOML files in the `languages/` directory. Each file defines:

- **Name**: Language identifier
- **Extensions**: File extensions associated with this language
- **Delimiters**: Characters that separate tokens
- **Keywords**: Language keywords with colors
- **Code Blocks**: Single-line and multi-line comments, strings
- **Tokens**: Regex-based patterns for numbers, operators, etc.

### Example: C# Configuration

```toml
name = "csharp"
extensions = [".cs", ".csx"]
delimiters = [" ", "\t", "(", ")", "{", "}", "[", "]", ";", ",", ".", "<", ">", "+", "-", "*", "/", "%", "&", "|", "^", "!", "~", "=", "?", ":", "@"]
ignore_case = false

[[keywords]]
name = "keyword"
foreground = "Blue"
keywords = ["abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", ...]

[[single_line_blocks]]
name = "comment"
foreground = "Green"
start = "//"
end = ""

[[single_line_blocks]]
name = "string"
foreground = "DarkRed"
start = "\""
end = "\""
escape = { escape_string = "\\", items = ["\\\"", "\\\\"] }

[[tokens]]
name = "number"
foreground = "Magenta"
pattern = "\\b\\d+\\.?\\d*([eE][+-]?\\d+)?[fFdDmMlLuU]?\\b"
```

## Supported Languages

The following language configurations are included:

- **C#** (`.cs`, `.csx`)
- **Rust** (`.rs`)
- **Python** (`.py`, `.pyw`, `.pyi`)
- **JavaScript** (`.js`, `.jsx`, `.mjs`, `.cjs`)

## Adding New Languages

1. Create a new TOML file in the `languages/` directory (e.g., `go.toml`)
2. Define the language configuration following the format above
3. The library will automatically detect it by file extension

## Color Names

The following named colors are supported:

- `Blue`, `Red`, `Green`, `Gray`/`Grey`
- `DarkRed`, `DarkCyan`, `Magenta`
- `CornflowerBlue`, `Chocolate`
- Hex colors (e.g., `#FF0000`)

## Architecture

The library consists of four main modules:

- **`profile`**: Language profile definitions and loading
- **`parser`**: Core syntax highlighting parser
- **`renderer`**: Output renderers (HTML, Terminal)
- **`error`**: Error types

## Comparison with Original C# Version

### Improvements

1. **Modern Configuration**: Uses TOML instead of XML
2. **Better Error Handling**: Comprehensive error types with `thiserror`
3. **Memory Safety**: Rust's ownership system prevents common bugs
4. **Performance**: Zero-cost abstractions and efficient parsing
5. **CLI Tool**: Built-in command-line interface

### Compatibility

- Supports the same language profile features as the original
- Compatible with existing color definitions
- Similar parsing algorithm and behavior

## License

MIT License