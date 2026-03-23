package examples;

import java.util.Arrays;
import java.util.Random;

/**
 * Java sorting algorithms implementation
 */
public class SortingAlgorithms {
    
    // Bubble Sort
    public static void bubbleSort(int[] arr) {
        int n = arr.length;
        for (int i = 0; i < n - 1; i++) {
            for (int j = 0; j < n - i - 1; j++) {
                if (arr[j] > arr[j + 1]) {
                    // Swap arr[j] and arr[j+1]
                    int temp = arr[j];
                    arr[j] = arr[j + 1];
                    arr[j + 1] = temp;
                }
            }
        }
    }
    
    // Selection Sort
    public static void selectionSort(int[] arr) {
        int n = arr.length;
        for (int i = 0; i < n - 1; i++) {
            int minIdx = i;
            for (int j = i + 1; j < n; j++) {
                if (arr[j] < arr[minIdx]) {
                    minIdx = j;
                }
            }
            // Swap the found minimum element with the first element
            int temp = arr[minIdx];
            arr[minIdx] = arr[i];
            arr[i] = temp;
        }
    }
    
    // Insertion Sort
    public static void insertionSort(int[] arr) {
        int n = arr.length;
        for (int i = 1; i < n; i++) {
            int key = arr[i];
            int j = i - 1;
            
            /* Move elements of arr[0..i-1], that are
               greater than key, to one position ahead
               of their current position */
            while (j >= 0 && arr[j] > key) {
                arr[j + 1] = arr[j];
                j = j - 1;
            }
            arr[j + 1] = key;
        }
    }
    
    // Quick Sort
    public static void quickSort(int[] arr, int low, int high) {
        if (low < high) {
            /* pi is partitioning index, arr[pi] is now at right place */
            int pi = partition(arr, low, high);
            
            // Recursively sort elements before partition and after partition
            quickSort(arr, low, pi - 1);
            quickSort(arr, pi + 1, high);
        }
    }
    
    private static int partition(int[] arr, int low, int high) {
        int pivot = arr[high];
        int i = (low - 1); // index of smaller element
        for (int j = low; j < high; j++) {
            // If current element is smaller than or equal to pivot
            if (arr[j] <= pivot) {
                i++;
                
                // swap arr[i] and arr[j]
                int temp = arr[i];
                arr[i] = arr[j];
                arr[j] = temp;
            }
        }
        
        // swap arr[i+1] and arr[high] (or pivot)
        int temp = arr[i + 1];
        arr[i + 1] = arr[high];
        arr[high] = temp;
        
        return i + 1;
    }
    
    // Merge Sort
    public static void mergeSort(int[] arr, int l, int r) {
        if (l < r) {
            // Find the middle point
            int m = l + (r - l) / 2;
            
            // Sort first and second halves
            mergeSort(arr, l, m);
            mergeSort(arr, m + 1, r);
            
            // Merge the sorted halves
            merge(arr, l, m, r);
        }
    }
    
    private static void merge(int[] arr, int l, int m, int r) {
        // Find sizes of two subarrays to be merged
        int n1 = m - l + 1;
        int n2 = r - m;
        
        /* Create temp arrays */
        int L[] = new int[n1];
        int R[] = new int[n2];
        
        /* Copy data to temp arrays */
        for (int i = 0; i < n1; ++i)
            L[i] = arr[l + i];
        for (int j = 0; j < n2; ++j)
            R[j] = arr[m + 1 + j];
        
        /* Merge the temp arrays */
        
        // Initial indexes of first and second subarrays
        int i = 0, j = 0;
        
        // Initial index of merged subarray array
        int k = l;
        while (i < n1 && j < n2) {
            if (L[i] <= R[j]) {
                arr[k] = L[i];
                i++;
            } else {
                arr[k] = R[j];
                j++;
            }
            k++;
        }
        
        /* Copy remaining elements of L[] if any */
        while (i < n1) {
            arr[k] = L[i];
            i++;
            k++;
        }
        
        /* Copy remaining elements of R[] if any */
        while (j < n2) {
            arr[k] = R[j];
            j++;
            k++;
        }
    }
    
    // Utility method to print array
    public static void printArray(int[] arr) {
        for (int value : arr) {
            System.out.print(value + " ");
        }
        System.out.println();
    }
    
    // Generate random array
    public static int[] generateRandomArray(int size, int bound) {
        Random random = new Random();
        int[] arr = new int[size];
        for (int i = 0; i < size; i++) {
            arr[i] = random.nextInt(bound);
        }
        return arr;
    }
    
    public static void main(String[] args) {
        System.out.println("=== Sorting Algorithms Demo ===");
        
        // Generate test data
        int[] testData = generateRandomArray(10, 100);
        System.out.println("Original array:");
        printArray(testData);
        
        // Test Bubble Sort
        int[] bubbleData = testData.clone();
        long startTime = System.nanoTime();
        bubbleSort(bubbleData);
        long endTime = System.nanoTime();
        System.out.println("\nBubble Sort:");
        printArray(bubbleData);
        System.out.println("Time: " + (endTime - startTime) + " ns");
        
        // Test Selection Sort
        int[] selectionData = testData.clone();
        startTime = System.nanoTime();
        selectionSort(selectionData);
        endTime = System.nanoTime();
        System.out.println("\nSelection Sort:");
        printArray(selectionData);
        System.out.println("Time: " + (endTime - startTime) + " ns");
        
        // Test Insertion Sort
        int[] insertionData = testData.clone();
        startTime = System.nanoTime();
        insertionSort(insertionData);
        endTime = System.nanoTime();
        System.out.println("\nInsertion Sort:");
        printArray(insertionData);
        System.out.println("Time: " + (endTime - startTime) + " ns");
        
        // Test Quick Sort
        int[] quickData = testData.clone();
        startTime = System.nanoTime();
        quickSort(quickData, 0, quickData.length - 1);
        endTime = System.nanoTime();
        System.out.println("\nQuick Sort:");
        printArray(quickData);
        System.out.println("Time: " + (endTime - startTime) + " ns");
        
        // Test Merge Sort
        int[] mergeData = testData.clone();
        startTime = System.nanoTime();
        mergeSort(mergeData, 0, mergeData.length - 1);
        endTime = System.nanoTime();
        System.out.println("\nMerge Sort:");
        printArray(mergeData);
        System.out.println("Time: " + (endTime - startTime) + " ns");
        
        // Verify all sorts produce same result
        boolean allSame = Arrays.equals(bubbleData, selectionData) &&
                         Arrays.equals(selectionData, insertionData) &&
                         Arrays.equals(insertionData, quickData) &&
                         Arrays.equals(quickData, mergeData);
        
        System.out.println("\nAll sorts produce same result: " + allSame);
    }
}