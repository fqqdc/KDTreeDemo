using KDTree;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Linq;
using WPFArrows.Arrows;

namespace KDTreeDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            ClearMainCanvas();
        }

        private void ClearMainCanvas()
        {
            ClearMouseLeftButtonDownElements();

            _RandomPointElements.ForEach(e => Canvas_Main.Children.Remove(e));
            _RandomPointElements.Clear();
            _RandomPoints.Clear();
            txtBuilderPoints.Text = "0";

            _LineElements.ForEach(e => Canvas_Main.Children.Remove(e));
            _LineElements.Clear();

            txtTraversePoints.Text = "0";
            txtCheckedPoints.Text = "0";

            StackPanel_Tree.Children.Clear();
            _NodeTreeItems.Clear();
            _ChangedNodeTreeItems.Clear();
        }

        const int PointNumberDefault = 100;
        int _RandomPointNumber = PointNumberDefault;

        readonly List<double[]> _RandomPoints = [];
        readonly List<Ellipse> _RandomPointElements = [];
        KDTree<double>? _KDTree;
        readonly List<FrameworkElement> _LineElements = [];

        static readonly Brush NormalPointBrush = Brushes.Gray;
        static readonly Brush NearestBrush = Brushes.DarkGreen;
        static readonly Brush MouseHitBrush = Brushes.Blue;
        static readonly Brush TraversePointBrush = Brushes.Red;
        static readonly Brush CheckedPointBrush = Brushes.Orange;
        static readonly Brush BranchCheckedBrush = Brushes.BlueViolet;
        static readonly Brush XAxisSplitBrush = Brushes.DarkRed;
        static readonly Brush YAxisSplitBrush = Brushes.YellowGreen;

        readonly Ellipse _NearestElement = new() { Width = 10, Height = 10, Fill = NearestBrush };
        readonly Ellipse _MouseHitElement = new() { Width = 10, Height = 10, StrokeThickness = 3, Stroke = MouseHitBrush };

        readonly List<Ellipse> _TraverseElements = [];
        int _TraverseCount = 0;
        readonly HashSet<KDTreeNode<double>> _TraversePoints = [];
        readonly List<Ellipse> _CheckedElements = [];
        int _CheckedCount = 0;
        readonly HashSet<KDTreeNode<double>> _CheckedPoints = [];
        readonly List<FrameworkElement> _BranchCheckedElements = [];
        readonly Dictionary<KDTreeNode<double>, Rectangle> _NodeTreeItems = [];
        readonly HashSet<KDTreeNode<double>> _ChangedNodeTreeItems = [];

        //=======================
        // KDTree event

        private void KDTree_NodeVisited(KDTreeNode<double> node)
        {
            if (_TraversePoints.Contains(node))
                return;

            Debug.WriteLine($"NodeVisited:({_TraverseCount}){node}");

            _TraverseCount++;
            _TraversePoints.Add(node);

            var traversePoint = new Ellipse() { Width = 10, Height = 10, Fill = TraversePointBrush };
            Canvas.SetLeft(traversePoint, node.Value[0] - 5);
            Canvas.SetTop(traversePoint, node.Value[1] - 5);
            Canvas_Main.Children.Add(traversePoint);
            _TraverseElements.Add(traversePoint);

            _NodeTreeItems[node].Fill = TraversePointBrush;
            _ChangedNodeTreeItems.Add(node);
        }

        private void KDTree_BestNodeChanged(KDTreeNode<double> node)
        {
            if (_CheckedPoints.Contains(node))
                return;

            Debug.WriteLine($"BestNodeChanged:({_CheckedCount}){node}");

            _CheckedCount++;
            _CheckedPoints.Add(node);

            var checkedPoint = new Ellipse() { Width = 10, Height = 10, Fill = CheckedPointBrush };
            Canvas.SetLeft(checkedPoint, node.Value[0] - 5);
            Canvas.SetTop(checkedPoint, node.Value[1] - 5);
            Canvas_Main.Children.Add(checkedPoint);
            _CheckedElements.Add(checkedPoint);

            _NodeTreeItems[node].Fill = CheckedPointBrush;
            _ChangedNodeTreeItems.Add(node);
        }
        private void KDTree_SwitchedSiblingBranch(double[] center, KDTreeNode<double> midNode, double radiusQquared)
        {
            Debug.WriteLine($"SwitchedSiblingBranch");

            var radius = Math.Sqrt(radiusQquared);
            var branchCheck = new Ellipse() { Width = radius * 2, Height = radius * 2, Stroke = BranchCheckedBrush, StrokeThickness = 0.5 };
            Canvas.SetLeft(branchCheck, center[0] - radius);
            Canvas.SetTop(branchCheck, center[1] - radius);
            Canvas_Main.Children.Add(branchCheck);
            _BranchCheckedElements.Add(branchCheck);


            Point startPoint;
            Point endPoint;


            if (midNode.Axis == 0)
            {
                startPoint.X = center[0];
                startPoint.Y = midNode.Value[1];

                double sign = double.Sign(center[0] - midNode.Value[0]);
                endPoint = startPoint with { X = startPoint.X - radius * sign };
            }
            else
            {
                startPoint.X = midNode.Value[0];
                startPoint.Y = center[1];

                double sign = double.Sign(center[1] - midNode.Value[1]);
                endPoint = startPoint with { Y = startPoint.Y - radius * sign };
            }
            var radiueLine = new ArrowLine()
            {
                StartPoint = startPoint,
                EndPoint = endPoint,
                StrokeThickness = 1,
                Stroke = BranchCheckedBrush,
            };
            Canvas_Main.Children.Add(radiueLine);
            _BranchCheckedElements.Add(radiueLine);
        }

        //=======================
        //btnPointGenerator_Click

        private void BtnPointGenerator_Click(object sender, RoutedEventArgs e)
        {
            ClearMainCanvas();

            if (!int.TryParse(txtRndPointNumber.Text, out _RandomPointNumber))
            {
                _RandomPointNumber = PointNumberDefault;
            }

            _RandomPointNumber = Math.Clamp(_RandomPointNumber, 1, 1023);

            var n = (int)Math.Log2(_RandomPointNumber);
            n = (int)Math.Pow(2, n + 1) - 1;

            _RandomPointNumber = n;
            txtRndPointNumber.Text = _RandomPointNumber.ToString();

            double maxWidth = Canvas_Main.ActualWidth - 20;
            double maxHeight = Canvas_Main.ActualHeight - 20;

            if (_RandomPointGenerationType == 0)
                AddRandomPoints(maxWidth, maxHeight);
            else
                AddCirclePoints(maxWidth, maxHeight);

            txtBuilderPoints.Text = this._RandomPointNumber.ToString();

            _KDTree = new(_RandomPoints.ToArray(), 2, 0);
            _KDTree.NodeVisited += KDTree_NodeVisited;
            _KDTree.BestNodeChanged += KDTree_BestNodeChanged;
            _KDTree.SwitchedSiblingBranch += KDTree_SwitchedSiblingBranch;

            _LineElements.ForEach(e => Canvas_Main.Children.Remove(e));
            _LineElements.Clear();

            Rect rect = new() { X = 0, Y = 0, Width = Canvas_Main.ActualWidth, Height = Canvas_Main.ActualHeight };
            double lineThickness = Math.Ceiling(Math.Log2(this._RandomPointNumber)) * 0.5 + 0.5;
            DrawSplitLine(_KDTree.Root, rect, lineThickness);

            GenerateTreeItems(_KDTree.Root, StackPanel_Tree);
        }

        private void GenerateTreeItems(KDTreeNode<double>? node, StackPanel vPanel)
        {
            if (node == null) return;

            Rectangle rectangle = new() { Fill = NormalPointBrush, Height = 19, Margin = new(1, 1, 0, 0) };
            _NodeTreeItems[node] = rectangle;
            vPanel.Children.Add(rectangle);

            var rowGrid = new Grid();
            rowGrid.ColumnDefinitions.Add(new() { Width = new(1, GridUnitType.Star) });
            rowGrid.ColumnDefinitions.Add(new() { Width = new(1, GridUnitType.Star) });


            var vLeftPanel = new StackPanel() { Orientation = Orientation.Vertical };
            GenerateTreeItems(node.Left, vLeftPanel);
            rowGrid.Children.Add(vLeftPanel);

            var vRightPanel = new StackPanel() { Orientation = Orientation.Vertical };
            Grid.SetColumn(vRightPanel, 1);
            GenerateTreeItems(node.Right, vRightPanel);
            rowGrid.Children.Add(vRightPanel);

            vPanel.Children.Add(rowGrid);
        }

        private void AddNormalPointElement(double x, double y)
        {
            _RandomPoints.Add([x, y]);
            Ellipse ellipse = new() { Width = 10, Height = 10, Fill = NormalPointBrush };
            Canvas.SetLeft(ellipse, x - 5);
            Canvas.SetTop(ellipse, y - 5);
            Canvas_Main.Children.Add(ellipse);
            _RandomPointElements.Add(ellipse);
        }

        private void AddCirclePoints(double maxWidth, double maxHeight)
        {
            Random rnd = new();
            double dRadian = Math.PI * 2 / _RandomPointNumber;
            double radius = Math.Min(maxWidth, maxHeight) * 0.5;
            double midWidth = maxWidth * 0.5;
            double midHeight = maxHeight * 0.5;

            for (int i = 0; i < _RandomPointNumber; i++)
            {
                var rad = i * dRadian * 0.95 + dRadian * (rnd.NextDouble() * 0.05);

                var x = midWidth + Math.Cos(rad) * radius * (rnd.NextDouble() * 0.05 + 0.95);
                var y = midHeight + Math.Sin(rad) * radius * (rnd.NextDouble() * 0.05 + 0.95);
                Debug.Assert(x > 0);
                Debug.Assert(y > 0);
                AddNormalPointElement(x, y);
            }
        }

        private void AddRandomPoints(double maxWidth, double maxHeight)
        {
            Random rnd = new();
            for (int i = 0; i < _RandomPointNumber; i++)
            {
                double[] point = [rnd.NextDouble() * maxWidth, rnd.NextDouble() * maxHeight];
                _RandomPoints.Add(point);
                Ellipse ellipse = new() { Width = 10, Height = 10, Fill = NormalPointBrush };
                Canvas.SetLeft(ellipse, point[0] - 5);
                Canvas.SetTop(ellipse, point[1] - 5);
                Canvas_Main.Children.Add(ellipse);
                _RandomPointElements.Add(ellipse);
            }
        }

        private void AddEmptyPoints()
        {
            _RandomPoints.AddRange(Enumerable.Repeat((double[])[0.0, 0.0], _RandomPointNumber));
        }

        //=======================
        // mainCanvas_MouseLeftButtonDown

        private Point? _LastMouseButtonGetPosition;
        private void MainCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _LastMouseButtonGetPosition = Mouse.GetPosition(Canvas_Main);
            FindNearest(_LastMouseButtonGetPosition.Value);
        }

        private void FindNearest(Point point)
        {
            Debug.Assert(_KDTree != null);

            Debug.WriteLine("");
            Debug.WriteLine("===========");

            ClearMouseLeftButtonDownElements();

            KDTreeNode<double> nearestNode;
            if (_AlgorithmType == 0)
                nearestNode = _KDTree.FindNearestRecursion([point.X, point.Y]);
            else
                nearestNode = _KDTree.FindNearest([point.X, point.Y]);

            Canvas.SetLeft(_NearestElement, nearestNode[0] - 5);
            Canvas.SetTop(_NearestElement, nearestNode[1] - 5);
            Canvas_Main.Children.Add(_NearestElement);
            _NodeTreeItems[nearestNode].Fill = NearestBrush;
            _ChangedNodeTreeItems.Add(nearestNode);


            Canvas.SetLeft(_MouseHitElement, point.X - 5);
            Canvas.SetTop(_MouseHitElement, point.Y - 5);
            Canvas_Main.Children.Add(_MouseHitElement);

            txtTraversePoints.Text = _TraverseCount.ToString();
            txtCheckedPoints.Text = _CheckedCount.ToString();
        }

        private void ClearMouseLeftButtonDownElements()
        {
            _TraverseElements.ForEach(e => Canvas_Main.Children.Remove(e));
            _TraverseElements.Clear();
            _TraversePoints.Clear();
            _TraverseCount = 0;
            _CheckedElements.ForEach(e => Canvas_Main.Children.Remove(e));
            _CheckedElements.Clear();
            _CheckedPoints.Clear();
            _CheckedCount = 0;
            _BranchCheckedElements.ForEach(e => Canvas_Main.Children.Remove(e));
            Canvas_Main.Children.Remove(_NearestElement);
            Canvas_Main.Children.Remove(_MouseHitElement);

            _ChangedNodeTreeItems.ToList().ForEach(node => _NodeTreeItems[node].Fill = NormalPointBrush);
        }

        private void DrawSplitLine(KDTreeNode<double>? node, Rect rect, double thickness, int typeBrush = 0)
        {
            if (node == null) return;

            Brush brush = typeBrush == 0 ? XAxisSplitBrush : YAxisSplitBrush;
            typeBrush = typeBrush == 0 ? 1 : 0;

            switch (node.Axis)
            {
                case 0:
                    {
                        var x = node[0];
                        Line line = new()
                        {
                            X1 = x,
                            Y1 = rect.Top,
                            X2 = x,
                            Y2 = rect.Top + rect.Height,
                            Stroke = brush,
                            StrokeThickness = thickness
                        };
                        Canvas_Main.Children.Add(line);
                        _LineElements.Add(line);

                        DrawSplitLine(node.Left, rect with { Width = x - rect.X }, thickness - 0.5, typeBrush);
                        DrawSplitLine(node.Right, rect with { X = x, Width = rect.Width - x + rect.X }, thickness - 0.5, typeBrush);
                    }
                    break;
                case 1:
                    {
                        var y = node[1];
                        Line line = new()
                        {
                            X1 = rect.Left,
                            Y1 = y,
                            X2 = rect.Left + rect.Width,
                            Y2 = y,
                            Stroke = brush,
                            StrokeThickness = thickness
                        };
                        Canvas_Main.Children.Add(line);
                        _LineElements.Add(line);

                        DrawSplitLine(node.Left, rect with { Height = y - rect.Y }, thickness - 0.5, typeBrush);
                        DrawSplitLine(node.Right, rect with { Y = y, Height = rect.Height - y + rect.Y }, thickness - 0.5, typeBrush);
                    }
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        //=======================
        //rbt _Click

        int _RandomPointGenerationType = 0;

        private void RbtRandom_Click(object sender, RoutedEventArgs e)
        {
            _RandomPointGenerationType = 0;
        }

        private void RbtCircle_Click(object sender, RoutedEventArgs e)
        {
            _RandomPointGenerationType = 1;
        }

        int _AlgorithmType = 0;

        private void RadioButton_Checked_T2B(object sender, RoutedEventArgs e)
        {
            _AlgorithmType = 0;
            if (_LastMouseButtonGetPosition == null)
                return;
            FindNearest(_LastMouseButtonGetPosition.Value);
        }

        private void RadioButton_Checked_B2T(object sender, RoutedEventArgs e)
        {
            _AlgorithmType = 1;
            if (_LastMouseButtonGetPosition == null)
                return;
            FindNearest(_LastMouseButtonGetPosition.Value);
        }
    }
}