using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace KDTree
{
    public class KDTreeNode<T>(int _Axis)
        where T : IComparable<T>
    {
        public T[] Value { get; init; } = [];
        public KDTreeNode<T>? Parent { get; init; }
        public KDTreeNode<T>? Left { get; private set; }
        public KDTreeNode<T>? Right { get; private set; }

        public int Axis { get => _Axis; }

        public bool IsRoot { get => Parent == null; }
        public bool IsLeaf { get => Left == null && Right == null; }

        public T this[int index] { get => Value[index]; }

        public static KDTreeNode<T> Create(int axis, int maxAxis, T[][] values)
        {
            Dictionary<int, IComparer<T[]>> comparerDict = [];

            var root = CreateRecursion(axis, maxAxis, values, comparerDict, null);
            Debug.Assert(root != null);
            return root;
        }

        private static KDTreeNode<T>? CreateRecursion(int axis, int maxAxis, T[][] values,
            Dictionary<int, IComparer<T[]>> comparerDict, in KDTreeNode<T>? parentNode)
        {
            if (values.Length == 0)
            {
                return null;
            }

            if (values.Length == 1)
            {
                return new(axis) { Parent = parentNode, Value = values[0] };
            }

            if (!comparerDict.TryGetValue(axis, out var comparer))
            {
                comparer = new ArrayIndexComparer<T>(axis);
                comparerDict[axis] = comparer;
            }

            Array.Sort(values, comparer);
            var midIndex = values.Length / 2;
            var value = new KDTreeNode<T>(axis) { Parent = parentNode, Value = values[midIndex] };

            value.Left = CreateRecursion((axis + 1) % maxAxis, maxAxis, values[..midIndex], comparerDict, value);
            value.Right = CreateRecursion((axis + 1) % maxAxis, maxAxis, values[(midIndex + 1)..], comparerDict, value);

            return value;
        }

        public override string ToString()
        {
            return $"<{string.Join(", ", Value)}>";
        }
    }
}
