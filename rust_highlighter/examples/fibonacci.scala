package examples

/**
 * Scala Fibonacci implementation with various approaches
 */
object Fibonacci {
  
  // Recursive approach (naive)
  def fibRecursive(n: Int): Int = {
    if (n <= 1) n
    else fibRecursive(n - 1) + fibRecursive(n - 2)
  }
  
  // Tail-recursive approach
  def fibTailRecursive(n: Int): Int = {
    @annotation.tailrec
    def fibHelper(n: Int, a: Int, b: Int): Int = {
      if (n == 0) a
      else fibHelper(n - 1, b, a + b)
    }
    fibHelper(n, 0, 1)
  }
  
  // Memoized approach using mutable Map
  def fibMemoized(n: Int): Int = {
    val memo = scala.collection.mutable.Map[Int, Int]()
    
    def fib(n: Int): Int = {
      memo.getOrElseUpdate(n, {
        if (n <= 1) n
        else fib(n - 1) + fib(n - 2)
      })
    }
    
    fib(n)
  }
  
  // Stream-based approach (lazy evaluation)
  def fibStream: LazyList[Int] = {
    def fibHelper(a: Int, b: Int): LazyList[Int] = 
      a #:: fibHelper(b, a + b)
    fibHelper(0, 1)
  }
  
  // Pattern matching approach
  def fibPatternMatch(n: Int): Int = n match {
    case 0 => 0
    case 1 => 1
    case _ => fibPatternMatch(n - 1) + fibPatternMatch(n - 2)
  }
  
  // Main function to demonstrate all approaches
  def main(args: Array[String]): Unit = {
    val n = 10
    
    println(s"Fibonacci of $n:")
    println(s"Recursive: ${fibRecursive(n)}")
    println(s"Tail Recursive: ${fibTailRecursive(n)}")
    println(s"Memoized: ${fibMemoized(n)}")
    println(s"Pattern Match: ${fibPatternMatch(n)}")
    println(s"Stream (first 10): ${fibStream.take(10).toList}")
    
    // Performance comparison
    val start = System.currentTimeMillis()
    val result = fibTailRecursive(30)
    val end = System.currentTimeMillis()
    println(s"Tail recursive fib(30) = $result, time: ${end - start}ms")
  }
}