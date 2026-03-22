using System;

class StepPrinter
{
    public static void PrintMatrix(string name, double[,] matrix, int n)
    {
        Console.WriteLine($"\n{name}:");
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
                Console.Write($"{matrix[i, j],8:F2}");
            Console.WriteLine();
        }
    }

    public static void PrintVector(string name, double[] vector)
    {
        Console.WriteLine($"\n{name}:");
        for (int i = 0; i < vector.Length; i++)
            Console.WriteLine($"{name}[{i}] = {vector[i]:F2}");
    }

    public static void PrintStep(string text)
    {
        Console.WriteLine("\n--- " + text + " ---");
    }
}