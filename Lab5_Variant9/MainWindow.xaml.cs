using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Lab5_Variant10
{
    public partial class MainWindow : Window
    {
        // Межі інтегрування
        private const double A = 0.0;
        private const double B = 5.0;

        public MainWindow()
        {
            InitializeComponent();

            Loaded += (s, e) =>
            {
                Compute((int)sliderN.Value);
            };
        }

        // ============================================
        // ФУНКЦІЯ ВАРІАНТУ 9
        // u(x) = 3 + sin(2x)/2
        // ============================================
        private static double U(double x)
        {
            return 3.0 + Math.Sin(2.0 * x) / 2.0;
        }

        // ============================================
        // МЕТОД ТРАПЕЦІЙ
        // ============================================
        private static double Trapezoidal(double a, double b, int n)
        {
            double h = (b - a) / n;

            double sum = (U(a) + U(b)) / 2.0;

            for (int i = 1; i < n; i++)
            {
                double x = a + i * h;

                sum += U(x);
            }

            return h * sum;
        }

        // ============================================
        // "ТОЧНЕ" ЗНАЧЕННЯ
        // ============================================
        private static double Reference()
        {
            return Trapezoidal(A, B, 100000);
        }

        // ============================================
        // ОСНОВНІ ОБЧИСЛЕННЯ
        // ============================================
        private void Compute(int n)
        {
            double h = (B - A) / n;

            double result = Trapezoidal(A, B, n);

            double reference = Reference();

            double error = Math.Abs(result - reference);

            txtN.Text = n.ToString();

            txtH.Text = $"h = {h:F6}";

            txtResult.Text = $"{result:F8}";

            txtAnalytical.Text = $"{reference:F8}";

            txtError.Text = $"{error:E6}";

            txtNodeInfo.Text =
                $"Метод: Формула трапецій\n" +
                $"Варіант: 9\n\n" +
                $"Функція:\n" +
                $"u(x) = 3 + sin(2x)/2\n\n" +
                $"Підінтервали: {n}\n" +
                $"Вузли: {n + 1}\n" +
                $"Крок h = {h:F6}";

            txtStatus.Text =
                $"Інтеграл обчислено. Похибка: {error:E4}";

            DrawChart(n, h);
        }

        // ============================================
        // SLIDER
        // ============================================
        private void sliderN_ValueChanged(
            object sender,
            RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded)
                return;

            Compute((int)sliderN.Value);
        }

        // ============================================
        // ЗМІНА РОЗМІРУ CANVAS
        // ============================================
        private void chartCanvas_SizeChanged(
            object sender,
            SizeChangedEventArgs e)
        {
            if (!IsLoaded)
                return;

            int n = (int)sliderN.Value;

            double h = (B - A) / n;

            DrawChart(n, h);
        }

        // ============================================
        // ПОБУДОВА ГРАФІКА
        // ============================================
        private void DrawChart(int n, double h)
        {
            var canvas = chartCanvas;

            if (canvas.ActualWidth < 10 ||
                canvas.ActualHeight < 10)
                return;

            canvas.Children.Clear();

            double cw = canvas.ActualWidth;
            double ch = canvas.ActualHeight;

            double padL = 50;
            double padR = 20;
            double padT = 20;
            double padB = 40;

            double plotW = cw - padL - padR;
            double plotH = ch - padT - padB;

            double yMin = 2.0;
            double yMax = 4.0;

            double ToCanvasX(double x)
            {
                return padL + (x - A) / (B - A) * plotW;
            }

            double ToCanvasY(double y)
            {
                return padT +
                       (1 - (y - yMin) / (yMax - yMin))
                       * plotH;
            }

            // Осі
            Line axisX = new Line
            {
                X1 = padL,
                Y1 = ToCanvasY(0),
                X2 = cw - padR,
                Y2 = ToCanvasY(0),
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };

            Line axisY = new Line
            {
                X1 = padL,
                Y1 = padT,
                X2 = padL,
                Y2 = ch - padB,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };

            canvas.Children.Add(axisX);
            canvas.Children.Add(axisY);

            // Графік
            Polyline graph = new Polyline
            {
                Stroke = Brushes.Blue,
                StrokeThickness = 2
            };

            int samples = 500;

            for (int i = 0; i <= samples; i++)
            {
                double x = A + (B - A) * i / samples;

                double y = U(x);

                graph.Points.Add(
                    new Point(ToCanvasX(x),
                    ToCanvasY(y)));
            }

            canvas.Children.Add(graph);

            // Трапеції
            for (int i = 0; i < n; i++)
            {
                double x1 = A + i * h;
                double x2 = x1 + h;

                double y1 = U(x1);
                double y2 = U(x2);

                Polygon trap = new Polygon
                {
                    Stroke = Brushes.DarkOrange,
                    Fill = Brushes.Gold,
                    Opacity = 0.4,
                    StrokeThickness = 1
                };

                trap.Points.Add(
                    new Point(ToCanvasX(x1),
                    ToCanvasY(0)));

                trap.Points.Add(
                    new Point(ToCanvasX(x1),
                    ToCanvasY(y1)));

                trap.Points.Add(
                    new Point(ToCanvasX(x2),
                    ToCanvasY(y2)));

                trap.Points.Add(
                    new Point(ToCanvasX(x2),
                    ToCanvasY(0)));

                canvas.Children.Add(trap);
            }
        }
    }
}