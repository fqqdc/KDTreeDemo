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
        public event Action<KDTreeNode<T>>? NodeVisited;
        public event Action<KDTreeNode<T>>? BestNodeChanged;
        public event Action<T[], KDTreeNode<T>, double>? SwitchedSiblingBranch;


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

        public KDTreeNode<T> FindNearestRecursion(in T[] value)
        {
            if (value.Length != _AxisNumber)
                throw new ArgumentOutOfRangeException(nameof(value), $"Length of {nameof(value)} must equal {_AxisNumber}.");

            var bestDistance = double.MaxValue;
            KDTreeNode<T>? bestNode = null;

            FindNearestRecursion(value, Root, ref bestDistance, ref bestNode);

            Debug.Assert(bestNode != null);
            return bestNode;
        }

        private void FindNearestRecursion(in T[] center, in KDTreeNode<T> currentNode, ref double bestDistance, ref KDTreeNode<T>? bestNode)
        {
            Debug.Assert(currentNode != null);

            var distance = CalcDistanceSquare(center, currentNode.Value);
            NodeVisited?.Invoke(currentNode);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestNode = currentNode;
                BestNodeChanged?.Invoke(bestNode);
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
                var axisDistance = KDTree<T>.CalcDistanceSquareByAxis(center, currentNode.Value, currentNode.Axis);

                if (axisDistance < bestDistance)
                {
                    SwitchedSiblingBranch?.Invoke(center, currentNode, bestDistance);
                    FindNearestRecursion(center, node1, ref bestDistance, ref bestNode);
                }
            }
        }

        public KDTreeNode<T> FindNearest(in T[] value)
        {
            if (value.Length != _AxisNumber)
                throw new ArgumentOutOfRangeException(nameof(value), $"Length of {nameof(value)} must equal {_AxisNumber}.");

            var bestDistance = double.MaxValue;
            KDTreeNode<T>? bestNode = null;
            HashSet<KDTreeNode<T>> visited = [];

            var node = KDTree<T>.FindLeafByAsix(Root, value);
            while (node != null)
            {
                var distance = CalcDistanceSquare(value, node.Value);
                visited.Add(node);
                NodeVisited?.Invoke(node);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestNode = node;
                    BestNodeChanged?.Invoke(bestNode);
                }

                if (node.Parent == null)
                    break;

                var siblingNode = node.Parent.Left != node ? node.Parent.Left : node.Parent.Right;

                if (siblingNode != null && !visited.Contains(siblingNode))
                {
                    var axisDistance = KDTree<T>.CalcDistanceSquareByAxis(value, node.Parent.Value, node.Parent.Axis);
                    if (axisDistance < bestDistance)
                    {
                        Debug.Assert(bestNode != null);
                        SwitchedSiblingBranch?.Invoke(value, node.Parent, bestDistance);
                        node = KDTree<T>.FindLeafByAsix(siblingNode, value);
                        continue;
                    }
                }

                node = node.Parent;
            }

            Debug.Assert(bestNode != null);
            return bestNode;
        }

        /// <summary>
        /// 寻找分支上的叶子，根据节点上的轴坐标来选择靠近目标的分支
        /// </summary>
        private static KDTreeNode<T> FindLeafByAsix(in KDTreeNode<T> root, T[] values)
        {
            var node = root;
            while (true)
            {
                if (values[node.Axis] < node.Value[node.Axis])
                {
                    if (node.Left == null)
                        break;
                    node = node.Left;
                }
                else
                {
                    if (node.Right == null)
                        break;
                    node = node.Right;
                }
            }
            return node;
        }


        private static double CalcDistanceSquareByAxis(T[] x, T[] y, int axis)
        {
            var dis = Convert.ToDouble(x[axis] - y[axis]);

            return dis * dis;
        }

        private double CalcDistanceSquare(T[] x, T[] y)
        {
            var distanceSquare = 0.0;
            for (int indexAxis = 0; indexAxis < _AxisNumber; indexAxis++)
            {
                var distanceAxis = Convert.ToDouble(x[indexAxis] - y[indexAxis]);
                distanceSquare += distanceAxis * distanceAxis;
            }

            return distanceSquare;
        }
    }
}
