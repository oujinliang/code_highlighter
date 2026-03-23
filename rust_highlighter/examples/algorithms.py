"""
Python algorithms implementation
"""
from typing import List, Tuple, Optional
import time
from functools import lru_cache

class Algorithms:
    
    @staticmethod
    def binary_search(arr: List[int], target: int) -> int:
        """
        Binary search implementation
        Returns index of target if found, otherwise -1
        """
        left, right = 0, len(arr) - 1
        
        while left <= right:
            mid = left + (right - left) // 2
            
            if arr[mid] == target:
                return mid
            elif arr[mid] < target:
                left = mid + 1
            else:
                right = mid - 1
        
        return -1
    
    @staticmethod
    def linear_search(arr: List[int], target: int) -> int:
        """
        Linear search implementation
        Returns index of target if found, otherwise -1
        """
        for i, value in enumerate(arr):
            if value == target:
                return i
        return -1
    
    @staticmethod
    def gcd(a: int, b: int) -> int:
        """
        Greatest Common Divisor using Euclidean algorithm
        """
        while b:
            a, b = b, a % b
        return a
    
    @staticmethod
    def lcm(a: int, b: int) -> int:
        """
        Least Common Multiple
        """
        return abs(a * b) // Algorithms.gcd(a, b)
    
    @staticmethod
    def is_prime(n: int) -> bool:
        """
        Check if a number is prime
        """
        if n < 2:
            return False
        if n == 2:
            return True
        if n % 2 == 0:
            return False
        
        for i in range(3, int(n**0.5) + 1, 2):
            if n % i == 0:
                return False
        return True
    
    @staticmethod
    def sieve_of_eratosthenes(limit: int) -> List[int]:
        """
        Sieve of Eratosthenes to find all primes up to limit
        """
        if limit < 2:
            return []
        
        sieve = [True] * (limit + 1)
        sieve[0] = sieve[1] = False
        
        for i in range(2, int(limit**0.5) + 1):
            if sieve[i]:
                for j in range(i*i, limit + 1, i):
                    sieve[j] = False
        
        return [i for i in range(limit + 1) if sieve[i]]
    
    @staticmethod
    def factorial(n: int) -> int:
        """
        Calculate factorial recursively
        """
        if n < 0:
            raise ValueError("Factorial not defined for negative numbers")
        if n == 0 or n == 1:
            return 1
        return n * Algorithms.factorial(n - 1)
    
    @staticmethod
    @lru_cache(maxsize=None)
    def fibonacci(n: int) -> int:
        """
        Calculate Fibonacci number with memoization
        """
        if n < 0:
            raise ValueError("Fibonacci not defined for negative numbers")
        if n == 0:
            return 0
        if n == 1:
            return 1
        return Algorithms.fibonacci(n - 1) + Algorithms.fibonacci(n - 2)
    
    @staticmethod
    def power(base: int, exponent: int) -> int:
        """
        Calculate power using fast exponentiation
        """
        if exponent == 0:
            return 1
        if exponent < 0:
            return 1 / Algorithms.power(base, -exponent)
        
        if exponent % 2 == 0:
            half = Algorithms.power(base, exponent // 2)
            return half * half
        else:
            return base * Algorithms.power(base, exponent - 1)
    
    @staticmethod
    def matrix_multiply(a: List[List[int]], b: List[List[int]]) -> List[List[int]]:
        """
        Matrix multiplication
        """
        if len(a[0]) != len(b):
            raise ValueError("Matrix dimensions don't match for multiplication")
        
        rows_a, cols_a = len(a), len(a[0])
        cols_b = len(b[0])
        
        result = [[0 for _ in range(cols_b)] for _ in range(rows_a)]
        
        for i in range(rows_a):
            for j in range(cols_b):
                for k in range(cols_a):
                    result[i][j] += a[i][k] * b[k][j]
        
        return result
    
    @staticmethod
    def transpose(matrix: List[List[int]]) -> List[List[int]]:
        """
        Transpose a matrix
        """
        if not matrix:
            return []
        
        rows, cols = len(matrix), len(matrix[0])
        return [[matrix[i][j] for i in range(rows)] for j in range(cols)]
    
    @staticmethod
    def determinant(matrix: List[List[int]]) -> int:
        """
        Calculate determinant of a square matrix
        """
        n = len(matrix)
        if n == 0:
            return 1
        if n == 1:
            return matrix[0][0]
        if n == 2:
            return matrix[0][0] * matrix[1][1] - matrix[0][1] * matrix[1][0]
        
        det = 0
        for j in range(n):
            # Create submatrix by removing first row and column j
            submatrix = []
            for i in range(1, n):
                row = []
                for k in range(n):
                    if k != j:
                        row.append(matrix[i][k])
                submatrix.append(row)
            
            sign = (-1) ** j
            det += sign * matrix[0][j] * Algorithms.determinant(submatrix)
        
        return det
    
    @staticmethod
    def knapsack_01(weights: List[int], values: List[int], capacity: int) -> int:
        """
        0/1 Knapsack problem using dynamic programming
        """
        n = len(weights)
        dp = [[0 for _ in range(capacity + 1)] for _ in range(n + 1)]
        
        for i in range(1, n + 1):
            for w in range(capacity + 1):
                if weights[i-1] <= w:
                    dp[i][w] = max(
                        dp[i-1][w],
                        dp[i-1][w - weights[i-1]] + values[i-1]
                    )
                else:
                    dp[i][w] = dp[i-1][w]
        
        return dp[n][capacity]
    
    @staticmethod
    def longest_common_subsequence(s1: str, s2: str) -> int:
        """
        Longest Common Subsequence using dynamic programming
        """
        m, n = len(s1), len(s2)
        dp = [[0] * (n + 1) for _ in range(m + 1)]
        
        for i in range(1, m + 1):
            for j in range(1, n + 1):
                if s1[i-1] == s2[j-1]:
                    dp[i][j] = dp[i-1][j-1] + 1
                else:
                    dp[i][j] = max(dp[i-1][j], dp[i][j-1])
        
        return dp[m][n]
    
    @staticmethod
    def edit_distance(s1: str, s2: str) -> int:
        """
        Edit Distance (Levenshtein Distance) using dynamic programming
        """
        m, n = len(s1), len(s2)
        dp = [[0] * (n + 1) for _ in range(m + 1)]
        
        for i in range(m + 1):
            dp[i][0] = i
        for j in range(n + 1):
            dp[0][j] = j
        
        for i in range(1, m + 1):
            for j in range(1, n + 1):
                if s1[i-1] == s2[j-1]:
                    dp[i][j] = dp[i-1][j-1]
                else:
                    dp[i][j] = 1 + min(
                        dp[i-1][j],      # deletion
                        dp[i][j-1],      # insertion
                        dp[i-1][j-1]     # substitution
                    )
        
        return dp[m][n]

def main():
    """Main function to demonstrate algorithms"""
    print("=== Python Algorithms Demo ===")
    
    # Binary Search
    print("\n--- Binary Search ---")
    arr = [1, 3, 5, 7, 9, 11, 13, 15]
    target = 7
    result = Algorithms.binary_search(arr, target)
    print(f"Array: {arr}")
    print(f"Search for {target}: index {result}")
    
    # GCD and LCM
    print("\n--- GCD and LCM ---")
    a, b = 12, 18
    print(f"GCD({a}, {b}) = {Algorithms.gcd(a, b)}")
    print(f"LCM({a}, {b}) = {Algorithms.lcm(a, b)}")
    
    # Prime numbers
    print("\n--- Prime Numbers ---")
    n = 20
    primes = Algorithms.sieve_of_eratosthenes(n)
    print(f"Primes up to {n}: {primes}")
    print(f"Is 17 prime? {Algorithms.is_prime(17)}")
    print(f"Is 15 prime? {Algorithms.is_prime(15)}")
    
    # Factorial
    print("\n--- Factorial ---")
    n = 10
    print(f"{n}! = {Algorithms.factorial(n)}")
    
    # Fibonacci
    print("\n--- Fibonacci ---")
    n = 15
    print(f"Fibonacci({n}) = {Algorithms.fibonacci(n)}")
    
    # Power
    print("\n--- Fast Exponentiation ---")
    base, exp = 2, 10
    print(f"{base}^{exp} = {Algorithms.power(base, exp)}")
    
    # Matrix operations
    print("\n--- Matrix Operations ---")
    matrix = [[1, 2, 3], [4, 5, 6], [7, 8, 9]]
    print(f"Matrix: {matrix}")
    print(f"Transpose: {Algorithms.transpose(matrix)}")
    print(f"Determinant: {Algorithms.determinant(matrix)}")
    
    # Knapsack problem
    print("\n--- 0/1 Knapsack Problem ---")
    weights = [10, 20, 30]
    values = [60, 100, 120]
    capacity = 50
    max_value = Algorithms.knapsack_01(weights, values, capacity)
    print(f"Weights: {weights}")
    print(f"Values: {values}")
    print(f"Capacity: {capacity}")
    print(f"Maximum value: {max_value}")
    
    # Longest Common Subsequence
    print("\n--- Longest Common Subsequence ---")
    s1 = "ABCDGH"
    s2 = "AEDFHR"
    lcs_length = Algorithms.longest_common_subsequence(s1, s2)
    print(f"String 1: {s1}")
    print(f"String 2: {s2}")
    print(f"LCS length: {lcs_length}")
    
    # Edit Distance
    print("\n--- Edit Distance ---")
    s1 = "kitten"
    s2 = "sitting"
    distance = Algorithms.edit_distance(s1, s2)
    print(f"String 1: {s1}")
    print(f"String 2: {s2}")
    print(f"Edit distance: {distance}")

if __name__ == "__main__":
    main()