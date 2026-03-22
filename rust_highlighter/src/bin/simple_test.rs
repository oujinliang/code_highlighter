use highlight_engine::{ProfileManager, HighlightParser, HtmlRenderer};
use std::path::Path;

fn main() -> Result<(), Box<dyn std::error::Error>> {
    println!("🧪 简单语言支持测试\n");
    
    // 创建配置管理器
    let mut manager = ProfileManager::new();
    manager.load_profiles_from_dir(Path::new("languages"))?;
    
    // 获取所有支持的语言
    let languages = manager.available_languages();
    println!("📚 支持的语言总数: {}", languages.len());
    println!("语言列表: {:?}\n", languages);
    
    // 测试现有的语言
    let test_cases = vec![
        ("csharp", "public class HelloWorld { public static void main(String[] args) { System.out.println(\"Hello, World!\"); } }"),
        ("javascript", "function hello(name) { console.log('Hello, ' + name + '!'); }"),
        ("python", "def hello(name):\n    print(f'Hello, {name}!')"),
        ("rust", "fn main() {\n    println!(\"Hello, Rust!\");\n}"),
    ];
    
    let mut success_count = 0;
    let mut total_count = 0;
    
    for (language, code) in test_cases {
        total_count += 1;
        print!("🔍 测试 {}: ", language);
        
        match manager.get_profile(language) {
            Some(profile) => {
                let mut parser = HighlightParser::new(profile.clone());
                let lines: Vec<&str> = code.lines().collect();
                
                match parser.parse(&lines, 1) {
                    Ok(parsed) => {
                        let renderer = HtmlRenderer::new();
                        let html = renderer.render(&parsed);
                        
                        // 检查是否生成了有效的 HTML
                        if html.contains("<span") && html.len() > 100 {
                            println!("✅ 成功 ({} 行, {} 字符)", lines.len(), html.len());
                            success_count += 1;
                        } else {
                            println!("⚠️  生成的 HTML 过短");
                        }
                    }
                    Err(e) => {
                        println!("❌ 解析失败: {}", e);
                    }
                }
            }
            None => {
                println!("❌ 未找到语言配置");
            }
        }
    }
    
    println!("\n📊 测试结果:");
    println!("成功: {}/{}", success_count, total_count);
    println!("成功率: {:.1}%", (success_count as f64 / total_count as f64) * 100.0);
    
    if success_count == total_count {
        println!("\n🎉 所有现有语言测试通过！");
    } else {
        println!("\n⚠️  部分语言测试失败");
    }
    
    Ok(())
}