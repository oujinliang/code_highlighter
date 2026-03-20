//! CLI adapter for the highlight engine.

use clap::Parser;
use std::path::PathBuf;
use std::process;

use highlight_engine::{HighlightParser, HtmlRenderer, TerminalRenderer, ProfileManager, ThemeManager, Theme};

/// A syntax highlighting tool for source code.
#[derive(Parser)]
#[command(name = "highlight")]
#[command(about = "Syntax highlighting for source code")]
#[command(version = "0.1.0")]
struct Cli {
    /// Input file to highlight
    file: PathBuf,

    /// Output format (html or terminal)
    #[arg(short, long, default_value = "terminal")]
    format: String,

    /// Output file (default: stdout)
    #[arg(short, long)]
    output: Option<PathBuf>,

    /// Language override (auto-detect from file extension if not specified)
    #[arg(short, long)]
    language: Option<String>,

    /// Languages directory (default: ./languages)
    #[arg(long, default_value = "languages")]
    languages_dir: PathBuf,

    /// Theme name or path to theme file (default: light)
    #[arg(short, long, default_value = "light")]
    theme: String,

    /// Themes directory (default: ./themes)
    #[arg(long, default_value = "themes")]
    themes_dir: PathBuf,

    /// Disable colors in terminal output
    #[arg(long)]
    no_colors: bool,

    /// Show line numbers
    #[arg(short = 'n', long)]
    line_numbers: bool,
}

fn main() {
    let cli = Cli::parse();
    
    if let Err(e) = run(cli) {
        eprintln!("Error: {}", e);
        process::exit(1);
    }
}

fn run(cli: Cli) -> anyhow::Result<()> {
    // Load language profiles
    let mut profile_manager = ProfileManager::new();

    if cli.languages_dir.exists() {
        profile_manager.load_profiles_from_dir(&cli.languages_dir)?;
    } else {
        eprintln!("Warning: Languages directory not found: {}", cli.languages_dir.display());
    }

    // Load theme
    let mut theme_manager = ThemeManager::new();

    if cli.themes_dir.exists() {
        theme_manager.load_themes_from_dir(&cli.themes_dir)?;
    }

    // Get theme (try as name first, then as file path)
    let theme = if let Some(theme) = theme_manager.get_theme(&cli.theme) {
        theme.clone()
    } else {
        // Try to load as file path
        let theme_path = PathBuf::from(&cli.theme);
        if theme_path.exists() {
            Theme::load(&theme_path)?
        } else {
            eprintln!("Warning: Theme '{}' not found, using default light theme", cli.theme);
            Theme::light()
        }
    };
    
    // Determine language
    let language_name = if let Some(lang) = &cli.language {
        lang.clone()
    } else {
        // Auto-detect from file extension
        let extension = cli.file.extension()
            .and_then(|ext| ext.to_str())
            .map(|ext| format!(".{}", ext))
            .unwrap_or_default();
        
        profile_manager.get_profile_by_extension(&extension)
            .map(|profile| profile.language.name.clone())
            .ok_or_else(|| {
                highlight_engine::HighlightError::UnsupportedExtension(extension)
            })?
    };
    
    // Get the profile
    let profile = profile_manager.get_profile(&language_name)
        .ok_or_else(|| {
            highlight_engine::HighlightError::LanguageNotFound(language_name.clone())
        })?;
    
    // Read input file
    let content = std::fs::read_to_string(&cli.file)?;
    let lines: Vec<&str> = content.lines().collect();
    
    // Parse the code
    let mut parser = HighlightParser::new(profile.clone());
    let parsed_lines = parser.parse(&lines, 1)?;
    
    // Add line numbers if requested
    let final_lines = if cli.line_numbers {
        add_line_numbers(&parsed_lines)
    } else {
        parsed_lines
    };
    
    // Render output
    let output = match cli.format.as_str() {
        "html" => {
            let renderer = HtmlRenderer::with_theme(theme);
            renderer.render(&final_lines)
        }
        "terminal" => {
            let renderer = TerminalRenderer::with_colors_and_theme(!cli.no_colors, theme);
            renderer.render(&final_lines)
        }
        _ => {
            anyhow::bail!("Unsupported format: {}. Use 'html' or 'terminal'", cli.format);
        }
    };
    
    // Write output
    if let Some(output_path) = &cli.output {
        std::fs::write(output_path, &output)?;
        println!("Output written to: {}", output_path.display());
    } else {
        print!("{}", output);
    }
    
    Ok(())
}

fn add_line_numbers(lines: &[highlight_engine::TextLineInfo]) -> Vec<highlight_engine::TextLineInfo> {
    let max_line_num = lines.len();
    let width = max_line_num.to_string().len();
    
    lines.iter().enumerate().map(|(i, line_info)| {
        let line_num = i + 1;
        let padded_num = format!("{:width$}", line_num, width = width);
        let new_text = format!("{} | {}", padded_num, line_info.text_line);
        
        // Adjust segment positions
        let offset = width + 3; // " | " = 3 chars
        let mut new_segments = Vec::new();
        
        // Add line number segment
        new_segments.push(highlight_engine::TextSegment {
            start_index: 0,
            length: width,
            token_type: Some(std::rc::Rc::new("line_number".to_string())),
        });

        // Add separator segment
        new_segments.push(highlight_engine::TextSegment {
            start_index: width,
            length: 3,
            token_type: Some(std::rc::Rc::new("separator".to_string())),
        });
        
        // Adjust existing segments
        for segment in &line_info.segments {
            new_segments.push(highlight_engine::TextSegment {
                start_index: segment.start_index + offset,
                length: segment.length,
                token_type: segment.token_type.clone(),
            });
        }
        
        highlight_engine::TextLineInfo {
            text_line: new_text,
            line_number: line_info.line_number,
            segments: new_segments,
        }
    }).collect()
}