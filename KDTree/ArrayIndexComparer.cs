namespace KDTree
{
    internal class ArrayIndexComparer<T>(int index) : IComparer<T[]>
        where T : IComparable<T>
    {
        int IComparer<T[]>.Compare(T[]? x, T[]? y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            return x[index].CompareTo(y[index]);
        }
    }
}
