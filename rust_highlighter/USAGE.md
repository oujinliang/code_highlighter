# Quick Usage Guide

## Installation

```bash
cd rust_highlighter
cargo build --release
```

## Basic Usage

### Highlight a file to terminal

```bash
cargo run --bin highlight -- examples/test.cs --languages-dir languages
```

### Generate HTML output

```bash
cargo run --bin highlight -- examples/test.cs --format html --output output.html --languages-dir languages
```

### Show line numbers

```bash
cargo run --bin highlight -- examples/test.rs --line-numbers --languages-dir languages
```

### Specify language explicitly

```bash
cargo run --bin highlight -- myfile.txt --language rust --languages-dir languages
```

### Disable colors

```bash
cargo run --bin highlight -- examples/test.cs --no-colors --languages-dir languages
```

## Supported Languages

- **C#** (.cs, .csx)
- **Rust** (.rs)
- **Python** (.py, .pyw, .pyi)
- **JavaScript** (.js, .jsx, .mjs, .cjs)

## Adding New Languages

1. Create a new TOML file in `languages/` directory
2. Follow the format of existing files (e.g., `csharp.toml`)
3. The language will be automatically detected by file extension

## Examples

```bash
# Highlight C# code
cargo run --bin highlight -- examples/test.cs --format html --output cs_output.html --languages-dir languages

# Highlight Rust code with line numbers
cargo run --bin highlight -- examples/test.rs --line-numbers --languages-dir languages

# Highlight Python code
cargo run --bin highlight -- examples/test.py --format terminal --languages-dir languages
```

## Library Usage

```rust
use highlight_engine::{HighlightParser, HtmlRenderer, ProfileManager};
use std::path::Path;

fn main() -> anyhow::Result<()> {
    let mut profile_manager = ProfileManager::new();
    profile_manager.load_profiles_from_dir(Path::new("languages"))?;
    
    let profile = profile_manager.get_profile("rust").unwrap();
    let parser = HighlightParser::new(profile.clone());
    
    let code = "fn main() { println!(\"Hello\"); }";
    let lines: Vec<&str> = code.lines().collect();
    let parsed = parser.parse(&lines, 1)?;
    
    let renderer = HtmlRenderer::new();
    let html = renderer.render(&parsed);
    
    println!("{}", html);
    Ok(())
}
```