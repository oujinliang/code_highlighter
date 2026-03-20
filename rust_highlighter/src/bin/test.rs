//! Simple test for the highlight engine.

use highlight_engine::{HighlightParser, HtmlRenderer, ProfileManager};
use std::path::Path;

fn main() -> anyhow::Result<()> {
    // Load language profiles
    let mut profile_manager = ProfileManager::new();
    profile_manager.load_profiles_from_dir(Path::new("languages"))?;
    
    // Get C# profile
    let profile = profile_manager.get_profile("csharp")
        .ok_or_else(|| anyhow::anyhow!("C# profile not found"))?;
    
    // Test code
    let code = r#"using System;

namespace Test
{
    public class Hello
    {
        public static void Main(string[] args)
        {
            // This is a comment
            Console.WriteLine("Hello, World!");
            int number = 42;
            double value = 3.14;
        }
    }
}"#;
    
    let lines: Vec<&str> = code.lines().collect();
    
    // Parse the code
    let mut parser = HighlightParser::new(profile.clone());
    let parsed_lines = parser.parse(&lines, 1)?;
    
    // Render to HTML
    let renderer = HtmlRenderer::new();
    let html = renderer.render(&parsed_lines);
    
    println!("{}", html);
    
    Ok(())
}