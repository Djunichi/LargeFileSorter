namespace Sorter;

/// <summary>
/// Records Comparer
/// </summary>
public record Comparer() : IComparer<(ReadOnlyMemory<char>, int)>
{
    /// <summary>
    /// Compares two records by string parts first then by number parts
    /// </summary>
    /// <param name="x">Left</param>
    /// <param name="y">Right</param>
    /// <returns>Result of compare. 0 if equal. greater than 0 if left is greater. less than 0 if right is greater.</returns>
    public int Compare((ReadOnlyMemory<char>, int) x, (ReadOnlyMemory<char>, int) y)
    {
        var spanX = x.Item1.Span;
        var spanY = y.Item1.Span;
        var dotPositionX = x.Item2;
        var dotPositionY = y.Item2;

        //Compare string parts
        var compareResult = spanX[(dotPositionX + 2)..].CompareTo(spanY[(dotPositionY + 2)..], StringComparison.OrdinalIgnoreCase);
        if (compareResult != 0) return compareResult;
        
        //If they are equal compare number parts
        return int.Parse(spanX[..dotPositionX]) - int.Parse(spanY[..dotPositionY]);
    }
}