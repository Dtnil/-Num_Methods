using System;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        Console.WriteLine("Виберіть рівняння:");

        Console.WriteLine("1  cos(sin(x^3)) - 0.7");
        Console.WriteLine("2  x^2 - 2x - 4");
        Console.WriteLine("3  sin(x^3) / 5");
        Console.WriteLine("4  x^3 + x^2 - 4x - 2");
        Console.WriteLine("5  x^2 cos(x) - 0.2");
        Console.WriteLine("6  1.5 - x^(1-cos(x))");
        Console.WriteLine("7  x^3 - 3x + 1.5");
        Console.WriteLine("8  cos(x^2 - x + 1)");
        Console.WriteLine("9  e^x - sin(2x) - 1");
        Console.WriteLine("10 (x^3 - 7x + 1) / 3");
        Console.WriteLine("11 cos(x^2) - 0.5");
        Console.WriteLine("12 sin(x) - 0.5 cos(x^2)");
        Console.WriteLine("13 ln(sin(2x)+1)");
        Console.WriteLine("14 sin(x) - cos(x^3)");

        int eq = int.Parse(Console.ReadLine());

        Console.WriteLine("Введіть початок інтервалу:");
        double a = double.Parse(Console.ReadLine());

        Console.WriteLine("Введіть кінець інтервалу:");
        double b = double.Parse(Console.ReadLine());

        RootFinder solver = new RootFinder();

        List<double> roots = solver.FindRoots(eq, a, b);

        Console.WriteLine("Знайдені корені:");

        foreach (double r in roots)
        {
            Console.WriteLine(Math.Round(r, 3));
        }

        Console.ReadLine();
    }
}