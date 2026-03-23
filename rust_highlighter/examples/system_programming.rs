//! Rust system programming examples
//! Demonstrates ownership, borrowing, and system-level operations

use std::collections::HashMap;
use std::fs::{self, File};
use std::io::{self, BufRead, BufReader, Write};
use std::path::Path;
use std::sync::{Arc, Mutex};
use std::thread;
use std::time::{Duration, Instant};

/// File processing utilities
struct FileProcessor;

impl FileProcessor {
    /// Read file and count lines, words, and characters
    fn count_file_stats<P: AsRef<Path>>(path: P) -> io::Result<(usize, usize, usize)> {
        let file = File::open(path)?;
        let reader = BufReader::new(file);
        
        let mut lines = 0;
        let mut words = 0;
        let mut chars = 0;
        
        for line in reader.lines() {
            let line = line?;
            lines += 1;
            chars += line.len();
            words += line.split_whitespace().count();
        }
        
        Ok((lines, words, chars))
    }
    
    /// Search for pattern in file and return matching lines
    fn search_in_file<P: AsRef<Path>>(path: P, pattern: &str) -> io::Result<Vec<String>> {
        let file = File::open(path)?;
        let reader = BufReader::new(file);
        let mut matches = Vec::new();
        
        for (line_num, line) in reader.lines().enumerate() {
            let line = line?;
            if line.contains(pattern) {
                matches.push(format!("Line {}: {}", line_num + 1, line));
            }
        }
        
        Ok(matches)
    }
    
    /// Copy file with progress tracking
    fn copy_file_with_progress<P: AsRef<Path>>(src: P, dst: P) -> io::Result<u64> {
        let mut src_file = File::open(src)?;
        let mut dst_file = File::create(dst)?;
        
        let mut buffer = [0; 8192];
        let mut total_bytes = 0;
        
        loop {
            let bytes_read = src_file.read(&mut buffer)?;
            if bytes_read == 0 {
                break;
            }
            
            dst_file.write_all(&buffer[..bytes_read])?;
            total_bytes += bytes_read as u64;
            
            // Simulate progress reporting
            if total_bytes % 1024 == 0 {
                print!(".");
                io::stdout().flush()?;
            }
        }
        
        println!();
        Ok(total_bytes)
    }
}

/// Concurrent data processing
struct ConcurrentProcessor;

impl ConcurrentProcessor {
    /// Process data in parallel using multiple threads
    fn process_parallel(data: Vec<i32>, num_threads: usize) -> Vec<i32> {
        let chunk_size = (data.len() + num_threads - 1) / num_threads;
        let data_arc = Arc::new(data);
        let results = Arc::new(Mutex::new(Vec::new()));
        
        let mut handles = vec![];
        
        for i in 0..num_threads {
            let data_clone = Arc::clone(&data_arc);
            let results_clone = Arc::clone(&results);
            
            let handle = thread::spawn(move || {
                let start = i * chunk_size;
                let end = std::cmp::min(start + chunk_size, data_clone.len());
                
                let mut local_result = Vec::new();
                for j in start..end {
                    // Simulate some processing
                    let processed = data_clone[j] * 2 + 1;
                    local_result.push(processed);
                }
                
                let mut results = results_clone.lock().unwrap();
                results.extend(local_result);
            });
            
            handles.push(handle);
        }
        
        for handle in handles {
            handle.join().unwrap();
        }
        
        let results = results.lock().unwrap();
        results.clone()
    }
    
    /// Producer-consumer pattern using channels
    fn producer_consumer_pattern() {
        use std::sync::mpsc;
        
        let (tx, rx) = mpsc::channel();
        let num_producers = 3;
        let num_consumers = 2;
        
        // Spawn producers
        let mut producer_handles = vec![];
        for i in 0..num_producers {
            let tx_clone = tx.clone();
            let handle = thread::spawn(move || {
                for j in 0..5 {
                    let message = format!("Producer {}: Message {}", i, j);
                    tx_clone.send(message).unwrap();
                    thread::sleep(Duration::from_millis(100));
                }
            });
            producer_handles.push(handle);
        }
        
        // Drop the original sender
        drop(tx);
        
        // Spawn consumers
        let mut consumer_handles = vec![];
        for i in 0..num_consumers {
            let rx_clone = rx.clone();
            let handle = thread::spawn(move || {
                while let Ok(message) = rx_clone.recv() {
                    println!("Consumer {}: Received '{}'", i, message);
                }
            });
            consumer_handles.push(handle);
        }
        
        // Wait for all producers to finish
        for handle in producer_handles {
            handle.join().unwrap();
        }
        
        // Wait for all consumers to finish
        for handle in consumer_handles {
            handle.join().unwrap();
        }
    }
}

/// Memory management examples
struct MemoryManager;

impl MemoryManager {
    /// Demonstrate ownership and borrowing
    fn demonstrate_ownership() {
        let mut data = vec![1, 2, 3, 4, 5];
        
        // Borrowing immutably
        let sum: i32 = data.iter().sum();
        println!("Sum: {}", sum);
        
        // Borrowing mutably
        for item in &mut data {
            *item *= 2;
        }
        
        println!("Doubled: {:?}", data);
        
        // Ownership transfer
        let data2 = data;
        println!("Moved data: {:?}", data2);
        // println!("{:?}", data); // This would cause compile error
    }
    
    /// Demonstrate smart pointers
    fn demonstrate_smart_pointers() {
        use std::rc::Rc;
        use std::cell::RefCell;
        
        // Rc (Reference Counting)
        let data = Rc::new(vec![1, 2, 3]);
        let data_clone1 = Rc::clone(&data);
        let data_clone2 = Rc::clone(&data);
        
        println!("Reference count: {}", Rc::strong_count(&data));
        println!("Data: {:?}", data);
        println!("Clone 1: {:?}", data_clone1);
        println!("Clone 2: {:?}", data_clone2);
        
        // RefCell (Interior Mutability)
        let cell = RefCell::new(5);
        *cell.borrow_mut() += 10;
        println!("RefCell value: {}", cell.borrow());
    }
    
    /// Demonstrate lifetimes
    fn demonstrate_lifetimes<'a>(x: &'a str, y: &'a str) -> &'a str {
        if x.len() > y.len() {
            x
        } else {
            y
        }
    }
}

/// Error handling examples
struct ErrorHandler;

impl ErrorHandler {
    /// Custom error type
    #[derive(Debug)]
    enum AppError {
        IoError(io::Error),
        ParseError(std::num::ParseIntError),
        CustomError(String),
    }
    
    impl From<io::Error> for AppError {
        fn from(error: io::Error) -> Self {
            AppError::IoError(error)
        }
    }
    
    impl From<std::num::ParseIntError> for AppError {
        fn from(error: std::num::ParseIntError) -> Self {
            AppError::ParseError(error)
        }
    }
    
    /// Function that returns Result
    fn read_and_parse_file<P: AsRef<Path>>(path: P) -> Result<i32, AppError> {
        let content = fs::read_to_string(path)?;
        let number: i32 = content.trim().parse()?;
        Ok(number)
    }
    
    /// Demonstrate error handling patterns
    fn demonstrate_error_handling() {
        // Using match
        match Self::read_and_parse_file("number.txt") {
            Ok(number) => println!("Parsed number: {}", number),
            Err(e) => println!("Error: {:?}", e),
        }
        
        // Using if let
        if let Ok(number) = Self::read_and_parse_file("number.txt") {
            println!("Successfully parsed: {}", number);
        }
        
        // Using unwrap_or
        let number = Self::read_and_parse_file("number.txt").unwrap_or(0);
        println!("Number with default: {}", number);
    }
}

/// Performance measurement utilities
struct PerformanceMonitor;

impl PerformanceMonitor {
    /// Measure execution time of a closure
    fn measure_time<F, T>(f: F) -> (T, Duration)
    where
        F: FnOnce() -> T,
    {
        let start = Instant::now();
        let result = f();
        let duration = start.elapsed();
        (result, duration)
    }
    
    /// Benchmark different algorithms
    fn benchmark_algorithms() {
        let data: Vec<i32> = (1..=1000).collect();
        
        // Benchmark sum calculation
        let (sum, duration) = Self::measure_time(|| {
            data.iter().sum::<i32>()
        });
        println!("Sum: {}, Time: {:?}", sum, duration);
        
        // Benchmark filtering
        let (evens, duration) = Self::measure_time(|| {
            data.iter().filter(|&&x| x % 2 == 0).collect::<Vec<_>>()
        });
        println!("Even numbers count: {}, Time: {:?}", evens.len(), duration);
        
        // Benchmark sorting
        let mut data_copy = data.clone();
        let (_, duration) = Self::measure_time(|| {
            data_copy.sort();
        });
        println!("Sorting time: {:?}", duration);
    }
}

/// Main function demonstrating all examples
fn main() {
    println!("=== Rust System Programming Examples ===");
    
    // File processing
    println!("\n--- File Processing ---");
    match FileProcessor::count_file_stats("Cargo.toml") {
        Ok((lines, words, chars)) => {
            println!("File stats - Lines: {}, Words: {}, Chars: {}", lines, words, chars);
        }
        Err(e) => println!("Error reading file: {}", e),
    }
    
    // Concurrent processing
    println!("\n--- Concurrent Processing ---");
    let data = vec![1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
    let processed = ConcurrentProcessor::process_parallel(data, 3);
    println!("Parallel processed: {:?}", processed);
    
    // Memory management
    println!("\n--- Memory Management ---");
    MemoryManager::demonstrate_ownership();
    MemoryManager::demonstrate_smart_pointers();
    
    let longer = MemoryManager::demonstrate_lifetimes("hello", "world!");
    println!("Longer string: {}", longer);
    
    // Error handling
    println!("\n--- Error Handling ---");
    ErrorHandler::demonstrate_error_handling();
    
    // Performance measurement
    println!("\n--- Performance Measurement ---");
    PerformanceMonitor::benchmark_algorithms();
    
    // Producer-consumer pattern
    println!("\n--- Producer-Consumer Pattern ---");
    ConcurrentProcessor::producer_consumer_pattern();
    
    println!("\n=== All examples completed ===");
}