using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace InterpolationApp
{
    public partial class MainWindow : Window
    {
        private readonly List<VariantData> _variants = VariantData.GetAll();
        private List<(double X, double LagrangeY, double SplineY)> _computedPoints = new();
        private bool _computed = false;

        public MainWindow()
        {
            InitializeComponent();
            LoadVariants();
        }

        private void LoadVariants()
        {
            for (int i = 0; i < _variants.Count; i++)
                cmbVariant.Items.Add($"Варіант {i + 1}  ({_variants[i].SplineType})");
            cmbVariant.SelectedIndex = 0;
        }

        private void CmbVariant_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int idx = cmbVariant.SelectedIndex;
            if (idx < 0) return;
            var v = _variants[idx];

            // Auto-set spline type
            cmbSplineType.SelectedIndex = v.SplineType == "Зімкнений" ? 0 : 1;

            // Show points
            var items = v.Points.Select((p, i) => new PointItem { Index = i, X = p.x.ToString("G"), Y = p.y.ToString("G") }).ToList();
            dgPoints.ItemsSource = items;

            _computed = false;
            dgValues.ItemsSource = null;
            ClearCanvases();
            txtStatus.Text = "Натисніть ▶ Обчислити";
            txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(108, 112, 134));
        }

        private void BtnCompute_Click(object sender, RoutedEventArgs e)
        {
            int idx = cmbVariant.SelectedIndex;
            if (idx < 0) { ShowError("Оберіть варіант!"); return; }

            var v = _variants[idx];
            bool clamped = cmbSplineType.SelectedIndex == 0;

            if (!double.TryParse(txtDx0.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double dx0))
                dx0 = 0.1;
            if (!double.TryParse(txtDxn.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double dxn))
                dxn = -0.5;

            var xs = v.Points.Select(p => p.x).ToArray();
            var ys = v.Points.Select(p => p.y).ToArray();

            double step = 0.2;
            double xMin = xs.First(), xMax = xs.Last();

            var lagrangeCalc = new LagrangeInterpolation(xs, ys);
            var splineCalc = clamped
                ? CubicSpline.Clamped(xs, ys, dx0, dxn)
                : CubicSpline.Natural(xs, ys);

            // Build dense points for chart
            _computedPoints = new List<(double, double, double)>();
            for (double x = xMin; x <= xMax + 1e-9; x += step)
            {
                double xc = Math.Min(x, xMax);
                double lv = lagrangeCalc.Evaluate(xc);
                double sv = splineCalc.Evaluate(xc);
                _computedPoints.Add((xc, lv, sv));
            }

            // Build table (non-node points only for diff, all for table)
            var nodeSet = new HashSet<double>(xs);
            var tableRows = new List<ValueRow>();
            foreach (var (x, lv, sv) in _computedPoints)
            {
                bool isNode = nodeSet.Any(nx => Math.Abs(nx - x) < 1e-9);
                tableRows.Add(new ValueRow
                {
                    X = x.ToString("F2"),
                    Lagrange = lv.ToString("F4"),
                    Spline = sv.ToString("F4"),
                    Diff = isNode ? "0.0000" : (lv - sv).ToString("F4")
                });
            }
            dgValues.ItemsSource = tableRows;

            DrawMainChart(xs, ys);
            DrawDiffChart(xs);

            _computed = true;
            txtStatus.Text = $"✓ Розраховано: {_computedPoints.Count} точок, крок h=0.2";
            txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(166, 227, 161));
        }

        private void DrawMainChart(double[] xs, double[] ys)
        {
            if (_computedPoints.Count == 0) return;
            var canvas = canvasMain;
            canvas.Children.Clear();

            double w = canvas.ActualWidth, h = canvas.ActualHeight;
            if (w < 10 || h < 10) return;

            double pad = 45;
            double plotW = w - pad * 2, plotH = h - pad * 2;

            var allY = _computedPoints.SelectMany(p => new[] { p.LagrangeY, p.SplineY }).Concat(ys);
            double xMin = _computedPoints.First().X, xMax = _computedPoints.Last().X;
            double yMin = allY.Min(), yMax = allY.Max();
            double yPad = (yMax - yMin) * 0.1 + 1;
            yMin -= yPad; yMax += yPad;

            Func<double, double> toCanvasX = x => pad + (x - xMin) / (xMax - xMin) * plotW;
            Func<double, double> toCanvasY = y => pad + plotH - (y - yMin) / (yMax - yMin) * plotH;

            DrawGrid(canvas, w, h, pad, plotW, plotH, xMin, xMax, yMin, yMax, toCanvasX, toCanvasY);

            // Lagrange polyline (blue)
            DrawPolyline(canvas, _computedPoints.Select(p => new Point(toCanvasX(p.X), toCanvasY(p.LagrangeY))).ToList(),
                Color.FromRgb(137, 180, 250), 2.0);

            // Spline polyline (green)
            DrawPolyline(canvas, _computedPoints.Select(p => new Point(toCanvasX(p.X), toCanvasY(p.SplineY))).ToList(),
                Color.FromRgb(166, 227, 161), 2.0);

            // Node points (red dots)
            for (int i = 0; i < xs.Length; i++)
            {
                double cx = toCanvasX(xs[i]), cy = toCanvasY(ys[i]);
                var e = new Ellipse { Width = 8, Height = 8, Fill = new SolidColorBrush(Color.FromRgb(243, 139, 168)) };
                Canvas.SetLeft(e, cx - 4); Canvas.SetTop(e, cy - 4);
                canvas.Children.Add(e);

                var lbl = new TextBlock
                {
                    Text = $"({xs[i]:G},{ys[i]:G})",
                    Foreground = new SolidColorBrush(Color.FromRgb(243, 139, 168)),
                    FontSize = 9
                };
                Canvas.SetLeft(lbl, cx + 5); Canvas.SetTop(lbl, cy - 8);
                canvas.Children.Add(lbl);
            }
        }

        private void DrawDiffChart(double[] xs)
        {
            if (_computedPoints.Count == 0) return;
            var canvas = canvasDiff;
            canvas.Children.Clear();

            double w = canvas.ActualWidth, h = canvas.ActualHeight;
            if (w < 10 || h < 10) return;

            double pad = 45;
            double plotW = w - pad * 2, plotH = h - pad * 2;

            var nodeSet = new HashSet<double>(xs);
            var diffPts = _computedPoints
                .Where(p => !nodeSet.Any(nx => Math.Abs(nx - p.X) < 1e-9))
                .Select(p => (p.X, Diff: p.LagrangeY - p.SplineY))
                .ToList();

            if (!diffPts.Any()) return;

            double xMin = _computedPoints.First().X, xMax = _computedPoints.Last().X;
            double yMin = diffPts.Min(p => p.Diff), yMax = diffPts.Max(p => p.Diff);
            double yPad = Math.Max((yMax - yMin) * 0.15, 0.05);
            yMin -= yPad; yMax += yPad;

            Func<double, double> toCanvasX = x => pad + (x - xMin) / (xMax - xMin) * plotW;
            Func<double, double> toCanvasY = y => pad + plotH - (y - yMin) / (yMax - yMin) * plotH;

            DrawGrid(canvas, w, h, pad, plotW, plotH, xMin, xMax, yMin, yMax, toCanvasX, toCanvasY);

            // Zero line
            double zeroY = toCanvasY(0);
            if (zeroY >= pad && zeroY <= pad + plotH)
            {
                var zeroLine = new Line
                {
                    X1 = pad, Y1 = zeroY, X2 = pad + plotW, Y2 = zeroY,
                    Stroke = new SolidColorBrush(Color.FromRgb(88, 91, 112)),
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 4, 4 }
                };
                canvas.Children.Add(zeroLine);
            }

            DrawPolyline(canvas, diffPts.Select(p => new Point(toCanvasX(p.X), toCanvasY(p.Diff))).ToList(),
                Color.FromRgb(250, 179, 135), 2.0);
        }

        private static void DrawGrid(Canvas canvas, double w, double h, double pad, double plotW, double plotH,
            double xMin, double xMax, double yMin, double yMax,
            Func<double, double> toX, Func<double, double> toY)
        {
            // Background
            var bg = new Rectangle
            {
                Width = plotW, Height = plotH,
                Fill = new SolidColorBrush(Color.FromRgb(24, 24, 37))
            };
            Canvas.SetLeft(bg, pad); Canvas.SetTop(bg, pad);
            canvas.Children.Add(bg);

            int gridCountX = 8, gridCountY = 6;
            var gridBrush = new SolidColorBrush(Color.FromRgb(50, 54, 68));
            var labelBrush = new SolidColorBrush(Color.FromRgb(108, 112, 134));

            // Vertical grid lines + X labels
            for (int i = 0; i <= gridCountX; i++)
            {
                double xVal = xMin + (xMax - xMin) * i / gridCountX;
                double cx = toX(xVal);
                var line = new Line { X1 = cx, Y1 = pad, X2 = cx, Y2 = pad + plotH, Stroke = gridBrush, StrokeThickness = 1 };
                canvas.Children.Add(line);
                var lbl = new TextBlock { Text = xVal.ToString("F1"), Foreground = labelBrush, FontSize = 9 };
                Canvas.SetLeft(lbl, cx - 12); Canvas.SetTop(lbl, pad + plotH + 4);
                canvas.Children.Add(lbl);
            }

            // Horizontal grid lines + Y labels
            for (int i = 0; i <= gridCountY; i++)
            {
                double yVal = yMin + (yMax - yMin) * i / gridCountY;
                double cy = toY(yVal);
                var line = new Line { X1 = pad, Y1 = cy, X2 = pad + plotW, Y2 = cy, Stroke = gridBrush, StrokeThickness = 1 };
                canvas.Children.Add(line);
                var lbl = new TextBlock { Text = yVal.ToString("F2"), Foreground = labelBrush, FontSize = 9 };
                Canvas.SetLeft(lbl, 2); Canvas.SetTop(lbl, cy - 7);
                canvas.Children.Add(lbl);
            }

            // Border
            var border = new Rectangle
            {
                Width = plotW, Height = plotH,
                Stroke = new SolidColorBrush(Color.FromRgb(69, 71, 90)),
                StrokeThickness = 1, Fill = Brushes.Transparent
            };
            Canvas.SetLeft(border, pad); Canvas.SetTop(border, pad);
            canvas.Children.Add(border);
        }

        private static void DrawPolyline(Canvas canvas, List<Point> pts, Color color, double thickness)
        {
            if (pts.Count < 2) return;
            var poly = new Polyline
            {
                Stroke = new SolidColorBrush(color),
                StrokeThickness = thickness,
                StrokeLineJoin = PenLineJoin.Round
            };
            foreach (var p in pts) poly.Points.Add(p);
            canvas.Children.Add(poly);
        }

        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_computed) BtnCompute_Click(null, null);
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            _computed = false;
            dgValues.ItemsSource = null;
            ClearCanvases();
            txtStatus.Text = "Скинуто. Натисніть ▶ Обчислити";
            txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(108, 112, 134));
        }

        private void ClearCanvases()
        {
            canvasMain.Children.Clear();
            canvasDiff.Children.Clear();
        }

        private void ShowError(string msg)
        {
            txtStatus.Text = "⚠ " + msg;
            txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(243, 139, 168));
        }
    }

    // Data Models
    public class PointItem { public int Index { get; set; } public string X { get; set; } public string Y { get; set; } }
    public class ValueRow { public string X { get; set; } public string Lagrange { get; set; } public string Spline { get; set; } public string Diff { get; set; } }

    public class VariantData
    {
        public List<(double x, double y)> Points { get; set; }
        public string SplineType { get; set; }

        public static List<VariantData> GetAll() => new List<VariantData>
        {
            new VariantData { Points = new List<(double,double)> {(1,3),(3,7),(6,9),(10,8),(14,11)}, SplineType = "Зімкнений" },
            new VariantData { Points = new List<(double,double)> {(0,0),(3,2),(6,5),(9,7),(12,4)},   SplineType = "Природній" },
            new VariantData { Points = new List<(double,double)> {(0,1),(4,3),(7,7),(10,5),(12,6)},  SplineType = "Зімкнений" },
            new VariantData { Points = new List<(double,double)> {(0,10),(3,6),(5,4),(7,1),(11,3)},  SplineType = "Природній" },
            new VariantData { Points = new List<(double,double)> {(0,5),(3,2),(6,0),(8,4),(10,6)},   SplineType = "Зімкнений" },
            new VariantData { Points = new List<(double,double)> {(2,2),(5,0),(7,-1),(9,1),(12,10)}, SplineType = "Природній" },
            new VariantData { Points = new List<(double,double)> {(1,1),(3,14),(5,8),(6,12),(9,10)}, SplineType = "Зімкнений" },
            new VariantData { Points = new List<(double,double)> {(1,9),(4,3),(8,6),(12,8),(14,3)},  SplineType = "Природній" },
            new VariantData { Points = new List<(double,double)> {(0,12),(3,5),(6,10),(8,7),(11,6)}, SplineType = "Зімкнений" },
            new VariantData { Points = new List<(double,double)> {(1,4),(5,17),(7,14),(9,8),(12,12)},SplineType = "Природній" },
            new VariantData { Points = new List<(double,double)> {(0,3),(4,1),(7,5),(10,8),(12,3)},  SplineType = "Зімкнений" },
            new VariantData { Points = new List<(double,double)> {(1,5),(4,8),(7,2),(10,3),(13,11)}, SplineType = "Природній" },
        };
    }
}
