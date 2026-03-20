//! Renderers for syntax-highlighted code.

use crate::parser::TextLineInfo;
use crate::theme::{Theme, Color};

/// HTML renderer for syntax-highlighted code.
pub struct HtmlRenderer {
    /// CSS class prefix for styling.
    pub class_prefix: String,

    /// Theme for colors.
    pub theme: Theme,
}

impl HtmlRenderer {
    /// Create a new HTML renderer with default theme.
    pub fn new() -> Self {
        Self {
            class_prefix: "hl".to_string(),
            theme: Theme::light(),
        }
    }

    /// Create a new HTML renderer with custom theme.
    pub fn with_theme(theme: Theme) -> Self {
        Self {
            class_prefix: "hl".to_string(),
            theme,
        }
    }

    /// Create a new HTML renderer with custom class prefix and theme.
    pub fn with_prefix_and_theme(prefix: &str, theme: Theme) -> Self {
        Self {
            class_prefix: prefix.to_string(),
            theme,
        }
    }
    
    /// Render lines of code to HTML.
    pub fn render(&self, lines: &[TextLineInfo]) -> String {
        let mut output = String::new();
        
        output.push_str("<!DOCTYPE html>\n");
        output.push_str("<html>\n<head>\n");
        output.push_str("<meta charset=\"utf-8\">\n");
        output.push_str("<style>\n");
        output.push_str(&self.generate_css());
        output.push_str("</style>\n");
        output.push_str("</head>\n<body>\n");
        output.push_str("<pre><code>\n");
        
        for line_info in lines {
            output.push_str(&self.render_line(line_info));
            output.push('\n');
        }
        
        output.push_str("</code></pre>\n");
        output.push_str("</body>\n</html>\n");
        
        output
    }
    
    /// Render a single line to HTML.
    pub fn render_line(&self, line_info: &TextLineInfo) -> String {
        let mut output = String::new();

        for segment in &line_info.segments {
            let text = &line_info.text_line[segment.start_index..segment.start_index + segment.length];

            if let Some(token_type) = &segment.token_type {
                let color = self.theme.get_color_or_default(token_type);
                let css_color = self.color_to_css(color);
                output.push_str(&format!(
                    "<span style=\"color: {}\">{}</span>",
                    css_color,
                    self.escape_html(text)
                ));
            } else {
                output.push_str(&self.escape_html(text));
            }
        }

        output
    }
    
    /// Generate CSS styles.
    fn generate_css(&self) -> String {
        format!(
            r#"
.{} {{
    font-family: 'Consolas', 'Monaco', 'Courier New', monospace;
    font-size: 14px;
    line-height: 1.4;
    background-color: {};
    color: {};
    padding: 10px;
    border-radius: 4px;
    overflow-x: auto;
}}
"#,
            self.class_prefix,
            self.theme.colors.background,
            self.theme.colors.foreground
        )
    }
    
    /// Convert color string to CSS format.
    fn color_to_css(&self, color: &Color) -> String {
        // Handle named colors
        match color.to_lowercase().as_str() {
            "blue" => "#0000FF".to_string(),
            "red" => "#FF0000".to_string(),
            "green" => "#008000".to_string(),
            "gray" | "grey" => "#808080".to_string(),
            "darkred" => "#8B0000".to_string(),
            "cornflowerblue" => "#6495ED".to_string(),
            "chocolate" => "#D2691E".to_string(),
            "magenta" => "#FF00FF".to_string(),
            "darkcyan" => "#008B8B".to_string(),
            _ => {
                // Assume it's already in hex format
                if color.starts_with('#') {
                    color.clone()
                } else {
                    format!("#{}", color)
                }
            }
        }
    }
    
    /// Escape HTML special characters.
    fn escape_html(&self, text: &str) -> String {
        text.replace('&', "&amp;")
            .replace('<', "&lt;")
            .replace('>', "&gt;")
            .replace('"', "&quot;")
    }
}

impl Default for HtmlRenderer {
    fn default() -> Self {
        Self::new()
    }
}

/// Terminal renderer for syntax-highlighted code (ANSI colors).
pub struct TerminalRenderer {
    /// Whether to use colors in output.
    pub use_colors: bool,

    /// Theme for colors.
    pub theme: Theme,
}

impl TerminalRenderer {
    /// Create a new terminal renderer with default theme.
    pub fn new() -> Self {
        Self {
            use_colors: true,
            theme: Theme::light(),
        }
    }

    /// Create a new terminal renderer with custom theme.
    pub fn with_theme(theme: Theme) -> Self {
        Self {
            use_colors: true,
            theme,
        }
    }

    /// Create a new terminal renderer with color control and theme.
    pub fn with_colors_and_theme(use_colors: bool, theme: Theme) -> Self {
        Self {
            use_colors,
            theme,
        }
    }
    
    /// Render lines of code to terminal output.
    pub fn render(&self, lines: &[TextLineInfo]) -> String {
        let mut output = String::new();
        
        for line_info in lines {
            output.push_str(&self.render_line(line_info));
            output.push('\n');
        }
        
        output
    }
    
    /// Render a single line to terminal output.
    pub fn render_line(&self, line_info: &TextLineInfo) -> String {
        let mut output = String::new();

        for segment in &line_info.segments {
            let text = &line_info.text_line[segment.start_index..segment.start_index + segment.length];

            if self.use_colors {
                if let Some(token_type) = &segment.token_type {
                    let color = self.theme.get_color_or_default(token_type);
                    let ansi_code = self.color_to_ansi(color);
                    output.push_str(&format!("\x1b[{}m{}\x1b[0m", ansi_code, text));
                } else {
                    output.push_str(text);
                }
            } else {
                output.push_str(text);
            }
        }

        output
    }
    
    /// Convert color string to ANSI escape code.
    fn color_to_ansi(&self, color: &Color) -> String {
        // Handle named colors
        let ansi_code = match color.to_lowercase().as_str() {
            "blue" => "34",      // Blue
            "red" => "31",       // Red
            "green" => "32",     // Green
            "gray" | "grey" => "90",  // Bright black (gray)
            "darkred" => "31",   // Red (close enough)
            "cornflowerblue" => "34", // Blue
            "chocolate" => "33", // Yellow (close to brown)
            "darkcyan" => "36",  // Cyan
            "magenta" => "35",   // Magenta
            _ => {
                // Try to parse hex color
                if color.starts_with('#') && color.len() == 7 {
                    // Convert hex to approximate ANSI color
                    let r = u8::from_str_radix(&color[1..3], 16).unwrap_or(0);
                    let g = u8::from_str_radix(&color[3..5], 16).unwrap_or(0);
                    let b = u8::from_str_radix(&color[5..7], 16).unwrap_or(0);

                    // Use 256-color mode
                    // Convert RGB to closest ANSI 256 color
                    let ansi_256 = self.rgb_to_ansi256(r, g, b);
                    return format!("38;5;{}", ansi_256);
                }
                "39" // Default
            }
        };
        ansi_code.to_string()
    }

    /// Convert RGB to ANSI 256 color code.
    fn rgb_to_ansi256(&self, r: u8, g: u8, b: u8) -> u8 {
        // Convert to 6x6x6 color cube (16-231)
        let r6 = (r as f32 / 255.0 * 5.0).round() as u8;
        let g6 = (g as f32 / 255.0 * 5.0).round() as u8;
        let b6 = (b as f32 / 255.0 * 5.0).round() as u8;

        16 + 36 * r6 + 6 * g6 + b6
    }
}

impl Default for TerminalRenderer {
    fn default() -> Self {
        Self::new()
    }
}