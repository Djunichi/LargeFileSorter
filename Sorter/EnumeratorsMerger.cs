namespace Sorter;

/// <summary>
/// Support class to merge enumerators sets from temp files
/// </summary>
public static class EnumeratorsMerger
{
    /// <summary>
    /// Merge enumerators sets from temp files
    /// </summary>
    /// <param name="sources">Enumerators sets</param>
    /// <param name="comparer">Records comparer</param>
    /// <typeparam name="T">Specifies the data type</typeparam>
    /// <returns>Merged and sorted enumerators set</returns>
    public static IEnumerable<T> MergeEnumerators<T>(this IEnumerable<IEnumerable<T>> sources, IComparer<T> comparer)
    {
        var heap = (from source in sources
            let enumerator = source.GetEnumerator()
            where enumerator.MoveNext()
            select enumerator).ToArray();

        var enumeratorComparer = new EnumeratorComparer<T>(comparer);
        heap.AsSpan().BuildBinaryHeap(enumeratorComparer);

        while (true)
        {
            var first = heap[0];
            if (first.Current != null) yield return first.Current;
            if (!first.MoveNext())
            {
                first.Dispose();
                if (heap.Length == 1) yield break;
                heap[0] = heap[^1];
                Array.Resize(ref heap, heap.Length - 1);
            }
            heap.AsSpan().RebuildBinaryHeap(0, enumeratorComparer);
        }
    }

    /// <summary>
    /// Sorts records
    /// </summary>
    /// <param name="Comparer">Records comparer</param>
    /// <typeparam name="T">Specifies the data type</typeparam>
    private record EnumeratorComparer<T>(IComparer<T> Comparer) : IComparer<IEnumerator<T>>
    {
        public int Compare(IEnumerator<T>? x, IEnumerator<T>? y)
        {
            return Comparer.Compare(x!.Current, y!.Current);
        }
    }
}