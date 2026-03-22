# Code Highlighter - Project Context

## Project Overview

This is a **source code syntax highlighting engine** originally designed for a code review tool. The project is built with **C# (.NET Framework 3.5)** and uses **WPF (Windows Presentation Foundation)** for rendering.

### Core Purpose
- Parse source code and apply syntax highlighting based on language-specific profiles
- Support multiple programming languages through XML-based configuration files
- Generate HTML output with syntax-highlighted code
- Provide a WPF-based demo application for visualization

### Architecture
The solution consists of three main projects:

1. **HighlightEngine** (Library)
   - Core parsing and highlighting logic
   - Language profile definitions via XML files
   - Supports keywords, code blocks (single/multi-line), and regex-based tokens

2. **HighlightDemo** (WPF Application)
   - Desktop demo application showcasing the highlighting engine
   - Uses WPF controls for code visualization

3. **GenerateHtml** (Console Application)
   - Command-line tool to convert source files to syntax-highlighted HTML
   - Takes input and output file paths as arguments

## Building and Running

### Prerequisites
- .NET Framework 3.5 or later
- Visual Studio 2008 or later (for building from IDE)
- MSBuild (for command-line builds)

### Build Commands

**Using Visual Studio:**
```
Open Highlightengine.sln in Visual Studio and build the solution (Ctrl+Shift+B)
```

**Using MSBuild (Command Line):**
```bash
# Build entire solution
msbuild Highlightengine.sln

# Build specific project
msbuild HighlightEngine/HighlightEngine.csproj
msbuild HighlightDemo/HighlightDemo.csproj
msbuild GenerateHtml/GenerateHtml.csproj
```

**Build Configurations:**
- `Debug` - Development build with debug symbols
- `Release` - Optimized production build

### Running the Applications

**HighlightDemo (WPF Application):**
```bash
# After building in Debug mode
HighlightDemo/bin/Debug/Org.Jinou.HighlightDemo.exe

# After building in Release mode
HighlightDemo/bin/Release/Org.Jinou.HighlightDemo.exe
```

**GenerateHtml (Console Tool):**
```bash
# Syntax: GenerateHtml.exe <input_file> <output_file>
GenerateHtml/bin/Debug/GenerateHtml.exe input.cs output.html
```

## Development Conventions

### Code Style
- **Namespace:** `Org.Jinou.HighlightEngine` for core engine
- **Language:** C# with .NET 3.5 features
- **Documentation:** XML documentation comments for public APIs
- **Naming:** PascalCase for public members, camelCase for private fields

### Project Structure
```
HighlightEngine/
├── Highlights/           # Language definition XML files
│   ├── csharp.xml
│   ├── cpp.xml
│   ├── javascript.xml
│   └── ...
├── HighlightParser.cs    # Main parsing logic
├── HighlightProfile.cs   # Profile data structures
├── HighlightProfileFactory.cs  # Profile loading
└── TextLineInfo.cs       # Line information model
```

### Language Profile Format
Language definitions are stored in XML files under `HighlightEngine/Highlights/`. Each profile defines:
- **Delimiters:** Characters that separate tokens
- **Keywords:** Language keywords with associated colors
- **Code Blocks:** Single-line and multi-line comment/string patterns
- **Tokens:** Regex-based patterns for numbers, preprocessor directives, etc.

Example structure (see `csharp.xml`):
```xml
<profile>
    <delimiter>...</delimiter>
    <ignoreCase>false</ignoreCase>
    <keywords name="keyword" foreground="Blue">
        <keyword>abstract</keyword>
        ...
    </keywords>
    <singleLineBlock name="comment" foreground="Green">
        <start>//</start>
        <end></end>
    </singleLineBlock>
    <token pattern="#.*" name="precompile" foreground="Gray" />
</profile>
```

### Testing
- **TODO:** No automated tests currently exist in the repository
- Manual testing via the HighlightDemo application
- Verify HTML output with GenerateHtml tool

### Dependencies
- **PresentationCore** - WPF core library
- **WindowsBase** - WPF base classes
- **PresentationFramework** - WPF UI framework (Demo only)
- **System.Xml.Linq** - XML parsing

## Key Files

| File | Purpose |
|------|---------|
| `Highlightengine.sln` | Visual Studio solution file |
| `HighlightEngine/HighlightParser.cs` | Core parsing algorithm |
| `HighlightEngine/HighlightProfile.cs` | Data structures for profiles |
| `HighlightEngine/Highlights/*.xml` | Language syntax definitions |
| `GenerateHtml/HtmlGenerator.cs` | HTML output generation |
| `HighlightDemo/MainWindow.cs` | WPF demo main window |

## Adding New Language Support

1. Create a new XML file in `HighlightEngine/Highlights/` (e.g., `python.xml`)
2. Define the profile structure following existing examples
3. Add the XML file to the HighlightEngine project as Content
4. The `HighlightProfileFactory` will automatically detect it by file extension

## Notes

- The project targets .NET Framework 3.5 (Visual Studio 2008 era)
- Uses WPF's `Brush` type for color definitions
- HTML generation converts WPF colors to hex format (#RRGGBB)
- The parser supports escape sequences in code blocks
- Case-insensitive matching is configurable per language profile
