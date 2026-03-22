#!/usr/bin/env python3
"""
语言配置文件生成器
批量生成 20 种流行编程语言的配置文件
"""

import os
import json
from pathlib import Path

# 语言配置数据
languages = {
    # 第一梯队：必须支持
    "java": {
        "name": "java",
        "extensions": [".java"],
        "delimiters": [" ", "\t", "(", ")", "{", "}", "[", "]", ";", ",", ".", "<", ">", "+", "-", "*", "/", "%", "&", "|", "^", "!", "~", "=", "?", ":", "@"],
        "ignore_case": False,
        "keywords": {
            "keyword": [
                "abstract", "assert", "boolean", "break", "byte", "case", "catch", "char", "class", "const",
                "continue", "default", "do", "double", "else", "enum", "extends", "final", "finally", "float",
                "for", "goto", "if", "implements", "import", "instanceof", "int", "interface", "long", "native",
                "new", "package", "private", "protected", "public", "return", "short", "static", "strictfp",
                "super", "switch", "synchronized", "this", "throw", "throws", "transient", "try", "void",
                "volatile", "while", "true", "false", "null", "var", "yield", "record", "sealed", "permits"
            ],
            "type": [
                "Boolean", "Byte", "Character", "Double", "Float", "Integer", "Long", "Short", "String",
                "Object", "Class", "System", "Thread", "Runnable", "Exception", "RuntimeException"
            ],
            "builtin": [
                "System", "Math", "Arrays", "Collections", "List", "ArrayList", "HashMap", "HashSet",
                "LinkedList", "TreeMap", "TreeSet", "Queue", "Stack", "Vector", "StringBuilder", "StringBuffer"
            ]
        },
        "single_line_blocks": [
            {"name": "comment", "start": "//", "end": ""},
            {"name": "string", "start": "\"", "end": "\"", "escape": {"escape_string": "\\", "items": ["\\\"", "\\\\"]}},
            {"name": "char", "start": "'", "end": "'", "escape": {"escape_string": "\\", "items": ["\\'", "\\\\"]}}
        ],
        "multi_line_blocks": [
            {"name": "multi-line comment", "start": "/*", "end": "*/"},
            {"name": "javadoc", "start": "/**", "end": "*/"}
        ],
        "tokens": [
            {"name": "number", "pattern": "\\b\\d+\\.?\\d*([eE][+-]?\\d+)?[fFdDlL]?\\b"},
            {"name": "hex number", "pattern": "\\b0[xX][0-9a-fA-F]+[lL]?\\b"},
            {"name": "octal number", "pattern": "\\b0[0-7]+[lL]?\\b"},
            {"name": "binary number", "pattern": "\\b0[bB][01]+[lL]?\\b"},
            {"name": "annotation", "pattern": "@\\w+"}
        ]
    },
    
    "typescript": {
        "name": "typescript",
        "extensions": [".ts", ".tsx"],
        "delimiters": [" ", "\t", "(", ")", "{", "}", "[", "]", ";", ",", ".", "<", ">", "+", "-", "*", "/", "%", "&", "|", "^", "!", "~", "=", "?", ":", "@"],
        "ignore_case": False,
        "keywords": {
            "keyword": [
                "abstract", "as", "async", "await", "break", "case", "catch", "class", "const", "constructor",
                "continue", "debugger", "declare", "default", "delete", "do", "else", "enum", "export", "extends",
                "finally", "for", "from", "function", "get", "if", "implements", "import", "in", "instanceof",
                "interface", "is", "keyof", "let", "module", "namespace", "new", "of", "package", "private",
                "protected", "public", "readonly", "require", "return", "set", "static", "super", "switch",
                "this", "throw", "try", "type", "typeof", "var", "void", "while", "with", "yield"
            ],
            "type": [
                "any", "boolean", "bigint", "never", "null", "number", "object", "string", "symbol",
                "undefined", "unknown", "void", "Array", "Map", "Set", "Promise", "Date", "RegExp"
            ],
            "builtin": [
                "console", "window", "document", "Math", "JSON", "Date", "Array", "Object", "String",
                "Number", "Boolean", "RegExp", "Error", "Map", "Set", "WeakMap", "WeakSet", "Promise",
                "Symbol", "Proxy", "Reflect", "Intl", "ArrayBuffer", "DataView", "Int8Array", "Int16Array"
            ]
        },
        "single_line_blocks": [
            {"name": "comment", "start": "//", "end": ""},
            {"name": "string", "start": "\"", "end": "\"", "escape": {"escape_string": "\\", "items": ["\\\"", "\\\\"]}},
            {"name": "string", "start": "'", "end": "'", "escape": {"escape_string": "\\", "items": ["\\'", "\\\\"]}},
            {"name": "template literal", "start": "`", "end": "`", "escape": {"escape_string": "\\", "items": ["\\`", "\\\\"]}}
        ],
        "multi_line_blocks": [
            {"name": "multi-line comment", "start": "/*", "end": "*/"}
        ],
        "tokens": [
            {"name": "number", "pattern": "\\b\\d+\\.?\\d*([eE][+-]?\\d+)?\\b"},
            {"name": "hex number", "pattern": "\\b0[xX][0-9a-fA-F]+\\b"},
            {"name": "octal number", "pattern": "\\b0[oO][0-7]+\\b"},
            {"name": "binary number", "pattern": "\\b0[bB][01]+\\b"},
            {"name": "regex", "pattern": "/[^/\\n]+/[gimsuy]*"},
            {"name": "arrow function", "pattern": "=>"},
            {"name": "spread operator", "pattern": "\\.\\.\\."},
            {"name": "optional chaining", "pattern": "\\?\\."},
            {"name": "nullish coalescing", "pattern": "\\?\\?"}
        ]
    },
    
    "go": {
        "name": "go",
        "extensions": [".go"],
        "delimiters": [" ", "\t", "(", ")", "{", "}", "[", "]", ";", ",", ".", "<", ">", "+", "-", "*", "/", "%", "&", "|", "^", "!", "~", "=", "?", ":", "@"],
        "ignore_case": False,
        "keywords": {
            "keyword": [
                "break", "case", "chan", "const", "continue", "default", "defer", "else", "fallthrough",
                "for", "func", "go", "goto", "if", "import", "interface", "map", "package", "range",
                "return", "select", "struct", "switch", "type", "var", "true", "false", "nil", "iota"
            ],
            "type": [
                "bool", "byte", "complex64", "complex128", "error", "float32", "float64", "int", "int8",
                "int16", "int32", "int64", "rune", "string", "uint", "uint8", "uint16", "uint32", "uint64",
                "uintptr", "any", "comparable"
            ],
            "builtin": [
                "append", "cap", "close", "complex", "copy", "delete", "imag", "len", "make", "new",
                "panic", "print", "println", "real", "recover", "error", "string", "int", "float64"
            ]
        },
        "single_line_blocks": [
            {"name": "comment", "start": "//", "end": ""},
            {"name": "string", "start": "\"", "end": "\"", "escape": {"escape_string": "\\", "items": ["\\\"", "\\\\"]}},
            {"name": "raw string", "start": "`", "end": "`"}
        ],
        "multi_line_blocks": [
            {"name": "multi-line comment", "start": "/*", "end": "*/"}
        ],
        "tokens": [
            {"name": "number", "pattern": "\\b\\d+\\.?\\d*([eE][+-]?\\d+)?\\b"},
            {"name": "hex number", "pattern": "\\b0[xX][0-9a-fA-F]+\\b"},
            {"name": "octal number", "pattern": "\\b0[0-7]+\\b"},
            {"name": "binary number", "pattern": "\\b0[bB][01]+\\b"},
            {"name": "imaginary number", "pattern": "\\b\\d+\\.?\\d*([eE][+-]?\\d+)?i\\b"}
        ]
    },
    
    "kotlin": {
        "name": "kotlin",
        "extensions": [".kt", ".kts"],
        "delimiters": [" ", "\t", "(", ")", "{", "}", "[", "]", ";", ",", ".", "<", ">", "+", "-", "*", "/", "%", "&", "|", "^", "!", "~", "=", "?", ":", "@"],
        "ignore_case": False,
        "keywords": {
            "keyword": [
                "abstract", "actual", "annotation", "as", "break", "by", "catch", "class", "companion",
                "const", "constructor", "continue", "crossinline", "data", "do", "else", "enum", "expect",
                "external", "false", "final", "finally", "for", "fun", "get", "if", "import", "in",
                "infix", "init", "inline", "inner", "interface", "internal", "is", "lateinit", "noinline",
                "null", "object", "open", "operator", "out", "override", "package", "private", "protected",
                "public", "reified", "return", "sealed", "set", "super", "suspend", "tailrec", "this",
                "throw", "true", "try", "typealias", "val", "var", "vararg", "when", "while", "yield"
            ],
            "type": [
                "Boolean", "Byte", "Char", "Double", "Float", "Int", "Long", "Short", "String",
                "Array", "List", "Map", "Set", "Pair", "Triple", "Unit", "Nothing", "Any", "Any?"
            ],
            "builtin": [
                "println", "print", "readLine", "readln", "TODO", "require", "check", "assert",
                "error", "listOf", "mapOf", "setOf", "arrayOf", "emptyList", "emptyMap", "emptySet"
            ]
        },
        "single_line_blocks": [
            {"name": "comment", "start": "//", "end": ""},
            {"name": "string", "start": "\"", "end": "\"", "escape": {"escape_string": "\\", "items": ["\\\"", "\\\\"]}},
            {"name": "char", "start": "'", "end": "'", "escape": {"escape_string": "\\", "items": ["\\'", "\\\\"]}}
        ],
        "multi_line_blocks": [
            {"name": "multi-line comment", "start": "/*", "end": "*/"},
            {"name": "kdoc", "start": "/**", "end": "*/"}
        ],
        "tokens": [
            {"name": "number", "pattern": "\\b\\d+\\.?\\d*([eE][+-]?\\d+)?[fFlL]?\\b"},
            {"name": "hex number", "pattern": "\\b0[xX][0-9a-fA-F]+[lL]?\\b"},
            {"name": "binary number", "pattern": "\\b0[bB][01]+[lL]?\\b"},
            {"name": "annotation", "pattern": "@\\w+"}
        ]
    },
    
    "swift": {
        "name": "swift",
        "extensions": [".swift"],
        "delimiters": [" ", "\t", "(", ")", "{", "}", "[", "]", ";", ",", ".", "<", ">", "+", "-", "*", "/", "%", "&", "|", "^", "!", "~", "=", "?", ":", "@"],
        "ignore_case": False,
        "keywords": {
            "keyword": [
                "associatedtype", "break", "case", "catch", "class", "continue", "convenience", "default",
                "defer", "deinit", "do", "else", "enum", "extension", "fallthrough", "false", "fileprivate",
                "final", "for", "func", "guard", "if", "import", "in", "indirect", "infix", "init", "inout",
                "internal", "is", "lazy", "let", "mutating", "nil", "nonmutating", "open", "operator",
                "optional", "override", "postfix", "precedencegroup", "prefix", "private", "protocol",
                "public", "repeat", "required", "rethrows", "return", "self", "Self", "set", "some", "static",
                "struct", "subscript", "super", "switch", "throw", "throws", "true", "try", "typealias",
                "unowned", "var", "weak", "where", "while", "willSet", "didSet", "get", "set"
            ],
            "type": [
                "Bool", "Int", "Int8", "Int16", "Int32", "Int64", "UInt", "UInt8", "UInt16", "UInt32",
                "UInt64", "Float", "Double", "String", "Character", "Array", "Dictionary", "Set", "Optional",
                "Result", "Error", "Any", "AnyObject", "Void"
            ],
            "builtin": [
                "print", "debugPrint", "dump", "fatalError", "precondition", "preconditionFailure",
                "assert", "assertionFailure", "abs", "min", "max", "zip", "map", "filter", "reduce",
                "forEach", "contains", "sorted", "reversed", "enumerated", "first", "last", "count"
            ]
        },
        "single_line_blocks": [
            {"name": "comment", "start": "//", "end": ""},
            {"name": "string", "start": "\"", "end": "\"", "escape": {"escape_string": "\\", "items": ["\\\"", "\\\\"]}}
        ],
        "multi_line_blocks": [
            {"name": "multi-line comment", "start": "/*", "end": "*/"},
            {"name": "documentation", "start": "/**", "end": "*/"}
        ],
        "tokens": [
            {"name": "number", "pattern": "\\b\\d+\\.?\\d*([eE][+-]?\\d+)?[fF]?\\b"},
            {"name": "hex number", "pattern": "\\b0[xX][0-9a-fA-F]+\\b"},
            {"name": "octal number", "pattern": "\\b0[oO][0-7]+\\b"},
            {"name": "binary number", "pattern": "\\b0[bB][01]+\\b"},
            {"name": "interpolation", "pattern": "\\\\\\(.*?\\)"}
        ]
    },
    
    "php": {
        "name": "php",
        "extensions": [".php", ".phtml", ".php3", ".php4", ".php5", ".php7", ".phps"],
        "delimiters": [" ", "\t", "(", ")", "{", "}", "[", "]", ";", ",", ".", "<", ">", "+", "-", "*", "/", "%", "&", "|", "^", "!", "~", "=", "?", ":", "$", "@"],
        "ignore_case": True,
        "keywords": {
            "keyword": [
                "abstract", "and", "array", "as", "break", "callable", "case", "catch", "class", "clone",
                "const", "continue", "declare", "default", "die", "do", "echo", "else", "elseif", "empty",
                "enddeclare", "endfor", "endforeach", "endif", "endswitch", "endwhile", "eval", "exit",
                "extends", "final", "finally", "fn", "for", "foreach", "function", "global", "goto", "if",
                "implements", "include", "include_once", "instanceof", "insteadof", "interface", "isset",
                "list", "match", "namespace", "new", "or", "print", "private", "protected", "public",
                "readonly", "require", "require_once", "return", "static", "switch", "throw", "trait",
                "try", "unset", "use", "var", "while", "xor", "yield", "yield from", "true", "false", "null"
            ],
            "type": [
                "bool", "int", "float", "string", "array", "object", "resource", "null", "mixed",
                "numeric", "scalar", "void", "never", "false", "true", "self", "parent", "static"
            ],
            "builtin": [
                "isset", "unset", "empty", "die", "exit", "echo", "print", "array", "list", "each",
                "key", "current", "next", "prev", "reset", "end", "count", "sizeof", "in_array",
                "array_push", "array_pop", "array_shift", "array_unshift", "array_merge", "array_slice"
            ]
        },
        "single_line_blocks": [
            {"name": "comment", "start": "//", "end": ""},
            {"name": "comment", "start": "#", "end": ""},
            {"name": "string", "start": "\"", "end": "\"", "escape": {"escape_string": "\\", "items": ["\\\"", "\\\\"]}},
            {"name": "string", "start": "'", "end": "'", "escape": {"escape_string": "\\", "items": ["\\'", "\\\\"]}}
        ],
        "multi_line_blocks": [
            {"name": "multi-line comment", "start": "/*", "end": "*/"},
            {"name": "heredoc", "start": "<<<", "end": ""}
        ],
        "tokens": [
            {"name": "number", "pattern": "\\b\\d+\\.?\\d*([eE][+-]?\\d+)?\\b"},
            {"name": "hex number", "pattern": "\\b0[xX][0-9a-fA-F]+\\b"},
            {"name": "octal number", "pattern": "\\b0[0-7]+\\b"},
            {"name": "variable", "pattern": "\\$[a-zA-Z_\\x7f-\\xff][a-zA-Z0-9_\\x7f-\\xff]*"},
            {"name": "heredoc", "pattern": "<<<[a-zA-Z_\\x7f-\\xff]"}
        ]
    },
    
    "ruby": {
        "name": "ruby",
        "extensions": [".rb", ".rbw", ".rake", ".gemspec", ".ru"],
        "delimiters": [" ", "\t", "(", ")", "{", "}", "[", "]", ";", ",", ".", "<", ">", "+", "-", "*", "/", "%", "&", "|", "^", "!", "~", "=", "?", ":", "@"],
        "ignore_case": False,
        "keywords": {
            "keyword": [
                "BEGIN", "END", "alias", "and", "begin", "break", "case", "class", "def", "defined?",
                "do", "else", "elsif", "end", "ensure", "false", "for", "if", "in", "module", "next",
                "nil", "not", "or", "redo", "rescue", "retry", "return", "self", "super", "then",
                "true", "undef", "unless", "until", "when", "while", "yield", "__FILE__", "__LINE__"
            ],
            "type": [
                "Array", "Hash", "String", "Integer", "Float", "Numeric", "Fixnum", "Bignum",
                "Symbol", "Regexp", "Range", "IO", "File", "Dir", "Time", "Proc", "Method",
                "Class", "Module", "Object", "BasicObject", "TrueClass", "FalseClass", "NilClass"
            ],
            "builtin": [
                "puts", "print", "p", "pp", "printf", "sprintf", "gets", "readline", "readlines",
                "require", "require_relative", "load", "include", "extend", "prepend", "raise",
                "fail", "throw", "catch", "loop", "each", "map", "select", "reject", "reduce"
            ]
        },
        "single_line_blocks": [
            {"name": "comment", "start": "#", "end": ""},
            {"name": "string", "start": "\"", "end": "\"", "escape": {"escape_string": "\\", "items": ["\\\"", "\\\\"]}},
            {"name": "string", "start": "'", "end": "'", "escape": {"escape_string": "\\", "items": ["\\'", "\\\\"]}}
        ],
        "multi_line_blocks": [
            {"name": "multi-line comment", "start": "=begin", "end": "=end"},
            {"name": "heredoc", "start": "<<", "end": ""}
        ],
        "tokens": [
            {"name": "number", "pattern": "\\b\\d+\\.?\\d*([eE][+-]?\\d+)?\\b"},
            {"name": "hex number", "pattern": "\\b0[xX][0-9a-fA-F]+\\b"},
            {"name": "octal number", "pattern": "\\b0[0-7]+\\b"},
            {"name": "binary number", "pattern": "\\b0[bB][01]+\\b"},
            {"name": "symbol", "pattern": ":[a-zA-Z_][a-zA-Z0-9_]*[?!]?"},
            {"name": "instance variable", "pattern": "@[a-zA-Z_][a-zA-Z0-9_]*"},
            {"name": "class variable", "pattern": "@@[a-zA-Z_][a-zA-Z0-9_]*"},
            {"name": "global variable", "pattern": "\\$[a-zA-Z_][a-zA-Z0-9_]*"}
        ]
    },
    
    "scala": {
        "name": "scala",
        "extensions": [".scala", ".sc"],
        "delimiters": [" ", "\t", "(", ")", "{", "}", "[", "]", ";", ",", ".", "<", ">", "+", "-", "*", "/", "%", "&", "|", "^", "!", "~", "=", "?", ":", "@"],
        "ignore_case": False,
        "keywords": {
            "keyword": [
                "abstract", "case", "catch", "class", "def", "do", "else", "extends", "false", "final",
                "finally", "for", "forSome", "if", "implicit", "import", "lazy", "match", "new", "null",
                "object", "override", "package", "private", "protected", "return", "sealed", "super",
                "this", "throw", "trait", "true", "try", "type", "val", "var", "while", "with", "yield",
                "given", "using", "export", "enum", "then", "else"
            ],
            "type": [
                "Boolean", "Byte", "Char", "Double", "Float", "Int", "Long", "Short", "String",
                "Unit", "Any", "AnyRef", "AnyVal", "Nothing", "Null", "Option", "Some", "None",
                "List", "Map", "Set", "Array", "Vector", "Seq", "Iterator", "Future", "Promise"
            ],
            "builtin": [
                "println", "print", "printf", "readLine", "readInt", "readDouble", "require",
                "assert", "assume", "ensuring", "implicitly", "identity", "locally", "???", "summon"
            ]
        },
        "single_line_blocks": [
            {"name": "comment", "start": "//", "end": ""},
            {"name": "string", "start": "\"", "end": "\"", "escape": {"escape_string": "\\", "items": ["\\\"", "\\\\"]}}
        ],
        "multi_line_blocks": [
            {"name": "multi-line comment", "start": "/*", "end": "*/"},
            {"name": "multi-line string", "start": "\"\"\"", "end": "\"\"\""}
        ],
        "tokens": [
            {"name": "number", "pattern": "\\b\\d+\\.?\\d*([eE][+-]?\\d+)?[fFdDlL]?\\b"},
            {"name": "hex number", "pattern": "\\b0[xX][0-9a-fA-F]+[lL]?\\b"},
            {"name": "annotation", "pattern": "@\\w+"}
        ]
    },
    
    "r": {
        "name": "r",
        "extensions": [".r", ".R", ".Rhistory", ".Rprofile", ".RData"],
        "delimiters": [" ", "\t", "(", ")", "{", "}", "[", "]", ";", ",", ".", "<", ">", "+", "-", "*", "/", "%", "&", "|", "^", "!", "~", "=", "?", ":", "$", "@"],
        "ignore_case": True,
        "keywords": {
            "keyword": [
                "if", "else", "repeat", "while", "function", "for", "in", "next", "break", "TRUE",
                "FALSE", "NULL", "Inf", "NaN", "NA", "NA_integer_", "NA_real_", "NA_complex_",
                "NA_character_", "return", "invisible", "library", "require", "source", "setwd",
                "getwd", "q", "quit", "save", "load", "rm", "remove", "ls", "objects", "class"
            ],
            "type": [
                "numeric", "double", "integer", "complex", "character", "logical", "raw", "list",
                "vector", "matrix", "array", "factor", "data.frame", "table", "formula", "function",
                "environment", "externalptr", "bytecode", "symbol", "pairlist", "promise", "language"
            ],
            "builtin": [
                "c", "length", "nrow", "ncol", "dim", "names", "colnames", "rownames", "class",
                "typeof", "mode", "str", "summary", "print", "cat", "paste", "paste0", "sprintf",
                "substr", "substring", "nchar", "tolower", "toupper", "trimws", "gsub", "sub"
            ]
        },
        "single_line_blocks": [
            {"name": "comment", "start": "#", "end": ""},
            {"name": "string", "start": "\"", "end": "\"", "escape": {"escape_string": "\\", "items": ["\\\"", "\\\\"]}},
            {"name": "string", "start": "'", "end": "'", "escape": {"escape_string": "\\", "items": ["\\'", "\\\\"]}}
        ],
        "multi_line_blocks": [],
        "tokens": [
            {"name": "number", "pattern": "\\b\\d+\\.?\\d*([eE][+-]?\\d+)?[iL]?\\b"},
            {"name": "hex number", "pattern": "\\b0[xX][0-9a-fA-F]+\\b"},
            {"name": "special", "pattern": "%[^%]+%"},
            {"name": "slot", "pattern": "@\\w+"}
        ]
    },
    
    "matlab": {
        "name": "matlab",
        "extensions": [".m"],
        "delimiters": [" ", "\t", "(", ")", "{", "}", "[", "]", ";", ",", ".", "<", ">", "+", "-", "*", "/", "%", "&", "|", "^", "!", "~", "=", "?", ":", "@"],
        "ignore_case": True,
        "keywords": {
            "keyword": [
                "break", "case", "catch", "classdef", "continue", "else", "elseif", "end", "for",
                "function", "global", "if", "otherwise", "parfor", "persistent", "return", "spmd",
                "switch", "try", "while", "methods", "properties", "events", "arguments", "enumeration"
            ],
            "type": [
                "double", "single", "int8", "int16", "int32", "int64", "uint8", "uint16", "uint32",
                "uint64", "logical", "char", "string", "cell", "struct", "table", "timetable",
                "function_handle", "categorical", "datetime", "duration", "calendarDuration"
            ],
            "builtin": [
                "disp", "display", "fprintf", "sprintf", "num2str", "int2str", "mat2str", "str2num",
                "str2double", "length", "size", "numel", "ndims", "iscolumn", "isrow", "ismatrix",
                "isempty", "isequal", "isequaln", "isnumeric", "isfloat", "isinteger", "islogical"
            ]
        },
        "single_line_blocks": [
            {"name": "comment", "start": "%", "end": ""},
            {"name": "string", "start": "'", "end": "'", "escape": {"escape_string": "'", "items": ["''"]}},
            {"name": "string", "start": "\"", "end": "\"", "escape": {"escape_string": "\\", "items": ["\\\"", "\\\\"]}}
        ],
        "multi_line_blocks": [
            {"name": "block comment", "start": "%{", "end": "%}"}
        ],
        "tokens": [
            {"name": "number", "pattern": "\\b\\d+\\.?\\d*([eE][+-]?\\d+)?[i]?\\b"},
            {"name": "hex number", "pattern": "\\b0[xX][0-9a-fA-F]+\\b"},
            {"name": "command", "pattern": "![^\\n]+"},
            {"name": "metaclass", "pattern": "\\?\\w+"}
        ]
    },

    # 第二梯队：重要支持
    "lua": {
        "name": "lua",
        "extensions": [".lua"],
        "delimiters": [" ", "\t", "(", ")", "{", "}", "[", "]", ";", ",", ".", "<", ">", "+", "-", "*", "/", "%", "&", "|", "^", "!", "~", "=", "?", ":", "#"],
        "ignore_case": False,
        "keywords": {
            "keyword": [
                "and", "break", "do", "else", "elseif", "end", "false", "for", "function", "goto",
                "if", "in", "local", "nil", "not", "or", "repeat", "return", "then", "true", "until", "while"
            ],
            "type": [
                "nil", "boolean", "number", "string", "function", "userdata", "thread", "table"
            ],
            "builtin": [
                "assert", "collectgarbage", "dofile", "error", "getmetatable", "ipairs", "load",
                "loadfile", "next", "pairs", "pcall", "print", "rawequal", "rawget", "rawlen",
                "rawset", "require", "select", "setmetatable", "tonumber", "tostring", "type", "warn"
            ]
        },
        "single_line_blocks": [
            {"name": "comment", "start": "--", "end": ""},
            {"name": "string", "start": "\"", "end": "\"", "escape": {"escape_string": "\\", "items": ["\\\"", "\\\\"]}},
            {"name": "string", "start": "'", "end": "'", "escape": {"escape_string": "\\", "items": ["\\'", "\\\\"]}}
        ],
        "multi_line_blocks": [
            {"name": "multi-line comment", "start": "--[[", "end": "]]"},
            {"name": "multi-line string", "start": "[[", "end": "]]"}
        ],
        "tokens": [
            {"name": "number", "pattern": "\\b\\d+\\.?\\d*([eE][+-]?\\d+)?\\b"},
            {"name": "hex number", "pattern": "\\b0[xX][0-9a-fA-F]+\\b"},
            {"name": "vararg", "pattern": "\\.\\.\\."}
        ]
    },

    "perl": {
        "name": "perl",
        "extensions": [".pl", ".pm", ".t", ".pod"],
        "delimiters": [" ", "\t", "(", ")", "{", "}", "[", "]", ";", ",", ".", "<", ">", "+", "-", "*", "/", "%", "&", "|", "^", "!", "~", "=", "?", ":", "$", "@", "%"],
        "ignore_case": False,
        "keywords": {
            "keyword": [
                "abs", "accept", "alarm", "and", "atan2", "bind", "binmode", "bless", "break",
                "caller", "chdir", "chmod", "chomp", "chop", "chown", "chr", "chroot", "close",
                "closedir", "connect", "continue", "cos", "crypt", "dbmclose", "dbmopen", "defined",
                "delete", "die", "do", "dump", "each", "else", "elsif", "endgrent", "endhostent",
                "endnetent", "endprotoent", "endpwent", "endservent", "eof", "eval", "exec", "exists",
                "exit", "exp", "fcntl", "fileno", "flock", "for", "foreach", "fork", "format",
                "formline", "getc", "getgrent", "getgrgid", "getgrnam", "gethostbyaddr", "gethostbyname",
                "gethostent", "getlogin", "getnetbyaddr", "getnetbyname", "getnetent", "getpeername",
                "getpgrp", "getppid", "getpriority", "getprotobyname", "getprotobynumber", "getprotoent",
                "getpwent", "getpwnam", "getpwuid", "getservbyname", "getservbyport", "getservent",
                "getsockname", "getsockopt", "glob", "gmtime", "goto", "grep", "hex", "if", "import",
                "index", "int", "ioctl", "join", "keys", "kill", "last", "lc", "lcfirst", "length",
                "link", "listen", "local", "localtime", "log", "lstat", "map", "mkdir", "msgctl",
                "msgget", "msgrcv", "msgsnd", "my", "next", "no", "not", "oct", "open", "opendir",
                "or", "ord", "our", "pack", "package", "pipe", "pop", "pos", "print", "printf",
                "prototype", "push", "quotemeta", "rand", "read", "readdir", "readline", "readlink",
                "readpipe", "recv", "redo", "ref", "rename", "require", "reset", "return", "reverse",
                "rewinddir", "rindex", "rmdir", "say", "scalar", "seek", "seekdir", "select", "semctl",
                "semget", "semop", "send", "setgrent", "sethostent", "setnetent", "setpgrp",
                "setpriority", "setprotoent", "setpwent", "setservent", "setsockopt", "shift", "shmctl",
                "shmget", "shmread", "shmwrite", "shutdown", "sin", "sleep", "socket", "socketpair",
                "sort", "splice", "split", "sprintf", "sqrt", "srand", "stat", "state", "study",
                "sub", "substr", "symlink", "syscall", "sysopen", "sysread", "sysseek", "system",
                "syswrite", "tell", "telldir", "tie", "tied", "time", "times", "truncate", "uc",
                "ucfirst", "umask", "undef", "unless", "unlink", "unpack", "unshift", "untie", "until",
                "use", "utime", "values", "vec", "wait", "waitpid", "wantarray", "warn", "while",
                "write", "xor"
            ],
            "type": [
                "ARRAY", "CODE", "FORMAT", "GLOB", "HASH", "IO", "LVALUE", "REF", "REGEXP",
                "SCALAR", "VFILE", "main", "CORE", "UNIVERSAL"
            ],
            "builtin": [
                "AUTOLOAD", "BEGIN", "CHECK", "DESTROY", "END", "INIT", "UNITCHECK", "abs",
                "accept", "alarm", "and", "atan2", "bind", "binmode", "bless", "break", "caller",
                "chdir", "chmod", "chomp", "chop", "chown", "chr", "chroot", "close", "closedir"
            ]
        },
        "single_line_blocks": [
            {"name": "comment", "start": "#", "end": ""},
            {"name": "string", "start": "\"", "end": "\"", "escape": {"escape_string": "\\", "items": ["\\\"", "\\\\"]}},
            {"name": "string", "start": "'", "end": "'", "escape": {"escape_string": "\\", "items": ["\\'", "\\\\"]}}
        ],
        "multi_line_blocks": [
            {"name": "multi-line comment", "start": "=", "end": "=cut"},
            {"name": "heredoc", "start": "<<", "end": ""}
        ],
        "tokens": [
            {"name": "number", "pattern": "\\b\\d+\\.?\\d*([eE][+-]?\\d+)?\\b"},
            {"name": "hex number", "pattern": "\\b0[xX][0-9a-fA-F]+\\b"},
            {"name": "octal number", "pattern": "\\b0[0-7]+\\b"},
            {"name": "scalar", "pattern": "\\$[a-zA-Z_\\x7f-\\xff][a-zA-Z0-9_\\x7f-\\xff]*"},
            {"name": "array", "pattern": "@[a-zA-Z_\\x7f-\\xff][a-zA-Z0-9_\\x7f-\\xff]*"},
            {"name": "hash", "pattern": "%[a-zA-Z_\\x7f-\\xff][a-zA-Z0-9_\\x7f-\\xff]*"},
            {"name": "glob", "pattern": "\\*[a-zA-Z_\\x7f-\\xff][a-zA-Z0-9_\\x7f-\\xff]*"}
        ]
    },

    "haskell": {
        "name": "haskell",
        "extensions": [".hs", ".lhs"],
        "delimiters": [" ", "\t", "(", ")", "{", "}", "[", "]", ";", ",", ".", "<", ">", "+", "-", "*", "/", "%", "&", "|", "^", "!", "~", "=", "?", ":", "@", "#"],
        "ignore_case": False,
        "keywords": {
            "keyword": [
                "case", "class", "data", "default", "deriving", "do", "else", "if", "import", "in",
                "infix", "infixl", "infixr", "instance", "let", "module", "newtype", "of", "then",
                "type", "where", "forall", "mdo", "family", "role", "pattern", "static", "stock",
                "anyclass", "via", "proc", "rec", "qualified", "as", "hiding"
            ],
            "type": [
                "Bool", "Char", "Double", "Float", "Int", "Integer", "String", "Maybe", "Either",
                "IO", "Ordering", "ReadS", "ShowS", "FilePath", "IOError", "Functor", "Applicative",
                "Monad", "MonadPlus", "Foldable", "Traversable", "Show", "Read", "Eq", "Ord"
            ],
            "builtin": [
                "print", "putStr", "putStrLn", "getLine", "readFile", "writeFile", "appendFile",
                "interact", "readIO", "readLn", "show", "read", "reads", "id", "const", "flip",
                "map", "filter", "foldr", "foldl", "scanr", "scanl", "iterate", "repeat", "replicate"
            ]
        },
        "single_line_blocks": [
            {"name": "comment", "start": "--", "end": ""},
            {"name": "string", "start": "\"", "end": "\"", "escape": {"escape_string": "\\", "items": ["\\\"", "\\\\"]}},
            {"name": "char", "start": "'", "end": "'", "escape": {"escape_string": "\\", "items": ["\\'", "\\\\"]}}
        ],
        "multi_line_blocks": [
            {"name": "multi-line comment", "start": "{-", "end": "-}"}
        ],
        "tokens": [
            {"name": "number", "pattern": "\\b\\d+\\.?\\d*([eE][+-]?\\d+)?\\b"},
            {"name": "hex number", "pattern": "\\b0[xX][0-9a-fA-F]+\\b"},
            {"name": "octal number", "pattern": "\\b0[oO][0-7]+\\b"},
            {"name": "operator", "pattern": "[!#$%&*+./<=>?@\\\\^|~-]+"}
        ]
    },

    "erlang": {
        "name": "erlang",
        "extensions": [".erl", ".hrl"],
        "delimiters": [" ", "\t", "(", ")", "{", "}", "[", "]", ";", ",", ".", "<", ">", "+", "-", "*", "/", "%", "&", "|", "^", "!", "~", "=", "?", ":", "#"],
        "ignore_case": False,
        "keywords": {
            "keyword": [
                "after", "begin", "case", "catch", "cond", "end", "fun", "if", "let", "of",
                "query", "receive", "try", "when", "andalso", "orelse", "maybe", "else"
            ],
            "type": [
                "atom", "binary", "bitstring", "boolean", "byte", "char", "float", "fun",
                "function", "integer", "iodata", "iolist", "list", "map", "maybe_improper_list",
                "mfa", "module", "neg_integer", "nil", "node", "non_neg_integer", "nonempty_list",
                "nonempty_improper_list", "nonempty_maybe_improper_list", "number", "pid",
                "port", "pos_integer", "reference", "string", "term", "timeout", "tuple"
            ],
            "builtin": [
                "abs", "apply", "atom_to_binary", "atom_to_list", "binary_part", "binary_to_atom",
                "binary_to_existing_atom", "binary_to_float", "binary_to_integer", "binary_to_list",
                "binary_to_term", "bit_size", "bitstring_to_list", "byte_size", "ceil", "check_process_code",
                "delete_module", "element", "erase", "exit", "float", "float_to_binary", "float_to_list",
                "floor", "garbage_collect", "get", "get_keys", "group_leader", "halt", "hd", "integer_to_binary"
            ]
        },
        "single_line_blocks": [
            {"name": "comment", "start": "%", "end": ""},
            {"name": "string", "start": "\"", "end": "\"", "escape": {"escape_string": "\\", "items": ["\\\"", "\\\\"]}}
        ],
        "multi_line_blocks": [],
        "tokens": [
            {"name": "number", "pattern": "\\b\\d+\\.?\\d*([eE][+-]?\\d+)?\\b"},
            {"name": "hex number", "pattern": "\\b#[xX][0-9a-fA-F]+\\b"},
            {"name": "base number", "pattern": "\\b\\d+#[0-9a-zA-Z]+\\b"},
            {"name": "atom", "pattern": "'[^']*'"},
            {"name": "variable", "pattern": "[A-Z_][a-zA-Z0-9_]*"},
            {"name": "pid", "pattern": "<\\d+\\.\\d+\\.\\d+>"}
        ]
    },

    "clojure": {
        "name": "clojure",
        "extensions": [".clj", ".cljs", ".cljc", ".edn"],
        "delimiters": [" ", "\t", "(", ")", "{", "}", "[", "]", ";", ",", ".", "<", ">", "+", "-", "*", "/", "%", "&", "|", "^", "!", "~", "=", "?", ":", "#", "@"],
        "ignore_case": False,
        "keywords": {
            "keyword": [
                "def", "defn", "defn-", "defmacro", "defmulti", "defmethod", "defonce", "defrecord",
                "deftype", "defprotocol", "definterface", "defstruct", "let", "letfn", "binding",
                "if", "if-let", "if-not", "if-some", "when", "when-let", "when-not", "when-some",
                "cond", "condp", "cond->", "cond->>", "case", "do", "fn", "loop", "recur", "throw",
                "try", "catch", "finally", "dotimes", "doseq", "dorun", "doall", "for", "comment",
                "declare", "ns", "in-ns", "refer", "use", "require", "import", "load", "compile",
                "gen-class", "gen-interface", "proxy", "reify", "deftype", "defrecord", "extend-type",
                "extend-protocol", "specify", "specify!", "this-as", "memfn", "bean", "agent", "send",
                "send-off", "send-via", "await", "await-for", "future", "future-call", "future-cancel",
                "future-cancelled?", "future-done?", "future?", "delay", "delay?, force", "promise",
                "deliver", "pvalues", "pmap", "pcalls", "pmap", "seque", "promise-chan", "thread",
                "thread-call", "go", "go-loop", "alt!", "alt!!", "timeout", "chan", "close!", "put!",
                "take!", "offer!", "poll!", "unsub", "unsub-all", "pub", "sub", "mix", "admix",
                "unmix", "unmix-all", "toggle", "solo-mode", "tap", "untap", "untap-all", "mult",
                "tap", "untap", "untap-all", "clone", "clone-fn", "merge", "merge-with", "select-keys",
                "get", "get-in", "assoc", "assoc-in", "update", "update-in", "dissoc", "contains?",
                "keys", "vals", "map", "mapv", "reduce", "filter", "remove", "take", "take-while",
                "drop", "drop-while", "sort", "sort-by", "partition", "partition-all", "partition-by",
                "group-by", "frequencies", "distinct", "dedupe", "interpose", "interleave", "zipmap",
                "into", "flatten", "reverse", "comp", "complement", "constantly", "identity", "memoize",
                "trampoline", "apply", "partial", "juxt", "comp", "complement", "constantly", "identity",
                "memoize", "trampoline", "apply", "partial", "juxt", "fnil", "every?", "not-every?",
                "some", "not-any?", "seq", "sequence", "seq?", "sequential?", "list", "list?", "vector",
                "vector?", "map?", "set?", "number?", "integer?", "float?", "ratio?", "decimal?",
                "rational?", "neg?", "pos?", "zero?", "even?", "odd?", "true?", "false?", "nil?",
                "string?", "symbol?", "keyword?", "char?", "regex?", "var?", "ifn?", "fn?",
                "coll?", "associative?", "indexed?", "counted?", "empty?", "not-empty", "count",
                "nth", "first", "rest", "next", "last", "butlast", "conj", "cons", "concat", "lazy-seq",
                "lazy-cat", "delay", "delay?, force", "promise", "deliver", "pvalues", "pmap", "pcalls",
                "pmap", "seque", "promise-chan", "thread", "thread-call", "go", "go-loop", "alt!",
                "alt!!", "timeout", "chan", "close!", "put!", "take!", "offer!", "poll!", "unsub",
                "unsub-all", "pub", "sub", "mix", "admix", "unmix", "unmix-all", "toggle", "solo-mode",
                "tap", "untap", "untap-all", "mult", "tap", "untap", "untap-all", "clone", "clone-fn"
            ],
            "type": [
                "String", "Long", "Double", "Boolean", "Character", "Integer", "Float", "Short",
                "Byte", "BigDecimal", "BigInteger", "Ratio", "Pattern", "Matcher", "File", "URI",
                "URL", "UUID", "Date", "Calendar", "TimeZone", "SimpleDateFormat", "Number",
                "Comparable", "Serializable", "Cloneable", "Iterable", "Collection", "List", "ArrayList",
                "LinkedList", "Vector", "Stack", "Queue", "Deque", "ArrayDeque", "Set", "HashSet",
                "TreeSet", "LinkedHashSet", "SortedSet", "NavigableSet", "Map", "HashMap", "TreeMap",
                "LinkedHashMap", "SortedMap", "NavigableMap", "ConcurrentHashMap", "AtomicInteger",
                "AtomicLong", "AtomicBoolean", "AtomicReference", "CountDownLatch", "CyclicBarrier",
                "Semaphore", "ReentrantLock", "Condition", "Future", "Callable", "Runnable", "Thread",
                "Executor", "ExecutorService", "ThreadPoolExecutor", "ScheduledExecutorService"
            ],
            "builtin": [
                "println", "print", "pr", "prn", "printf", "format", "str", "string?", "string=",
                "string<", "string>", "string<=", "string>=", "string-ci=", "string-ci<", "string-ci>",
                "string-ci<=", "string-ci>=", "substring", "string-append", "string->list", "list->string",
                "string-copy", "string-copy!", "string-fill!", "string-upcase", "string-downcase",
                "string-titlecase", "string-trim", "string-trim-left", "string-trim-right", "string-split",
                "string-join", "string-replace", "string-index", "string-index-right", "string-skip",
                "string-skip-right", "string-count", "string-pad", "string-pad-right", "string-take",
                "string-take-right", "string-drop", "string-drop-right", "string-filter", "string-remove",
                "string-map", "string-for-each", "string-fold", "string-fold-right", "string-unfold",
                "string-unfold-right", "string-any", "string-every", "string-tabulate", "string-concatenate",
                "string-concatenate-reverse", "string-join", "string-split", "string-replace", "string-replace-all"
            ]
        },
        "single_line_blocks": [
            {"name": "comment", "start": ";", "end": ""},
            {"name": "string", "start": "\"", "end": "\"", "escape": {"escape_string": "\\", "items": ["\\\"", "\\\\"]}}
        ],
        "multi_line_blocks": [
            {"name": "comment", "start": "#_(", "end": ")"},
            {"name": "comment", "start": "#_\\{", "end": "\\}"},
            {"name": "comment", "start": "#_[", "end": "]"}
        ],
        "tokens": [
            {"name": "number", "pattern": "\\b\\d+\\.?\\d*([eE][+-]?\\d+)?[MN]?\\b"},
            {"name": "hex number", "pattern": "\\b0[xX][0-9a-fA-F]+\\b"},
            {"name": "octal number", "pattern": "\\b0[0-7]+\\b"},
            {"name": "radix number", "pattern": "\\b\\d+r[0-9a-zA-Z]+\\b"},
            {"name": "keyword", "pattern": ":[a-zA-Z*+!_?-][a-zA-Z0-9*+!_?-]*"},
            {"name": "symbol", "pattern": "[a-zA-Z*+!_?-][a-zA-Z0-9*+!_?-]*"},
            {"name": "metadata", "pattern": "\\^[a-zA-Z*+!_?-][a-zA-Z0-9*+!_?-]*"},
            {"name": "reader macro", "pattern": "#[a-zA-Z*+!_?-][a-zA-Z0-9*+!_?-]*"}
        ]
    },

    # 第三梯队：特色支持
    "dart": {
        "name": "dart",
        "extensions": [".dart"],
        "delimiters": [" ", "\t", "(", ")", "{", "}", "[", "]", ";", ",", ".", "<", ">", "+", "-", "*", "/", "%", "&", "|", "^", "!", "~", "=", "?", ":", "@"],
        "ignore_case": False,
        "keywords": {
            "keyword": [
                "abstract", "as", "assert", "async", "await", "break", "case", "catch", "class",
                "const", "continue", "covariant", "default", "deferred", "do", "dynamic", "else",
                "enum", "export", "extends", "extension", "external", "factory", "false", "final",
                "finally", "for", "Function", "get", "hide", "if", "implements", "import", "in",
                "interface", "is", "late", "library", "mixin", "new", "null", "on", "operator",
                "part", "required", "rethrow", "return", "set", "show", "static", "super", "switch",
                "sync", "this", "throw", "true", "try", "typedef", "var", "void", "while", "with", "yield"
            ],
            "type": [
                "bool", "double", "int", "num", "String", "List", "Map", "Set", "Iterable",
                "Iterator", "Future", "Stream", "dynamic", "void", "Never", "Object?", "Null"
            ],
            "builtin": [
                "print", "identical", "identityHashCode", "runtimeType", "toString", "hashCode",
                "length", "isEmpty", "isNotEmpty", "contains", "indexOf", "lastIndexOf", "substring",
                "trim", "trimLeft", "trimRight", "toUpperCase", "toLowerCase", "replaceAll", "split"
            ]
        },
        "single_line_blocks": [
            {"name": "comment", "start": "//", "end": ""},
            {"name": "string", "start": "\"", "end": "\"", "escape": {"escape_string": "\\", "items": ["\\\"", "\\\\"]}},
            {"name": "string", "start": "'", "end": "'", "escape": {"escape_string": "\\", "items": ["\\'", "\\\\"]}}
        ],
        "multi_line_blocks": [
            {"name": "multi-line comment", "start": "/*", "end": "*/"},
            {"name": "documentation", "start": "/**", "end": "*/"},
            {"name": "multi-line string", "start": "\"\"\"", "end": "\"\"\""},
            {"name": "multi-line string", "start": "'''", "end": "'''"}
        ],
        "tokens": [
            {"name": "number", "pattern": "\\b\\d+\\.?\\d*([eE][+-]?\\d+)?\\b"},
            {"name": "hex number", "pattern": "\\b0[xX][0-9a-fA-F]+\\b"},
            {"name": "symbol", "pattern": "#[a-zA-Z_][a-zA-Z0-9_]*"},
            {"name": "annotation", "pattern": "@\\w+"}
        ]
    },

    "elixir": {
        "name": "elixir",
        "extensions": [".ex", ".exs"],
        "delimiters": [" ", "\t", "(", ")", "{", "}", "[", "]", ";", ",", ".", "<", ">", "+", "-", "*", "/", "%", "&", "|", "^", "!", "~", "=", "?", ":", "@"],
        "ignore_case": False,
        "keywords": {
            "keyword": [
                "after", "and", "case", "catch", "cond", "def", "defp", "defmodule", "defprotocol",
                "defimpl", "defrecord", "defstruct", "defexception", "defdelegate", "defoverridable",
                "defguard", "defmacro", "defmacrop", "do", "else", "end", "fn", "for", "if", "import",
                "in", "not", "or", "quote", "raise", "receive", "require", "rescue", "try", "unless",
                "unquote", "unquote_splicing", "use", "when", "with", "nil", "true", "false", "__MODULE__",
                "__DIR__", "__ENV__", "__CALLER__", "__block__", "__aliases__", "__cursor__", "__scope__"
            ],
            "type": [
                "Atom", "BitString", "Float", "Function", "Integer", "List", "Map", "PID", "Port",
                "Reference", "Tuple", "Any", "Boolean", "Exception", "Module", "Regex", "Range",
                "Keyword", "MapSet", "HashSet", "HashDict", "GenServer", "Supervisor", "Application",
                "Agent", "Task", "Stream", "Enum", "String", "IO", "File", "Path", "System", "Process"
            ],
            "builtin": [
                "inspect", "puts", "print", "IO.puts", "IO.inspect", "IO.write", "IO.read", "IO.gets",
                "String.to_atom", "String.to_integer", "String.to_float", "Atom.to_string", "Integer.to_string",
                "Float.to_string", "List.to_string", "Tuple.to_list", "Map.to_list", "Keyword.to_list",
                "Enum.map", "Enum.filter", "Enum.reduce", "Enum.each", "Enum.find", "Enum.any?", "Enum.all?"
            ]
        },
        "single_line_blocks": [
            {"name": "comment", "start": "#", "end": ""},
            {"name": "string", "start": "\"", "end": "\"", "escape": {"escape_string": "\\", "items": ["\\\"", "\\\\"]}}
        ],
        "multi_line_blocks": [
            {"name": "heredoc", "start": "\"\"\"", "end": "\"\"\""},
            {"name": "heredoc", "start": "'''", "end": "'''"}
        ],
        "tokens": [
            {"name": "number", "pattern": "\\b\\d+\\.?\\d*([eE][+-]?\\d+)?\\b"},
            {"name": "hex number", "pattern": "\\b0[xX][0-9a-fA-F]+\\b"},
            {"name": "octal number", "pattern": "\\b0[0-7]+\\b"},
            {"name": "binary number", "pattern": "\\b0[bB][01]+\\b"},
            {"name": "atom", "pattern": ":[a-zA-Z_][a-zA-Z0-9_]*[?!]?"},
            {"name": "sigil", "pattern": "~[a-zA-Z]\\{.*?\\}"},
            {"name": "module attribute", "pattern": "@\\w+"}
        ]
    },

    "fsharp": {
        "name": "fsharp",
        "extensions": [".fs", ".fsx", ".fsi"],
        "delimiters": [" ", "\t", "(", ")", "{", "}", "[", "]", ";", ",", ".", "<", ">", "+", "-", "*", "/", "%", "&", "|", "^", "!", "~", "=", "?", ":", "@"],
        "ignore_case": False,
        "keywords": {
            "keyword": [
                "abstract", "and", "as", "assert", "base", "begin", "class", "default", "delegate",
                "do", "done", "downcast", "downto", "elif", "else", "end", "exception", "extern",
                "false", "finally", "for", "fun", "function", "global", "if", "in", "inherit",
                "inline", "interface", "internal", "lazy", "let", "let!", "match", "match!", "member",
                "module", "mutable", "namespace", "new", "not", "null", "of", "open", "or", "override",
                "private", "public", "rec", "return", "return!", "select", "static", "struct", "then",
                "to", "true", "try", "type", "upcast", "use", "use!", "val", "void", "when", "while",
                "with", "yield", "yield!", "const", "fixed", "volatile", "atomic", "constructor",
                "destructor", "property", "method", "field", "event", "enum", "struct", "class",
                "interface", "abstract", "sealed", "static", "default", "override", "virtual", "extern",
                "mutable", "volatile", "const", "fixed", "atomic", "constructor", "destructor", "property",
                "method", "field", "event", "enum", "struct", "class", "interface", "abstract", "sealed",
                "static", "default", "override", "virtual", "extern", "mutable", "volatile", "const",
                "fixed", "atomic", "constructor", "destructor", "property", "method", "field", "event"
            ],
            "type": [
                "bool", "byte", "sbyte", "int16", "uint16", "int", "uint32", "int64", "uint64",
                "nativeint", "unativeint", "decimal", "float", "float32", "string", "char", "unit",
                "option", "list", "array", "seq", "map", "set", "ref", "byref", "outref", "inref",
                "Result", "Choice", "Lazy", "Async", "Task", "MailboxProcessor", "Agent", "Event",
                "IObservable", "IObserver", "IDisposable", "IComparable", "IEquatable", "IEnumerable"
            ],
            "builtin": [
                "printf", "printfn", "sprintf", "fprintf", "failwith", "failwithf", "raise", "reraise",
                "box", "unbox", "typeof", "typedefof", "sizeof", "typeof", "typedefof", "sizeof",
                "nameof", "typedefof<_>", "sizeof<_>", "typeof<_>", "nameof<_>", "typedefof<_>"
            ]
        },
        "single_line_blocks": [
            {"name": "comment", "start": "//", "end": ""},
            {"name": "string", "start": "\"", "end": "\"", "escape": {"escape_string": "\\", "items": ["\\\"", "\\\\"]}},
            {"name": "char", "start": "'", "end": "'", "escape": {"escape_string": "\\", "items": ["\\'", "\\\\"]}}
        ],
        "multi_line_blocks": [
            {"name": "multi-line comment", "start": "(*", "end": "*)"},
            {"name": "multi-line string", "start": "\"\"\"", "end": "\"\"\""}
        ],
        "tokens": [
            {"name": "number", "pattern": "\\b\\d+\\.?\\d*([eE][+-]?\\d+)?[fFmM]?\\b"},
            {"name": "hex number", "pattern": "\\b0[xX][0-9a-fA-F]+\\b"},
            {"name": "octal number", "pattern": "\\b0[0-7]+\\b"},
            {"name": "binary number", "pattern": "\\b0[bB][01]+\\b"},
            {"name": "active pattern", "pattern": "\\|[^|]+\\|"},
            {"name": "operator", "pattern": "[!$%&*+-./<=>?@^|~]+"}
        ]
    },

    "groovy": {
        "name": "groovy",
        "extensions": [".groovy", ".gvy", ".gy", ".gsh"],
        "delimiters": [" ", "\t", "(", ")", "{", "}", "[", "]", ";", ",", ".", "<", ">", "+", "-", "*", "/", "%", "&", "|", "^", "!", "~", "=", "?", ":", "@"],
        "ignore_case": False,
        "keywords": {
            "keyword": [
                "abstract", "as", "assert", "boolean", "break", "byte", "case", "catch", "char",
                "class", "const", "continue", "def", "default", "do", "double", "else", "enum",
                "extends", "false", "final", "finally", "float", "for", "goto", "if", "implements",
                "import", "in", "instanceof", "int", "interface", "long", "native", "new", "null",
                "package", "private", "protected", "public", "return", "short", "static", "strictfp",
                "super", "switch", "synchronized", "this", "threadsafe", "throw", "throws", "trait",
                "transient", "true", "try", "void", "volatile", "while", "yield", "var", "record",
                "sealed", "permits", "non-sealed", "when", "as", "in", "trait", "threadsafe"
            ],
            "type": [
                "Boolean", "Byte", "Character", "Double", "Float", "Integer", "Long", "Short",
                "String", "Object", "Class", "System", "Thread", "Runnable", "Exception",
                "RuntimeException", "List", "ArrayList", "LinkedList", "Map", "HashMap", "TreeMap",
                "Set", "HashSet", "TreeSet", "Collection", "Collections", "Arrays", "Math"
            ],
            "builtin": [
                "println", "print", "printf", "sprintf", "sprintf", "sleep", "each", "eachWithIndex",
                "find", "findAll", "collect", "collectMany", "inject", "grep", "any", "every", "sum",
                "min", "max", "sort", "sorted", "reverse", "flatten", "unique", "intersect", "disjoint",
                "plus", "minus", "multiply", "div", "mod", "power", "leftShift", "rightShift"
            ]
        },
        "single_line_blocks": [
            {"name": "comment", "start": "//", "end": ""},
            {"name": "string", "start": "\"", "end": "\"", "escape": {"escape_string": "\\", "items": ["\\\"", "\\\\"]}},
            {"name": "string", "start": "'", "end": "'", "escape": {"escape_string": "\\", "items": ["\\'", "\\\\"]}},
            {"name": "gstring", "start": "\"", "end": "\"", "escape": {"escape_string": "\\", "items": ["\\\"", "\\\\"]}}
        ],
        "multi_line_blocks": [
            {"name": "multi-line comment", "start": "/*", "end": "*/"},
            {"name": "groovydoc", "start": "/**", "end": "*/"},
            {"name": "multi-line string", "start": "\"\"\"", "end": "\"\"\""},
            {"name": "multi-line string", "start": "'''", "end": "'''"}
        ],
        "tokens": [
            {"name": "number", "pattern": "\\b\\d+\\.?\\d*([eE][+-]?\\d+)?[fFdDlL]?\\b"},
            {"name": "hex number", "pattern": "\\b0[xX][0-9a-fA-F]+[lL]?\\b"},
            {"name": "octal number", "pattern": "\\b0[0-7]+[lL]?\\b"},
            {"name": "binary number", "pattern": "\\b0[bB][01]+[lL]?\\b"},
            {"name": "annotation", "pattern": "@\\w+"},
            {"name": "gstring interpolation", "pattern": "\\$\\{[^}]+\\}"},
            {"name": "gstring variable", "pattern": "\\$[a-zA-Z_][a-zA-Z0-9_]*"}
        ]
    },

    "objectivec": {
        "name": "objectivec",
        "extensions": [".m", ".mm", ".h"],
        "delimiters": [" ", "\t", "(", ")", "{", "}", "[", "]", ";", ",", ".", "<", ">", "+", "-", "*", "/", "%", "&", "|", "^", "!", "~", "=", "?", ":", "@", "#"],
        "ignore_case": False,
        "keywords": {
            "keyword": [
                "auto", "break", "case", "char", "const", "continue", "default", "do", "double",
                "else", "enum", "extern", "float", "for", "goto", "if", "inline", "int", "long",
                "register", "restrict", "return", "short", "signed", "sizeof", "static", "struct",
                "switch", "typedef", "union", "unsigned", "void", "volatile", "while", "_Bool",
                "_Complex", "_Imaginary", "asm", "fortran", "objc_class", "objc_selector", "objc_protocol",
                "objc_public", "objc_private", "objc_protected", "objc_package", "synchronized",
                "autoreleasepool", "try", "catch", "finally", "throw", "class", "interface", "implementation",
                "protocol", "end", "private", "public", "protected", "package", "property", "synthesize",
                "dynamic", "optional", "required", "encode", "defs", "selector", "protocol", "encode",
                "defs", "selector", "protocol", "encode", "defs", "selector", "protocol"
            ],
            "type": [
                "id", "Class", "SEL", "IMP", "BOOL", "NSInteger", "NSUInteger", "CGFloat", "CGPoint",
                "CGSize", "CGRect", "NSRange", "NSString", "NSMutableString", "NSArray", "NSMutableArray",
                "NSDictionary", "NSMutableDictionary", "NSSet", "NSMutableSet", "NSNumber", "NSValue",
                "NSData", "NSMutableData", "NSDate", "NSError", "NSObject", "NSProxy", "NSNull"
            ],
            "builtin": [
                "NSLog", "NSAssert", "NSCAssert", "NSParameterAssert", "NSCParameterAssert",
                "NSGetSizeAndAlignment", "objc_getClass", "objc_getProtocol", "objc_getClassList",
                "objc_lookUpClass", "objc_getMetaClass", "objc_registerClassPair", "objc_disposeClassPair"
            ]
        },
        "single_line_blocks": [
            {"name": "comment", "start": "//", "end": ""},
            {"name": "string", "start": "\"", "end": "\"", "escape": {"escape_string": "\\", "items": ["\\\"", "\\\\"]}},
            {"name": "character", "start": "'", "end": "'", "escape": {"escape_string": "\\", "items": ["\\'", "\\\\"]}}
        ],
        "multi_line_blocks": [
            {"name": "multi-line comment", "start": "/*", "end": "*/"},
            {"name": "documentation", "start": "/**", "end": "*/"},
            {"name": "pragma", "start": "#pragma", "end": ""}
        ],
        "tokens": [
            {"name": "number", "pattern": "\\b\\d+\\.?\\d*([eE][+-]?\\d+)?[fFlLuU]?\\b"},
            {"name": "hex number", "pattern": "\\b0[xX][0-9a-fA-F]+[lLuU]?\\b"},
            {"name": "octal number", "pattern": "\\b0[0-7]+[lLuU]?\\b"},
            {"name": "preprocessor", "pattern": "#\\w+"},
            {"name": "directive", "pattern": "@\\w+"},
            {"name": "selector", "pattern": "@selector\\([^)]+\\)"},
            {"name": "protocol", "pattern": "@protocol\\([^)]+\\)"},
            {"name": "encode", "pattern": "@encode\\([^)]+\\)"}
        ]
    }
}

def generate_toml_config(lang_config):
    """生成 TOML 格式的语言配置"""
    lines = []
    
    # 基本信息
    lines.append(f'name = "{lang_config["name"]}"')
    lines.append(f'extensions = {json.dumps(lang_config["extensions"])}')
    lines.append(f'delimiters = {json.dumps(lang_config["delimiters"])}')
    lines.append(f'back_delimiters = []')
    lines.append(f'ignore_case = {str(lang_config["ignore_case"]).lower()}')
    lines.append('')
    
    # 关键字
    for category, keywords in lang_config["keywords"].items():
        lines.append('[[keywords]]')
        lines.append(f'name = "{category}"')
        lines.append('keywords = [')
        # 每行显示5个关键字
        for i in range(0, len(keywords), 5):
            chunk = keywords[i:i+5]
            line = '    ' + ', '.join(f'"{kw}"' for kw in chunk)
            if i + 5 < len(keywords):
                line += ','
            lines.append(line)
        lines.append(']')
        lines.append('')
    
    # 单行代码块
    for block in lang_config["single_line_blocks"]:
        lines.append('[[single_line_blocks]]')
        lines.append(f'name = "{block["name"]}"')
        lines.append(f'start = "{block["start"]}"')
        lines.append(f'end = "{block["end"]}"')
        if "escape" in block:
            escape = block["escape"]
            lines.append(f'escape = {{ escape_string = "{escape["escape_string"]}", items = {json.dumps(escape["items"])} }}')
        lines.append('')
    
    # 多行代码块
    for block in lang_config["multi_line_blocks"]:
        lines.append('[[multi_line_blocks]]')
        lines.append(f'name = "{block["name"]}"')
        lines.append(f'start = "{block["start"]}"')
        lines.append(f'end = "{block["end"]}"')
        lines.append('')
    
    # 正则表达式标记
    for token in lang_config["tokens"]:
        lines.append('[[tokens]]')
        lines.append(f'name = "{token["name"]}"')
        lines.append(f'pattern = "{token["pattern"]}"')
        lines.append('')
    
    return '\n'.join(lines)

def main():
    """主函数"""
    # 创建输出目录
    output_dir = Path("languages_new")
    output_dir.mkdir(exist_ok=True)
    
    print(f"开始生成 {len(languages)} 种语言的配置文件...")
    
    for lang_name, lang_config in languages.items():
        filename = f"{lang_name}.toml"
        filepath = output_dir / filename
        
        # 生成配置内容
        content = generate_toml_config(lang_config)
        
        # 写入文件
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(content)
        
        print(f"✅ 已生成: {filename}")
    
    print(f"\n🎉 成功生成 {len(languages)} 种语言配置文件到 {output_dir} 目录")
    print("\n下一步:")
    print("1. 检查生成的配置文件")
    print("2. 将文件移动到 languages/ 目录")
    print("3. 运行测试验证功能")

if __name__ == "__main__":
    main()