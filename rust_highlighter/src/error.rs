//! Error types for the highlight engine.

use std::io;
use thiserror::Error;

/// Result type alias for highlight engine operations.
pub type Result<T> = std::result::Result<T, HighlightError>;

/// Errors that can occur in the highlight engine.
#[derive(Error, Debug)]
pub enum HighlightError {
    /// IO error when reading files.
    #[error("IO error: {0}")]
    Io(#[from] io::Error),

    /// Error parsing TOML configuration.
    #[error("TOML parsing error: {0}")]
    Toml(#[from] toml::de::Error),

    /// Error compiling regex pattern.
    #[error("Regex compilation error: {0}")]
    Regex(#[from] regex::Error),

    /// Invalid language profile.
    #[error("Invalid language profile: {0}")]
    InvalidProfile(String),

    /// Language not found.
    #[error("Language not found: {0}")]
    LanguageNotFound(String),

    /// File extension not supported.
    #[error("File extension not supported: {0}")]
    UnsupportedExtension(String),
}