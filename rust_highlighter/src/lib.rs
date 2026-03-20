//! # Highlight Engine
//! 
//! A syntax highlighting engine for source code, originally designed for a code review tool.
//! This is a Rust port of the original C# implementation.

pub mod error;
pub mod profile;
pub mod parser;
pub mod renderer;
pub mod interner;
pub mod theme;

pub use error::{HighlightError, Result};
pub use profile::{HighlightProfile, LanguageProfile, ProfileManager};
pub use parser::{HighlightParser, TextLineInfo, TextSegment};
pub use renderer::{HtmlRenderer, TerminalRenderer};
pub use interner::StringInterner;
pub use theme::{Theme, ThemeManager};