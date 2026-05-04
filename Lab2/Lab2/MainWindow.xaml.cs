using System;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows;

namespace LinearSolver
{
    public partial class MainWindow : Window
    {
        DataTable table = new DataTable();

        // 🔷 ВСІ варіанти
        double[][,] examples = new double[][,]
        {
            new double[,] { {2,1,-5,17},{3,4.5,4,25},{4,2,1,20} },
            new double[,] { {3,2,1,10},{3,1,4,12},{5,-8,1,18} },
            new double[,] { {3,-1,-1.5,1},{2,-1,3,-3.5},{1,-1,-0.5,0.75} },
            new double[,] { {4,3,-1,16},{0.5,1,1,20},{3.5,0,1,24} },
            new double[,] { {4,7,4,-17},{1,2,6,12},{9,5,3,14} },
            new double[,] { {-2,-3,11,-15},{1,12,-5,40},{-1,1,-1,35} },
            new double[,] { {4,1,2,3},{5,2.2,1.5,11},{12,3.8,14,5} },
            new double[,] { {5,2,1,3},{1,3,7,14},{4,6,-1,21} },
            new double[,] { {-2,1,1,15},{1,-2,3,10},{-1,3,-6,-12} },
            new double[,] { {3,2,4,8},{5,4,1,3},{6,8,3,12} }
        };

        public MainWindow()
        {
            InitializeComponent();

            for (int i = 0; i < examples.Length; i++)
                ExampleBox.Items.Add("Варіант " + (i + 1));

            ExampleBox.SelectedIndex = 0;
        }

        private void CreateMatrix_Click(object sender, RoutedEventArgs e)
        {
            int n = int.Parse(SizeBox.Text);
            table = new DataTable();

            for (int i = 0; i < n; i++)
                table.Columns.Add("x" + (i + 1));

            table.Columns.Add("b");

            for (int i = 0; i < n; i++)
                table.Rows.Add(table.NewRow());

            MatrixGrid.ItemsSource = table.DefaultView;
        }

        private void FillExample_Click(object sender, RoutedEventArgs e)
        {
            var ex = examples[ExampleBox.SelectedIndex];
            int n = ex.GetLength(0);

            SizeBox.Text = n.ToString();
            CreateMatrix_Click(null, null);

            for (int i = 0; i < n; i++)
                for (int j = 0; j < n + 1; j++)
                    table.Rows[i][j] = ex[i, j];
        }

        private void Solve_Click(object sender, RoutedEventArgs e)
        {
            int n = table.Rows.Count;

            double[,] A = new double[n, n];
            double[] b = new double[n];

            for (int i = 0; i < n; i++)
                for (int j = 0; j < n + 1; j++)
                    if (!double.TryParse(table.Rows[i][j].ToString(), out _))
                    {
                        MessageBox.Show($"Помилка: рядок {i+1}, стовпець {j+1}");
                        return;
                    }

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                    A[i, j] = Convert.ToDouble(table.Rows[i][j]);

                b[i] = Convert.ToDouble(table.Rows[i][n]);
            }

            StringBuilder steps = new StringBuilder();
            double[] result = MethodBox.SelectedIndex == 0 ? Gauss(A, b, steps) : LU(A, b, steps);

            StepsBox.Text = steps.ToString();
            ResultText.Text = string.Join("\n", result.Select((x, i) => $"x{i + 1} = {x:F3}"));
        }

        private void MatrixGrid_CellEditEnding(object sender, System.Windows.Controls.DataGridCellEditEndingEventArgs e)
        {
            var tb = e.EditingElement as System.Windows.Controls.TextBox;
            if (tb != null && !double.TryParse(tb.Text, out _))
                tb.Background = System.Windows.Media.Brushes.DarkRed;
        }

        // --- Гаус
        double[] Gauss(double[,] A, double[] b, StringBuilder steps)
        {
            int n = b.Length;

            for (int k = 0; k < n; k++)
            {
                steps.AppendLine($"Крок {k + 1}");

                for (int i = k + 1; i < n; i++)
                {
                    double f = A[i, k] / A[k, k];
                    steps.AppendLine($"R{i+1} = R{i+1} - {f:F2}*R{k+1}");

                    for (int j = k; j < n; j++)
                        A[i, j] -= f * A[k, j];

                    b[i] -= f * b[k];
                }
            }

            double[] x = new double[n];
            for (int i = n - 1; i >= 0; i--)
            {
                x[i] = b[i];
                for (int j = i + 1; j < n; j++)
                    x[i] -= A[i, j] * x[j];

                x[i] /= A[i, i];
            }

            return x;
        }

        // --- LU
        double[] LU(double[,] A, double[] b, StringBuilder steps)
        {
            int n = b.Length; // Розмірність системи (кількість невідомих)
            double[,] L = new double[n, n]; // Нижня трикутна матриця (Lower)
            double[,] U = new double[n, n]; // Верхня трикутна матриця (Upper)

            // --- ЕТАП 1: РОЗКЛАД МАТРИЦІ А НА L ТА U ---
            for (int i = 0; i < n; i++)
            {
                steps.AppendLine($"\nКрок {i + 1}:");

                // Обчислення елементів матриці U (Верхня трикутна)
                // Вона включає значення на головній діагоналі та над нею.
                for (int k = i; k < n; k++)
                {
                    double sum = 0;
                    for (int j = 0; j < i; j++)
                        sum += L[i, j] * U[j, k]; // Сума добутків попередніх рядків L та стовпців U

                    U[i, k] = A[i, k] - sum; // Значення U - це різниця між оригіналом та накопиченою сумою

                    steps.AppendLine($"U[{i + 1},{k + 1}] = {U[i, k]:F2}");
                }

                // Обчислення елементів матриці L (Нижня трикутна)
                // Вона включає значення під головною діагоналлю.
                for (int k = i; k < n; k++)
                {
                    if (i == k)
                    {
                        L[i, i] = 1; // На головній діагоналі матриці L завжди стоять одиниці
                        steps.AppendLine($"L[{i + 1},{i + 1}] = 1");
                    }
                    else
                    {
                        double sum = 0;
                        for (int j = 0; j < i; j++)
                            sum += L[k, j] * U[j, i];

                        // Елемент L розраховується як різниця, ділена на діагональний елемент U
                        L[k, i] = (A[k, i] - sum) / U[i, i];

                        steps.AppendLine($"L[{k + 1},{i + 1}] = {L[k, i]:F2}");
                    }
                }
            }

            // --- ЕТАП 2: ПРЯМИЙ ХІД (Розв'язуємо Ly = b) ---
            // Шукаємо допоміжний вектор 'y'. Оскільки L - нижня трикутна, 
            // ми йдемо зверху вниз (від 1-го рівняння до n-го).
            steps.AppendLine("\nРозв'язуємо Ly = b");

            double[] y = new double[n];
            for (int i = 0; i < n; i++)
            {
                y[i] = b[i];
                for (int j = 0; j < i; j++)
                    y[i] -= L[i, j] * y[j]; // Віднімаємо вже знайдені значення y

                steps.AppendLine($"y{i + 1} = {y[i]:F2}");
            }

            // --- ЕТАП 3: ЗВОРОТНИЙ ХІД (Розв'язуємо Ux = y) ---
            // Тепер шукаємо кінцевий результат 'x'. Оскільки U - верхня трикутна,
            // ми йдемо знизу вгору (від останнього рівняння до 1-го).
            steps.AppendLine("\nРозв'язуємо Ux = y");

            double[] x = new double[n];
            for (int i = n - 1; i >= 0; i--)
            {
                x[i] = y[i];
                for (int j = i + 1; j < n; j++)
                    x[i] -= U[i, j] * x[j]; // Віднімаємо вже знайдені значення x

                x[i] /= U[i, i]; // Ділимо на коефіцієнт при невідомому x[i]

                steps.AppendLine($"x{i + 1} = {x[i]:F2}");
            }

            return x; // Повертаємо масив знайдених коренів системи
        }
    }   
}