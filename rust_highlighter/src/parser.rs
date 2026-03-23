//! Core syntax highlighting parser.

use crate::error::Result;
use crate::profile::HighlightProfile;
use crate::interner::StringInterner;
use std::collections::HashSet;
use std::rc::Rc;
use std::cell::RefCell;

/// A segment of text with highlighting information.
#[derive(Debug, Clone)]
pub struct TextSegment {
    /// Start index in the line.
    pub start_index: usize,

    /// Length of the segment.
    pub length: usize,

    /// Token type name (e.g., "keyword", "comment", "string").
    /// This is used to look up colors from the theme.
    pub token_type: Option<Rc<String>>,
}

/// Information about a parsed line of text.
#[derive(Debug, Clone)]
pub struct TextLineInfo {
    /// The original text line.
    pub text_line: String,
    
    /// Line number (1-based).
    pub line_number: usize,
    
    /// Text segments with highlighting.
    pub segments: Vec<TextSegment>,
}

/// State for tracking multi-line blocks across lines.
#[derive(Debug, Clone)]
pub struct MultiLineState {
    /// Token type name for the active multi-line block.
    pub token_type: String,

    /// End marker to look for.
    pub end_marker: String,

    /// Escape configuration.
    pub escape: Option<crate::profile::BlockEscape>,
}

/// The main syntax highlighting parser.
pub struct HighlightParser {
    profile: HighlightProfile,
    delimiter_set: HashSet<char>,
    /// String interner for common tokens (using RefCell for interior mutability)
    interner: RefCell<StringInterner>,
}

impl HighlightParser {
    /// Create a new parser with the given profile.
    pub fn new(profile: HighlightProfile) -> Self {
        let delimiter_set: HashSet<char> = profile.language.delimiters.iter().cloned().collect();

        Self {
            profile,
            delimiter_set,
            interner: RefCell::new(StringInterner::new()),
        }
    }
    
    /// Parse multiple lines of text.
    pub fn parse(&mut self, lines: &[&str], start_line_number: usize) -> Result<Vec<TextLineInfo>> {
        let mut result = Vec::new();
        let mut multi_line_state: Option<MultiLineState> = None;

        for (i, line) in lines.iter().enumerate() {
            let line_number = start_line_number + i;
            let line_info = self.parse_line(line, line_number, &mut multi_line_state)?;
            result.push(line_info);
        }

        Ok(result)
    }
    
    /// Parse a single line of text.
    pub fn parse_line(
        &mut self,
        line: &str,
        line_number: usize,
        multi_line_state: &mut Option<MultiLineState>,
    ) -> Result<TextLineInfo> {
        let mut segments = Vec::new();
        let mut current_pos = 0;

        // Check if we're in a multi-line block from previous line
        if let Some(state) = multi_line_state.take() {
            if let Some(end_pos) = self.find_multi_line_end(line, &state, current_pos) {
                // Found end of multi-line block
                segments.push(TextSegment {
                    start_index: current_pos,
                    length: end_pos + state.end_marker.len() - current_pos,
                    token_type: Some(self.interner.borrow_mut().intern(&state.token_type)),
                });
                current_pos = end_pos + state.end_marker.len();
            } else {
                // Entire line is part of multi-line block
                segments.push(TextSegment {
                    start_index: current_pos,
                    length: line.len() - current_pos,
                    token_type: Some(self.interner.borrow_mut().intern(&state.token_type)),
                });
                *multi_line_state = Some(state);

                return Ok(TextLineInfo {
                    text_line: line.to_string(),
                    line_number,
                    segments,
                });
            }
        }

        // Parse the rest of the line
        let mut last_parse_end = current_pos;

        while current_pos < line.len() {
            // Try to match multi-line blocks first
            if let Some((block, start_pos)) = self.find_multi_line_start(line, current_pos) {
                // Add any text before the block
                if start_pos > last_parse_end {
                    self.parse_segment(line, last_parse_end, start_pos, &mut segments)?;
                }

                // Look for end of multi-line block
                let search_start = start_pos + block.start.len();
                if let Some(end_pos) = self.find_multi_line_end_in_block(line, &block, search_start) {
                    // Found end on same line
                    segments.push(TextSegment {
                        start_index: start_pos,
                        length: end_pos + block.end.len() - start_pos,
                        token_type: Some(self.interner.borrow_mut().intern(&block.name)),
                    });
                    current_pos = end_pos + block.end.len();
                } else {
                    // Multi-line block continues to next line
                    segments.push(TextSegment {
                        start_index: start_pos,
                        length: line.len() - start_pos,
                        token_type: Some(self.interner.borrow_mut().intern(&block.name)),
                    });

                    *multi_line_state = Some(MultiLineState {
                        token_type: block.name.clone(),
                        end_marker: block.end.clone(),
                        escape: block.escape.clone(),
                    });

                    current_pos = line.len();
                }
                last_parse_end = current_pos;
                continue;
            }

            // Try to match single-line blocks
            if let Some((block, start_pos)) = self.find_single_line_block(line, current_pos) {
                // Add any text before the block
                if start_pos > last_parse_end {
                    self.parse_segment(line, last_parse_end, start_pos, &mut segments)?;
                }

                // Find end of single-line block
                let search_start = start_pos + block.start.len();
                let end_pos = if block.end.is_empty() {
                    // End at end of line
                    line.len()
                } else {
                    self.find_single_line_end(line, &block, search_start)
                        .unwrap_or(line.len())
                };

                segments.push(TextSegment {
                    start_index: start_pos,
                    length: end_pos - start_pos,
                    token_type: Some(self.interner.borrow_mut().intern(&block.name)),
                });

                current_pos = end_pos;
                last_parse_end = current_pos;
                continue;
            }

            // Try to match tokens
            if let Some((token, start_pos, end_pos)) = self.find_token(line, current_pos) {
                // Add any text before the token
                if start_pos > last_parse_end {
                    self.parse_segment(line, last_parse_end, start_pos, &mut segments)?;
                }

                segments.push(TextSegment {
                    start_index: start_pos,
                    length: end_pos - start_pos,
                    token_type: Some(self.interner.borrow_mut().intern(&token.name)),
                });

                current_pos = end_pos;
                last_parse_end = current_pos;
                continue;
            }

            // No match found, move to next character
            current_pos += 1;
        }

        // Parse any remaining text for keywords
        if last_parse_end < line.len() {
            self.parse_segment(line, last_parse_end, line.len(), &mut segments)?;
        }

        Ok(TextLineInfo {
            text_line: line.to_string(),
            line_number,
            segments,
        })
    }
    
    /// Parse a segment of text for keywords.
    fn parse_segment(
        &self,
        line: &str,
        start: usize,
        end: usize,
        segments: &mut Vec<TextSegment>,
    ) -> Result<()> {
        let segment_text = &line[start..end];
        let mut current_pos = 0;

        while current_pos < segment_text.len() {
            // Find next delimiter
            let delimiter_pos = segment_text[current_pos..]
                .find(|c: char| self.delimiter_set.contains(&c))
                .map(|pos| current_pos + pos)
                .unwrap_or(segment_text.len());

            if delimiter_pos > current_pos {
                // Extract word
                let word = &segment_text[current_pos..delimiter_pos];

                // Check if it's a keyword
                if let Some(keyword_collection) = self.find_keyword(word) {
                    segments.push(TextSegment {
                        start_index: start + current_pos,
                        length: word.len(),
                        token_type: Some(self.interner.borrow_mut().intern(&keyword_collection.name)),
                    });
                } else {
                    // Regular text
                    segments.push(TextSegment {
                        start_index: start + current_pos,
                        length: word.len(),
                        token_type: None,
                    });
                }
            }

            // Add delimiter if present
            if delimiter_pos < segment_text.len() {
                segments.push(TextSegment {
                    start_index: start + delimiter_pos,
                    length: 1,
                    token_type: None,
                });
                current_pos = delimiter_pos + 1;
            } else {
                current_pos = delimiter_pos;
            }
        }

        Ok(())
    }

    /// Find a keyword in the profile using binary search.
    fn find_keyword(&self, word: &str) -> Option<&crate::profile::KeywordCollection> {
        let compare_word = if self.profile.language.ignore_case {
            word.to_lowercase()
        } else {
            word.to_string()
        };

        for keyword_collection in &self.profile.language.keywords {
            // Use binary search on pre-sorted keywords
            if keyword_collection.sorted_keywords.binary_search(&compare_word).is_ok() {
                return Some(keyword_collection);
            }
        }

        None
    }
    
    /// Find a multi-line block start.
    fn find_multi_line_start<'a>(
        &'a self,
        line: &str,
        start_pos: usize,
    ) -> Option<(&'a crate::profile::CodeBlock, usize)> {
        // Ensure start_pos is a valid char boundary
        if !line.is_char_boundary(start_pos) {
            return None;
        }

        let search_text = &line[start_pos..];

        for block in &self.profile.language.multi_line_blocks {
            if let Some(pos) = search_text.find(&block.start) {
                return Some((block, start_pos + pos));
            }
        }

        None
    }
    
    /// Find end of multi-line block.
    fn find_multi_line_end(
        &self,
        line: &str,
        state: &MultiLineState,
        start_pos: usize,
    ) -> Option<usize> {
        // Ensure start_pos is a valid char boundary
        if !line.is_char_boundary(start_pos) {
            return None;
        }

        let search_text = &line[start_pos..];

        // Handle escape sequences
        if let Some(escape) = &state.escape {
            let mut pos = 0;
            while pos < search_text.len() {
                if let Some(escape_str) = &escape.escape_string {
                    if search_text[pos..].starts_with(escape_str) {
                        pos += escape_str.len() + 1; // Skip escape + next char
                        continue;
                    }
                }

                for item in &escape.items {
                    if search_text[pos..].starts_with(item) {
                        pos += item.len();
                        continue;
                    }
                }

                if search_text[pos..].starts_with(&state.end_marker) {
                    return Some(start_pos + pos);
                }

                // Move to next character boundary
                if let Some(next_char) = search_text[pos..].chars().next() {
                    pos += next_char.len_utf8();
                } else {
                    break;
                }
            }
        } else {
            if let Some(pos) = search_text.find(&state.end_marker) {
                return Some(start_pos + pos);
            }
        }

        None
    }
    
    /// Find end of multi-line block within a block definition.
    fn find_multi_line_end_in_block(
        &self,
        line: &str,
        block: &crate::profile::CodeBlock,
        start_pos: usize,
    ) -> Option<usize> {
        // Ensure start_pos is a valid char boundary
        if !line.is_char_boundary(start_pos) {
            return None;
        }

        let search_text = &line[start_pos..];

        if let Some(escape) = &block.escape {
            let mut pos = 0;
            while pos < search_text.len() {
                if let Some(escape_str) = &escape.escape_string {
                    if search_text[pos..].starts_with(escape_str) {
                        pos += escape_str.len() + 1;
                        continue;
                    }
                }

                for item in &escape.items {
                    if search_text[pos..].starts_with(item) {
                        pos += item.len();
                        continue;
                    }
                }

                if search_text[pos..].starts_with(&block.end) {
                    return Some(start_pos + pos);
                }

                // Move to next character boundary
                if let Some(next_char) = search_text[pos..].chars().next() {
                    pos += next_char.len_utf8();
                } else {
                    break;
                }
            }
        } else {
            if let Some(pos) = search_text.find(&block.end) {
                return Some(start_pos + pos);
            }
        }

        None
    }
    
    /// Find a single-line block.
    fn find_single_line_block<'a>(
        &'a self,
        line: &str,
        start_pos: usize,
    ) -> Option<(&'a crate::profile::CodeBlock, usize)> {
        // Ensure start_pos is a valid char boundary
        if !line.is_char_boundary(start_pos) {
            return None;
        }

        let search_text = &line[start_pos..];

        for block in &self.profile.language.single_line_blocks {
            if let Some(pos) = search_text.find(&block.start) {
                return Some((block, start_pos + pos));
            }
        }

        None
    }
    
    /// Find end of single-line block.
    fn find_single_line_end(
        &self,
        line: &str,
        block: &crate::profile::CodeBlock,
        start_pos: usize,
    ) -> Option<usize> {
        if block.end.is_empty() {
            return Some(line.len());
        }

        // Ensure start_pos is a valid char boundary
        if !line.is_char_boundary(start_pos) {
            return None;
        }

        let search_text = &line[start_pos..];

        if let Some(escape) = &block.escape {
            let mut pos = 0;
            while pos < search_text.len() {
                if let Some(escape_str) = &escape.escape_string {
                    if search_text[pos..].starts_with(escape_str) {
                        pos += escape_str.len() + 1;
                        continue;
                    }
                }

                for item in &escape.items {
                    if search_text[pos..].starts_with(item) {
                        pos += item.len();
                        continue;
                    }
                }

                if search_text[pos..].starts_with(&block.end) {
                    return Some(start_pos + pos);
                }

                // Move to next character boundary
                if let Some(next_char) = search_text[pos..].chars().next() {
                    pos += next_char.len_utf8();
                } else {
                    break;
                }
            }
        } else {
            if let Some(pos) = search_text.find(&block.end) {
                return Some(start_pos + pos);
            }
        }

        None
    }
    
    /// Find a token match.
    fn find_token(
        &self,
        line: &str,
        start_pos: usize,
    ) -> Option<(&crate::profile::CompiledToken, usize, usize)> {
        // Ensure start_pos is a valid char boundary
        if !line.is_char_boundary(start_pos) {
            return None;
        }

        let search_text = &line[start_pos..];

        for token in &self.profile.compiled_tokens {
            if let Some(captures) = token.pattern.captures(search_text) {
                if let Some(match_) = captures.get(0) {
                    return Some((token, start_pos + match_.start(), start_pos + match_.end()));
                }
            }
        }

        None
    }
}