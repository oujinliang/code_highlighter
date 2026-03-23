package test

object MultilineTest {
  
  // 单行注释
  val singleLine = "This is a single line string"
  
  /* 多行注释开始
   * 这是第二行
   * 这是第三行
   * 多行注释结束 */
  val normalString = "Normal string"
  
  /** Scaladoc 注释
   * @param x 参数 x
   * @param y 参数 y
   * @return 返回值
   */
  def add(x: Int, y: Int): Int = x + y
  
  // 多行字符串测试
  val multiLineString = """This is line 1
This is line 2
This is line 3"""
  
  // 带插值的多行字符串
  val name = "Scala"
  val interpolatedString = s"""Hello, $name!
Welcome to the world of Scala.
This is line 3."""
  
  // 复杂的多行字符串
  val jsonExample = s"""{
  "name": "$name",
  "version": "2.13",
  "features": [
    "functional",
    "object-oriented",
    "type-safe"
  ]
}"""
  
  // 嵌套的多行注释（Scala 不支持，但测试一下）
  /* 
   * 外层注释开始
   * /* 这可能会导致问题 */
   * 外层注释结束
   */
  
  // 字符串中的转义字符
  val escapedString = "Line 1\nLine 2\tTabbed"
  
  // 原始字符串（不转义）
  val rawString = raw"No escaping: \n \t"
  
  def main(args: Array[String]): Unit = {
    println("Testing multiline features")
    println(singleLine)
    println(multiLineString)
    println(interpolatedString)
    println(jsonExample)
  }
}