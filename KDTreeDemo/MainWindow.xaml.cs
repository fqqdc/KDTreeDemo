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

            _RandomPointElements.ForEach(e => mainCanvas.Children.Remove(e));
            _RandomPointElements.Clear();
            _RandomPoints.Clear();
            txtBuilderPoints.Text = "0";

            _LineElements.ForEach(e => mainCanvas.Children.Remove(e));
            _LineElements.Clear();

            txtTraversePoints.Text = "0";
            txtCheckedPoints.Text = "0";
        }

        const int PointNumberDefault = 100;
        int _RandomPointNumber = PointNumberDefault;

        readonly List<double[]> _RandomPoints = [];
        readonly List<Ellipse> _RandomPointElements = [];
        KDTree<double>? _KDTree;
        readonly List<FrameworkElement> _LineElements = [];

        static readonly Brush NormalPointBrush = Brushes.Black;
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

        //=======================
        // KDTree event

        private void KDTree_AddTraversePoint(KDTreeNode<double> node)
        {
            if (_TraversePoints.Contains(node))
                return;

            Debug.WriteLine($"Traverse:({_TraverseCount}){node}");

            _TraverseCount++;
            _TraversePoints.Add(node);

            var traversePoint = new Ellipse() { Width = 10, Height = 10, Fill = TraversePointBrush };
            Canvas.SetLeft(traversePoint, node.Value[0] - 5);
            Canvas.SetTop(traversePoint, node.Value[1] - 5);
            mainCanvas.Children.Add(traversePoint);
            _TraverseElements.Add(traversePoint);
        }

        private void KDTree_AddCheckedPoint(KDTreeNode<double> node)
        {
            if (_CheckedPoints.Contains(node))
                return;

            Debug.WriteLine($"Checked:({_CheckedCount}){node}");

            _CheckedCount++;
            _CheckedPoints.Add(node);

            var checkedPoint = new Ellipse() { Width = 10, Height = 10, Fill = CheckedPointBrush };
            Canvas.SetLeft(checkedPoint, node.Value[0] - 5);
            Canvas.SetTop(checkedPoint, node.Value[1] - 5);
            mainCanvas.Children.Add(checkedPoint);
            _CheckedElements.Add(checkedPoint);
        }
        private void KDTree_BranchChecked(double[] center, KDTreeNode<double> minNode,double radiusQquared)
        {
            Debug.WriteLine($"CheckBranch");

            var radius = Math.Sqrt(radiusQquared);
            var branchCheck = new Ellipse() { Width = radius * 2, Height = radius * 2, Stroke = BranchCheckedBrush, StrokeThickness = 0.5 };
            Canvas.SetLeft(branchCheck, center[0] - radius);
            Canvas.SetTop(branchCheck, center[1] - radius);
            mainCanvas.Children.Add(branchCheck);
            _BranchCheckedElements.Add(branchCheck);


            Point startPoint;
            Point endPoint;


            if (minNode.Axis == 0)
            {
                startPoint.X = center[0];
                startPoint.Y = minNode.Value[1];

                double sign = double.Sign(center[0] - minNode.Value[0]);
                endPoint = startPoint with { X = startPoint.X - radius * sign };
            }
            else
            {
                startPoint.X = minNode.Value[0];
                startPoint.Y = center[1];

                double sign = double.Sign( center[1] - minNode.Value[1]);
                endPoint = startPoint with { Y = startPoint.Y - radius * sign };
            }
            var radiueLine = new ArrowLine()
            {
                StartPoint = startPoint,
                EndPoint = endPoint,
                StrokeThickness = 1,
                Stroke = BranchCheckedBrush,
            };
            mainCanvas.Children.Add(radiueLine);
            _BranchCheckedElements.Add(radiueLine);
        }

        //=======================
        //btnPointGenerator_Click

        private void BtnPointGenerator_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtRndPointNumber.Text, out _RandomPointNumber))
            {
                _RandomPointNumber = PointNumberDefault;
            }
            _RandomPointNumber = Math.Clamp(_RandomPointNumber, 10, 9999);
            txtRndPointNumber.Text = _RandomPointNumber.ToString();

            ClearMainCanvas();

            double maxWidth = mainCanvas.ActualWidth - 20;
            double maxHeight = mainCanvas.ActualHeight - 20;

            if (_RandomPointGenerationType == 0)
                AddRandomPoints(maxWidth, maxHeight);
            else
                AddCirclePoints(maxWidth, maxHeight);

            txtBuilderPoints.Text = this._RandomPointNumber.ToString();

            _KDTree = new(_RandomPoints.ToArray(), 2, 0);
            _KDTree.CalcNodeDistance += KDTree_AddTraversePoint;
            _KDTree.ChangedBestNode += KDTree_AddCheckedPoint;
            _KDTree.BranchChecked += KDTree_BranchChecked;

            _LineElements.ForEach(e => mainCanvas.Children.Remove(e));
            _LineElements.Clear();

            Rect rect = new() { X = 0, Y = 0, Width = mainCanvas.ActualWidth, Height = mainCanvas.ActualHeight };
            double lineThickness = Math.Ceiling(Math.Log2(this._RandomPointNumber)) * 0.5 + 0.5;
            DrawSplitLine(_KDTree.Root, rect, lineThickness);
        }

        private void AddNormalPointElement(double x, double y)
        {
            _RandomPoints.Add([x, y]);
            Ellipse ellipse = new() { Width = 10, Height = 10, Fill = NormalPointBrush };
            Canvas.SetLeft(ellipse, x - 5);
            Canvas.SetTop(ellipse, y - 5);
            mainCanvas.Children.Add(ellipse);
            _RandomPointElements.Add(ellipse);
        }

        private void AddCirclePoints(double maxWidth, double maxHeight)
        {
            Random rnd = new();
            double dRadian = Math.PI * 2 / _RandomPointNumber;
            double radius = Math.Min(maxWidth, maxHeight) * 0.5;
            double midWidth = maxWidth * 0.5;
            double midHeight = maxHeight * 0.5;

            for (double r = 0; r < Math.PI * 2; r += dRadian)
            {
                var rad = r + dRadian * (rnd.NextDouble() * 0.05);

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
                mainCanvas.Children.Add(ellipse);
                _RandomPointElements.Add(ellipse);
            }
        }

        //=======================
        // mainCanvas_MouseLeftButtonDown

        private void MainCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Debug.Assert(_KDTree != null);

            Debug.WriteLine("");
            Debug.WriteLine("===========");

            ClearMouseLeftButtonDownElements();

            var point = Mouse.GetPosition(mainCanvas);
            var hitPoint = _KDTree.FindNearest([point.X, point.Y]);

            Canvas.SetLeft(_NearestElement, hitPoint[0] - 5);
            Canvas.SetTop(_NearestElement, hitPoint[1] - 5);
            mainCanvas.Children.Add(_NearestElement);

            Canvas.SetLeft(_MouseHitElement, point.X - 5);
            Canvas.SetTop(_MouseHitElement, point.Y - 5);
            mainCanvas.Children.Add(_MouseHitElement);

            txtTraversePoints.Text = _TraverseCount.ToString();
            txtCheckedPoints.Text = _CheckedCount.ToString();
        }

        private void ClearMouseLeftButtonDownElements()
        {
            _TraverseElements.ForEach(e => mainCanvas.Children.Remove(e));
            _TraverseElements.Clear();
            _TraversePoints.Clear();
            _TraverseCount = 0;
            _CheckedElements.ForEach(e => mainCanvas.Children.Remove(e));
            _CheckedElements.Clear();
            _CheckedPoints.Clear();
            _CheckedCount = 0;
            _BranchCheckedElements.ForEach(e => mainCanvas.Children.Remove(e));
            mainCanvas.Children.Remove(_NearestElement);
            mainCanvas.Children.Remove(_MouseHitElement);
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
                        mainCanvas.Children.Add(line);
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
                        mainCanvas.Children.Add(line);
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
    }
}