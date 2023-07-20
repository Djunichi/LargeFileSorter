namespace Sorter;

/// <summary>
/// Support class of enumerators binary heap
/// </summary>
public static class BinaryHeap
{
    /// <summary>
    /// Rebuild and sort binary heap of enumerators
    /// </summary>
    /// <param name="heap">Enumerators set</param>
    /// <param name="index"></param>
    /// <param name="comparer">Records comparer</param>
    /// <typeparam name="T">Specifies the data type</typeparam>
    public static void RebuildBinaryHeap<T>(this Span<T> heap, int index, IComparer<T> comparer)
    {
        ArgumentNullException.ThrowIfNull(comparer);

        var largest = index;
        while (true)
        {
            var leftChild = 2 * index + 1;
            var rightChild = 2 * index + 2;
            var current = heap[index];

            if (rightChild < heap.Length && comparer.Compare(current, heap[rightChild]) > 0)
            {
                largest = rightChild;
                current = heap[largest];
            }

            if (leftChild < heap.Length && comparer.Compare(current, heap[leftChild]) > 0)
            {
                largest = leftChild;
            }

            if (largest == index) break;

            (heap[index], heap[largest]) = (heap[largest], heap[index]);

            index = largest;
        }
    }

    /// <summary>
    /// Build binary heap of enumerators
    /// </summary>
    /// <param name="heap">Enumerators set</param>
    /// <param name="comparer">Records comparer</param>
    /// <typeparam name="T">Specifies the data type</typeparam>
    public static void BuildBinaryHeap<T>(this Span<T> heap, IComparer<T> comparer)
    {
        ArgumentNullException.ThrowIfNull(comparer);

        for (var i = heap.Length / 2; i >= 0; i--)
        {
            RebuildBinaryHeap(heap, i, comparer);
        }
    }
}