//! Language profile definitions and loading.

use serde::Deserialize;
use std::collections::HashMap;
use std::path::Path;
use crate::error::Result;

/// A complete language highlighting profile.
#[derive(Debug, Clone, Deserialize)]
pub struct LanguageProfile {
    /// Language name (e.g., "csharp", "rust").
    pub name: String,
    
    /// File extensions associated with this language (e.g., [".cs", ".csx"]).
    #[serde(default)]
    pub extensions: Vec<String>,
    
    /// Delimiter characters that separate tokens.
    #[serde(default)]
    pub delimiters: Vec<char>,
    
    /// Back delimiters (if found, push back a character).
    #[serde(default)]
    pub back_delimiters: Vec<char>,
    
    /// Whether to ignore case when matching keywords.
    #[serde(default)]
    pub ignore_case: bool,
    
    /// Keyword collections with their colors.
    #[serde(default)]
    pub keywords: Vec<KeywordCollection>,
    
    /// Single-line code blocks (e.g., comments, strings).
    #[serde(default)]
    pub single_line_blocks: Vec<CodeBlock>,
    
    /// Multi-line code blocks (e.g., multi-line comments).
    #[serde(default)]
    pub multi_line_blocks: Vec<CodeBlock>,
    
    /// Regex-based tokens (e.g., numbers, preprocessor directives).
    #[serde(default)]
    pub tokens: Vec<TokenDefinition>,
}

/// A collection of keywords with the same highlighting style.
#[derive(Debug, Clone, Deserialize)]
pub struct KeywordCollection {
    /// Name of this keyword category (e.g., "keyword", "type").
    /// This is used to look up colors from the theme.
    pub name: String,

    /// List of keywords.
    pub keywords: Vec<String>,

    /// Pre-sorted keywords for binary search (populated during profile loading).
    #[serde(skip)]
    pub sorted_keywords: Vec<String>,
}

/// A code block definition (single-line or multi-line).
#[derive(Debug, Clone, Deserialize)]
pub struct CodeBlock {
    /// Name of this block type (e.g., "comment", "string").
    /// This is used to look up colors from the theme.
    pub name: String,

    /// Start marker.
    pub start: String,

    /// End marker (empty for single-line blocks that end at newline).
    #[serde(default)]
    pub end: String,

    /// Escape sequences within the block.
    #[serde(default)]
    pub escape: Option<BlockEscape>,
}

/// Escape sequences within a code block.
#[derive(Debug, Clone, Deserialize)]
pub struct BlockEscape {
    /// Escape string that skips the next character.
    #[serde(default)]
    pub escape_string: Option<String>,
    
    /// Items to skip when encountered.
    #[serde(default)]
    pub items: Vec<String>,
}

/// A regex-based token definition.
#[derive(Debug, Clone, Deserialize)]
pub struct TokenDefinition {
    /// Name of this token type (e.g., "number", "preprocessor").
    /// This is used to look up colors from the theme.
    pub name: String,

    /// Regex pattern to match.
    pub pattern: String,

    /// Named groups within the pattern with their own token types.
    #[serde(default)]
    pub groups: Vec<TokenGroup>,
}

/// A named group within a regex token.
#[derive(Debug, Clone, Deserialize)]
pub struct TokenGroup {
    /// Name of the group (used to look up colors from the theme).
    pub name: String,
}

/// Highlight profile with compiled regex patterns.
#[derive(Debug, Clone)]
pub struct HighlightProfile {
    /// The language profile configuration.
    pub language: LanguageProfile,
    
    /// Compiled regex patterns for tokens.
    pub compiled_tokens: Vec<CompiledToken>,
}

/// A token with compiled regex pattern.
#[derive(Debug, Clone)]
pub struct CompiledToken {
    /// Name of this token type.
    pub name: String,

    /// Compiled regex pattern.
    pub pattern: regex::Regex,

    /// Named groups with their token types.
    pub groups: Vec<TokenGroup>,
}

impl HighlightProfile {
    /// Create a new highlight profile from a language profile.
    pub fn new(mut language: LanguageProfile) -> Result<Self> {
        // Pre-sort keywords for binary search
        for keyword_collection in &mut language.keywords {
            let mut sorted = keyword_collection.keywords.clone();
            if language.ignore_case {
                sorted.sort_by(|a, b| a.to_lowercase().cmp(&b.to_lowercase()));
            } else {
                sorted.sort();
            }
            keyword_collection.sorted_keywords = sorted;
        }

        // Compile regex patterns
        let mut compiled_tokens = Vec::new();

        for token_def in &language.tokens {
            let pattern = if language.ignore_case {
                format!("(?i){}", token_def.pattern)
            } else {
                token_def.pattern.clone()
            };

            let compiled_pattern = regex::Regex::new(&pattern)?;

            compiled_tokens.push(CompiledToken {
                name: token_def.name.clone(),
                pattern: compiled_pattern,
                groups: token_def.groups.clone(),
            });
        }

        Ok(Self {
            language,
            compiled_tokens,
        })
    }
}

/// Manager for loading and caching language profiles.
pub struct ProfileManager {
    profiles: HashMap<String, HighlightProfile>,
    extension_map: HashMap<String, String>,
}

impl ProfileManager {
    /// Create a new profile manager.
    pub fn new() -> Self {
        Self {
            profiles: HashMap::new(),
            extension_map: HashMap::new(),
        }
    }
    
    /// Load a language profile from a TOML file.
    pub fn load_profile(&mut self, path: &Path) -> Result<()> {
        let content = std::fs::read_to_string(path)?;
        let language: LanguageProfile = toml::from_str(&content)?;
        
        let name = language.name.clone();
        let extensions = language.extensions.clone();
        
        let profile = HighlightProfile::new(language)?;
        
        // Register extensions
        for ext in extensions {
            self.extension_map.insert(ext, name.clone());
        }
        
        self.profiles.insert(name, profile);
        Ok(())
    }
    
    /// Load all profiles from a directory.
    pub fn load_profiles_from_dir(&mut self, dir: &Path) -> Result<()> {
        for entry in std::fs::read_dir(dir)? {
            let entry = entry?;
            let path = entry.path();
            
            if path.extension().and_then(|s| s.to_str()) == Some("toml") {
                self.load_profile(&path)?;
            }
        }
        Ok(())
    }
    
    /// Get a profile by language name.
    pub fn get_profile(&self, name: &str) -> Option<&HighlightProfile> {
        self.profiles.get(name)
    }
    
    /// Get a profile by file extension.
    pub fn get_profile_by_extension(&self, extension: &str) -> Option<&HighlightProfile> {
        let language_name = self.extension_map.get(extension)?;
        self.profiles.get(language_name)
    }
    
    /// Get all available language names.
    pub fn available_languages(&self) -> Vec<&String> {
        self.profiles.keys().collect()
    }
}

impl Default for ProfileManager {
    fn default() -> Self {
        Self::new()
    }
}