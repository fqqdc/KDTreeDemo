using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

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

        /// <summary>
        /// FindNearest方法的递归实现
        /// </summary>
        public KDTreeNode<T> FindNearestRecursion(in T[] targetValue)
        {
            if (targetValue.Length != _AxisNumber)
                throw new ArgumentOutOfRangeException(nameof(targetValue), $"Length of {nameof(targetValue)} must equal {_AxisNumber}.");

            var bestDistance = double.MaxValue;
            KDTreeNode<T>? bestNode = null;

            FindNearestRecursion(targetValue, Root, ref bestDistance, ref bestNode);

            Debug.Assert(bestNode != null);
            return bestNode;
        }

        private void FindNearestRecursion(in T[] targetValue, in KDTreeNode<T> currentNode, ref double bestDistance, ref KDTreeNode<T>? bestNode)
        {
            Debug.Assert(currentNode != null);

            var distance = CalcDistanceSquare(targetValue, currentNode.Value);
            NodeVisited?.Invoke(currentNode);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestNode = currentNode;
                BestNodeChanged?.Invoke(bestNode);
            }

            KDTreeNode<T>? node0;
            KDTreeNode<T>? node1;

            if (targetValue[currentNode.Axis] < currentNode[currentNode.Axis])
            {
                (node0, node1) = (currentNode.Left, currentNode.Right); // 先遍历左子树
            }
            else
            {
                (node0, node1) = (currentNode.Right, currentNode.Left); // 先遍历右子树
            }


            if (node0 != null)
            {
                FindNearestRecursion(targetValue, currentNode: node0, ref bestDistance, ref bestNode);
            }

            if (node1 != null)
            {
                var axisDistance = KDTree<T>.CalcDistanceSquareByAxis(targetValue, currentNode.Value, currentNode.Axis);
                //如果node1在currentNode.Axis轴上的距离小于当前最小距离，那么有可能在node1的子树上有更近的节点
                if (axisDistance < bestDistance)
                {
                    SwitchedSiblingBranch?.Invoke(targetValue, currentNode, bestDistance);
                    FindNearestRecursion(targetValue, node1, ref bestDistance, ref bestNode);
                }
            }
        }

        /// <summary>
        /// 寻找离targetValue最近的节点
        /// </summary>
        public KDTreeNode<T> FindNearest(in T[] targetValue)
        {
            if (targetValue.Length != _AxisNumber)
                throw new ArgumentOutOfRangeException(nameof(targetValue), $"Length of {nameof(targetValue)} must equal {_AxisNumber}.");

            var bestDistance = double.MaxValue;
            KDTreeNode<T>? bestNode = null;
            HashSet<KDTreeNode<T>> visited = [];

            var node = KDTree<T>.FindLeafByAsix(Root, targetValue);
            while (node != null)
            {
                var distance = CalcDistanceSquare(targetValue, node.Value);
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
                    var axisDistance = KDTree<T>.CalcDistanceSquareByAxis(targetValue, node.Parent.Value, node.Parent.Axis);
                    //如果siblingNode在node.Parent.Axis轴上的距离小于当前最小距离，那么有可能在siblingNode的子树上有更近的节点
                    if (axisDistance < bestDistance)
                    {
                        Debug.Assert(bestNode != null);
                        SwitchedSiblingBranch?.Invoke(targetValue, node.Parent, bestDistance);
                        node = KDTree<T>.FindLeafByAsix(siblingNode, targetValue);
                        continue;
                    }
                }

                node = node.Parent;
            }

            Debug.Assert(bestNode != null);
            return bestNode;
        }

        /// <summary>
        /// 寻找分支上的叶子，根据node.Axis轴的坐标来选择靠近目标的分支
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
