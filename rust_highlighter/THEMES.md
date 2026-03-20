# Theme System Documentation

## 🎨 Overview

The highlight engine now supports customizable color themes! Themes allow you to change the visual appearance of syntax highlighting without modifying language profiles.

## 📁 Theme Structure

### Theme Files

Themes are stored as TOML files in the `themes/` directory:

```
themes/
├── light.toml          # Default light theme
├── dark.toml           # Default dark theme
├── monokai.toml        # Monokai theme
├── solarized-dark.toml # Solarized Dark theme
└── solarized-light.toml # Solarized Light theme
```

### Theme File Format

```toml
name = "theme-name"
description = "Theme description"

[colors]
background = "#FFFFFF"
foreground = "#000000"
line_number = "#888888"
selection = "#ADD6FF"
cursor = "#000000"

[token_colors]
keyword = "#0000FF"
type = "#008080"
string = "#8B0000"
comment = "#008000"
number = "#FF00FF"
preprocessor = "#808080"
operator = "#000000"
punctuation = "#000000"
attribute = "#6495ED"
decorator = "#D2691E"
default = "#000000"
```

## 🚀 Usage

### Command Line

```bash
# Use built-in theme
highlight examples/test.cs --theme dark

# Use custom theme file
highlight examples/test.cs --theme /path/to/custom-theme.toml

# Specify themes directory
highlight examples/test.cs --theme mytheme --themes-dir /path/to/themes
```

### Library Usage

```rust
use highlight_engine::{ThemeManager, Theme, HtmlRenderer};

// Load themes
let mut theme_manager = ThemeManager::new();
theme_manager.load_themes_from_dir(Path::new("themes"))?;

// Get a theme
let theme = theme_manager.get_theme("monokai").unwrap();

// Use with renderer
let renderer = HtmlRenderer::with_theme(theme.clone());
let html = renderer.render(&parsed_lines);
```

## 🎨 Built-in Themes

### Light Theme
- **Background:** White (#FFFFFF)
- **Foreground:** Black (#000000)
- **Keywords:** Blue (#0000FF)
- **Comments:** Green (#008000)
- **Strings:** Dark Red (#8B0000)

### Dark Theme
- **Background:** Dark Gray (#1E1E1E)
- **Foreground:** Light Gray (#D4D4D4)
- **Keywords:** Blue (#569CD6)
- **Comments:** Green (#6A9955)
- **Strings:** Orange (#CE9178)

### Monokai Theme
- **Background:** Dark (#272822)
- **Foreground:** Light (#F8F8F2)
- **Keywords:** Pink (#F92672)
- **Comments:** Gray (#75715E)
- **Strings:** Yellow (#E6DB74)

### Solarized Dark Theme
- **Background:** Dark Blue (#002B36)
- **Foreground:** Light Gray (#839496)
- **Keywords:** Green (#859900)
- **Comments:** Gray (#586E75)
- **Strings:** Cyan (#2AA198)

### Solarized Light Theme
- **Background:** Light (#FDF6E3)
- **Foreground:** Dark Gray (#657B83)
- **Keywords:** Green (#859900)
- **Comments:** Light Gray (#93A1A1)
- **Strings:** Cyan (#2AA198)

## 🛠️ Creating Custom Themes

### Step 1: Create Theme File

Create a new TOML file (e.g., `my-theme.toml`):

```toml
name = "my-theme"
description = "My custom theme"

[colors]
background = "#1a1a1a"
foreground = "#f0f0f0"
line_number = "#666666"
selection = "#333333"
cursor = "#f0f0f0"

[token_colors]
keyword = "#ff6b6b"
type = "#4ecdc4"
string = "#ffe66d"
comment = "#999999"
number = "#a29bfe"
preprocessor = "#fd79a8"
operator = "#f0f0f0"
punctuation = "#f0f0f0"
attribute = "#74b9ff"
default = "#f0f0f0"
```

### Step 2: Use Your Theme

```bash
# Use custom theme file directly
highlight examples/test.cs --theme my-theme.toml

# Or place in themes directory
cp my-theme.toml themes/
highlight examples/test.cs --theme my-theme
```

## 📊 Token Types

The following token types are commonly used:

| Token Type | Description | Example |
|------------|-------------|---------|
| `keyword` | Language keywords | `if`, `else`, `for`, `while` |
| `type` | Type names | `int`, `string`, `MyClass` |
| `string` | String literals | `"hello"`, `'world'` |
| `comment` | Comments | `// comment`, `/* block */` |
| `number` | Numeric literals | `42`, `3.14`, `0xFF` |
| `preprocessor` | Preprocessor directives | `#include`, `#define` |
| `operator` | Operators | `+`, `-`, `*`, `/` |
| `punctuation` | Punctuation | `(`, `)`, `{`, `}` |
| `attribute` | Attributes/Decorators | `[Serializable]`, `@decorator` |
| `default` | Default text | Unrecognized text |

## 🎯 Advanced Features

### Theme Inheritance (Future)

```toml
name = "my-dark-theme"
base = "dark"

[token_colors]
# Override specific colors
keyword = "#ff0000"
```

### Dynamic Theme Loading

```rust
use highlight_engine::Theme;

// Load from URL (future)
let theme = Theme::load_from_url("https://example.com/theme.toml").await?;

// Load from string
let theme_str = std::fs::read_to_string("theme.toml")?;
let theme: Theme = toml::from_str(&theme_str)?;
```

### Theme Validation

```rust
use highlight_engine::Theme;

let theme = Theme::load(Path::new("theme.toml"))?;

// Validate required fields
if theme.token_colors.is_empty() {
    eprintln!("Warning: No token colors defined");
}

// Check for missing token types
let required = ["keyword", "type", "string", "comment"];
for token_type in &required {
    if !theme.token_colors.contains_key(*token_type) {
        eprintln!("Warning: Missing token type: {}", token_type);
    }
}
```

## 🔧 Troubleshooting

### Theme Not Found

```bash
# Check available themes
ls themes/

# Use built-in theme
highlight examples/test.cs --theme light

# Use absolute path
highlight examples/test.cs --theme /full/path/to/theme.toml
```

### Colors Not Showing

1. Check terminal color support
2. Verify theme file syntax
3. Ensure token types match language profile

### Custom Token Types

If your language profile uses custom token types, add them to your theme:

```toml
[token_colors]
# Standard types
keyword = "#ff0000"
type = "#00ff00"

# Custom types from your language profile
my_custom_type = "#0000ff"
another_type = "#ffff00"
```

## 📚 Examples

### Example 1: High Contrast Theme

```toml
name = "high-contrast"
description = "High contrast theme for accessibility"

[colors]
background = "#000000"
foreground = "#FFFFFF"
line_number = "#888888"
selection = "#444444"
cursor = "#FFFFFF"

[token_colors]
keyword = "#FF0000"
type = "#00FF00"
string = "#FFFF00"
comment = "#888888"
number = "#FF00FF"
preprocessor = "#00FFFF"
operator = "#FFFFFF"
punctuation = "#FFFFFF"
default = "#FFFFFF"
```

### Example 2: Pastel Theme

```toml
name = "pastel"
description = "Soft pastel colors"

[colors]
background = "#f5f5f5"
foreground = "#333333"
line_number = "#999999"
selection = "#e0e0e0"
cursor = "#333333"

[token_colors]
keyword = "#e74c3c"
type = "#3498db"
string = "#2ecc71"
comment = "#95a5a6"
number = "#9b59b6"
preprocessor = "#e67e22"
operator = "#333333"
punctuation = "#333333"
default = "#333333"
```

## 🎉 Conclusion

The theme system provides flexible, customizable syntax highlighting. Create your own themes to match your preferred color scheme or accessibility needs!

For more examples, see the `themes/` directory.