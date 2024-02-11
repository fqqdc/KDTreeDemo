using System;
using System.Collections.Generic;
using System.Linq;

namespace KDTree
{
    internal class ArrayEqualityComparer<T>() : IEqualityComparer<T[]>
        where T : IComparable<T>
    {
        bool IEqualityComparer<T[]>.Equals(T[]? x, T[]? y)
        {
            if (x == null || y == null) return false;

            return x.SequenceEqual(y);
        }

        int IEqualityComparer<T[]>.GetHashCode(T[] array)
        {
            HashCode hashCode = new();
            foreach (var item in array)
            {
                hashCode.Add(item);
            }
            return hashCode.ToHashCode();
        }
    }
}
