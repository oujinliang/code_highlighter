//! A simple Rust example for testing syntax highlighting.

use std::collections::HashMap;

/// A struct to represent a person.
#[derive(Debug, Clone)]
pub struct Person {
    name: String,
    age: u32,
}

impl Person {
    /// Create a new person.
    pub fn new(name: String, age: u32) -> Self {
        Self { name, age }
    }

    /// Get the person's name.
    pub fn name(&self) -> &str {
        &self.name
    }

    /// Get the person's age.
    pub fn age(&self) -> u32 {
        self.age
    }
}

fn main() {
    // Create a new person
    let person = Person::new("Alice".to_string(), 30);

    // Print person info
    println!("Name: {}", person.name());
    println!("Age: {}", person.age());

    // Create a hash map
    let mut scores: HashMap<String, i32> = HashMap::new();
    scores.insert("Alice".to_string(), 95);
    scores.insert("Bob".to_string(), 87);

    // Iterate over scores
    for (name, score) in &scores {
        println!("{}: {}", name, score);
    }

    // Test with different number formats
    let hex = 0xFF;
    let binary = 0b1010;
    let octal = 0o755;
    let float = 3.14159;

    println!("Hex: {}, Binary: {}, Octal: {}, Float: {}", hex, binary, octal, float);

    // Test with raw strings
    let raw = r#"This is a "raw" string with \n literal backslashes"#;
    println!("{}", raw);

    // Test with format strings
    let formatted = format!("Hello, {}! You are {} years old.", person.name(), person.age());
    println!("{}", formatted);
}