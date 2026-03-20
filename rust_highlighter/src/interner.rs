//! String interning for common tokens to reduce memory allocations.

use std::collections::HashMap;
use std::rc::Rc;

/// A simple string interner for common tokens.
#[derive(Debug, Clone)]
pub struct StringInterner {
    cache: HashMap<String, Rc<String>>,
}

impl StringInterner {
    /// Create a new string interner.
    pub fn new() -> Self {
        Self {
            cache: HashMap::new(),
        }
    }
    
    /// Intern a string, returning a reference-counted pointer.
    /// If the string has been interned before, returns the existing pointer.
    pub fn intern(&mut self, s: &str) -> Rc<String> {
        self.cache
            .entry(s.to_string())
            .or_insert_with(|| Rc::new(s.to_string()))
            .clone()
    }
    
    /// Get the number of interned strings.
    pub fn len(&self) -> usize {
        self.cache.len()
    }
    
    /// Check if the interner is empty.
    pub fn is_empty(&self) -> bool {
        self.cache.is_empty()
    }
}

impl Default for StringInterner {
    fn default() -> Self {
        Self::new()
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_interner_basic() {
        let mut interner = StringInterner::new();
        
        let s1 = interner.intern("hello");
        let s2 = interner.intern("hello");
        let s3 = interner.intern("world");
        
        // Same string should return same reference
        assert!(Rc::ptr_eq(&s1, &s2));
        
        // Different strings should return different references
        assert!(!Rc::ptr_eq(&s1, &s3));
        
        // Check values
        assert_eq!(*s1, "hello");
        assert_eq!(*s3, "world");
        
        // Check interner size
        assert_eq!(interner.len(), 2);
    }
}