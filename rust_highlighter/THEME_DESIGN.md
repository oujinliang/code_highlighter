# Theme System Design

## 🎯 Goals

1. **Separate concerns**: Decouple color/style definitions from language profiles
2. **Multiple themes**: Support different visual styles (light, dark, monokai, etc.)
3. **Easy customization**: Users can create custom themes
4. **Backward compatibility**: Existing code should work with minimal changes

## 📐 Architecture

### Current State
```
LanguageProfile
├── keywords (with colors)
├── blocks (with colors)
└── tokens (with colors)
```

### New Design
```
LanguageProfile
├── keywords (with token types)
├── blocks (with token types)
└── tokens (with token types)

Theme
├── token_colors (mapping token types to colors)
├── background_color
├── foreground_color
└── font_settings
```

## 🎨 Theme Structure

### Theme File Format (TOML)
```toml
name = "monokai"
description = "Monokai color theme"

[colors]
background = "#272822"
foreground = "#F8F8F2"
line_number = "#75715E"
selection = "#49483E"

[token_colors]
keyword = "#F92672"      # Pink
type = "#66D9EF"         # Cyan
string = "#E6DB74"       # Yellow
comment = "#75715E"      # Gray
number = "#AE81FF"       # Purple
preprocessor = "#A6E22E" # Green
operator = "#F92672"     # Pink
punctuation = "#F8F8F2"  # White
default = "#F8F8F2"      # White
```

### Language Profile Format (Updated)
```toml
name = "csharp"
extensions = [".cs", ".csx"]

[[keywords]]
name = "keyword"  # Token type (not color)
keywords = ["abstract", "as", "base", ...]

[[keywords]]
name = "type"
keywords = ["Boolean", "Byte", "Char", ...]

[[single_line_blocks]]
name = "comment"
start = "//"
end = ""

[[tokens]]
name = "number"
pattern = "\\b\\d+\\.?\\d*([eE][+-]?\\d+)?[fFdDmMlLuU]?\\b"
```

## 🔧 Implementation Plan

### 1. Create Theme Module
- `src/theme.rs` - Theme data structures and loading
- `themes/` directory - Built-in theme files

### 2. Update Profile Module
- Remove color information from language profiles
- Add token type names instead

### 3. Update Parser
- Store token types instead of colors
- Apply theme during rendering

### 4. Update Renderers
- Accept theme as parameter
- Look up colors from theme

### 5. Update CLI
- Add `--theme` option
- Load theme from file or use default

## 📊 Data Flow

```
Language Profile → Parser → Token Types → Theme Lookup → Colors → Renderer
```

## 🎯 Benefits

1. **Separation of Concerns**: Language and presentation are separate
2. **Reusability**: One theme works with all languages
3. **Customization**: Easy to create new themes
4. **Consistency**: Same colors across all languages
5. **Flexibility**: Switch themes without changing language configs

## 📝 Example Usage

```rust
// Load language profile
let mut profile_manager = ProfileManager::new();
profile_manager.load_profiles_from_dir(Path::new("languages"))?;

// Load theme
let theme = Theme::load(Path::new("themes/monokai.toml"))?;

// Parse with language profile
let profile = profile_manager.get_profile("rust").unwrap();
let mut parser = HighlightParser::new(profile.clone());
let parsed = parser.parse(&lines, 1)?;

// Render with theme
let renderer = HtmlRenderer::with_theme(theme);
let html = renderer.render(&parsed);
```

## 🚀 Future Extensions

1. **Theme inheritance**: Base themes with overrides
2. **Dynamic themes**: Load from URLs or APIs
3. **Theme editor**: GUI for creating themes
4. **Theme marketplace**: Share themes online
