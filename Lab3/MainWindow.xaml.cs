using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace NonlinearSolver
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<IterationRow> _iterations = new();
        private readonly VariantDefinition[] _variants;

        public MainWindow()
        {
            InitializeComponent();
            _variants = VariantDefinitions.GetAll();
            IterationsGrid.ItemsSource = _iterations;

            foreach (var v in _variants)
                VariantComboBox.Items.Add($"Варіант {v.Number}");

            VariantComboBox.SelectedIndex = 0;
            Log("🚀 NonlinearSolver готовий до роботи.");
            Log("   Оберіть варіант або введіть власні рівняння.");
        }

        private void VariantComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VariantComboBox.SelectedIndex < 0) return;
            var v = _variants[VariantComboBox.SelectedIndex];
            MethodLabel.Text = v.MethodName;
            SystemDisplay.Text = v.SystemText;

            // Pre-fill initial guesses
            X0TextBox.Text = v.X0.ToString("G6");
            Y0TextBox.Text = v.Y0.ToString("G6");
            Z0TextBox.Text = v.Z0.HasValue ? v.Z0.Value.ToString("G6") : "0.0";
        }

        private void SolveVariant_Click(object sender, RoutedEventArgs e)
        {
            if (VariantComboBox.SelectedIndex < 0) return;
            var v = _variants[VariantComboBox.SelectedIndex];

            if (!TryGetParams(out double x0, out double y0, out double z0, out double eps, out int maxIter))
                return;

            Log($"\n▶ Розв'язання варіанту {v.Number}: {v.MethodName}");
            Log($"  Початкові значення: x₀={x0}, y₀={y0}" + (v.Is3D ? $", z₀={z0}" : ""));

            try
            {
                SolverResult result;
                switch (v.Method)
                {
                    case SolvingMethod.Newton:
                        result = NewtonMethod.Solve(v.F, v.J, x0, y0, v.Is3D ? z0 : double.NaN, eps, maxIter);
                        break;
                    case SolvingMethod.Seidel:
                        result = SeidelMethod.Solve(v.G, x0, y0, v.Is3D ? z0 : double.NaN, eps, maxIter);
                        break;
                    default:
                        result = SimpleIterationMethod.Solve(v.G, x0, y0, v.Is3D ? z0 : double.NaN, eps, maxIter);
                        break;
                }
                DisplayResult(result, v.MethodName);
            }
            catch (Exception ex)
            {
                Log($"❌ Помилка: {ex.Message}");
                StatusLabel.Text = "Помилка розрахунку";
            }
        }

        private void SolveCustom_Click(object sender, RoutedEventArgs e)
        {
            if (!TryGetParams(out double x0, out double y0, out double z0, out double eps, out int maxIter))
                return;

            string eq1Str = Eq1TextBox.Text.Trim();
            string eq2Str = Eq2TextBox.Text.Trim();
            string eq3Str = Eq3TextBox.Text.Trim();

            if (string.IsNullOrEmpty(eq1Str) || string.IsNullOrEmpty(eq2Str))
            {
                MessageBox.Show("Введіть принаймні два рівняння.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool is3D = !string.IsNullOrEmpty(eq3Str);
            string methodStr = (CustomMethodComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Метод Ньютона";

            Log($"\n▶ Власні рівняння | {methodStr}");
            Log($"  f₁: {eq1Str}");
            Log($"  f₂: {eq2Str}");
            if (is3D) Log($"  f₃: {eq3Str}");

            try
            {
                var parser = new ExpressionParser();

                Func<double[], double>[] fFuncs;
                if (is3D)
                    fFuncs = new[] {
                        parser.Parse(eq1Str, new[] {"x","y","z"}),
                        parser.Parse(eq2Str, new[] {"x","y","z"}),
                        parser.Parse(eq3Str, new[] {"x","y","z"})
                    };
                else
                    fFuncs = new[] {
                        parser.Parse(eq1Str, new[] {"x","y"}),
                        parser.Parse(eq2Str, new[] {"x","y"})
                    };

                SolverResult result;
                if (methodStr.Contains("Ньютона"))
                {
                    result = NewtonMethod.SolveNumericJacobian(fFuncs, x0, y0, is3D ? z0 : double.NaN, eps, maxIter);
                    DisplayResult(result, "Ньютона");
                }
                else if (methodStr.Contains("Зейделя"))
                {
                    result = SeidelMethod.SolveFromFunctions(fFuncs, x0, y0, is3D ? z0 : double.NaN, eps, maxIter);
                    DisplayResult(result, "Зейделя");
                }
                else
                {
                    result = SimpleIterationMethod.SolveFromFunctions(fFuncs, x0, y0, is3D ? z0 : double.NaN, eps, maxIter);
                    DisplayResult(result, "Проста ітерація");
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Помилка парсингу або обчислення: {ex.Message}");
                StatusLabel.Text = "Помилка";
            }
        }

        private void DisplayResult(SolverResult result, string methodName)
        {
            _iterations.Clear();
            foreach (var row in result.Iterations)
                _iterations.Add(row);

            UsedMethodLabel.Text = methodName;
            IterCountLabel.Text = result.Iterations.Count.ToString();

            if (result.Converged)
            {
                var last = result.Iterations.LastOrDefault();
                ErrorLabel.Text = last?.DeltaX1 ?? "—";
                StatusLabel.Text = $"✓ Збіжність! x₁={result.X1:G8}  x₂={result.X2:G8}" +
                                   (result.X3.HasValue ? $"  x₃={result.X3.Value:G8}" : "");
                StatusLabel.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(63, 185, 80));
                Log($"✓ Збіжність досягнута за {result.Iterations.Count} ітерацій.");
                Log($"  Розв'язок: x₁ = {result.X1:G10}");
                Log($"             x₂ = {result.X2:G10}");
                if (result.X3.HasValue)
                    Log($"             x₃ = {result.X3.Value:G10}");
            }
            else
            {
                ErrorLabel.Text = "∞";
                StatusLabel.Text = $"⚠ Метод не збігся за {result.Iterations.Count} ітерацій";
                StatusLabel.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(240, 136, 62));
                Log($"⚠ Метод не збігся. Спробуйте змінити початкові значення.");
            }
        }

        private bool TryGetParams(out double x0, out double y0, out double z0, out double eps, out int maxIter)
        {
            x0 = y0 = z0 = eps = 0; maxIter = 0;
            if (!double.TryParse(X0TextBox.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out x0) ||
                !double.TryParse(Y0TextBox.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out y0))
            {
                MessageBox.Show("Невірні початкові значення.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            double.TryParse(Z0TextBox.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out z0);
            if (!double.TryParse(EpsTextBox.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out eps) || eps <= 0)
                eps = 1e-4;
            if (!int.TryParse(MaxIterTextBox.Text, out maxIter) || maxIter <= 0)
                maxIter = 100;
            return true;
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            _iterations.Clear();
            StatusLabel.Text = "Очікування розрахунку...";
            StatusLabel.Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(139, 148, 158));
            IterCountLabel.Text = "—";
            ErrorLabel.Text = "—";
            UsedMethodLabel.Text = "—";
        }

        private void ClearLog_Click(object sender, RoutedEventArgs e) => LogTextBox.Clear();

        private void Log(string msg)
        {
            LogTextBox.AppendText(msg + "\n");
            LogScrollViewer.ScrollToEnd();
        }
    }

    // ==================== DATA MODELS ====================

    public class IterationRow
    {
        public string Iteration { get; set; } = "";
        public string X1 { get; set; } = "";
        public string X2 { get; set; } = "";
        public string X3 { get; set; } = "";
        public string DeltaX1 { get; set; } = "";
        public string DeltaX2 { get; set; } = "";
        public string DeltaX3 { get; set; } = "";
        public string FNorm { get; set; } = "";
    }

    public class SolverResult
    {
        public bool Converged { get; set; }
        public double X1 { get; set; }
        public double X2 { get; set; }
        public double? X3 { get; set; }
        public List<IterationRow> Iterations { get; set; } = new();
    }

    public enum SolvingMethod { Newton, Seidel, SimpleIteration }

    public class VariantDefinition
    {
        public int Number { get; set; }
        public string MethodName { get; set; } = "";
        public SolvingMethod Method { get; set; }
        public string SystemText { get; set; } = "";
        public bool Is3D { get; set; }
        public double X0 { get; set; }
        public double Y0 { get; set; }
        public double? Z0 { get; set; }
        // For Newton: system functions F(x) and Jacobian J(x)
        public Func<double[], double[]>? F { get; set; }
        public Func<double[], double[,]>? J { get; set; }
        // For Seidel/SimpleIteration: iteration functions G(x)
        public Func<double[], double[]>? G { get; set; }
    }

    // ==================== NEWTON METHOD ====================

    public static class NewtonMethod
    {
        public static SolverResult Solve(
            Func<double[], double[]>? F,
            Func<double[], double[,]>? J,
            double x0, double y0, double z0,
            double eps, int maxIter)
        {
            if (F == null) throw new ArgumentNullException(nameof(F));
            bool is3D = !double.IsNaN(z0);
            var result = new SolverResult();
            double[] P = is3D ? new[] { x0, y0, z0 } : new[] { x0, y0 };

            for (int k = 0; k < maxIter; k++)
            {
                double[] fVal = F(P);
                double[,] jVal = J != null ? J(P) : NumericJacobian(F, P);
                double fNorm = Math.Sqrt(fVal.Sum(v => v * v));

                double[] dP = SolveLinear(jVal, fVal.Select(v => -v).ToArray());

                var row = new IterationRow
                {
                    Iteration = k.ToString(),
                    X1 = P[0].ToString("G8"),
                    X2 = P[1].ToString("G8"),
                    X3 = is3D ? P[2].ToString("G8") : "—",
                    DeltaX1 = Math.Abs(dP[0]).ToString("E4"),
                    DeltaX2 = Math.Abs(dP[1]).ToString("E4"),
                    DeltaX3 = is3D ? Math.Abs(dP[2]).ToString("E4") : "—",
                    FNorm = fNorm.ToString("E4")
                };
                result.Iterations.Add(row);

                double[] P_new = P.Zip(dP, (a, b) => a + b).ToArray();

                double maxDelta = dP.Select(Math.Abs).Max();
                if (maxDelta < eps && fNorm < eps * 10)
                {
                    result.Converged = true;
                    result.X1 = P_new[0];
                    result.X2 = P_new[1];
                    result.X3 = is3D ? P_new[2] : (double?)null;
                    // Add final row
                    double[] fFinal = F(P_new);
                    result.Iterations.Add(new IterationRow
                    {
                        Iteration = (k + 1).ToString() + "*",
                        X1 = P_new[0].ToString("G8"),
                        X2 = P_new[1].ToString("G8"),
                        X3 = is3D ? P_new[2].ToString("G8") : "—",
                        DeltaX1 = "0",
                        DeltaX2 = "0",
                        DeltaX3 = is3D ? "0" : "—",
                        FNorm = Math.Sqrt(fFinal.Sum(v => v * v)).ToString("E4")
                    });
                    return result;
                }
                P = P_new;
            }

            result.X1 = P[0]; result.X2 = P[1];
            result.X3 = is3D ? P[2] : (double?)null;
            return result;
        }

        public static SolverResult SolveNumericJacobian(
            Func<double[], double>[] fFuncs, double x0, double y0, double z0, double eps, int maxIter)
        {
            bool is3D = !double.IsNaN(z0);
            Func<double[], double[]> F = p => fFuncs.Select(f => f(p)).ToArray();
            return Solve(F, null, x0, y0, z0, eps, maxIter);
        }

        public static double[,] NumericJacobian(Func<double[], double[]> F, double[] P)
        {
            double h = 1e-7;
            int n = P.Length;
            double[] f0 = F(P);
            double[,] J = new double[n, n];
            for (int j = 0; j < n; j++)
            {
                double[] Pj = (double[])P.Clone();
                Pj[j] += h;
                double[] fj = F(Pj);
                for (int i = 0; i < n; i++)
                    J[i, j] = (fj[i] - f0[i]) / h;
            }
            return J;
        }

        public static double[] SolveLinear(double[,] A, double[] b)
        {
            int n = b.Length;
            double[,] M = new double[n, n + 1];
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++) M[i, j] = A[i, j];
                M[i, n] = b[i];
            }
            // Gaussian elimination with partial pivoting
            for (int col = 0; col < n; col++)
            {
                int maxRow = col;
                for (int row = col + 1; row < n; row++)
                    if (Math.Abs(M[row, col]) > Math.Abs(M[maxRow, col])) maxRow = row;
                for (int j = 0; j <= n; j++) { double tmp = M[col, j]; M[col, j] = M[maxRow, j]; M[maxRow, j] = tmp; }
                if (Math.Abs(M[col, col]) < 1e-12) throw new InvalidOperationException("Матриця Якобі вироджена");
                for (int row = col + 1; row < n; row++)
                {
                    double factor = M[row, col] / M[col, col];
                    for (int j = col; j <= n; j++) M[row, j] -= factor * M[col, j];
                }
            }
            double[] x = new double[n];
            for (int i = n - 1; i >= 0; i--)
            {
                x[i] = M[i, n];
                for (int j = i + 1; j < n; j++) x[i] -= M[i, j] * x[j];
                x[i] /= M[i, i];
            }
            return x;
        }
    }

    // ==================== SEIDEL METHOD ====================

    public static class SeidelMethod
    {
        public static SolverResult Solve(Func<double[], double[]>? G, double x0, double y0, double z0,
            double eps, int maxIter)
        {
            if (G == null) throw new ArgumentNullException(nameof(G));
            bool is3D = !double.IsNaN(z0);
            var result = new     SolverResult();
            double[] P = is3D ? new[] { x0, y0, z0 } : new[] { x0, y0 };

            for (int k = 0; k < maxIter; k++)
            {
                double[] P_new = (double[])P.Clone();        // Клонуємо старі значення у новий масив
                for (int i = 0; i < P.Length; i++)
                {
                    double[] args = (double[])P_new.Clone(); // Копіюємо стан масиву НА ЦЕЙ МОМЕНТ
                    double[] gAll = G(args);                 // Рахуємо функції переходу
                    P_new[i] = gAll[i];                      // Одразу оновлюємо i-ту змінну
                }

                double[] delta = P.Zip(P_new, (a, b) => Math.Abs(b - a)).ToArray();
                double fNorm = delta.Max();

                var row = new IterationRow
                {
                    Iteration = k.ToString(),
                    X1 = P[0].ToString("G8"),
                    X2 = P[1].ToString("G8"),
                    X3 = is3D ? P[2].ToString("G8") : "—",
                    DeltaX1 = delta[0].ToString("E4"),
                    DeltaX2 = delta[1].ToString("E4"),
                    DeltaX3 = is3D ? delta[2].ToString("E4") : "—",
                    FNorm = fNorm.ToString("E4")
                };
                result.Iterations.Add(row);

                if (delta.Max() < eps)
                {
                    result.Converged = true;
                    result.X1 = P_new[0]; result.X2 = P_new[1];
                    result.X3 = is3D ? P_new[2] : (double?)null;
                    return result;
                }
                P = P_new;
            }
            result.X1 = P[0]; result.X2 = P[1];
            result.X3 = is3D ? P[2] : (double?)null;
            return result;
        }

        public static SolverResult SolveFromFunctions(Func<double[], double>[] fFuncs,
            double x0, double y0, double z0, double eps, int maxIter)
        {
            bool is3D = !double.IsNaN(z0);
            int n = is3D ? 3 : 2;
            // Build iteration functions: g_i = x_i - alpha_i * f_i
            Func<double[], double[]> G = p =>
            {
                double[] res = new double[n];
                for (int i = 0; i < n; i++)
                    res[i] = p[i] - fFuncs[i](p);
                return res;
            };
            return Solve(G, x0, y0, z0, eps, maxIter);
        }
    }

    // ==================== SIMPLE ITERATION ====================

    public static class SimpleIterationMethod
    {
        public static SolverResult Solve(Func<double[], double[]>? G, double x0, double y0, double z0,
            double eps, int maxIter)
        {
            if (G == null) throw new ArgumentNullException(nameof(G));
            bool is3D = !double.IsNaN(z0);
            var result = new SolverResult();
            double[] P = is3D ? new[] { x0, y0, z0 } : new[] { x0, y0 };

            for (int k = 0; k < maxIter; k++)
            {
                double[] P_new = G(P);
                double[] delta = P.Zip(P_new, (a, b) => Math.Abs(b - a)).ToArray();

                var row = new IterationRow
                {
                    Iteration = k.ToString(),
                    X1 = P[0].ToString("G8"),
                    X2 = P[1].ToString("G8"),
                    X3 = is3D ? P[2].ToString("G8") : "—",
                    DeltaX1 = delta[0].ToString("E4"),
                    DeltaX2 = delta[1].ToString("E4"),
                    DeltaX3 = is3D ? delta[2].ToString("E4") : "—",
                    FNorm = delta.Max().ToString("E4")
                };
                result.Iterations.Add(row);

                if (delta.Max() < eps)
                {
                    result.Converged = true;
                    result.X1 = P_new[0]; result.X2 = P_new[1];
                    result.X3 = is3D ? P_new[2] : (double?)null;
                    return result;
                }
                P = P_new;
            }
            result.X1 = P[0]; result.X2 = P[1];
            result.X3 = is3D ? P[2] : (double?)null;
            return result;
        }

        public static SolverResult SolveFromFunctions(Func<double[], double>[] fFuncs,
            double x0, double y0, double z0, double eps, int maxIter)
        {
            bool is3D = !double.IsNaN(z0);
            int n = is3D ? 3 : 2;
            Func<double[], double[]> G = p =>
            {
                double[] res = new double[n];
                for (int i = 0; i < n; i++)
                    res[i] = p[i] - fFuncs[i](p);
                return res;
            };
            return Solve(G, x0, y0, z0, eps, maxIter);
        }
    }

    // ==================== EXPRESSION PARSER ====================

    public class ExpressionParser
    {
        public Func<double[], double> Parse(string expr, string[] varNames)
        {
            string e = expr.Trim()
                .Replace("^", "**")
                .Replace("tg(", "tan(")
                .Replace("ln(", "log(");
            return args =>
            {
                var vars = new Dictionary<string, double>();
                for (int i = 0; i < varNames.Length && i < args.Length; i++)
                    vars[varNames[i]] = args[i];
                return EvalExpr(e, vars);
            };
        }

        private double EvalExpr(string expr, Dictionary<string, double> vars)
        {
            expr = expr.Trim();

            // Try parse as a number
            if (double.TryParse(expr, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double num))
                return num;

            // Try as variable
            if (vars.ContainsKey(expr)) return vars[expr];

            // Handle parentheses wrapping entire expression
            if (expr.StartsWith("(") && expr.EndsWith(")") && MatchingParen(expr, 0) == expr.Length - 1)
                return EvalExpr(expr.Substring(1, expr.Length - 2), vars);

            // Find lowest-precedence operator outside parentheses (right to left for left-assoc)
            int depth = 0;
            int addSubPos = -1, mulDivPos = -1, powPos = -1;

            for (int i = expr.Length - 1; i >= 0; i--)
            {
                char c = expr[i];
                if (c == ')') depth++;
                else if (c == '(') depth--;
                if (depth != 0) continue;

                if ((c == '+' || c == '-') && i > 0)
                {
                    // Make sure it's a binary operator, not unary
                    char prev = expr[i - 1];
                    if (prev != '(' && prev != '+' && prev != '-' && prev != '*' && prev != '/' && prev != '^')
                    { addSubPos = i; break; }
                }
                if (c == '*' || c == '/') { mulDivPos = i; }
                if (c == '*' || c == '*') { } // handled above
            }

            // Scan left to right for + and -
            depth = 0;
            for (int i = 0; i < expr.Length; i++)
            {
                char c = expr[i];
                if (c == '(') depth++;
                else if (c == ')') depth--;
                if (depth != 0) continue;
                if ((c == '+' || c == '-') && i > 0)
                {
                    char prev = expr[i - 1];
                    if (prev != '(' && prev != 'e' && prev != 'E')
                    {
                        double left = EvalExpr(expr.Substring(0, i), vars);
                        double right = EvalExpr(expr.Substring(i + 1), vars);
                        return c == '+' ? left + right : left - right;
                    }
                }
            }

            // Scan for * and /
            depth = 0;
            for (int i = 0; i < expr.Length; i++)
            {
                char c = expr[i];
                if (c == '(') depth++;
                else if (c == ')') depth--;
                if (depth != 0) continue;
                if (c == '*' || c == '/')
                {
                    double left = EvalExpr(expr.Substring(0, i), vars);
                    double right = EvalExpr(expr.Substring(i + 1), vars);
                    return c == '*' ? left * right : left / right;
                }
            }

            // Scan for **
            depth = 0;
            for (int i = 0; i < expr.Length - 1; i++)
            {
                char c = expr[i]; char c2 = expr[i + 1];
                if (c == '(') depth++;
                else if (c == ')') depth--;
                if (depth != 0) continue;
                if (c == '*' && c2 == '*')
                {
                    double left = EvalExpr(expr.Substring(0, i), vars);
                    double right = EvalExpr(expr.Substring(i + 2), vars);
                    return Math.Pow(left, right);
                }
            }

            // Unary minus
            if (expr.StartsWith("-"))
                return -EvalExpr(expr.Substring(1), vars);

            // Functions
            foreach (var (name, fn) in new[] {
                ("sqrt(", (Func<double,double>)Math.Sqrt),
                ("sin(", Math.Sin), ("cos(", Math.Cos),
                ("tan(", Math.Tan), ("log(", Math.Log),
                ("exp(", Math.Exp), ("abs(", Math.Abs) })
            {
                if (expr.StartsWith(name))
                {
                    string inner = expr.Substring(name.Length, expr.Length - name.Length - 1);
                    return fn(EvalExpr(inner, vars));
                }
            }

            throw new ArgumentException($"Неможливо обчислити: '{expr}'");
        }

        private int MatchingParen(string s, int pos)
        {
            int depth = 0;
            for (int i = pos; i < s.Length; i++)
            {
                if (s[i] == '(') depth++;
                else if (s[i] == ')') { depth--; if (depth == 0) return i; }
            }
            return -1;
        }
    }

    // ==================== VARIANT DEFINITIONS ====================

    public static class VariantDefinitions
    {
        public static VariantDefinition[] GetAll() => new[]
        {
            // Варіант 1: Newton
            new VariantDefinition {
                Number = 1, Method = SolvingMethod.Newton, MethodName = "Метод Ньютона",
                SystemText = "2x₁ - 4x₂³ - 1.5 = 0\nx₁³ - 5x₂² + 0.25 = 0",
                Is3D = false, X0 = 1.0, Y0 = 0.5,
                F = p => new[] {
                    2*p[0] - 4*Math.Pow(p[1],3) - 1.5,
                    Math.Pow(p[0],3) - 5*p[1]*p[1] + 0.25 },
                J = p => new double[,] {
                    { 2, -12*p[1]*p[1] },
                    { 3*p[0]*p[0], -10*p[1] } }
            },
            // Варіант 2: Seidel
            new VariantDefinition {
                Number = 2, Method = SolvingMethod.Seidel, MethodName = "Метод Зейделя",
                SystemText = "2x₁² - 3x₂³ - 3 = 0\nx₁ + 2x₂² - 5 = 0",
                Is3D = false, X0 = 1.5, Y0 = 1.0,
                G = p => new[] {
                    Math.Sqrt(Math.Abs((3*Math.Pow(p[1],3) + 3) / 2)) * Math.Sign(p[0]),
                    Math.Sqrt(Math.Abs((5 - p[0]) / 2)) * Math.Sign(p[1]) }
            },
            // Варіант 3: Simple Iteration
            new VariantDefinition {
                Number = 3, Method = SolvingMethod.SimpleIteration, MethodName = "Проста ітерація",
                SystemText = "x + xy³ = 9\nxy - 4·x·tg(y) = 1.86",
                Is3D = false, X0 = 2.0, Y0 = 1.0,
                G = p => new[] {
                    9.0 / (1 + p[1]*p[1]*p[1]),
                    p[0] > 0.1 ? (1.86 + 4*p[0]*Math.Tan(p[1])) / p[0] : p[1] }
            },
            // Варіант 4: Seidel
            new VariantDefinition {
                Number = 4, Method = SolvingMethod.Seidel, MethodName = "Метод Зейделя",
                SystemText = "x₁ - (3/2)cos(x₂) = 0\nx₁ + 9x₂² - 1 = 0",
                Is3D = false, X0 = 0.5, Y0 = 0.3,
                G = p => new[] {
                    1.5 * Math.Cos(p[1]),
                    Math.Sqrt(Math.Abs((1 - p[0]) / 9)) * Math.Sign(p[1]) }
            },
            // Варіант 5: Newton
            new VariantDefinition {
                Number = 5, Method = SolvingMethod.Newton, MethodName = "Метод Ньютона",
                SystemText = "sin(x) + √(2y³) = 4\ntg(x) - y² = -4",
                Is3D = false, X0 = 1.5, Y0 = 2.0,
                F = p => new[] {
                    Math.Sin(p[0]) + Math.Sqrt(2*Math.Pow(Math.Abs(p[1]),3)) - 4,
                    Math.Tan(p[0]) - p[1]*p[1] + 4 },
                J = p => new double[,] {
                    { Math.Cos(p[0]), 3*Math.Sqrt(2)*Math.Sqrt(Math.Abs(p[1])) },
                    { 1.0/(Math.Cos(p[0])*Math.Cos(p[0])), -2*p[1] } }
            },
            // Варіант 6: Simple Iteration
            new VariantDefinition {
                Number = 6, Method = SolvingMethod.SimpleIteration, MethodName = "Проста ітерація",
                SystemText = "x₁² - 3.5·x₁·x₂ + 3x₂³ = 0\n3x₁ - 8x₁³x₂² - 4.7x₂ = 0",
                Is3D = false, X0 = 1.0, Y0 = 0.5,
                G = p => new[] {
                    p[0] - 0.1*(p[0]*p[0] - 3.5*p[0]*p[1] + 3*Math.Pow(p[1],3)),
                    p[1] - 0.1*(3*p[0] - 8*Math.Pow(p[0],3)*p[1]*p[1] - 4.7*p[1]) }
            },
            // Варіант 7: Newton
            new VariantDefinition {
                Number = 7, Method = SolvingMethod.Newton, MethodName = "Метод Ньютона",
                SystemText = "4x₁ + 11x₂² = 0\n11x₁ + 7x₂³ + 33 = 0",
                Is3D = false, X0 = -1.0, Y0 = -1.0,
                F = p => new[] {
                    4*p[0] + 11*p[1]*p[1],
                    11*p[0] + 7*Math.Pow(p[1],3) + 33 },
                J = p => new double[,] {
                    { 4, 22*p[1] },
                    { 11, 21*p[1]*p[1] } }
            },
            // Варіант 8: Seidel
            new VariantDefinition {
                Number = 8, Method = SolvingMethod.Seidel, MethodName = "Метод Зейделя",
                SystemText = "8sin(x) - 3y - 0.56 = 0\nx - cos(y)² = 0",
                Is3D = false, X0 = 0.5, Y0 = 0.5,
                G = p => new[] {
                    Math.Cos(p[1]) * Math.Cos(p[1]),
                    (8*Math.Sin(p[0]) - 0.56) / 3.0 }
            },
            // Варіант 9: Simple Iteration
            new VariantDefinition {
                Number = 9, Method = SolvingMethod.SimpleIteration, MethodName = "Проста ітерація",
                SystemText = "2x² - xy - 4y + 2 = 0\nx - 2ln(y) - 2y² = 0",
                Is3D = false, X0 = 2.0, Y0 = 0.5,
                G = p => new[] {
                    p[1] > 0.01 ? Math.Sqrt(Math.Abs((p[0]*p[1] + 4*p[1] - 2) / 2)) : 0.1,
                    p[1] > 0.01 ? Math.Exp((p[0] - 2*p[1]*p[1]) / 2) : 0.5 }
            },
            // Варіант 10: Newton
            new VariantDefinition {
                Number = 10, Method = SolvingMethod.Newton, MethodName = "Метод Ньютона",
                SystemText = "sin(x₁) - x₂² = 0\ntg(x₁)² - x₂ = 0",
                Is3D = false, X0 = 1.0, Y0 = 0.8,
                F = p => new[] {
                    Math.Sin(p[0]) - p[1]*p[1],
                    Math.Tan(p[0])*Math.Tan(p[0]) - p[1] },
                J = p => {
                    double t = Math.Tan(p[0]); double c = Math.Cos(p[0]);
                    return new double[,] {
                        { Math.Cos(p[0]), -2*p[1] },
                        { 2*t/(c*c), -1 } }; }
            },
            // Варіант 11: Seidel (3D)
            new VariantDefinition {
                Number = 11, Method = SolvingMethod.Seidel, MethodName = "Метод Зейделя",
                SystemText = "x₁ + 2x₂² + 3x₃³ = 0\n3x₁³ + x₂ + 2x₃² = 0\nx₁² + 8x₂³ + x₃ = 0",
                Is3D = true, X0 = -0.5, Y0 = -0.5, Z0 = -0.5,
                G = p => new[] {
                    -2*p[1]*p[1] - 3*Math.Pow(p[2],3),
                    -3*Math.Pow(p[0],3) - 2*p[2]*p[2],
                    -p[0]*p[0] - 8*Math.Pow(p[1],3) }
            },
            // Варіант 12: Simple Iteration (3D)
            new VariantDefinition {
                Number = 12, Method = SolvingMethod.SimpleIteration, MethodName = "Проста ітерація",
                SystemText = "3x₁ + x₂² - 5x₃ = 0\n4x₁ + x₂·tg(x₂) = 0\n8x₁ - x₂ + 2x₃² = 0",
                Is3D = true, X0 = 0.1, Y0 = 0.1, Z0 = 0.1,
                G = p => new[] {
                    (5*p[2] - p[1]*p[1]) / 3.0,
                    p[1] - 0.1*(4*p[0] + p[1]*Math.Tan(p[1])),
                    Math.Sqrt(Math.Abs((p[1] - 8*p[0]) / 2)) * Math.Sign(p[2]) }
            },
            // Варіант 13: Newton (3D)
            new VariantDefinition {
                Number = 13, Method = SolvingMethod.Newton, MethodName = "Метод Ньютона",
                SystemText = "x₁ - tg(x₂) + x₃ = 0\n2x₁ - cos(x₂) = 0\nx₁ + sin(x₂)² - 3x₃³ = 0",
                Is3D = true, X0 = 0.5, Y0 = 0.3, Z0 = 0.2,
                F = p => new[] {
                    p[0] - Math.Tan(p[1]) + p[2],
                    2*p[0] - Math.Cos(p[1]),
                    p[0] + Math.Sin(p[1])*Math.Sin(p[1]) - 3*Math.Pow(p[2],3) },
                J = p => new double[,] {
                    { 1, -1.0/(Math.Cos(p[1])*Math.Cos(p[1])), 1 },
                    { 2, Math.Sin(p[1]), 0 },
                    { 1, Math.Sin(2*p[1]), -9*p[2]*p[2] } }
            },
            // Варіант 14: Seidel (3D)
            new VariantDefinition {
                Number = 14, 
                Method = SolvingMethod.Seidel, 
                MethodName = "Метод Зейделя",
                SystemText = "x₁² + 2x₂² + 3x₃² = 0\n3x₁ + x₂³ + 8x₃ = 0\n5x₁² + 8x₂ + 7x₃² = 0",
                Is3D = true, 
                X0 = 0.1, 
                Y0 = 0.1, 
                Z0 = 0.1,
                G = p => new[] {
                    // З рівн.1 (метод релаксації з кроком 0.5, щоб уникнути квадратного кореня з мінуса)
                    p[0] - 0.5 * (p[0] * p[0] + 2 * p[1] * p[1] + 3 * p[2] * p[2]),
        
                    // З рівн.2: x2 = (-5*x1^2 - 7*x3^2) / 8
                    (-5 * p[0] * p[0] - 7 * p[2] * p[2]) / 8.0,
        
                    // З рівн.3: x3 = (-3*x1 - x2^3) / 8
                    (-3 * p[0] - Math.Pow(p[1], 3)) / 8.0
                }
            },
            // Варіант 15: Simple Iteration
            new VariantDefinition {
                Number = 15, Method = SolvingMethod.SimpleIteration, MethodName = "Проста ітерація",
                SystemText = "2x³ - xy + 4y² = 1\n3x² - 2x²y - y = 0",
                Is3D = false, X0 = 1.0, Y0 = 0.5,
                G = p => new[] {
                    p[0] - 0.05*(2*Math.Pow(p[0],3) - p[0]*p[1] + 4*p[1]*p[1] - 1),
                    p[1] - 0.05*(3*p[0]*p[0] - 2*p[0]*p[0]*p[1] - p[1]) }
            },
            // Варіант 16: Newton (3D)
            new VariantDefinition {
                Number = 16, Method = SolvingMethod.Newton, MethodName = "Метод Ньютона",
                SystemText = "sin(x₁) - tg(x₂) + cos(x₃) = 0\ntg(x₁) - x₂ - cos(x₃)² = 0\ntg(x₁²) + 4x₂ - cos(x₃) = 0",
                Is3D = true, X0 = 0.5, Y0 = 0.5, Z0 = 1.0,
                F = p => new[] {
                    Math.Sin(p[0]) - Math.Tan(p[1]) + Math.Cos(p[2]),
                    Math.Tan(p[0]) - p[1] - Math.Cos(p[2])*Math.Cos(p[2]),
                    Math.Tan(p[0]*p[0]) + 4*p[1] - Math.Cos(p[2]) },
                J = p => {
                    double c0 = Math.Cos(p[0]); double c1 = Math.Cos(p[1]);
                    double s2 = Math.Sin(p[2]); double c2 = Math.Cos(p[2]);
                    return new double[,] {
                        { c0, -1.0/(c1*c1), -s2 },
                        { 1.0/(c0*c0), -1, 2*c2*s2 },
                        { 2*p[0]/(Math.Cos(p[0]*p[0])*Math.Cos(p[0]*p[0])), 4, s2 } }; }
            },
            // Варіант 17: Seidel
            new VariantDefinition {
                Number = 17, Method = SolvingMethod.Seidel, MethodName = "Метод Зейделя",
                SystemText = "5x² + 3x - y² = 0.55\n8x² - y + 2y² = 0.32",
                Is3D = false, X0 = 0.3, Y0 = 1.0,
                G = p => new[] {
                    (-3 + Math.Sqrt(9 + 4*5*(p[1]*p[1] + 0.55))) / (2*5),
                    (8*p[0]*p[0] - 0.32) / (1 - 2*p[1]) }
            },
            // Варіант 18: Simple Iteration (3D)
            new VariantDefinition {
                Number = 18, Method = SolvingMethod.SimpleIteration, MethodName = "Проста ітерація",
                SystemText = "x₁ + 2x₂² + 3x₃³ = 0\n3x₁³ + x₂ + 2x₃² = 0\nx₁² + 8x₂³ + x₃ = 0",
                Is3D = true, X0 = -0.1, Y0 = -0.1, Z0 = -0.1,
                G = p => new[] {
                    -2*p[1]*p[1] - 3*Math.Pow(p[2],3),
                    -3*Math.Pow(p[0],3) - 2*p[2]*p[2],
                    -p[0]*p[0] - 8*Math.Pow(p[1],3) }
            }
        };
    }
}
