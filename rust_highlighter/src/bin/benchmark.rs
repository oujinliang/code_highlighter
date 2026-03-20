//! Performance benchmark for the highlight engine.

use highlight_engine::{HighlightParser, ProfileManager};
use std::path::Path;
use std::time::Instant;

fn main() -> anyhow::Result<()> {
    // Load language profiles
    let mut profile_manager = ProfileManager::new();
    profile_manager.load_profiles_from_dir(Path::new("languages"))?;
    
    // Get C# profile
    let profile = profile_manager.get_profile("csharp")
        .ok_or_else(|| anyhow::anyhow!("C# profile not found"))?;
    
    // Generate test data of different sizes
    let test_cases = vec![
        ("Small (100 lines)", generate_test_code(100)),
        ("Medium (1000 lines)", generate_test_code(1000)),
        ("Large (10000 lines)", generate_test_code(10000)),
    ];
    
    for (name, code) in test_cases {
        println!("\n=== {} ===", name);
        
        let lines: Vec<&str> = code.lines().collect();
        let line_count = lines.len();
        
        // Warm up
        let mut parser = HighlightParser::new(profile.clone());
        let _ = parser.parse(&lines[0..10.min(line_count)], 1)?;

        // Benchmark
        let start = Instant::now();
        let result = parser.parse(&lines, 1)?;
        let duration = start.elapsed();
        
        println!("  Lines: {}", line_count);
        println!("  Time: {:?}", duration);
        println!("  Lines/sec: {:.0}", line_count as f64 / duration.as_secs_f64());
        println!("  Segments: {}", result.iter().map(|l| l.segments.len()).sum::<usize>());
    }
    
    Ok(())
}

fn generate_test_code(lines: usize) -> String {
    let mut code = String::new();
    
    code.push_str("using System;\n");
    code.push_str("using System.Collections.Generic;\n\n");
    code.push_str("namespace TestNamespace\n{\n");
    
    for i in 0..lines {
        match i % 10 {
            0 => {
                code.push_str(&format!("    // Comment line {}\n", i));
            }
            1 => {
                code.push_str(&format!("    public class TestClass{}\n", i));
                code.push_str("    {\n");
            }
            2 => {
                code.push_str(&format!("        private int _field{} = {};\n", i, i));
            }
            3 => {
                code.push_str(&format!("        public string Property{} {{ get; set; }}\n", i));
            }
            4 => {
                code.push_str(&format!("        public void Method{}()\n", i));
                code.push_str("        {\n");
            }
            5 => {
                code.push_str(&format!("            Console.WriteLine(\"Method {}\");\n", i));
            }
            6 => {
                code.push_str(&format!("            var list = new List<int> {{ {}, {}, {} }};\n", i, i+1, i+2));
            }
            7 => {
                code.push_str(&format!("            int result = {} * {} + {};\n", i, i+1, i+2));
            }
            8 => {
                code.push_str("        }\n");
            }
            9 => {
                code.push_str("    }\n");
            }
            _ => unreachable!(),
        }
    }
    
    code.push_str("}\n");
    code
}