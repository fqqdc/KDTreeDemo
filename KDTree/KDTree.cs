using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace KDTree
{
    public class KDTree<T>
        where T : struct, INumber<T>
    {
        public event Action<KDTreeNode<T>>? CalcNodeDistance;
        public event Action<KDTreeNode<T>>? ChangedBestNode;
        public event Action<T[], KDTreeNode<T>, T>? BranchChecked;


        public KDTree(in IEnumerable<IEnumerable<T>> vectors, int numberAxis, T zeroValue)
        {
            if (!vectors.Any())
                throw new ArgumentOutOfRangeException(nameof(vectors), $"Size of {nameof(vectors)} cannot be empty.");

            var emptyVector = Enumerable.Repeat(zeroValue, numberAxis);
            var eqLengthVectors = vectors.Select(set => set.Concat(emptyVector).Take(numberAxis).ToArray());
            var distinctVectors = eqLengthVectors.Distinct(new ArrayEqualityComparer<T>()).ToArray();

            _AxisNumber = numberAxis;
            Root = KDTreeNode<T>.Create(0, _AxisNumber, distinctVectors);
        }

        public KDTreeNode<T> Root { get; init; }
        private readonly int _AxisNumber;

        public T[] FindNearest(in T[] center)
        {
            if (center.Length != _AxisNumber)
                throw new ArgumentOutOfRangeException(nameof(center), $"Length of {nameof(center)} must equal {_AxisNumber}.");

            CalcNodeDistance?.Invoke(Root);

            var bestDistance = CalcDistanceSquare(center, Root.Value);
            var bestNode = Root;

            ChangedBestNode?.Invoke(bestNode);

            FindNearestRecursion(center, Root, ref bestDistance, ref bestNode);
            return [.. bestNode.Value];
        }

        private void FindNearestRecursion(in T[] center, in KDTreeNode<T>? currentNode, ref T bestDistance, ref KDTreeNode<T> bestNode)
        {
            Debug.Assert(currentNode != null);

            var distance = CalcDistanceSquare(center, currentNode.Value);
            CalcNodeDistance?.Invoke(currentNode);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestNode = currentNode;
                ChangedBestNode?.Invoke(bestNode);
            }

            KDTreeNode<T>? node0;
            KDTreeNode<T>? node1;

            if (center[currentNode.Axis] < currentNode[currentNode.Axis])
            {
                (node0, node1) = (currentNode.Left, currentNode.Right);
            }
            else
            {
                (node0, node1) = (currentNode.Right, currentNode.Left);
            }


            if (node0 != null)
            {
                FindNearestRecursion(center, currentNode: node0, ref bestDistance, ref bestNode);
            }

            if (node1 != null)
            {                
                var axisDistance = KDTree<T>.CalcAxisDistanceSquare(center, currentNode.Value, currentNode.Axis);

                if (axisDistance < bestDistance)
                {
                    Debug.WriteLine("axisDistance < bestDistance");
                    BranchChecked?.Invoke(center, currentNode, bestDistance);

                    FindNearestRecursion(center, node1, ref bestDistance, ref bestNode);
                }
            }
        }

        private static T CalcAxisDistanceSquare(T[] x, T[] y, int axis)
        {
            var dis = x[axis] - y[axis];

            return dis * dis;
        }

        private T CalcDistanceSquare(T[] x, T[] y)
        {
            var distanceSquare = T.Zero;
            for (int indexAxis = 0; indexAxis < _AxisNumber; indexAxis++)
            {
                var distanceAxis = x[indexAxis] - y[indexAxis];
                distanceSquare += distanceAxis * distanceAxis;
            }

            return distanceSquare;
        }
    }
}
