package examples

/**
 * Kotlin data structures implementation
 */
class DataStructures {
    
    // Generic Stack implementation
    class Stack<T> {
        private val elements = mutableListOf<T>()
        
        fun push(element: T) {
            elements.add(element)
        }
        
        fun pop(): T? {
            if (elements.isEmpty()) {
                return null
            }
            return elements.removeAt(elements.size - 1)
        }
        
        fun peek(): T? {
            return elements.lastOrNull()
        }
        
        fun isEmpty(): Boolean = elements.isEmpty()
        
        fun size(): Int = elements.size
        
        override fun toString(): String = "Stack(${elements.joinToString(", ")})"
    }
    
    // Generic Queue implementation
    class Queue<T> {
        private val elements = mutableListOf<T>()
        
        fun enqueue(element: T) {
            elements.add(element)
        }
        
        fun dequeue(): T? {
            if (elements.isEmpty()) {
                return null
            }
            return elements.removeAt(0)
        }
        
        fun peek(): T? {
            return elements.firstOrNull()
        }
        
        fun isEmpty(): Boolean = elements.isEmpty()
        
        fun size(): Int = elements.size
        
        override fun toString(): String = "Queue(${elements.joinToString(", ")})"
    }
    
    // Binary Search Tree implementation
    class BinarySearchTree<T : Comparable<T>> {
        data class Node<T>(
            val value: T,
            var left: Node<T>? = null,
            var right: Node<T>? = null
        )
        
        private var root: Node<T>? = null
        
        fun insert(value: T) {
            root = insertRecursive(root, value)
        }
        
        private fun insertRecursive(node: Node<T>?, value: T): Node<T> {
            if (node == null) {
                return Node(value)
            }
            
            when {
                value < node.value -> node.left = insertRecursive(node.left, value)
                value > node.value -> node.right = insertRecursive(node.right, value)
                // Value already exists, do nothing
            }
            
            return node
        }
        
        fun search(value: T): Boolean {
            return searchRecursive(root, value)
        }
        
        private fun searchRecursive(node: Node<T>?, value: T): Boolean {
            if (node == null) {
                return false
            }
            
            return when {
                value == node.value -> true
                value < node.value -> searchRecursive(node.left, value)
                else -> searchRecursive(node.right, value)
            }
        }
        
        fun inorderTraversal(): List<T> {
            val result = mutableListOf<T>()
            inorderRecursive(root, result)
            return result
        }
        
        private fun inorderRecursive(node: Node<T>?, result: MutableList<T>) {
            if (node != null) {
                inorderRecursive(node.left, result)
                result.add(node.value)
                inorderRecursive(node.right, result)
            }
        }
        
        fun preorderTraversal(): List<T> {
            val result = mutableListOf<T>()
            preorderRecursive(root, result)
            return result
        }
        
        private fun preorderRecursive(node: Node<T>?, result: MutableList<T>) {
            if (node != null) {
                result.add(node.value)
                preorderRecursive(node.left, result)
                preorderRecursive(node.right, result)
            }
        }
        
        fun postorderTraversal(): List<T> {
            val result = mutableListOf<T>()
            postorderRecursive(root, result)
            return result
        }
        
        private fun postorderRecursive(node: Node<T>?, result: MutableList<T>) {
            if (node != null) {
                postorderRecursive(node.left, result)
                postorderRecursive(node.right, result)
                result.add(node.value)
            }
        }
        
        fun height(): Int {
            return heightRecursive(root)
        }
        
        private fun heightRecursive(node: Node<T>?): Int {
            if (node == null) {
                return -1
            }
            
            val leftHeight = heightRecursive(node.left)
            val rightHeight = heightRecursive(node.right)
            
            return maxOf(leftHeight, rightHeight) + 1
        }
    }
    
    // Linked List implementation
    class LinkedList<T> {
        data class Node<T>(
            val value: T,
            var next: Node<T>? = null
        )
        
        private var head: Node<T>? = null
        private var size = 0
        
        fun addFirst(value: T) {
            head = Node(value, head)
            size++
        }
        
        fun addLast(value: T) {
            if (head == null) {
                head = Node(value)
            } else {
                var current = head
                while (current?.next != null) {
                    current = current.next
                }
                current?.next = Node(value)
            }
            size++
        }
        
        fun removeFirst(): T? {
            if (head == null) {
                return null
            }
            
            val value = head?.value
            head = head?.next
            size--
            return value
        }
        
        fun removeLast(): T? {
            if (head == null) {
                return null
            }
            
            if (head?.next == null) {
                val value = head?.value
                head = null
                size--
                return value
            }
            
            var current = head
            while (current?.next?.next != null) {
                current = current?.next
            }
            
            val value = current?.next?.value
            current?.next = null
            size--
            return value
        }
        
        fun get(index: Int): T? {
            if (index < 0 || index >= size) {
                return null
            }
            
            var current = head
            repeat(index) {
                current = current?.next
            }
            
            return current?.value
        }
        
        fun isEmpty(): Boolean = size == 0
        
        fun size(): Int = size
        
        fun toList(): List<T> {
            val result = mutableListOf<T>()
            var current = head
            while (current != null) {
                result.add(current.value)
                current = current.next
            }
            return result
        }
        
        override fun toString(): String = "LinkedList(${toList().joinToString(", ")})"
    }
    
    companion object {
        @JvmStatic
        fun main(args: Array<String>) {
            println("=== Data Structures Demo ===")
            
            // Stack demo
            println("\n--- Stack Demo ---")
            val stack = Stack<Int>()
            stack.push(1)
            stack.push(2)
            stack.push(3)
            println("Stack: $stack")
            println("Pop: ${stack.pop()}")
            println("Peek: ${stack.peek()}")
            println("Size: ${stack.size()}")
            
            // Queue demo
            println("\n--- Queue Demo ---")
            val queue = Queue<String>()
            queue.enqueue("First")
            queue.enqueue("Second")
            queue.enqueue("Third")
            println("Queue: $queue")
            println("Dequeue: ${queue.dequeue()}")
            println("Peek: ${queue.peek()}")
            println("Size: ${queue.size()}")
            
            // Binary Search Tree demo
            println("\n--- Binary Search Tree Demo ---")
            val bst = BinarySearchTree<Int>()
            val values = listOf(5, 3, 7, 2, 4, 6, 8)
            values.forEach { bst.insert(it) }
            
            println("Inserted values: $values")
            println("Inorder: ${bst.inorderTraversal()}")
            println("Preorder: ${bst.preorderTraversal()}")
            println("Postorder: ${bst.postorderTraversal()}")
            println("Height: ${bst.height()}")
            println("Search 4: ${bst.search(4)}")
            println("Search 9: ${bst.search(9)}")
            
            // Linked List demo
            println("\n--- Linked List Demo ---")
            val linkedList = LinkedList<Char>()
            linkedList.addLast('A')
            linkedList.addLast('B')
            linkedList.addFirst('C')
            linkedList.addLast('D')
            println("Linked List: $linkedList")
            println("Get index 1: ${linkedList.get(1)}")
            println("Remove first: ${linkedList.removeFirst()}")
            println("Remove last: ${linkedList.removeLast()}")
            println("Final list: $linkedList")
            println("Size: ${linkedList.size()}")
        }
    }
}