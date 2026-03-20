//! Theme definitions for syntax highlighting.

use serde::Deserialize;
use std::collections::HashMap;
use std::path::Path;
use crate::error::Result;

/// Color representation (RGB hex format like "#FF0000" for red).
pub type Color = String;

/// A complete color theme for syntax highlighting.
#[derive(Debug, Clone, Deserialize)]
pub struct Theme {
    /// Theme name (e.g., "monokai", "solarized-dark").
    pub name: String,
    
    /// Optional description.
    #[serde(default)]
    pub description: String,
    
    /// General colors.
    #[serde(default)]
    pub colors: ThemeColors,
    
    /// Token type to color mapping.
    #[serde(default)]
    pub token_colors: HashMap<String, Color>,
}

/// General theme colors.
#[derive(Debug, Clone, Deserialize)]
pub struct ThemeColors {
    /// Background color.
    #[serde(default = "default_background")]
    pub background: Color,
    
    /// Default foreground (text) color.
    #[serde(default = "default_foreground")]
    pub foreground: Color,
    
    /// Line number color.
    #[serde(default = "default_line_number")]
    pub line_number: Color,
    
    /// Selection highlight color.
    #[serde(default = "default_selection")]
    pub selection: Color,
    
    /// Cursor color.
    #[serde(default = "default_cursor")]
    pub cursor: Color,
}

fn default_background() -> Color {
    "#FFFFFF".to_string()
}

fn default_foreground() -> Color {
    "#000000".to_string()
}

fn default_line_number() -> Color {
    "#888888".to_string()
}

fn default_selection() -> Color {
    "#ADD6FF".to_string()
}

fn default_cursor() -> Color {
    "#000000".to_string()
}

impl Default for ThemeColors {
    fn default() -> Self {
        Self {
            background: default_background(),
            foreground: default_foreground(),
            line_number: default_line_number(),
            selection: default_selection(),
            cursor: default_cursor(),
        }
    }
}

impl Theme {
    /// Load a theme from a TOML file.
    pub fn load(path: &Path) -> Result<Self> {
        let content = std::fs::read_to_string(path)?;
        let theme: Theme = toml::from_str(&content)?;
        Ok(theme)
    }
    
    /// Get color for a token type.
    pub fn get_color(&self, token_type: &str) -> Option<&Color> {
        self.token_colors.get(token_type)
    }
    
    /// Get color for a token type with fallback to default.
    pub fn get_color_or_default(&self, token_type: &str) -> &Color {
        self.token_colors
            .get(token_type)
            .or_else(|| self.token_colors.get("default"))
            .unwrap_or(&self.colors.foreground)
    }
    
    /// Create a default light theme.
    pub fn light() -> Self {
        let mut token_colors = HashMap::new();
        token_colors.insert("keyword".to_string(), "#0000FF".to_string());
        token_colors.insert("type".to_string(), "#008080".to_string());
        token_colors.insert("string".to_string(), "#8B0000".to_string());
        token_colors.insert("comment".to_string(), "#008000".to_string());
        token_colors.insert("number".to_string(), "#FF00FF".to_string());
        token_colors.insert("preprocessor".to_string(), "#808080".to_string());
        token_colors.insert("operator".to_string(), "#000000".to_string());
        token_colors.insert("punctuation".to_string(), "#000000".to_string());
        token_colors.insert("default".to_string(), "#000000".to_string());
        
        Self {
            name: "light".to_string(),
            description: "Default light theme".to_string(),
            colors: ThemeColors::default(),
            token_colors,
        }
    }
    
    /// Create a default dark theme.
    pub fn dark() -> Self {
        let mut token_colors = HashMap::new();
        token_colors.insert("keyword".to_string(), "#569CD6".to_string());
        token_colors.insert("type".to_string(), "#4EC9B0".to_string());
        token_colors.insert("string".to_string(), "#CE9178".to_string());
        token_colors.insert("comment".to_string(), "#6A9955".to_string());
        token_colors.insert("number".to_string(), "#B5CEA8".to_string());
        token_colors.insert("preprocessor".to_string(), "#C586C0".to_string());
        token_colors.insert("operator".to_string(), "#D4D4D4".to_string());
        token_colors.insert("punctuation".to_string(), "#D4D4D4".to_string());
        token_colors.insert("default".to_string(), "#D4D4D4".to_string());
        
        Self {
            name: "dark".to_string(),
            description: "Default dark theme".to_string(),
            colors: ThemeColors {
                background: "#1E1E1E".to_string(),
                foreground: "#D4D4D4".to_string(),
                line_number: "#858585".to_string(),
                selection: "#264F78".to_string(),
                cursor: "#D4D4D4".to_string(),
            },
            token_colors,
        }
    }
    
    /// Create a Monokai theme.
    pub fn monokai() -> Self {
        let mut token_colors = HashMap::new();
        token_colors.insert("keyword".to_string(), "#F92672".to_string());
        token_colors.insert("type".to_string(), "#66D9EF".to_string());
        token_colors.insert("string".to_string(), "#E6DB74".to_string());
        token_colors.insert("comment".to_string(), "#75715E".to_string());
        token_colors.insert("number".to_string(), "#AE81FF".to_string());
        token_colors.insert("preprocessor".to_string(), "#A6E22E".to_string());
        token_colors.insert("operator".to_string(), "#F92672".to_string());
        token_colors.insert("punctuation".to_string(), "#F8F8F2".to_string());
        token_colors.insert("default".to_string(), "#F8F8F2".to_string());
        
        Self {
            name: "monokai".to_string(),
            description: "Monokai color theme".to_string(),
            colors: ThemeColors {
                background: "#272822".to_string(),
                foreground: "#F8F8F2".to_string(),
                line_number: "#75715E".to_string(),
                selection: "#49483E".to_string(),
                cursor: "#F8F8F0".to_string(),
            },
            token_colors,
        }
    }
}

/// Manager for loading and caching themes.
pub struct ThemeManager {
    themes: HashMap<String, Theme>,
}

impl ThemeManager {
    /// Create a new theme manager.
    pub fn new() -> Self {
        let mut themes = HashMap::new();
        
        // Add built-in themes
        themes.insert("light".to_string(), Theme::light());
        themes.insert("dark".to_string(), Theme::dark());
        themes.insert("monokai".to_string(), Theme::monokai());
        
        Self { themes }
    }
    
    /// Load a theme from a TOML file.
    pub fn load_theme(&mut self, path: &Path) -> Result<()> {
        let theme = Theme::load(path)?;
        self.themes.insert(theme.name.clone(), theme);
        Ok(())
    }
    
    /// Load all themes from a directory.
    pub fn load_themes_from_dir(&mut self, dir: &Path) -> Result<()> {
        for entry in std::fs::read_dir(dir)? {
            let entry = entry?;
            let path = entry.path();
            
            if path.extension().and_then(|s| s.to_str()) == Some("toml") {
                self.load_theme(&path)?;
            }
        }
        Ok(())
    }
    
    /// Get a theme by name.
    pub fn get_theme(&self, name: &str) -> Option<&Theme> {
        self.themes.get(name)
    }
    
    /// Get all available theme names.
    pub fn available_themes(&self) -> Vec<&String> {
        self.themes.keys().collect()
    }
}

impl Default for ThemeManager {
    fn default() -> Self {
        Self::new()
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_theme_default() {
        let theme = Theme::light();
        assert_eq!(theme.name, "light");
        assert!(theme.get_color("keyword").is_some());
        assert_eq!(theme.get_color_or_default("keyword"), "#0000FF");
    }

    #[test]
    fn test_theme_manager() {
        let manager = ThemeManager::new();
        assert!(manager.get_theme("light").is_some());
        assert!(manager.get_theme("dark").is_some());
        assert!(manager.get_theme("monokai").is_some());
        assert!(manager.get_theme("nonexistent").is_none());
    }
}