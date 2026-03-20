using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeHighlighter.Example
{
    /// <summary>
    /// A simple example class for testing syntax highlighting.
    /// </summary>
    public class Calculator
    {
        private readonly List<double> _history = new List<double>();
        
        public double Add(double a, double b)
        {
            double result = a + b;
            _history.Add(result);
            return result;
        }
        
        public double Multiply(double a, double b)
        {
            double result = a * b;
            _history.Add(result);
            return result;
        }
        
        public IEnumerable<double> GetHistory()
        {
            return _history.AsReadOnly();
        }
        
        public void ClearHistory()
        {
            _history.Clear();
        }
    }
    
    class Program
    {
        static void Main(string[] args)
        {
            var calculator = new Calculator();
            
            // Test addition
            double sum = calculator.Add(5.5, 3.2);
            Console.WriteLine($"Sum: {sum}");
            
            // Test multiplication
            double product = calculator.Multiply(4.0, 2.5);
            Console.WriteLine($"Product: {product}");
            
            // Display history
            Console.WriteLine("Calculation History:");
            foreach (var result in calculator.GetHistory())
            {
                Console.WriteLine($"  {result}");
            }
            
            // Test with hex numbers
            int hexValue = 0xFF;
            Console.WriteLine($"Hex value: {hexValue}");
            
            // Test with preprocessor
            #if DEBUG
            Console.WriteLine("Debug mode");
            #else
            Console.WriteLine("Release mode");
            #endif
        }
    }
}