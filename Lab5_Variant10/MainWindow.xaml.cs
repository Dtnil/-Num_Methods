using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Lab5_Variant10
{
    public partial class MainWindow : Window
    {
        // Integration bounds (струм і)
        private const double A = 0.0;
        private const double B = 5.0;

        // Integrand: u(x) = 2x * cos(x^3)   [Variant 10]
        private static double U(double x) => 2.0 * x * Math.Cos(x * x * x);

        public MainWindow()
        {
            InitializeComponent();
            Compute(14);
        }

        // ── Simpson's composite rule ──────────────────────────────────────────────
        private static double Simpson(double a, double b, int n)
        {
            // n must be even for Simpson
            if (n % 2 != 0) n++;
            double h = (b - a) / n;
            double sum = U(a) + U(b);

            for (int k = 1; k < n; k++)
            {
                double x = a + k * h;
                sum += (k % 2 == 0) ? 2.0 * U(x) : 4.0 * U(x);
            }
            return (h / 3.0) * sum;
        }

        // ── Analytical integral via adaptive Gauss (high-accuracy reference) ──────
        // We use 10000-point Simpson as "analytical" reference
        private static double Reference() => Simpson(A, B, 10000);

        // ── Main computation + UI update ──────────────────────────────────────────
        private void Compute(int n)
        {
            if (n % 2 != 0) n++;  // Simpson needs even n

            double h = (B - A) / n;
            double result = Simpson(A, B, n);
            double reference = Reference();
            double error = Math.Abs(result - reference);

            // --- Update info panels ---
            txtN.Text = n.ToString();
            txtH.Text = $"h = {h:F6}";
            txtResult.Text = $"{result:F8}";
            txtAnalytical.Text = $"{reference:F8}";
            txtError.Text = error < 1e-15 ? "< 1e-15" : $"{error:E4}";

            int totalNodes = n + 1;
            txtNodeInfo.Text =
                $"k = (s-1)·m + 1\n" +
                $"s = 3 (точок у формулі)\n" +
                $"m = {n / 2} (застосувань формули)\n" +
                $"k = {totalNodes} вузлових точок\n" +
                $"h = |b-a|/(k-1) = {h:F6}\n\n" +
                $"Парних вузлів: {n / 2 - 1} (×2)\n" +
                $"Непарних вузлів: {n / 2} (×4)";

            txtStatus.Text = $"Обчислено {n} підінтервалів, {totalNodes} точок. " +
                             $"Похибка: {(error < 1e-15 ? "< 1e-15" : error.ToString("E3"))}";

            DrawChart(n, h);
        }

        // ── Chart drawing ─────────────────────────────────────────────────────────
        private void DrawChart(int n, double h)
        {
            var canvas = chartCanvas;
            if (canvas.ActualWidth < 10 || canvas.ActualHeight < 10) return;

            canvas.Children.Clear();

            double cw = canvas.ActualWidth;
            double ch = canvas.ActualHeight;

            double padL = 52, padR = 16, padT = 16, padB = 36;
            double plotW = cw - padL - padR;
            double plotH = ch - padT - padB;

            // --- Y range ---
            int sampleCount = 500;
            double yMin = 0, yMax = 0;
            for (int i = 0; i <= sampleCount; i++)
            {
                double x = A + (B - A) * i / sampleCount;
                double y = U(x);
                if (y < yMin) yMin = y;
                if (y > yMax) yMax = y;
            }
            double yPad = (yMax - yMin) * 0.12;
            yMin -= yPad; yMax += yPad;
            if (Math.Abs(yMax - yMin) < 0.001) { yMin = -1; yMax = 1; }

            double ToCanvasX(double x) => padL + (x - A) / (B - A) * plotW;
            double ToCanvasY(double y) => padT + (1.0 - (y - yMin) / (yMax - yMin)) * plotH;

            // --- Grid lines ---
            int gridX = 5, gridY = 5;
            for (int i = 0; i <= gridX; i++)
            {
                double x = A + (B - A) * i / gridX;
                double cx = ToCanvasX(x);
                var line = new Line { X1 = cx, Y1 = padT, X2 = cx, Y2 = padT + plotH,
                    Stroke = new SolidColorBrush(Color.FromArgb(40, 139, 148, 158)), StrokeThickness = 1 };
                canvas.Children.Add(line);
                var lbl = new TextBlock { Text = x.ToString("F1"), FontFamily = new FontFamily("Consolas"),
                    FontSize = 9, Foreground = new SolidColorBrush(Color.FromArgb(150, 139, 148, 158)) };
                Canvas.SetLeft(lbl, cx - 10);
                Canvas.SetTop(lbl, padT + plotH + 4);
                canvas.Children.Add(lbl);
            }
            for (int i = 0; i <= gridY; i++)
            {
                double y = yMin + (yMax - yMin) * i / gridY;
                double cy = ToCanvasY(y);
                var line = new Line { X1 = padL, Y1 = cy, X2 = padL + plotW, Y2 = cy,
                    Stroke = new SolidColorBrush(Color.FromArgb(40, 139, 148, 158)), StrokeThickness = 1 };
                canvas.Children.Add(line);
                var lbl = new TextBlock { Text = y.ToString("F1"), FontFamily = new FontFamily("Consolas"),
                    FontSize = 9, Foreground = new SolidColorBrush(Color.FromArgb(150, 139, 148, 158)) };
                Canvas.SetRight(lbl, cw - padL + 2);
                Canvas.SetTop(lbl, cy - 7);
                canvas.Children.Add(lbl);
            }

            // --- Y axis zero line ---
            if (yMin < 0 && yMax > 0)
            {
                double cy0 = ToCanvasY(0);
                var zeroLine = new Line { X1 = padL, Y1 = cy0, X2 = padL + plotW, Y2 = cy0,
                    Stroke = new SolidColorBrush(Color.FromArgb(80, 88, 166, 255)), StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 4, 3 } };
                canvas.Children.Add(zeroLine);
            }

            // --- Simpson trapezoids (shaded) ---
            for (int k = 0; k < n; k += 2)
            {
                double x0 = A + k * h;
                double x1 = A + (k + 1) * h;
                double x2 = A + (k + 2) * h;

                // Draw the filled parabolic segment approximation
                var poly = new Polygon();
                poly.Fill = new SolidColorBrush(Color.FromArgb(55, 240, 136, 62));
                poly.Stroke = new SolidColorBrush(Color.FromArgb(120, 240, 136, 62));
                poly.StrokeThickness = 0.5;

                double zero = ToCanvasY(0);
                poly.Points.Add(new Point(ToCanvasX(x0), zero));
                // Parabola through 3 points using 20 sub-samples
                for (int j = 0; j <= 20; j++)
                {
                    double t = (double)j / 20.0;
                    double xc = x0 + t * (x2 - x0);
                    // Lagrange interpolation through (x0,U(x0)), (x1,U(x1)), (x2,U(x2))
                    double yc = LagrangeInterp3(x0, x1, x2, U(x0), U(x1), U(x2), xc);
                    poly.Points.Add(new Point(ToCanvasX(xc), ToCanvasY(yc)));
                }
                poly.Points.Add(new Point(ToCanvasX(x2), zero));
                canvas.Children.Add(poly);
            }

            // --- Vertical node lines ---
            for (int k = 0; k <= n; k++)
            {
                double x = A + k * h;
                double y = U(x);
                bool isOdd = (k % 2 != 0);
                var color = isOdd
                    ? Color.FromArgb(100, 88, 166, 255)
                    : Color.FromArgb(70, 63, 185, 80);
                var line = new Line { X1 = ToCanvasX(x), Y1 = ToCanvasY(0), X2 = ToCanvasX(x), Y2 = ToCanvasY(y),
                    Stroke = new SolidColorBrush(color), StrokeThickness = 1 };
                canvas.Children.Add(line);
            }

            // --- Function curve ---
            var points = new PointCollection();
            for (int i = 0; i <= sampleCount; i++)
            {
                double x = A + (B - A) * i / sampleCount;
                double y = U(x);
                points.Add(new Point(ToCanvasX(x), ToCanvasY(y)));
            }
            var polyline = new Polyline
            {
                Points = points,
                Stroke = new SolidColorBrush(Color.FromRgb(88, 166, 255)),
                StrokeThickness = 2.5,
                StrokeLineJoin = PenLineJoin.Round
            };
            canvas.Children.Add(polyline);

            // --- Node dots ---
            for (int k = 0; k <= n; k++)
            {
                double x = A + k * h;
                double y = U(x);
                bool isOdd = (k % 2 != 0);
                double r = isOdd ? 4.5 : 3.5;
                var dot = new Ellipse
                {
                    Width = r * 2, Height = r * 2,
                    Fill = isOdd
                        ? new SolidColorBrush(Color.FromRgb(88, 166, 255))
                        : new SolidColorBrush(Color.FromRgb(63, 185, 80)),
                    Stroke = new SolidColorBrush(Color.FromRgb(13, 17, 23)),
                    StrokeThickness = 1.5
                };
                Canvas.SetLeft(dot, ToCanvasX(x) - r);
                Canvas.SetTop(dot, ToCanvasY(y) - r);
                canvas.Children.Add(dot);
            }

            // --- Axes ---
            var axisX = new Line { X1 = padL, Y1 = padT + plotH, X2 = padL + plotW, Y2 = padT + plotH,
                Stroke = new SolidColorBrush(Color.FromRgb(139, 148, 158)), StrokeThickness = 1.5 };
            var axisY = new Line { X1 = padL, Y1 = padT, X2 = padL, Y2 = padT + plotH,
                Stroke = new SolidColorBrush(Color.FromRgb(139, 148, 158)), StrokeThickness = 1.5 };
            canvas.Children.Add(axisX);
            canvas.Children.Add(axisY);

            // --- Y axis label ---
            var yLabel = new TextBlock { Text = "u (В)", FontFamily = new FontFamily("Consolas"),
                FontSize = 10, Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158)),
                RenderTransformOrigin = new Point(0.5, 0.5) };
            yLabel.RenderTransform = new RotateTransform(-90);
            Canvas.SetLeft(yLabel, 2);
            Canvas.SetTop(yLabel, padT + plotH / 2 - 8);
            canvas.Children.Add(yLabel);
        }

        private static double LagrangeInterp3(double x0, double x1, double x2,
                                               double y0, double y1, double y2, double x)
        {
            double l0 = (x - x1) / (x0 - x1) * ((x - x2) / (x0 - x2));
            double l1 = (x - x0) / (x1 - x0) * ((x - x2) / (x1 - x2));
            double l2 = (x - x0) / (x2 - x0) * ((x - x1) / (x2 - x1));
            return y0 * l0 + y1 * l1 + y2 * l2;
        }

        // ── Event handlers ────────────────────────────────────────────────────────
        private void sliderN_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsInitialized) return;
            int n = (int)sliderN.Value;
            if (n % 2 != 0) n++;
            Compute(n);
        }

        private void chartCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!IsInitialized) return;
            int n = (int)sliderN.Value;
            if (n % 2 != 0) n++;
            DrawChart(n, (B - A) / n);
        }
    }
}
