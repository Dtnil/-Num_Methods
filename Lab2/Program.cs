using System;

class Program
{
    static void Main()
    {
        int n = 3;

        double[,] A = {
            { -2, -3, 11 },
            {  1, 12, -5 },
            { -1,  1, -1 }
        };

        double[] b = { -15, 40, 35 };

        double[,] L = new double[n, n];
        double[,] U = new double[n, n];

        StepPrinter.PrintStep("Початкова матриця A");
        StepPrinter.PrintMatrix("A", A, n);

        // LU розклад
        for (int i = 0; i < n; i++)
        {
           StepPrinter.PrintStep($"Ітерація {i + 1}");

            // U
            for (int k = i; k < n; k++)
            {
                double sum = 0;
                for (int j = 0; j < i; j++)
                    sum += L[i, j] * U[j, k];

                U[i, k] = A[i, k] - sum;
                                                                            
                Console.WriteLine($"U[{i},{k}] = {A[i, k]} - {sum:F2} = {U[i, k]:F2}");
            }

            // L
            for (int k = i; k < n; k++)
            {
                if (i == k)
                {
                    L[i, i] = 1;
                    Console.WriteLine($"L[{i},{i}] = 1");
                }
                else
                {
                    double sum = 0;
                    for (int j = 0; j < i; j++)
                        sum += L[k, j] * U[j, i];

                    L[k, i] = (A[k, i] - sum) / U[i, i];

                    Console.WriteLine($"L[{k},{i}] = ({A[k, i]} - {sum:F2}) / {U[i, i]:F2} = {L[k, i]:F2}");
                }
            }
        }
        
        Console.WriteLine("\n---Утвореня матриць L та U---");
        
        StepPrinter.PrintMatrix("L", L, n);
        StepPrinter.PrintMatrix("U", U, n);

        // Ly = b
        StepPrinter.PrintStep("Розв’язання Ly = b");

        double[] y = new double[n];
        for (int i = 0; i < n; i++)
        {
            double sum = 0;
            for (int j = 0; j < i; j++)
                sum += L[i, j] * y[j];

            y[i] = b[i] - sum;

            Console.WriteLine($"y[{i}] = {b[i]} - {sum:F2} = {y[i]:F2}");
        }

        StepPrinter.PrintVector("y", y);

        // Ux = y
        StepPrinter.PrintStep("Розв’язання Ux = y");

        double[] x = new double[n];
        for (int i = n - 1; i >= 0; i--)
        {
            double sum = 0;
            for (int j = i + 1; j < n; j++)
                sum += U[i, j] * x[j];

            x[i] = (y[i] - sum) / U[i, i];

            Console.WriteLine($"x[{i}] = ({y[i]:F2} - {sum:F2}) / {U[i, i]:F2} = {x[i]:F2}");
        }

        StepPrinter.PrintVector("Розв’язок x", x);
    }
}